using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;

public static class Mage
{
    private static MageFoodManager _foodManager = new MageFoodManager();
    private static float _meleeRange = 5f;
    private static float _range = 28f;
    private static bool _usingWand = false;
    private static bool _isBackingUp = false;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static bool _iCanUseWand = false;
    private static bool _saveCalcuCombatRangeSetting = wManager.wManagerSetting.CurrentSetting.CalcuCombatRange;

    public static void Initialize()
    {
        Main.settingRange = _range;
        wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = false;
        Main.Log("Initialized.");
        ZEMageSettings.Load();

        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _isBackingUp = false;
            _iCanUseWand = false;
            _usingWand = false;
            Main.settingRange = _range;
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (UseWand.IsSpellUsable)
                _iCanUseWand = true;
        };

        // Fight Loop
        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            bool condition = ((ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova")) &&
                ObjectManager.Target.IsAlive && ObjectManager.Target.GetDistance < _meleeRange + 4 && !Me.IsCast && !_isBackingUp
                && ObjectManager.Target.HealthPercent > 5 && Main.settingRange != _meleeRange);
            if (condition)
            {
                Main.LogDebug("Backing up");
                _isBackingUp = true;
                var pos = 1;
                if (Me.IsAlive && ObjectManager.Target.IsAlive && pos == 1)
                {
                    Vector3 position = ToolBox.BackofVector3(Me.Position, Me, 15f);
                    MovementManager.Go(PathFinder.FindPath(position), false);

                    while (MovementManager.InMovement && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Target.GetDistance < 10f && ObjectManager.Target.IsAlive)
                    {
                        // Wait follow path
                        Thread.Sleep(2000);
                        pos = 0;
                    }
                }
            }
            _isBackingUp = false;
        };

