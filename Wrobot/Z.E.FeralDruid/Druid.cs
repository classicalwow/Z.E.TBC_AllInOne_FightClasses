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
using wManager.Wow.Bot.Tasks;

public static class Druid
{
    private static float _meleRange = Main.settingRange;
    private static float _pullRange = 27f;
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEDruidSettings _settings;
    private static bool _fightingACaster = false;
    private static List<string> _casterEnemies = new List<string>();
    private static bool _pullFromAfar = false;

    public static void Initialize()
    {
        Main.Log("Initialized");
        Main.settingRange = _pullRange;
        ZEDruidSettings.Load();
        _settings = ZEDruidSettings.CurrentSetting;

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
            _pullFromAfar = false;
            Main.settingRange = _pullRange;
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
			Thread.Sleep(Usefuls.Latency + 20);
		}
        Main.Log("Stopped.");
    }

    internal static void BuffRotation()
    {
        if (!Me.IsMounted && !Me.IsCast)
        {
            // Mark of the Wild
            if (!Me.HaveBuff("Mark of the Wild"))
            {
                Lua.RunMacroText("/target player");
                if (Cast(MarkOfTheWild))
                    Lua.RunMacroText("/cleartarget");
            }

            // Rejuvenation
            if (Me.HealthPercent < 70 && !Me.HaveBuff("Rejuvenation"))
                if (Cast(Rejuvenation))
                    return;

            // Healing Touch
            if (Me.HealthPercent < 50)
                if (Cast(Wrath))
                    return;
        }
    }

    internal static void Pull()
    {
        _pullFromAfar = true;
        // Check if surrounding enemies
        if (ObjectManager.Target.GetDistance < _pullRange && !_pullFromAfar)
            _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange);

        // Pull from afar
        if ((_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000) || _settings.AlwaysPull
            && ObjectManager.Target.GetDistance < _pullRange + 2)
        {
            // pull here
        }

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 && ObjectManager.Target.GetDistance <= _pullRange + 2)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
        {
            Main.LogDebug("Going in Melee range");
            Main.settingRange = _meleRange;
            _pullMeleeTimer.Reset();
        }

        // Check if caster in list
        if (_casterEnemies.Contains(ObjectManager.Target.Name))
            _fightingACaster = true;

        // Pull with Wrath
        if (_pullFromAfar && ObjectManager.Target.GetDistance < _pullRange + 2)
        {
            MovementManager.StopMoveTo(false, 500);
            if (Cast(Wrath))
            {
                Thread.Sleep(2000);
                return;
            }
        }
    }

    internal static void CombatRotation()
    {
        bool _shouldBeInterrupted = ToolBox.EnemyCasting();
        bool _inMeleeRange = ObjectManager.Target.GetDistance < 6f;
        WoWUnit _target = ObjectManager.Target;

        // Check Auto-Attacking
        ToolBox.CheckAutoAttack(Attack);

        // Check if fighting a caster
        if (_shouldBeInterrupted)
        {
            _fightingACaster = true;
            if (!_casterEnemies.Contains(_target.Name))
                _casterEnemies.Add(_target.Name);
        }

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds > 0)
            _pullMeleeTimer.Reset();

        if (_meleeTimer.ElapsedMilliseconds <= 0 && _pullFromAfar)
            _meleeTimer.Start();

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000) && Main.settingRange != _meleRange)
        {
            Main.LogDebug("Going in Melee range 2");
            Main.settingRange = _meleRange;
            _meleeTimer.Stop();
        }

        // Interrupt
        //if (_shouldBeInterrupted)

        // Rejuvenation
        if (Me.HealthPercent < 70 && !Me.HaveBuff("Rejuvenation"))
            if (Cast(Rejuvenation))
                return;

        // Healing Touch
        if (Me.HealthPercent < 50)
            if (Cast(Wrath))
                return;

        // Moonfire
        if (!_target.HaveBuff("Moonfire") && Me.ManaPercentage > 50 && _target.HealthPercent > 30)
            if (Cast(Moonfire))
                return;

        // Wrath Low level DPS
        if (_target.GetDistance < _pullRange && Me.ManaPercentage > 60 && _target.HealthPercent > 30)
            if (Cast(Wrath))
                return;
    }

    public static void ShowConfiguration()
    {
        ZEDruidSettings.Load();
        ZEDruidSettings.CurrentSetting.ToForm();
        ZEDruidSettings.CurrentSetting.Save();
    }

    private static Spell Attack = new Spell("Attack");
    private static Spell HealingTouch = new Spell("Healing Touch");
    private static Spell Wrath = new Spell("Wrath");
    private static Spell MarkOfTheWild = new Spell("Mark of the Wild");
    private static Spell Moonfire = new Spell("Moonfire");
    private static Spell Rejuvenation = new Spell("Rejuvenation");

    internal static bool Cast(Spell s)
    {
        Main.LogDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;
        
        s.Launch();
        Usefuls.WaitIsCasting();
        return true;
    }
}
