using System;
using System.ComponentModel;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class Warlock
{
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static float _maxRange = 27f;
    private static bool _usingWand = false;
    private static int _innerManaSaveThreshold = 20;
    private static int _wandThreshold;
    private static ZEWarlockSettings _settings;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEWarlockSettings.Load();
        _settings = ZEWarlockSettings.CurrentSetting;
        _wandThreshold = ZEWarlockSettings.CurrentSetting.WandThreshold > 100 ? 50 : ZEWarlockSettings.CurrentSetting.WandThreshold;
        Main.settingRange = _maxRange;

        // Set pet mode
        if (_settings.PetInPassiveWhenOOC)
            Lua.LuaDoString("PetPassiveMode();");
        else
            Lua.LuaDoString("PetDefensiveMode();");
        
        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _usingWand = false;
            Main.settingRange = _maxRange;
            if (_settings.PetInPassiveWhenOOC)
                Lua.LuaDoString("PetPassiveMode();");
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            Lua.LuaDoString("PetDefensiveMode();");
        };

        Rotation();
    }


    public static void Dispose()
    {
        Main.Log("Stop in progress.");
    }

    internal static void Rotation()
	{
        Main.Log("Started");
		while (Main._isLaunched)
		{
			try
			{
				if (!Products.InPause && !ObjectManager.Me.IsDeadMe)
                {
                    if (!Fight.InFight)
                    {
                        BuffRotation();
                    }

                    if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable && ObjectManager.Target.IsAlive)
                    {
                        if (ObjectManager.GetNumberAttackPlayer() < 1 && !ObjectManager.Target.InCombatFlagOnly)
                            Pull();
                        else
                            CombatRotation();
                    }
				}
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(Usefuls.Latency + 20);
		}
        Main.Log("Stopped.");
    }

    internal static void BuffRotation()
    {
        if (!Me.IsMounted)
        {
            // Switch Auto Torment off
            if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
            {
                ToolBox.TogglePetSpellAuto("Torment", false);
                ToolBox.TogglePetSpellAuto("Suffering", false, false);
            }

            // Summon Void Walker
            if ((!ObjectManager.Pet.IsValid || !PetAndConsumables.MyWarlockPet().Equals("Voidwalker")) && SummonVoidwalker.KnownSpell)
                if (Cast(SummonVoidwalker))
                    return;

            // Summon Imp
            if (!ObjectManager.Pet.IsValid && SummonImp.KnownSpell && 
                (!SummonVoidwalker.KnownSpell || ToolBox.CountItemStacks("Soul Shard") < 1))
                if (Cast(SummonImp))
                    return;

            // Life Tap
            if (Me.HealthPercent > 80 && Me.ManaPercentage < 50 && _settings.UseLifeTap)
                if (Cast(LifeTap))
                    return;

            // Unending Breath
            if (!Me.HaveBuff("Unending Breath") && UnendingBreath.KnownSpell)
                if (Cast(UnendingBreath))
                    return;

            // Demon Skin
            if (!Me.HaveBuff("Demon Skin") && !DemonArmor.KnownSpell && DemonSkin.KnownSpell)
                if (Cast(DemonSkin))
                    return;

            // Demon Armor
            if ((!Me.HaveBuff("Demon Armor") || Me.HaveBuff("Demon Skin")) && DemonArmor.KnownSpell)
                if (Cast(DemonArmor))
                    return;

            // Health Funnel
            if (ObjectManager.Pet.HealthPercent < 50 && Me.HealthPercent > 60)
            {
                if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
                    ToolBox.PetSpellCast("Consume Shadows");

                if (Cast(HealthFunnel))
                    return;
            }

            // Health Stone
            if (!PetAndConsumables.HaveHealthstone())
                if (Cast(CreateHealthStone))
                    return;

            // Create Soul Stone
            if (!PetAndConsumables.HaveSoulstone() && CreateSoulstone.KnownSpell)
                if (Cast(CreateSoulstone))
                    return;

            // Use Soul Stone
            if (!Me.HaveBuff("Soulstone Resurrection") && CreateSoulstone.KnownSpell && 
                ToolBox.GetItemCooldown(PetAndConsumables.SoulStones()) <= 0)
                PetAndConsumables.UseSoulstone();
        }
    }

    internal static void Pull()
    {
        // Pet attack
        if (ObjectManager.Pet.Target != ObjectManager.Me.Target)
        {
            Lua.LuaDoString("PetAttack();", false);
        }

        // Amplify Curse
        if (AmplifyCurse.IsSpellUsable && AmplifyCurse.KnownSpell)
            AmplifyCurse.Launch();
        
        // Corruption
        if (ObjectManager.Target.GetDistance < _maxRange + 2)
            if (Cast(Corruption, true, true))
                return;

        // Immolate
        if (ObjectManager.Target.GetDistance < _maxRange + 2 && !Corruption.KnownSpell)
            if (Cast(Immolate, true, true))
                return;

        // Shadow Bolt
        if (ObjectManager.Target.GetDistance < _maxRange + 2 && !Immolate.KnownSpell)
            if (Cast(ShadowBolt, true, true))
                return;
    }

    internal static void CombatRotation()
    {
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
        WoWUnit Me = ObjectManager.Me;
        WoWUnit Target = ObjectManager.Target;
        double _myManaPC = Me.ManaPercentage;

        // Voidwalker Torment
        if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker") && Target.Target == Me.Guid)
            ToolBox.PetSpellCast("Torment");

        // Pet attack
        if (ObjectManager.Pet.Target != ObjectManager.Me.Target)
            Lua.LuaDoString("PetAttack();", false);

        // Drain Soul
        if (ToolBox.CountItemStacks("Soul Shard") < 2 && Target.HealthPercent < 40)
            if (Cast(DrainSoul, true))
                return;

        // Use Health Stone
        if (Me.HealthPercent < 30)
            PetAndConsumables.UseHealthstone();

        // Curse of Agony
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Curse of Agony") && _myManaPC > _innerManaSaveThreshold
            && Target.HealthPercent > 20)
            if (Cast(CurseOfAgony, true, true))
                return;

        // Corruption
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Corruption") && _myManaPC > _innerManaSaveThreshold
            && Target.HealthPercent > 20)
            if (Cast(Corruption, true, true))
                return;

        // Immolate
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Immolate") && _myManaPC > _innerManaSaveThreshold
            && Target.HealthPercent > 30)
            if (Cast(Immolate, true, true))
                return;

        // Drain Life
        if (Me.HealthPercent < 70 && _myManaPC > _innerManaSaveThreshold && Target.HealthPercent > 20)
            if (Cast(DrainLife, true))
                return;

        // Health Funnel
        if (ObjectManager.Pet.HealthPercent < 30 && Me.HealthPercent > 50)
        {
            Main.settingRange = 19f;
            if (HealthFunnel.IsDistanceGood && Cast(HealthFunnel))
                return;
        }

        // Shadow Bolt
        if ((ObjectManager.Target.HealthPercent > 50 || Me.ManaPercentage > 70) && _myManaPC > 40
            && ObjectManager.Target.GetDistance < _maxRange)
            if (Cast(ShadowBolt, true))
                return;

        // Life Tap
        if (Me.HealthPercent > 80 && Me.ManaPercentage < _innerManaSaveThreshold && !ObjectManager.Target.IsTargetingMe
            && _settings.UseLifeTap)
            if (Cast(LifeTap, true, true))
                return;

        // Use Wand on conditions
        if ((ObjectManager.Target.HealthPercent < _wandThreshold || _myManaPC <= _innerManaSaveThreshold) &&
            !_usingWand && UseWand.IsSpellUsable)
        {
            UseWand.Launch();
            return;
        }
        
        // Use wand because nothing else to do
        if (!_usingWand && UseWand.IsSpellUsable)
        {
            UseWand.Launch();
            return;
        }

        // Go in melee because nothing else to do
        if (!_usingWand && !UseWand.IsSpellUsable && Main.settingRange != 5f)
        {
            Main.Log("Going in melee");
            Main.settingRange = 5f;
            return;
        }
    }

    public static void ShowConfiguration()
    {
        ZEWarlockSettings.Load();
        ZEWarlockSettings.CurrentSetting.ToForm();
        ZEWarlockSettings.CurrentSetting.Save();
    }

    private static Spell DemonSkin = new Spell("Demon Skin");
    private static Spell DemonArmor = new Spell("Demon Armor");
    private static Spell ShadowBolt = new Spell("Shadow Bolt");
    private static Spell UseWand = new Spell("Shoot");
    private static Spell Fear = new Spell("Fear");
    private static Spell Immolate = new Spell("Immolate");
    private static Spell Corruption = new Spell("Corruption");
    private static Spell LifeTap = new Spell("Life Tap");
    private static Spell SummonImp = new Spell("Summon Imp");
    private static Spell SummonVoidwalker = new Spell("Summon Voidwalker");
    private static Spell CurseOfAgony = new Spell("Curse of Agony");
    private static Spell DrainSoul = new Spell("Drain Soul");
    private static Spell DrainLife = new Spell("Drain Life");
    private static Spell CreateHealthStone = new Spell("Create HealthStone");
    private static Spell HealthFunnel = new Spell("Health Funnel");
    private static Spell CreateSoulstone = new Spell("Create Soulstone");
    private static Spell AmplifyCurse = new Spell("Amplify Curse");
    private static Spell UnendingBreath = new Spell("Unending Breath");

    private static bool Cast(Spell s, bool castEvenIfWanding = false, bool waitGCD = false)
    {
        Main.LogDebug("Into Cast for " + s.Name);

        if (_usingWand && !castEvenIfWanding)
            return false;

        if (_usingWand && castEvenIfWanding)
            StopWandWaitGCD();

        if (!s.IsSpellUsable)
            return false;
        
        s.Launch();

        if (waitGCD)
            ToolBox.WaitGlobalCoolDown(ShadowBolt);
        return true;
    }

    private static void StopWandWaitGCD()
    {
        if (Me.ManaPercentage > 15)
        {
            UseWand.Launch();
            int c = 0;
            while (!ShadowBolt.IsSpellUsable)
            {
                c += 50;
                Thread.Sleep(50);
                if (c >= 1500)
                    return;
            }
            Main.LogDebug("Waited for GCD : " + c);
            if (c >= 1500)
                UseWand.Launch();
        }
    }
}