        Rotation();
	}

    public static void Dispose()
    {
        wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = _saveCalcuCombatRangeSetting;
        _usingWand = false;
        _isBackingUp = false;
        Main.Log("Stopped in progress.");
	}

    public static void ShowConfiguration()
    {
        ZEMageSettings.Load();
        ZEMageSettings.CurrentSetting.ToForm();
        ZEMageSettings.CurrentSetting.Save();
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
        _foodManager.CheckIfEnoughFoodAndDrinks();
        _foodManager.CheckIfThrowFoodAndDrinks();
        _foodManager.CheckIfHaveManaStone();

        // Frost Armor
        if (!Me.HaveBuff("Frost Armor"))
            if (Cast(FrostArmor))
                return;

        // Arcane Intellect
        if (!Me.HaveBuff("Arcane Intellect"))
        {
            Lua.RunMacroText("/target player");
            if (Cast(ArcaneIntellect))
            {
                Lua.RunMacroText("/cleartarget");
                return;
            }
        }

        // Evocation
        if (Me.ManaPercentage < 30)
            if (Cast(Evocation))
                return;
    }

    internal static void Pull()
    {
        WoWUnit _target = ObjectManager.Target;

        // Ice Barrier
        if (IceBarrier.IsSpellUsable && !Me.HaveBuff("Ice Barrier"))
            if (Cast(IceBarrier))
                return;

        // Frost Bolt
        if (_target.GetDistance < _range + 1 && Me.Level >= 6 && (_target.HealthPercent > ZEMageSettings.CurrentSetting.WandThreshold
            || ObjectManager.GetNumberAttackPlayer() > 1 || Me.HealthPercent < 30 || !_iCanUseWand))
            if (Cast(Frostbolt))
                return;

        // Low level Frost Bolt
        if (_target.GetDistance < _range + 1 && _target.HealthPercent > 30 && Me.Level < 6)
            if (Cast(Frostbolt))
                return;

        // Low level FireBall
        if (_target.GetDistance < _range + 1 && !Frostbolt.KnownSpell && _target.HealthPercent > 30)
            if (Cast(Fireball))
                return;
    }

    internal static void CombatRotation()
    {
        Lua.LuaDoString("PetAttack();", false);
        bool _hasCurse = ToolBox.HasCurseDebuff();
        WoWUnit _target = ObjectManager.Target;
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");

        // Remove Curse
        if (_hasCurse)
            if (Cast(RemoveCurse))
                return;

        // Summon Water Elemental
        if (_target.HealthPercent > 95 || ObjectManager.GetNumberAttackPlayer() > 1)
            if (Cast(SummonWaterElemental))
                return;

        // Ice Barrier
        if (IceBarrier.IsSpellUsable && !Me.HaveBuff("Ice Barrier"))
            if (Cast(IceBarrier))
                return;

        // Cold Snap
        if (ObjectManager.GetNumberAttackPlayer() > 1 && !Me.HaveBuff("Icy Veins") && !IcyVeins.IsSpellUsable)
            if (Cast(ColdSnap))
                return;

        // Icy Veins
        if ((ObjectManager.GetNumberAttackPlayer() > 1 && ZEMageSettings.CurrentSetting.IcyVeinMultiPull)
            || !ZEMageSettings.CurrentSetting.IcyVeinMultiPull)
            if (Cast(IcyVeins))
                return;

        // Use Mana Stone
        if (((ObjectManager.GetNumberAttackPlayer() > 1 && Me.ManaPercentage < 50) || Me.ManaPercentage < 5)
            && _foodManager.ManaStone != "")
        {
            _foodManager.UseManaStone();
            _foodManager.ManaStone = "";
        }

        // Ice Lance
        if (_target.HaveBuff("Frostbite") || _target.HaveBuff("Frost Nova"))
            if (Cast(IceLance))
                return;

        // Frost Nova
        if (_target.GetDistance < _meleeRange + 2 && _target.HealthPercent > 5 && !_target.HaveBuff("Frostbite"))
            if (Cast(FrostNova))
                return;

        // Fire Blast
        if (_target.GetDistance < 20f && _target.HealthPercent > 30f)
            if (Cast(FireBlast))
                return;

        // Cone of Cold
        if (_target.GetDistance < 10 && ZEMageSettings.CurrentSetting.UseConeOfCold)
            if (Cast(ConeOfCold))
                return;

        // Frost Bolt
        if (_target.GetDistance < _range + 1 && Me.Level >= 6 && (_target.HealthPercent > ZEMageSettings.CurrentSetting.WandThreshold
            || ObjectManager.GetNumberAttackPlayer() > 1 || Me.HealthPercent < 30 || !_iCanUseWand))
            if (Cast(Frostbolt))
                return;

        // Low level Frost Bolt
        if (_target.GetDistance < _range + 1 && _target.HealthPercent > 30 && Me.Level < 6)
            if (Cast(Frostbolt))
                return;

        // Low level FireBall
        if (_target.GetDistance < _range + 1 && !Frostbolt.KnownSpell && _target.HealthPercent > 30)
            if (Cast(Fireball))
                return;

        // Use Wand
        if (!_usingWand && _iCanUseWand && ObjectManager.Target.GetDistance <= _range && !_isBackingUp)
        {
            UseWand.Launch();
            return;
        }

        // Go in melee because nothing else to do
        if (!_usingWand && !UseWand.IsSpellUsable && Main.settingRange != _meleeRange && !_isBackingUp)
        {
            Main.Log("Going in melee");
            Main.settingRange = _meleeRange;
            return;
        }
        return;
    }

    private static bool Cast(Spell s, bool castEvenIfWanding = true, bool waitGCD = true)
    {
        Main.LogDebug("Into Cast for " + s.Name);

        if ((_usingWand && !castEvenIfWanding) || _isBackingUp)
            return false;

        if (_usingWand && castEvenIfWanding)
            StopWandWaitGCD();

        if (!s.IsSpellUsable)
            return false;
        
        s.Launch();

        if (waitGCD)
            ToolBox.WaitGlobalCoolDown(Fireball);
        return true;
    }

    private static void StopWandWaitGCD()
    {
        if (Me.ManaPercentage > 15)
        {
            UseWand.Launch();
            int c = 0;
            while (!Fireball.IsSpellUsable)
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

    private static Spell FrostArmor = new Spell("Frost Armor");
    private static Spell Fireball = new Spell("Fireball");
    private static Spell Frostbolt = new Spell("Frostbolt");
    private static Spell FireBlast = new Spell("Fire Blast");
    private static Spell ArcaneIntellect = new Spell("Arcane Intellect");
    private static Spell FrostNova = new Spell("Frost Nova");
    private static Spell UseWand = new Spell("Shoot");
    private static Spell IcyVeins = new Spell("Icy Veins");
    private static Spell CounterSpell = new Spell("Counterspell");
    private static Spell ConeOfCold = new Spell("Cone of Cold");
    private static Spell Evocation = new Spell("Evocation");
    private static Spell ColdSnap = new Spell("Cold Snap");
    private static Spell Polymorph = new Spell("Polymorph");
    private static Spell IceBarrier = new Spell("Ice Barrier");
    private static Spell SummonWaterElemental = new Spell("Summon Water Elemental");
    private static Spell IceLance = new Spell("Ice Lance");
    private static Spell RemoveCurse = new Spell("Remove Curse");
}
