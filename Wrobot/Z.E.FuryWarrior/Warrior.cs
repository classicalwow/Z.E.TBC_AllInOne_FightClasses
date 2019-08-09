using System;
using System.Diagnostics;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Collections.Generic;

public static class Warrior
{
    private static float _meleRange = Main.settingRange;
    private static float _pullRange = 25f;
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEWarriorSettings _settings;
    private static bool _fightingACaster = false;
    private static List<string> _casterEnemies = new List<string>();
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
            Main.settingRange = _meleRange;
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
        if (ObjectManager.Target.GetDistance < _pullRange && !_pullFromAfar)
            _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange);

        // Pull from afar
        if (_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000)
        {
            bool _castPull = Cast(Throw) || Cast(Shoot);
            if (_castPull)
            {
                Main.settingRange = _pullRange;
                Thread.Sleep(2000);
            }
            else
                Main.Log("Can't pull from distance. Please equip a ranged weapon in order to Throw or Shoot.");
        }

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 && ObjectManager.Target.GetDistance <= _pullRange + 3)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
        {
            Main.LogDebug("Going in Melee range");
            Main.settingRange = _meleRange;
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
        bool _saveRage = ((Cleave.KnownSpell && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f))
            || (Execute.KnownSpell && ObjectManager.Target.HealthPercent < 40) 
            || (Bloodthirst.KnownSpell && ObjectManager.Me.Rage < 40 && ObjectManager.Target.HealthPercent > 40));

        // Check Auto-Attacking
        ToolBox.CheckAutoAttack(Attack);

        // Check if we need to interrupt
        _shouldBeInterrupted = ToolBox.EnemyCasting();

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds > 0)
            _pullMeleeTimer.Reset();

        if (_meleeTimer.ElapsedMilliseconds <= 0 && _pullFromAfar)
            _meleeTimer.Start();

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000) && Main.settingRange != _meleRange)
        {
            if (!_casterEnemies.Contains(ObjectManager.Target.Name))
                _casterEnemies.Add(ObjectManager.Target.Name);
            _fightingACaster = true;
            Main.LogDebug("Going in Melee range 2");
            Main.settingRange = 5f;
            _meleeTimer.Stop();
        }

        // Interrupt !
        if (_fightingACaster)
        {

        }

        // Execute
        if (ObjectManager.Target.HealthPercent < 20)
            if (Cast(Execute))
                return;

        // Overpower
        if (Cast(Overpower))
            return;

        // Bloodthirst
        if (Cast(Bloodthirst))
            return;

        // Sweeping Strikes
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f))
            if (Cast(SweepingStrikes))
                return;

        // Retaliation
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f))
            if (Cast(Retaliation))
                return;

        // Cleave
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f) && 
            (!SweepingStrikes.IsSpellUsable || !SweepingStrikes.KnownSpell) && ObjectManager.Me.Rage > 40)
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
        if (!ObjectManager.Target.HaveBuff("Rend") && ToolBox.CanBleed(ObjectManager.Target) && _inMeleeRange && !_saveRage)
            if (Cast(Rend))
                return;

        // Demoralizing Shout
        if (_settings.UseDemoralizingShout && !ObjectManager.Target.HaveBuff("Demoralizing Shout") 
            && (ObjectManager.GetNumberAttackPlayer() > 1 || !ToolBox.CheckIfEnemiesClose(15f)) && _inMeleeRange)
            if (Cast(DemoralizingShout))
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
    private static Spell SweepingStrikes = new Spell("Sweeping Strikes");
    private static Spell Bloodthirst = new Spell("Bloodthirst");

    internal static bool Cast(Spell s)
    {
        Main.LogDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;
        
        s.Launch();
        return true;
    }

    private static bool HeroicStrikeOn()
    {
        return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Heroic Strike') then isAutoRepeat = true end", "isAutoRepeat");
    }
}
