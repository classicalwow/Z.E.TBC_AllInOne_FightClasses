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
using System.ComponentModel;
using robotManager.FiniteStateMachine;

public static class Druid
{
    private static float _meleRange = Main.settingRange;
    private static float _pullRange = 28f;
    private static bool _isStealthApproching;
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEDruidSettings _settings;
    private static bool _fightingACaster = false;
    private static List<string> _casterEnemies = new List<string>();
    private static bool _pullFromAfar = false;
    private static int _bigHealComboCost;
    private static int _smallHealComboCost;

    public static void Initialize()
    {
        Main.Log("Initialized");
        Main.settingRange = _pullRange;
        ZEDruidSettings.Load();
        _settings = ZEDruidSettings.CurrentSetting;

        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
            _pullFromAfar = false;
            Main.settingRange = _pullRange;
            _isStealthApproching = false;
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (Regrowth.KnownSpell)
            {
                _bigHealComboCost = ToolBox.GetSpellCost("Regrowth") + ToolBox.GetSpellCost("Rejuvenation") +
                ToolBox.GetSpellCost("Bear Form");
                _smallHealComboCost = ToolBox.GetSpellCost("Regrowth") + ToolBox.GetSpellCost("Bear Form");
            }
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
            // Regrowth
            if (Me.HealthPercent < 50 && !Me.HaveBuff("Regrowth"))
                if (Cast(Regrowth))
                    return;

            // Rejuvenation
            if (Me.HealthPercent < 50 && !Me.HaveBuff("Rejuvenation") && !Regrowth.KnownSpell)
                if (Cast(Rejuvenation))
                    return;

            // Healing Touch
            if (Me.HealthPercent < 40 && !Regrowth.KnownSpell)
                if (Cast(HealingTouch))
                    return;

            // Mark of the Wild
            if (!Me.HaveBuff("Mark of the Wild") && MarkOfTheWild.KnownSpell && MarkOfTheWild.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                if (Cast(MarkOfTheWild))
                    Lua.RunMacroText("/cleartarget");
                return;
            }

            // Thorns
            if (!Me.HaveBuff("Thorns") && Thorns.KnownSpell && Thorns.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                if (Cast(Thorns))
                    Lua.RunMacroText("/cleartarget");
                return;
            }

            // Cat Form
            if (!Me.HaveBuff("Cat Form"))
                if (Cast(CatForm))
                    return;
        }
    }

    internal static void Pull()
    {
        if (!BearForm.KnownSpell || (!Me.HaveBuff("Bear Form") && !Me.HaveBuff("Cat Form")))
            _pullFromAfar = true;

        // Check if surrounding enemies
        if (ObjectManager.Target.GetDistance < _pullRange && !_pullFromAfar)
            _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange);

        // Pull from afar
        if (((_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000) || _settings.AlwaysPull)
            && ObjectManager.Target.GetDistance <= _pullRange)
        {
            Main.settingRange = _pullRange;
            MovementManager.StopMoveTo(false, 500);
            if (Cast(FaerieFire) || Cast(Wrath))
            {
                Thread.Sleep(2000);
                return;
            }
        }

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 && ObjectManager.Target.GetDistance <= _pullRange)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
        {
            Main.Log("Going in Melee range (pull)");
            Main.settingRange = _meleRange;
            _pullMeleeTimer.Reset();
        }

        // Check if caster in list
        if (_casterEnemies.Contains(ObjectManager.Target.Name))
            _fightingACaster = true;

        // Cat Form
        if (!Me.HaveBuff("Cat Form") && !_pullFromAfar)
            if (Cast(CatForm))
                return;

        // Prowl
        if (Me.HaveBuff("Cat Form") && !_pullFromAfar)
            if (Cast(Prowl))
                return;

        // Pull Bear/Cat
        if (Me.HaveBuff("Bear Form") || Me.HaveBuff("Cat Form") || !_pullFromAfar)
        {
            Main.settingRange = _meleRange;

            // Prowl approach
            if (Me.HaveBuff("Prowl") && ObjectManager.Target.GetDistance > 6f)
            {
                _isStealthApproching = true;
                var pos = 1;
                if (ObjectManager.Me.IsAlive && ObjectManager.Target.IsAlive && pos == 1)
                {
                    Vector3 position = ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 5f);
                    //MovementManager.Go(PathFinder.FindPath(position), false);
                    MovementManager.MoveTo(position);

                    while (MovementManager.InMovement && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Target.GetDistance > 6f)
                    {
                        // Wait follow path
                        Thread.Sleep(1000);
                        pos = 0;
                    }
                }
                Main.LogDebug("Approcahing");
            }
        }
        
        // Pull from distance
        if (_pullFromAfar && ObjectManager.Target.GetDistance <= _pullRange && !Me.HaveBuff("Bear Form"))
        {
            MovementManager.StopMoveTo(false, 500);
            if (Cast(FaerieFire) || Cast(Wrath))
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

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 3000) && Main.settingRange != _meleRange)
        {
            Main.Log("Going in Melee range (combat)");
            Main.settingRange = _meleRange;
            _meleeTimer.Stop();
        }

        // Regrowth + Rejuvenation
        if (Me.HealthPercent < 50 && !Me.HaveBuff("Regrowth") && Me.Mana > _bigHealComboCost)
            if (Cast(Regrowth) && Cast(Rejuvenation))
                return;

        // Regrowth
        if (Me.HealthPercent < 50 && !Me.HaveBuff("Regrowth") && Me.Mana > _smallHealComboCost)
            if (Cast(Regrowth))
                return;

        // Rejuvenation
        if (Me.HealthPercent < 50 && !Me.HaveBuff("Rejuvenation") && !Regrowth.KnownSpell)
            if (Cast(Rejuvenation))
                return;

        // Healing Touch
        if (Me.HealthPercent < 30 && !Regrowth.KnownSpell)
            if (Cast(HealingTouch))
                return;

        // Catorm
        if (!Me.HaveBuff("Cat Form"))
            if (Cast(CatForm))
                return;

        // Bear Form
        if (!Me.HaveBuff("Bear Form") && !CatForm.KnownSpell)
            if (Cast(BearForm))
                return;

        #region Cat Form Rotation

        // **************** CAT FORM ROTATION ****************

        if (Me.HaveBuff("Cat Form"))
        {
            Main.settingRange = _meleRange;
            // Rip logic
            if (!_target.HaveBuff("Rip"))
            {
                if (Me.ComboPoint >= 3 && _target.HealthPercent > 50)
                    if (Cast(Rip))
                        return;

                if (Me.ComboPoint >= 1 && _target.HealthPercent <= 50)
                    if (Cast(Rip))
                        return;
            }

            // Claw
            if (Me.ComboPoint < 5)
                if (Cast(Claw))
                    return;
        }

        #endregion

        #region Bear form rotation

        // **************** BEAR FORM ROTATION ****************

        if (Me.HaveBuff("Bear Form"))
        {
            Main.settingRange = _meleRange;
            // Swipe
            if (ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(8f))
                if (Cast(Swipe))
                    return;

            // Interrupt with Bash
            if (_shouldBeInterrupted)
                if (Cast(Bash))
                    return;

            // Enrage
            if (_settings.UseEnrage)
                if (Cast(Enrage))
                    return;

            // Demoralizing Roar
            if (!_target.HaveBuff("Demoralizing Roar") && _target.GetDistance < 9f)
                if (Cast(DemoralizingRoar))
                    return;

            // Maul
            if (!MaulOn())
                if (Cast(Maul))
                    return;
        }

        #endregion

        #region Human form rotation
        
        // **************** HUMAN FORM ROTATION ****************

        if (!Me.HaveBuff("Bear Form") && !Me.HaveBuff("Cat Form"))
        {
            // Moonfire
            if (!_target.HaveBuff("Moonfire") && Me.ManaPercentage > 35 && _target.HealthPercent > 30 && Me.Level >= 8)
                if (Cast(Moonfire))
                    return;

            // Wrath
            if (_target.GetDistance <= _pullRange && Me.ManaPercentage > 45 && _target.HealthPercent > 30 && Me.Level >= 8)
                if (Cast(Wrath))
                    return;

            // Moonfire Low level DPS
            if (!_target.HaveBuff("Moonfire") && Me.ManaPercentage > 50 && _target.HealthPercent > 30 && Me.Level < 8)
                if (Cast(Moonfire))
                    return;

            // Wrath Low level DPS
            if (_target.GetDistance <= _pullRange && Me.ManaPercentage > 60 && _target.HealthPercent > 30 && Me.Level < 8)
                if (Cast(Wrath))
                    return;
        }
        #endregion
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
    private static Spell Thorns = new Spell("Thorns");
    private static Spell BearForm = new Spell("Bear Form");
    private static Spell CatForm = new Spell("Cat Form");
    private static Spell Maul = new Spell("Maul");
    private static Spell DemoralizingRoar = new Spell("Demoralizing Roar");
    private static Spell Enrage = new Spell("Enrage");
    private static Spell Regrowth = new Spell("Regrowth");
    private static Spell Bash = new Spell("Bash");
    private static Spell Swipe = new Spell("Swipe");
    private static Spell FaerieFire = new Spell("Faerie Fire");
    private static Spell Claw = new Spell("Claw");
    private static Spell Prowl = new Spell("Prowl");
    private static Spell Rip = new Spell("Rip");

    private static bool MaulOn()
    {
        return Lua.LuaDoString<bool>("maulon = false; if IsCurrentSpell('Maul') then maulon = true end", "maulon");
    }

    private static bool Cast(Spell s)
    {
        if (!s.KnownSpell)
            return false;

        Main.LogDebug("*----------- INTO CAST FOR " + s.Name);
        float _spellCD = ToolBox.GetSpellCooldown(s.Name);
        Main.LogDebug("Cooldown is " + _spellCD);

        if (ToolBox.GetSpellCost(s.Name) > Me.Mana)
        {
            Main.LogDebug(s.Name + ": Not enough mana, SKIPPING");
            return false;
        }

        if (_spellCD >= 2f)
        {
            Main.LogDebug("Didn't cast because cd is too long");
            return false;
        }

        if (_spellCD < 2f && _spellCD > 0f)
        {
            if (ToolBox.GetSpellCastTime(s.Name) < 1f)
            {
                Main.LogDebug(s.Name + " is instant and low CD, recycle");
                return true;
            }

            int t = 0;
            while (ToolBox.GetSpellCooldown(s.Name) > 0)
            {
                Thread.Sleep(50);
                t += 50;
                if (t > 2000)
                {
                    Main.LogDebug(s.Name + ": waited for tool long, give up");
                    return false;
                }
            }
            Thread.Sleep(100 + Usefuls.Latency);
            Main.LogDebug(s.Name + ": waited " + (t + 100) + " for it to be ready");
        }

        if (!s.IsSpellUsable)
        {
            Main.LogDebug("Didn't cast because spell somehow not usable");
            return false;
        }

        Main.LogDebug("Launching");
        if (ObjectManager.Target.IsAlive || (!Fight.InFight && ObjectManager.Target.Guid < 1))
            s.Launch();
        return true;
    }
}
