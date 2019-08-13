using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.Collections.Generic;

public static class Mage
{
    private static MageFoodManager _foodManager = new MageFoodManager();
    private static int _meleeRange = 5;
    private static bool _usingWand = false;
    private static bool _isBackingUp = false;
    //private List<WoWUnit> _listUnitsAttackingMe;

    public static void Initialize()
    {
        Main.settingRange = 29f;
        Main.Log("Initialized.");
        ZEMageSettings.Load();

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _isBackingUp = false;
            _usingWand = false;
        };

        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            bool condition = ((ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova")) &&
                ObjectManager.Target.IsAlive && ObjectManager.Target.GetDistance < _meleeRange && !ObjectManager.Me.IsCast && !_isBackingUp);
            if (condition)
            {
                _isBackingUp = true;
                Move.Backward(Move.MoveAction.DownKey, 700);
                Thread.Sleep(700 + Usefuls.Latency);
                _isBackingUp = false;
            }
        };

        Rotation();
	}

    public static void Dispose()
	{
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
                    if (!Fight.InFight && !ObjectManager.Me.InCombatFlagOnly)
                    {
                        _foodManager.CheckIfEnoughFoodAndDrinks();
                        _foodManager.CheckIfThrowFoodAndDrinks();
                        _foodManager.CheckIfHaveManaStone();

                        // Evocation
                        if (ObjectManager.Me.ManaPercentage < 30)
                            Cast(Evocation);

                        // Frost Armor
                        if (!ObjectManager.Me.HaveBuff("Frost Armor"))
                            Cast(FrostArmor);

                        // Arcane Intellect
                        if (!ObjectManager.Me.HaveBuff("Arcane Intellect"))
                            Cast(ArcaneIntellect);
                    }

                    if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable)
						CombatRotation();
				}
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(50);
		}
		Main.Log("Stopped.");
	}
    
	internal static void CombatRotation()
    {/*
        if (ObjectManager.GetNumberAttackPlayer() > 1)
        {
            WoWUnit unitToSheep = new WoWUnit(0);
            WoWUnit currenTarget = ObjectManager.Target;
            _listUnitsAttackingMe = ObjectManager.GetUnitAttackPlayer();
            foreach(WoWUnit unit in _listUnitsAttackingMe)
            {
                Interact.ClearTarget();
                Lua.LuaDoString("TargetUnit(\"" + unit.Guid + "\")");
                Logging.WriteDebug(unit.Name + " is a " + unit.CreatureTypeTarget);
                if (unit.Guid != currenTarget.Guid && unit.CreatureTypeTarget == "Humanoid" || unit.CreatureTypeTarget == "Beast")
                unitToSheep = unit;
            }
            if (unitToSheep.Guid != 0 && !unitToSheep.HaveBuff("Polymorph") && !_usingWand)
            {
                Logging.WriteDebug(unitToSheep.Name + " will get polymorphed ");
                Interact.ClearTarget();
                wManager.Wow.Helpers.Interact.InteractGameObject(unitToSheep.GetBaseAddress);
                Lua.LuaDoString("TargetUnit(\"" + unitToSheep.Guid + "\")");
                Cast(Polymorph);
                Interact.ClearTarget();
                Lua.LuaDoString("TargetUnit(\"" + currenTarget.Guid + "\")");
            }
        }*/

        Lua.LuaDoString("PetAttack();", false);
        bool _hasCurse = ToolBox.HasCurseDebuff();
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");

        // Use Wand
        if ((ObjectManager.Target.HealthPercent < ZEMageSettings.CurrentSetting.WandThreshold || 
            ObjectManager.Me.ManaPercentage < 5 ) && !_usingWand && UseWand.IsSpellUsable && ObjectManager.GetNumberAttackPlayer() < 2
            && (ObjectManager.Me.HealthPercent > 30 || ObjectManager.Me.ManaPercentage < 1))
            UseWand.Launch();

        // Remove Curse
        if (_hasCurse)
            Cast(RemoveCurse);

        // Summon Water Elemental
        if (ObjectManager.Target.HealthPercent > 95 || ObjectManager.GetNumberAttackPlayer() > 1)
            Cast(SummonWaterElemental);

        // Ice Barrier
        if (IceBarrier.IsSpellUsable && !ObjectManager.Me.HaveBuff("Ice Barrier"))
            Cast(IceBarrier);

        // Cold Snap
        if (ObjectManager.GetNumberAttackPlayer() > 1 && !ObjectManager.Me.HaveBuff("Icy Veins") && !IcyVeins.IsSpellUsable)
            Cast(ColdSnap);

        // Icy Veins
        if ((ObjectManager.GetNumberAttackPlayer() > 1 && ZEMageSettings.CurrentSetting.IcyVeinMultiPull) 
            || !ZEMageSettings.CurrentSetting.IcyVeinMultiPull)
            Cast(IcyVeins);

        // Use Mana Stone
        if (((ObjectManager.GetNumberAttackPlayer() > 1 && ObjectManager.Me.ManaPercentage < 50) || ObjectManager.Me.ManaPercentage < 5)
            && _foodManager.ManaStone != "")
        {
            _foodManager.UseManaStone();
            _foodManager.ManaStone = "";
        }

        // Ice Lance
        if (ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova"))
            Cast(IceLance);

        // Fire Blast
        if (ObjectManager.Target.GetDistance < 20f && ObjectManager.Target.HealthPercent > 30f)
            Cast(FireBlast);

        // Con of Cold
        if (ObjectManager.Target.GetDistance < 10 && ZEMageSettings.CurrentSetting.UseConeOfCold)
            Cast(ConeOfCold);

        // Frost Nova
        if (ObjectManager.Target.GetDistance < _meleeRange && ObjectManager.Target.HealthPercent > 5 && !(ObjectManager.Target.HaveBuff("Frostbite")))
            Cast(FrostNova);

        // Frost Bolt
        if (Frostbolt.IsDistanceGood && ObjectManager.Me.Level >= 6)
            Cast(Frostbolt);

        // Low level Frost Bolt
        if (Frostbolt.IsDistanceGood && ObjectManager.Target.HealthPercent > 30 && ObjectManager.Me.Level < 6)
            Cast(Frostbolt);

        // FireBall
        if (Fireball.IsDistanceGood && !Frostbolt.KnownSpell && ObjectManager.Target.HealthPercent > 30)
            Cast(Fireball);
    }

    private static void Cast(Spell s)
    {
        if (s.IsSpellUsable && s.KnownSpell && !_usingWand && !_isBackingUp)
            s.Launch();
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
