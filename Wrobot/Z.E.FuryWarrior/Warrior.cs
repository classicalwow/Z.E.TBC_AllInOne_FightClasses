using System;
using System.Diagnostics;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using robotManager.FiniteStateMachine;
using System.ComponentModel;
using System.Collections.Generic;

public static class Warrior
{
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEWarriorSettings _settings;
    private static bool _fightingACaster = false;
    private static float _pullRange = 25f;
    private static List<string> _casterEnemies = new List<string>();
    private static List<WoWUnit> surroundingEnemies = new List<WoWUnit>();
    private static bool _pullFromAfar = false;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEWarriorSettings.Load();
        _settings = ZEWarriorSettings.CurrentSetting;

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
            _pullFromAfar = false;
            Main.settingRange = 5f;
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
                    // Buff rotation
                    if (!Fight.InFight && ObjectManager.GetNumberAttackPlayer() < 1)
                    {
                        BuffRotation();
                    }

                    // Pull & Combat rotation
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
			Thread.Sleep(Usefuls.Latency + 10);
		}
        Main.Log("Stopped.");
    }

    internal static void BuffRotation()
    {
        if (!Me.IsMounted && !Me.IsCast)
        {
            // Battle Shout
            if (!Me.HaveBuff("Battle Shout") && BattleShout.IsSpellUsable)
                if (Cast(BattleShout))
                    return;
        }
    }

    internal static void Pull()
    {
        // Check if surrounding enemies
        if (ObjectManager.Target.GetDistance < 25f && !_pullFromAfar)
            _pullFromAfar = CheckIfEnemiesOnPull(ObjectManager.Target);

        // Pull from afar
        if (_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000)
        {
            if (Cast(Throw) || Cast(Shoot))
            {
                Main.settingRange = 25f;
                Thread.Sleep(500);
            }
            else
            {
                Main.Log("Can't pull from distance. Please equip a ranged weapon in order to Throw or Shoot.");
            }
        }

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 && ObjectManager.Target.GetDistance <= _pullRange + 3)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
        {
            Main.LogDebug("Going in Melee range");
            Main.settingRange = 5f;
            _pullMeleeTimer.Reset();
        }

        // Check if caster
        if (_casterEnemies.Contains(ObjectManager.Target.Name))
            _fightingACaster = true;

        // Charge
        if (ObjectManager.Target.GetDistance > 9f && ObjectManager.Target.GetDistance < 24f && Charge.KnownSpell 
            && Charge.IsSpellUsable && !_pullFromAfar)
            Charge.Launch();
    }

    internal static void CombatRotation()
    {
        bool _shouldBeInterrupted = false;
        bool _inMeleeRange = ObjectManager.Target.GetDistance < 6f;
        bool _saveRage = ((ObjectManager.GetNumberAttackPlayer() > 1 && CheckIfEnemiesClose() && Cleave.KnownSpell)
            || (Execute.KnownSpell && ObjectManager.Target.HealthPercent < 40));

        // Check Auto-Attacking
        CheckAutoAttack();

        // Check if we need to interrupt
        int channelTimeLeft = Lua.LuaDoString<int>(@"local spell, _, _, _, endTimeMS = UnitChannelInfo('target')
                                    if spell then
                                     local finish = endTimeMS / 1000 - GetTime()
                                     return finish
                                    end");
        if (channelTimeLeft < 0 || ObjectManager.Target.CastingTimeLeft > Usefuls.Latency)
            _shouldBeInterrupted = true;
        
        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds > 0)
            _pullMeleeTimer.Reset();

        if (_meleeTimer.ElapsedMilliseconds <= 0 && _pullFromAfar)
            _meleeTimer.Start();

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000) && Main.settingRange != 5f)
        {
            if (!_casterEnemies.Contains(ObjectManager.Target.Name))
                _casterEnemies.Add(ObjectManager.Target.Name);
            _fightingACaster = true;
            Main.LogDebug("Going in Melee range 2");
            Main.settingRange = 5f;
            _meleeTimer.Stop();
        }

        // Interrupt?
        if (_fightingACaster)
        {

        }

        // Overpower
        if (Overpower.IsSpellUsable)
            if (Cast(Overpower))
                return;

        // Retaliation
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1)
            if (Cast(Retaliation))
                return;

        // Cleave
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1 && CheckIfEnemiesClose())
            if (Cast(Cleave))
                return;

        // Blood Rage
        if (_settings.UseBloodRage && Me.HealthPercent > 90)
            if (Cast(BloodRage))
                return;

        // Hamstring
        if (ObjectManager.Target.CreatureTypeTarget == "Humanoid" && _inMeleeRange && _settings.UseHamstring && ObjectManager.Target.HealthPercent < 40
            && !ObjectManager.Target.HaveBuff("Hamstring"))
            if (Cast(Hamstring))
                return;

        // Battle Shout
        if (!Me.HaveBuff("Battle Shout"))
            if (Cast(BattleShout))
                return;

        // Rend
        if (!ObjectManager.Target.HaveBuff("Rend") && CanBleed(ObjectManager.Target) && _inMeleeRange && !_saveRage)
            if (Cast(Rend))
                return;

        // Demoralizing Shout
        if (_settings.UseDemoralizingShout && !ObjectManager.Target.HaveBuff("Demoralizing Shout") 
            && (!CheckIfEnemiesClose() || ObjectManager.GetNumberAttackPlayer() > 1) && _inMeleeRange)
            if (Cast(DemoralizingShout))
                return;

        // Execute
        if (ObjectManager.Target.HealthPercent < 20)
            if (Cast(Execute))
                return;
        
        // Heroic Strike
        if (_inMeleeRange && !HeroicStrikeOn() && !_saveRage)
            if (Cast(HeroicStrike))
                return;
    }

    public static void ShowConfiguration()
    {
        ZEWarriorSettings.Load();
        ZEWarriorSettings.CurrentSetting.ToForm();
        ZEWarriorSettings.CurrentSetting.Save();
    }

    private static Spell Attack = new Spell("Attack");
    private static Spell HeroicStrike = new Spell("Heroic Strike");
    private static Spell BattleShout = new Spell("Battle Shout");
    private static Spell Charge = new Spell("Charge");
    private static Spell Rend = new Spell("Rend");
    private static Spell Hamstring = new Spell("Hamstring");
    private static Spell BloodRage = new Spell("Bloodrage");
    private static Spell Overpower = new Spell("Overpower");
    private static Spell DemoralizingShout = new Spell("Demoralizing Shout");
    private static Spell Throw = new Spell("Throw");
    private static Spell Shoot = new Spell("Shoot");
    private static Spell Retaliation = new Spell("Retaliation");
    private static Spell Cleave = new Spell("Cleave");
    private static Spell Execute = new Spell("Execute");

    private static bool CheckIfEnemiesOnPull(WoWUnit target)
    {
        surroundingEnemies = ObjectManager.GetObjectWoWUnit();
        WoWUnit closestUnit = null;
        float closestUnitDistance = 100;

        foreach (WoWUnit unit in surroundingEnemies)
        {
            bool flagHostile = unit.Reaction.ToString().Equals("Hostile");
            float distanceFromTarget = unit.Position.DistanceTo(target.Position);

            if (unit.IsAlive && !unit.IsTapDenied && unit.IsValid && !unit.IsTaggedByOther && !unit.PlayerControlled 
                && unit.IsAttackable && distanceFromTarget < closestUnitDistance && flagHostile && unit.Guid != target.Guid)
            {
                closestUnit = unit;
                closestUnitDistance = distanceFromTarget;
            }
        }
        
        if (closestUnit != null && closestUnitDistance < 25)
        {
            Main.LogDebug(closestUnit.Guid.ToString());
            Main.LogDebug(target.Guid.ToString());
            Main.Log("Enemy too close: " + closestUnit.Name + ", pulling");
            return true;
        }
        return false;
    }

    private static bool CheckIfEnemiesClose()
    {
        surroundingEnemies = ObjectManager.GetObjectWoWUnit();
        WoWUnit closestUnit = null;
        float closestUnitDistance = 50;

        foreach (WoWUnit unit in surroundingEnemies)
        {
            float distanceFromTarget = unit.Position.DistanceTo(ObjectManager.Me.Position);

            if (unit.IsAlive && !unit.IsTapDenied && unit.IsValid && !unit.IsTaggedByOther && !unit.PlayerControlled
                && unit.IsAttackable && distanceFromTarget < closestUnitDistance && unit.Guid != ObjectManager.Target.Guid)
            {
                closestUnit = unit;
                closestUnitDistance = distanceFromTarget;
            }
        }

        if (closestUnit != null && closestUnitDistance < 10)
        {
            Main.Log("Enemy too close: " + closestUnit.Name);
            return true;
        }
        return false;
    }

    internal static bool Cast(Spell s)
    {
        Main.LogDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;
        
        s.Launch();
        return true;
    }

    private static void CheckAutoAttack()
    {
        bool _autoAttacking = Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Attack') then isAutoRepeat = true end", "isAutoRepeat");
        if (!_autoAttacking && ObjectManager.GetNumberAttackPlayer() > 0)
        {
            Main.LogDebug("Re-activating attack");
            Attack.Launch();
        }
    }

    private static bool CanBleed(WoWUnit unit)
    {
        return unit.CreatureTypeTarget != "Elemental" && unit.CreatureTypeTarget != "Mechanical";
    }

    private static bool HeroicStrikeOn()
    {
        return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Heroic Strike') then isAutoRepeat = true end", "isAutoRepeat");
    }
}
