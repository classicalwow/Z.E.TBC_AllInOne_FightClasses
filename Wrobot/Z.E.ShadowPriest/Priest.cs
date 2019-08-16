using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class Priest
{
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static float _maxRange = 28f;
    private static bool _usingWand = false;
    private static int _innerManaSaveThreshold = 20;
    private static int _wandThreshold;
    private static bool _goInMFRange = false;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEPriestSettings.Load();
        _wandThreshold = ZEPriestSettings.CurrentSetting.WandThreshold > 100 ? 50 : ZEPriestSettings.CurrentSetting.WandThreshold;
        
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _usingWand = false;
            _goInMFRange = false;
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
                    Main.settingRange = _goInMFRange ? 17f : _maxRange - 2;
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
            // OOC Cure Disease
            if (ToolBox.HasDiseaseDebuff() && CureDisease.KnownSpell && CureDisease.IsSpellUsable)
                Cast(CureDisease, true, true);

            // OOC Renew
            if (Me.HealthPercent < 60 && !Me.HaveBuff("Renew") && Renew.KnownSpell && Renew.IsSpellUsable)
                Cast(Renew, true, true);

            // OOC Power WOrd Shield
            if (Me.HealthPercent < 50 && !Me.HaveBuff("Power Word: Shield") && !ToolBox.HasDebuff("Weakened Soul")
                && ObjectManager.GetNumberAttackPlayer() > 0 && PowerWordShield.KnownSpell && PowerWordShield.IsSpellUsable)
                Cast(PowerWordShield, true, true);

            // OOC Psychic Scream
            if (Me.HealthPercent < 30 && ObjectManager.GetNumberAttackPlayer() > 1 && PsychicScream.KnownSpell 
                && PsychicScream.IsSpellUsable)
                Cast(PsychicScream, true, true);

            // OOC Power Word Fortitude
            if (!Me.HaveBuff("Power Word: Fortitude") && PowerWordFortitude.KnownSpell && PowerWordFortitude.IsSpellUsable)
                Cast(PowerWordFortitude, true, true);

            // OOC Inner Fire
            if (!Me.HaveBuff("Inner Fire") && ZEPriestSettings.CurrentSetting.UseInnerFire && InnerFire.KnownSpell)
                Cast(InnerFire, true, true);

            // OOC Shadowguard
            if (!Me.HaveBuff("Shadowguard") && Shadowguard.KnownSpell && Shadowguard.IsSpellUsable && ZEPriestSettings.CurrentSetting.UseShadowGuard)
                Cast(Shadowguard, true, true);

            // OOC Shadow Protection
            if (!Me.HaveBuff("Shadow Protection") && ShadowProtection.KnownSpell && ZEPriestSettings.CurrentSetting.UseShadowProtection)
                Cast(ShadowProtection, true, true);

            // OOC ShadowForm
            if (!Me.HaveBuff("ShadowForm") && Shadowform.KnownSpell && Shadowform.IsSpellUsable && ObjectManager.GetNumberAttackPlayer() < 1)
                Cast(Shadowform, true, true);
        }
    }

    internal static void Pull()
    {
        // Power Word Shield
        if (!ToolBox.HasDebuff("Weakened Soul") && ZEPriestSettings.CurrentSetting.UseShieldOnPull && PowerWordShield.KnownSpell && PowerWordShield.IsSpellUsable
            && !Me.HaveBuff("Power Word: Shield"))
            Cast(PowerWordShield, true, true);

        // Vampiric Touch
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange && VampiricTouch.KnownSpell && VampiricTouch.IsSpellUsable)
        {
            Cast(VampiricTouch, true, false);
            return;
        }

        // MindBlast
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange && MindBlast.KnownSpell && MindBlast.IsSpellUsable
            && !VampiricTouch.KnownSpell)
        {
            Cast(MindBlast, true, false);
            return;
        }

        // Shadow Word Pain
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange && (!MindBlast.KnownSpell || !MindBlast.IsSpellUsable)
            && ShadowWordPain.KnownSpell && ShadowWordPain.IsSpellUsable)
        {
            Cast(ShadowWordPain, true, true);
            return;
        }

        // Holy Fire
        if (ObjectManager.Target.GetDistance <= _maxRange && HolyFire.KnownSpell && HolyFire.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
        {
            Cast(HolyFire, true, false);
            return;
        }

        // Smite
        if (ObjectManager.Target.GetDistance <= _maxRange && Smite.KnownSpell && !HolyFire.KnownSpell && Smite.IsSpellUsable 
            && !Me.HaveBuff("ShadowForm"))
        {
            Cast(Smite, false, false);
            return;
        }
    }

    internal static void CombatRotation()
    {
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
        bool _hasMagicDebuff = ToolBox.HasMagicDebuff();
        bool _hasDisease = ToolBox.HasDiseaseDebuff();
        bool _hasWeakenedSoul = ToolBox.HasDebuff("Weakened Soul");
        double _myManaPC = Me.ManaPercentage;
        bool _inShadowForm = Me.HaveBuff("ShadowForm");
        int _mindBlastCD = Lua.LuaDoString<int>("local start, duration, enabled = GetSpellCooldown(\"Mind Blast\"); return start + duration - GetTime();");
        int _innerFocusCD = Lua.LuaDoString<int>("local start, duration, enabled = GetSpellCooldown(\"Inner Focus\"); return start + duration - GetTime();");
        bool _shoulBeInterrupted = ToolBox.EnemyCasting();

        // Power Word Shield
        if (Me.HealthPercent < 70 && !Me.HaveBuff("Power Word: Shield") && !_hasWeakenedSoul && PowerWordShield.KnownSpell)
            if (Cast(PowerWordShield, true, true))
                return;

        // Renew
        if (Me.HealthPercent < 60 && !Me.HaveBuff("Renew") && Renew.KnownSpell && !_inShadowForm)
            if (Cast(Renew, true, true))
                return;

        // Psychic Scream
        if (Me.HealthPercent < 50 && ObjectManager.GetNumberAttackPlayer() > 1 && PsychicScream.KnownSpell)
            if (Cast(PsychicScream, true, true))
                return;

        // Flash Heal
        if (Me.HealthPercent < 50 && FlashHeal.KnownSpell)
            if (Cast(FlashHeal, true, false))
                return;

        // Heal
        if (Me.HealthPercent < 50 && Heal.KnownSpell)
            if (Cast(Heal, true, false))
                return;

        // Lesser Heal
        if (Me.HealthPercent < 50 && LesserHeal.KnownSpell && !FlashHeal.KnownSpell)
            if (Cast(LesserHeal, true, false))
                return;

        // Silence
        if (_shoulBeInterrupted && Silence.KnownSpell)
            if (Cast(Silence, true, true))
                return;

        // Cure Disease
        if (_hasDisease && CureDisease.KnownSpell && !_inShadowForm)
            if (Cast(CureDisease, true, true))
                return;

        // Dispel Magic self
        if (_hasMagicDebuff && DispelMagic.KnownSpell && _myManaPC > 10)
        {
            if (_usingWand)
                StopWandWaitGCD();
            Lua.RunMacroText("/target player");
            Lua.RunMacroText("/cast Dispel Magic");
            ToolBox.WaitGlobalCoolDown(Smite);
            return;
        }

        // Vampiric Touch
        if (ObjectManager.Target.GetDistance <= _maxRange && VampiricTouch.KnownSpell && VampiricTouch.IsSpellUsable
            && !ObjectManager.Target.HaveBuff("Vampiric Touch") && _myManaPC > _innerManaSaveThreshold 
            && ObjectManager.Target.HealthPercent > _wandThreshold)
        {
            Cast(VampiricTouch, true, false);
            return;
        }

        // Vampiric Embrace
        if (!ObjectManager.Target.HaveBuff("Vampiric Embrace") && VampiricEmbrace.KnownSpell && _myManaPC > _innerManaSaveThreshold)
            if (Cast(VampiricEmbrace, true, true))
                return;

        // ShadowFiend
        if (Shadowfiend.KnownSpell && ObjectManager.GetNumberAttackPlayer() > 1)
            if (Cast(Shadowfiend, true, true))
                return;

        // Shadow Word Pain
        if (_myManaPC > 10 && ObjectManager.Target.GetDistance < _maxRange && ObjectManager.Target.HealthPercent > 15
            && !ObjectManager.Target.HaveBuff("Shadow Word: Pain") && ShadowWordPain.KnownSpell)
            if (Cast(ShadowWordPain, true, true))
                return;

        // Inner Fire
        if (!Me.HaveBuff("Inner Fire") && ZEPriestSettings.CurrentSetting.UseInnerFire && InnerFire.KnownSpell
            && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.HealthPercent > _wandThreshold)
            if (Cast(InnerFire, true, true))
                return;

        // Shadowguard
        if (!Me.HaveBuff("Shadowguard") && Shadowguard.KnownSpell && _myManaPC > _innerManaSaveThreshold
            && ZEPriestSettings.CurrentSetting.UseShadowGuard && ObjectManager.Target.HealthPercent > _wandThreshold)
            if (Cast(Shadowguard, true, true))
                return;

        // Shadow Protection
        if (!Me.HaveBuff("Shadow Protection") && ShadowProtection.KnownSpell && _myManaPC > 70
            && ZEPriestSettings.CurrentSetting.UseShadowProtection)
            if (Cast(ShadowProtection, true, true))
                return;

        // Use Wand
        if ((ObjectManager.Target.HealthPercent < _wandThreshold || _myManaPC <= _innerManaSaveThreshold) && 
            !_usingWand && UseWand.IsSpellUsable)
        {
            UseWand.Launch();
            return;
        }

        // Shadow Word Death
        if (_myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange && ZEPriestSettings.CurrentSetting.UseShadowWordDeath
            && ShadowWordDeath.KnownSpell && ObjectManager.Target.HealthPercent < 15)
            if (Cast(ShadowWordDeath, true, false))
                return;

        // Mind Blast + Inner Focus
        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange
            && MindBlast.KnownSpell && ObjectManager.Target.HealthPercent > 50 && !Me.HaveBuff("Power Word: Shield")
            && _mindBlastCD <= 0)
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus, true, true);

            if (Cast(MindBlast, true, false))
                return;
        }

        // Mind Blast + Inner Focus
        if (_inShadowForm && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange
            && MindBlast.KnownSpell && _mindBlastCD <= 0 && ObjectManager.Target.HealthPercent > _wandThreshold)
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus, true, true);

            if (Cast(MindBlast, true, false))
                return;
        }

        // Mind Flay Range check
        if (_inShadowForm && !MindFlay.IsDistanceGood && MindFlay.KnownSpell && Me.HaveBuff("Power Word: Shield"))
        {
            Main.LogDebug("Approaching to be in Mind Flay range");
            _goInMFRange = true;
            return;
        }

        // Mind FLay
        if (Me.HaveBuff("Power Word: Shield") && MindFlay.KnownSpell && MindFlay.IsDistanceGood 
            && _myManaPC > _innerManaSaveThreshold)
            if (Cast(MindFlay, false, false))
                return;

        // Low level Smite
        if (Me.Level < 5 && (ObjectManager.Target.HealthPercent > 30 || Me.ManaPercentage > 80) && _myManaPC > _innerManaSaveThreshold 
            && ObjectManager.Target.GetDistance < _maxRange)
            if (Cast(Smite, false, false))
                return;

        // Smite
        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange && Smite.KnownSpell
            && Me.Level >= 5)
            if (Cast(Smite, false, false))
                return;

        // Use wand
        if (!_usingWand && UseWand.IsSpellUsable)
        {
            UseWand.Launch();
            return;
        }
    }

    public static void ShowConfiguration()
    {
        ZEPriestSettings.Load();
        ZEPriestSettings.CurrentSetting.ToForm();
        ZEPriestSettings.CurrentSetting.Save();
    }

    private static Spell Smite = new Spell("Smite");
    private static Spell LesserHeal = new Spell("Lesser Heal");
    private static Spell PowerWordFortitude = new Spell("Power Word: Fortitude");
    private static Spell PowerWordShield = new Spell("Power Word: Shield");
    private static Spell ShadowWordPain = new Spell("Shadow Word: Pain");
    private static Spell ShadowWordDeath = new Spell("Shadow Word: Death");
    private static Spell UseWand = new Spell("Shoot");
    private static Spell Renew = new Spell("Renew");
    private static Spell MindBlast = new Spell("Mind Blast");
    private static Spell InnerFire = new Spell("Inner Fire");
    private static Spell CureDisease = new Spell("Cure Disease");
    private static Spell PsychicScream = new Spell("Psychic Scream");
    private static Spell Heal = new Spell("Heal");
    private static Spell MindFlay = new Spell("Mind Flay");
    private static Spell HolyFire = new Spell("Holy Fire");
    private static Spell DispelMagic = new Spell("Dispel Magic");
    private static Spell FlashHeal = new Spell("Flash Heal");
    private static Spell VampiricEmbrace = new Spell("Vampiric Embrace");
    private static Spell Shadowguard = new Spell("Shadowguard");
    private static Spell ShadowProtection = new Spell("Shadow Protection");
    private static Spell Shadowform = new Spell("Shadowform");
    private static Spell VampiricTouch = new Spell("Vampiric Touch");
    private static Spell InnerFocus = new Spell("Inner Focus");
    private static Spell Shadowfiend = new Spell("Shadowfiend");
    private static Spell Silence = new Spell("Silence");

    private static bool Cast(Spell s, bool castEvenIfWanding, bool waitGCD)
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
            ToolBox.WaitGlobalCoolDown(Smite);
        return true;
    }

    private static void StopWandWaitGCD()
    {
        if (Me.ManaPercentage > 15)
        {
            UseWand.Launch();
            int c = 0;
            while (!Smite.IsSpellUsable)
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
