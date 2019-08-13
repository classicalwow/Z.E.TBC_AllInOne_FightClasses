using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    private static bool _iCanUseWand = false;
    private static ZEWarlockSettings _settings;
    private static readonly BackgroundWorker _petPulseThread = new BackgroundWorker();
    internal static Stopwatch _addCheckTimer = new Stopwatch();

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEWarlockSettings.Load();
        _settings = ZEWarlockSettings.CurrentSetting;
        _wandThreshold = ZEWarlockSettings.CurrentSetting.WandThreshold > 100 ? 50 : ZEWarlockSettings.CurrentSetting.WandThreshold;
        Main.settingRange = _maxRange;
        _petPulseThread.DoWork += PetThread;
        _petPulseThread.RunWorkerAsync();

        // Set pet mode
        if (_settings.PetInPassiveWhenOOC)
            Lua.LuaDoString("PetPassiveMode();");
        else
            Lua.LuaDoString("PetDefensiveMode();");
        
        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _usingWand = false;
            _iCanUseWand = false;
            Main.settingRange = _maxRange;
            if (_settings.PetInPassiveWhenOOC)
                Lua.LuaDoString("PetPassiveMode();");
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (UseWand.IsSpellUsable)
                _iCanUseWand = true;
            Lua.LuaDoString("PetDefensiveMode();");
        };

        Rotation();
    }
    
    public static void Dispose()
    {
        _petPulseThread.DoWork -= PetThread;
        _petPulseThread.Dispose();
        Main.Log("Stop in progress.");
    }

    // Pet thread
    private static void PetThread(object sender, DoWorkEventArgs args)
    {
        while (Main._isLaunched)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause && !ObjectManager.Me.IsOnTaxi && ObjectManager.Me.IsAlive)
                {
                    // Voidwalker Torment
                    if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker") && ObjectManager.Target.Target == Me.Guid
                        && Me.InCombatFlagOnly)
                        ToolBox.PetSpellCast("Torment");
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError(string.Concat(arg), true);
            }
            Thread.Sleep(300);
        }
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
            // Make sure we have mana to summon
            if (!ObjectManager.Pet.IsValid && ObjectManager.Me.ManaPercentage < 70)
            {
                Main.Log("Not enough mana to summon")
            }
            // Switch Auto Torment off
            if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
            {
                ToolBox.TogglePetSpellAuto("Torment", false);
                ToolBox.TogglePetSpellAuto("Suffering", false);
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
            if (Me.HealthPercent > 50 && Me.ManaPercentage < 80 && _settings.UseLifeTap)
                if (Cast(LifeTap))
                    return;

            // Unending Breath
            if (!Me.HaveBuff("Unending Breath") && UnendingBreath.KnownSpell)
            {
                Lua.RunMacroText("/target player");
                if (Cast(UnendingBreath))
                {
                    Lua.RunMacroText("/cleartarget");
                    return;
                }
            }

            // Demon Skin
            if (!Me.HaveBuff("Demon Skin") && !DemonArmor.KnownSpell && DemonSkin.KnownSpell)
                if (Cast(DemonSkin))
                    return;

            // Demon Armor
            if ((!Me.HaveBuff("Demon Armor") || Me.HaveBuff("Demon Skin")) && DemonArmor.KnownSpell)
                if (Cast(DemonArmor))
                    return;

            // Health Funnel
            if (ObjectManager.Pet.HealthPercent < 50 && Me.HealthPercent > 40 && ObjectManager.Pet.GetDistance < 19
                && !ObjectManager.Pet.InCombatFlagOnly)
            {
                if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
                {
                    ToolBox.PetSpellCast("Consume Shadows");
                }

                StopWandWaitGCD();
                if (Cast(HealthFunnel))
                {
                    Usefuls.WaitIsCasting();
                    return;
                }
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
            {
                Lua.RunMacroText("/target player");
                PetAndConsumables.UseSoulstone();
                Usefuls.WaitIsCasting();
                Lua.RunMacroText("/cleartarget");
            }
        }
    }

    internal static void Pull()
    {
        // Pet attack
        if (ObjectManager.Pet.Target != ObjectManager.Me.Target)
            Lua.LuaDoString("PetAttack();", false);

        // Life Tap
        if (Me.HealthPercent > 50 && Me.ManaPercentage < 80 && _settings.UseLifeTap)
            if (Cast(LifeTap))
                return;

        // Amplify Curse
        if (AmplifyCurse.IsSpellUsable && AmplifyCurse.KnownSpell)
            AmplifyCurse.Launch();

        // Curse of Agony
        if (ObjectManager.Target.GetDistance < _maxRange + 2)
            if (Cast(CurseOfAgony))
                return;

        // Corruption
        if (ObjectManager.Target.GetDistance < _maxRange + 2)
            if (Cast(Corruption))
                return;

        // Immolate
        if (ObjectManager.Target.GetDistance < _maxRange + 2 && !Corruption.KnownSpell)
            if (Cast(Immolate))
                return;

        // Shadow Bolt
        if (ObjectManager.Target.GetDistance < _maxRange + 2 && !Immolate.KnownSpell)
            if (Cast(ShadowBolt))
                return;
    }

    internal static void CombatRotation()
    {
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
        WoWUnit Me = ObjectManager.Me;
        WoWUnit Target = ObjectManager.Target;
        double _myManaPC = Me.ManaPercentage;
        bool _overLowManaThreshold = _myManaPC > _innerManaSaveThreshold;

        // Multi aggro
        if (ObjectManager.GetNumberAttackPlayer() > 1 && 
            (_addCheckTimer.ElapsedMilliseconds > 6000 || _addCheckTimer.ElapsedMilliseconds <= 0))
        {
            _addCheckTimer.Restart();
            WoWUnit _currenTarget = ObjectManager.Target;
            List<WoWUnit> _listUnitsAttackingMe = ObjectManager.GetUnitAttackPlayer();
            foreach (WoWUnit unit in _listUnitsAttackingMe)
            {
                Thread.Sleep(500);
                if (unit.Target == Me.Guid && unit.Guid != Me.Target)
                {
                    Lua.RunMacroText("/cleartarget");
                    Lua.LuaDoString("/TargetUnit('" + unit.Guid + "')");
                }
            }
        }

        // Pet attack
        if (ObjectManager.Pet.Target != ObjectManager.Me.Target)
            Lua.LuaDoString("PetAttack();", false);

        // Drain Soul
        if (ToolBox.CountItemStacks("Soul Shard") < 2 && Target.HealthPercent < 40)
            if (Cast(DrainSoul))
                return;

        // Use Health Stone
        if (Me.HealthPercent < 15)
            PetAndConsumables.UseHealthstone();

        // Shadow Trance
        if (Me.HaveBuff("Shadow Trance") && _overLowManaThreshold)
            if (Cast(ShadowBolt))
                return;

        // Curse of Agony
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Curse of Agony") && _overLowManaThreshold
            && Target.HealthPercent > 20)
            if (Cast(CurseOfAgony))
                return;

        // Corruption
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Corruption") && _overLowManaThreshold
            && Target.HealthPercent > 20)
            if (Cast(Corruption))
                return;

        // Immolate
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Immolate") && _overLowManaThreshold
            && Target.HealthPercent > 30)
            if (Cast(Immolate))
                return;

        // Siphon Life
        if (Me.HealthPercent < 90 && _overLowManaThreshold && Target.HealthPercent > 20
            && _settings.UseSiphonLife)
            if (Cast(SiphonLife))
                return;

        // Drain Life
        if (Me.HealthPercent < 70 && Target.HealthPercent > 20)
            if (Cast(DrainLife))
                return;

        // Health Funnel
        if (ObjectManager.Pet.IsValid && ObjectManager.Pet.HealthPercent < 30 && Me.HealthPercent > 30)
        {
            Main.settingRange = 19f;
            if (HealthFunnel.IsDistanceGood && Cast(HealthFunnel))
                return;
        }

        // Dark Pact
        if (Me.ManaPercentage < 70 && ObjectManager.Pet.Mana > 0 && ObjectManager.Pet.ManaPercentage > 60)
            if (Cast(DarkPact))
                return;

        // Drain Mana
        if (Me.ManaPercentage < 70 && Target.Mana > 0 && Target.ManaPercentage > 30)
            if (Cast(DrainMana))
                return;

        // Shadow Bolt
        if ((!_settings.PrioritizeWandingOverSB || !_iCanUseWand) && 
            (ObjectManager.Target.HealthPercent > 50 || (Me.ManaPercentage > 90 && ObjectManager.Target.HealthPercent > 10)) 
            && _myManaPC > 40 && ObjectManager.Target.GetDistance < _maxRange)
            if (Cast(ShadowBolt))
                return;

        // Life Tap
        if (Me.HealthPercent > 60 && Me.ManaPercentage < 40 && !ObjectManager.Target.IsTargetingMe
            && _settings.UseLifeTap)
            if (Cast(LifeTap))
                return;

        // Use Wand on conditions
        if ((ObjectManager.Target.HealthPercent < _wandThreshold || _myManaPC <= _innerManaSaveThreshold) &&
            !_usingWand && _iCanUseWand && ObjectManager.Target.GetDistance < _maxRange + 2)
        {
            UseWand.Launch();
            return;
        }
        
        // Use wand because nothing else to do
        if (!_usingWand && _iCanUseWand && ObjectManager.Target.GetDistance < _maxRange + 2)
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
    private static Spell SiphonLife = new Spell("Siphon Life");
    private static Spell DrainMana = new Spell("Drain Mana");
    private static Spell DarkPact = new Spell("Dark Pact");

    private static bool Cast(Spell s, bool castEvenIfWanding = true, bool waitGCD = true)
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
