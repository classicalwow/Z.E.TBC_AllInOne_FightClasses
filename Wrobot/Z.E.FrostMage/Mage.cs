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

                        if (ObjectManager.Me.ManaPercentage < 30)
                            Cast(Evocation);

                        if (!ObjectManager.Me.HaveBuff("Frost Armor"))
                            Cast(FrostArmor);

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
        string _debuff = GetDebuffType();
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");

        if ((ObjectManager.Target.HealthPercent < ZEMageSettings.CurrentSetting.WandThreshold || 
            ObjectManager.Me.ManaPercentage < 5 ) && !_usingWand && UseWand.IsSpellUsable && ObjectManager.GetNumberAttackPlayer() < 2
            && (ObjectManager.Me.HealthPercent > 30 || ObjectManager.Me.ManaPercentage < 1))
            UseWand.Launch();

        if (_debuff == "Curse")
            Cast(RemoveCurse);

        if (ObjectManager.Target.HealthPercent > 95 || ObjectManager.GetNumberAttackPlayer() > 1)
            Cast(SummonWaterElemental);

        if (IceBarrier.IsSpellUsable && !ObjectManager.Me.HaveBuff("Ice Barrier"))
            Cast(IceBarrier);

        if (ObjectManager.GetNumberAttackPlayer() > 1 && !ObjectManager.Me.HaveBuff("Icy Veins") && !IcyVeins.IsSpellUsable)
            Cast(ColdSnap);

        if ((ObjectManager.GetNumberAttackPlayer() > 1 && ZEMageSettings.CurrentSetting.IcyVeinMultiPull) 
            || !ZEMageSettings.CurrentSetting.IcyVeinMultiPull)
            Cast(IcyVeins);

        if (((ObjectManager.GetNumberAttackPlayer() > 1 && ObjectManager.Me.ManaPercentage < 50) || ObjectManager.Me.ManaPercentage < 5)
            && _foodManager.ManaStone != "")
        {
            _foodManager.UseManaStone();
            _foodManager.ManaStone = "";
        }

        if (ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova"))
            Cast(IceLance);

        if (ObjectManager.Target.GetDistance < 20f && ObjectManager.Target.HealthPercent > 30f)
            Cast(FireBlast);

        if (ObjectManager.Target.GetDistance < 10 && ZEMageSettings.CurrentSetting.UseConeOfCold)
            Cast(ConeOfCold);

        if (ObjectManager.Target.GetDistance < _meleeRange && ObjectManager.Target.HealthPercent > 5 && !(ObjectManager.Target.HaveBuff("Frostbite")))
            Cast(FrostNova);

        if (Frostbolt.IsDistanceGood)
            Cast(Frostbolt);

        if (Fireball.IsDistanceGood && !Frostbolt.KnownSpell)
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

    private static string GetDebuffType()
    {
        string debuffType = Lua.LuaDoString<string>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Curse' then
                return d
                end
            end");
        return debuffType;
    }
}
