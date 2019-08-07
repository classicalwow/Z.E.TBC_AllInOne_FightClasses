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

public class ZEFuryWarrior : ICustomClass
{
    private static bool _debug = true;
    
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEWarriorSettings _settings;
    private bool _isLaunched;
    private bool _goInMelee = false;
    private bool _fightingACaster = false;
    private float _pullRange = 28f;
    List<string> _casterEnemies = new List<string>();

    public float Range
	{
		get
        {
            //return _goInMelee ? 5f : _pullRange;
            return 5f;
        }
    }

    public void Initialize()
    {
        _isLaunched = true;
        Log("Initialized");
        ZEWarriorSettings.Load();
        _settings = ZEWarriorSettings.CurrentSetting;

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _goInMelee = false;
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
        };
            
        Rotation();
    }


    public void Dispose()
    {
        _isLaunched = false;
        Log("Stop in progress.");
    }
    
	internal void Rotation()
	{
        Log("Started");
		while (_isLaunched)
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
        Log("Stopped.");
    }

    internal void BuffRotation()
    {
        if (!Me.IsMounted && !Me.IsCast)
        {
            // Battle Shout
            if (!Me.HaveBuff("Battle Shout") && BattleShout.IsSpellUsable)
                if (Cast(BattleShout))
                    return;
        }
    }

    internal void Pull()
    {
        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 && ObjectManager.Target.GetDistance <= _pullRange + 3)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 8000 && !_goInMelee)
        {
            _goInMelee = true;
            _pullMeleeTimer.Reset();
        }

        // Check if caster
        if (_casterEnemies.Contains(ObjectManager.Target.Name))
            _fightingACaster = true;

        // Charge
        if (ObjectManager.Target.GetDistance > 9f && ObjectManager.Target.GetDistance < 24f && Charge.KnownSpell 
            && Charge.IsSpellUsable)
            Charge.Launch();

        // Pull with Lightning Bolt
        /*if (ObjectManager.Target.GetDistance <= _pullRange + 1)
        {
            bool cast = false;
            if (_settings.PullRankOneLightningBolt && LightningBolt.IsSpellUsable)
            {
                MovementManager.StopMove();
                Lua.RunMacroText("/cast Lightning Bolt(Rank 1)");
                cast = true;
            }
            else
            {
                if (Cast(LightningBolt))
                {
                    cast = true;
                }
            }
            
            if (cast)
                return;
        }*/
    }

    internal void CombatRotation()
    {
        bool _shouldBeInterrupted = false;
        bool _inMeleeRange = ObjectManager.Target.GetDistance < 6f;

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

        if (_meleeTimer.ElapsedMilliseconds <= 0 && !_goInMelee)
            _meleeTimer.Start();

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000) && !_goInMelee)
        {
            Debug("Going in melee range");
            if (!_casterEnemies.Contains(ObjectManager.Target.Name))
                _casterEnemies.Add(ObjectManager.Target.Name);
            _fightingACaster = true;
            _goInMelee = true;
            _meleeTimer.Stop();
        }

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
        if (!ObjectManager.Target.HaveBuff("Rend") && CanBleed(ObjectManager.Target) && _inMeleeRange)
            if (Cast(Rend))
                return;
        
        // Heroic Strike
        if (_inMeleeRange && !HeroicStrikeOn())
            if (Cast(HeroicStrike))
                return;
    }

    public void ShowConfiguration()
    {
        ZEWarriorSettings.Load();
        ZEWarriorSettings.CurrentSetting.ToForm();
        ZEWarriorSettings.CurrentSetting.Save();
    }
    
    private Spell Attack = new Spell("Attack");
    private Spell HeroicStrike = new Spell("Heroic Strike");
    private Spell BattleShout = new Spell("Battle Shout");
    private Spell Charge = new Spell("Charge");
    private Spell Rend = new Spell("Rend");
    private Spell Hamstring = new Spell("Hamstring");
    private Spell BloodRage = new Spell("Bloodrage");

    internal static bool Cast(Spell s)
    {
        Debug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;
        
        s.Launch();
        return true;
    }

    private void CheckAutoAttack()
    {
        bool _autoAttacking = Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Attack') then isAutoRepeat = true end", "isAutoRepeat");
        if (!_autoAttacking && ObjectManager.GetNumberAttackPlayer() > 0)
        {
            Log("Re-activating attack");
            Attack.Launch();
        }
    }

    private bool CanBleed(WoWUnit unit)
    {
        return unit.CreatureTypeTarget != "Elemental" && unit.CreatureTypeTarget != "Mechanical";
    }

    private bool HeroicStrikeOn()
    {
        return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Heroic Strike') then isAutoRepeat = true end", "isAutoRepeat");
    }

    internal static void Log(string s)
    {
        Logging.WriteDebug("[Z.E.Warrior] " + s);
    }

    internal static void Debug(string s)
    {
        if (_debug)
            Logging.WriteDebug("[Z.E.Warrior DEBUG] " + s);
    }
}
