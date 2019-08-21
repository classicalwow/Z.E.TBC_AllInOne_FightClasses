using System;
using System.ComponentModel;
using System.Diagnostics;
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
    private static float _meleeRange = 5f;
    private static bool _usingWand = false;
    private static bool _iCanUseWand = false;
    private static int _innerManaSaveThreshold = 20;
    private static int _wandThreshold;
    private static bool _goInMFRange = false;
    private static Stopwatch _dispelTimer = new Stopwatch();
    private static ZEPriestSettings _settings;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEPriestSettings.Load();
        _settings = ZEPriestSettings.CurrentSetting;
        _wandThreshold = _settings.WandThreshold > 100 ? 50 : _settings.WandThreshold;

        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _usingWand = false;
            _goInMFRange = false;
            _dispelTimer.Reset();
            _iCanUseWand = false;
            Main.settingRange = _maxRange;
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
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
            if (ToolBox.HasDiseaseDebuff())
                if (Cast(CureDisease))
                    return;

            // OOC Renew
            if (Me.HealthPercent < 60 && !Me.HaveBuff("Renew"))
                if (Cast(Renew))
                    return;

            // OOC Power Word Shield
            if (Me.HealthPercent < 50 && !Me.HaveBuff("Power Word: Shield") && !ToolBox.HasDebuff("Weakened Soul")
                && ObjectManager.GetNumberAttackPlayer() > 0)
                if (Cast(PowerWordShield))
                    return;

            // OOC Psychic Scream
            if (Me.HealthPercent < 30 && ObjectManager.GetNumberAttackPlayer() > 1)
                if (Cast(PsychicScream))
                    return;

            // OOC Power Word Fortitude
            if (!Me.HaveBuff("Power Word: Fortitude") && PowerWordFortitude.KnownSpell && PowerWordFortitude.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                if (Cast(PowerWordFortitude))
                {
                    Lua.RunMacroText("/cleartarget");
                    return;
                }
            }

            // OOC Inner Fire
            if (!Me.HaveBuff("Inner Fire") && _settings.UseInnerFire)
                if (Cast(InnerFire))
                    return;

            // OOC Shadowguard
            if (!Me.HaveBuff("Shadowguard") && _settings.UseShadowGuard && Shadowguard.KnownSpell && Shadowguard.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                if (Cast(Shadowguard))
                {
                    Lua.RunMacroText("/cleartarget");
                    return;
                }
            }

            // OOC Shadow Protection
            if (!Me.HaveBuff("Shadow Protection") && ShadowProtection.KnownSpell && _settings.UseShadowProtection
                && ShadowProtection.KnownSpell && ShadowProtection.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                if (Cast(ShadowProtection))
                {
                    Lua.RunMacroText("/cleartarget");
                    return;
                }
            }

            // OOC ShadowForm
            if (!Me.HaveBuff("ShadowForm") && ObjectManager.GetNumberAttackPlayer() < 1)
                if (Cast(Shadowform))
                    return;
        }
    }

    internal static void Pull()
    {
        // Pull ShadowForm
        if (!Me.HaveBuff("ShadowForm"))
            if (Cast(Shadowform))
                return;

        // Power Word Shield
        if (!ToolBox.HasDebuff("Weakened Soul") && _settings.UseShieldOnPull
            && !Me.HaveBuff("Power Word: Shield"))
            if (Cast(PowerWordShield))
                return;

        // Vampiric Touch
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange 
            && !ObjectManager.Target.HaveBuff("Vampiric Touch"))
            if (Cast(VampiricTouch))
                return;

        // MindBlast
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange
            && !VampiricTouch.KnownSpell)
            if (Cast(MindBlast))
                return;

        // Shadow Word Pain
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange 
            && (!MindBlast.KnownSpell || !MindBlast.IsSpellUsable)
            && !ObjectManager.Target.HaveBuff("Shadow Word: Pain"))
            if (Cast(ShadowWordPain))
                return;

        // Holy Fire
        if (ObjectManager.Target.GetDistance <= _maxRange && HolyFire.KnownSpell 
            && HolyFire.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
            if (Cast(HolyFire))
                return;

        // Smite
        if (ObjectManager.Target.GetDistance <= _maxRange && Smite.KnownSpell 
            && !HolyFire.KnownSpell && Smite.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
            if (Cast(Smite, false))
                return;
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
        WoWUnit _target = ObjectManager.Target;

        // Power Word Shield on multi aggro
        if (!Me.HaveBuff("Power Word: Shield") && !_hasWeakenedSoul && ObjectManager.GetNumberAttackPlayer() > 1)
            if (Cast(PowerWordShield))
                return;

        // Power Word Shield
        if (Me.HealthPercent < 70 && !Me.HaveBuff("Power Word: Shield") && !_hasWeakenedSoul)
            if (Cast(PowerWordShield))
                return;

        // Renew
        if (Me.HealthPercent < 60 && !Me.HaveBuff("Renew") && !_inShadowForm)
            if (Cast(Renew))
                return;

        // Psychic Scream
        if (Me.HealthPercent < 50 && ObjectManager.GetNumberAttackPlayer() > 1)
            if (Cast(PsychicScream))
                return;

        // Flash Heal
        if (Me.HealthPercent < 50)
            if (Cast(FlashHeal))
                return;

        // Heal
        if (Me.HealthPercent < 50)
            if (Cast(Heal))
                return;

        // Lesser Heal
        if (Me.HealthPercent < 50 && !FlashHeal.KnownSpell)
            if (Cast(LesserHeal))
                return;

        // Silence
        if (_shoulBeInterrupted)
            if (Cast(Silence))
                return;

        // Cure Disease
        if (_hasDisease && !_inShadowForm)
            if (Cast(CureDisease))
                return;

        // Dispel Magic self
        if (_hasMagicDebuff && _myManaPC > 10 && DispelMagic.KnownSpell && DispelMagic.IsSpellUsable
            && (_dispelTimer.ElapsedMilliseconds > 10000 || _dispelTimer.ElapsedMilliseconds <= 0))
        {
            if (_usingWand)
                ToolBox.StopWandWaitGCD(UseWand, Smite);
            Lua.RunMacroText("/target player");
            Lua.RunMacroText("/cast Dispel Magic");
            _dispelTimer.Restart();
            ToolBox.WaitGlobalCoolDown(Smite);
            return;
        }

        // Vampiric Touch
        if (_target.GetDistance <= _maxRange && !_target.HaveBuff("Vampiric Touch") 
            && _myManaPC > _innerManaSaveThreshold && _target.HealthPercent > _wandThreshold)
            if (Cast(VampiricTouch))
                return;

        // Vampiric Embrace
        if (!_target.HaveBuff("Vampiric Embrace") && _myManaPC > _innerManaSaveThreshold)
            if (Cast(VampiricEmbrace))
                return;

        // ShadowFiend
        if (ObjectManager.GetNumberAttackPlayer() > 1)
            if (Cast(Shadowfiend))
                return;

        // Shadow Word Pain
        if (_myManaPC > 10 && _target.GetDistance < _maxRange && _target.HealthPercent > 15
            && !_target.HaveBuff("Shadow Word: Pain"))
            if (Cast(ShadowWordPain))
                return;

        // Inner Fire
        if (!Me.HaveBuff("Inner Fire") && _settings.UseInnerFire && InnerFire.KnownSpell
            && _myManaPC > _innerManaSaveThreshold && _target.HealthPercent > _wandThreshold)
            if (Cast(InnerFire))
                return;

        // Shadowguard
        if (!Me.HaveBuff("Shadowguard") && _myManaPC > _innerManaSaveThreshold
            && _settings.UseShadowGuard && _target.HealthPercent > _wandThreshold)
            if (Cast(Shadowguard))
                return;

        // Shadow Protection
        if (!Me.HaveBuff("Shadow Protection") && _myManaPC > 70 && _settings.UseShadowProtection)
            if (Cast(ShadowProtection))
                return;

        // Shadow Word Death
        if (_myManaPC > _innerManaSaveThreshold && _target.GetDistance < _maxRange 
            && _settings.UseShadowWordDeath && _target.HealthPercent < 15)
            if (Cast(ShadowWordDeath))
                return;

        // Mind Blast + Inner Focus
        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && _target.GetDistance < _maxRange
            && _target.HealthPercent > 50 && !Me.HaveBuff("Power Word: Shield") && _mindBlastCD <= 0
            && _target.HealthPercent > _wandThreshold)
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus);

            if (Cast(MindBlast))
                return;
        }

        // Shadow Form Mind Blast + Inner Focus
        if (_inShadowForm && _myManaPC > _innerManaSaveThreshold && _target.GetDistance < _maxRange
            && _mindBlastCD <= 0 && _target.HealthPercent > _wandThreshold)
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus);

            if (Cast(MindBlast))
                return;
        }

        // Mind Flay Range check
        if (_inShadowForm && !MindFlay.IsDistanceGood && Me.HaveBuff("Power Word: Shield"))
        {
            Main.LogDebug("Approaching to be in Mind Flay range");
            _goInMFRange = true;
            return;
        }

        // Mind FLay
        if (Me.HaveBuff("Power Word: Shield") && MindFlay.IsDistanceGood 
            && _myManaPC > _innerManaSaveThreshold && _target.HealthPercent > _wandThreshold)
            if (Cast(MindFlay, false))
                return;

        // Low level Smite
        if (Me.Level < 5 && (_target.HealthPercent > 30 || Me.ManaPercentage > 80) && _myManaPC > _innerManaSaveThreshold 
            && _target.GetDistance < _maxRange)
            if (Cast(Smite, false))
                return;

        // Smite
        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && _target.GetDistance < _maxRange
            && Me.Level >= 5)
            if (Cast(Smite, false))
                return;

        // Use Wand
        if (!_usingWand && _iCanUseWand && _target.GetDistance <= _maxRange + 2)
            if (Cast(UseWand, false))
                return;

        // Go in melee because nothing else to do
        if (!_usingWand && !_iCanUseWand && Main.settingRange != _meleeRange)
        {
            Main.Log("Going in melee");
            Main.settingRange = _meleeRange;
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

    private static bool Cast(Spell s, bool castEvenIfWanding = true)
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

        if (_usingWand && !castEvenIfWanding)
        {
            Main.LogDebug("Didn't cast because we were backing up or wanding");
            return false;
        }

        if (_spellCD >= 2f)
        {
            Main.LogDebug("Didn't cast because cd is too long");
            return false;
        }

        if (_usingWand && castEvenIfWanding)
            ToolBox.StopWandWaitGCD(UseWand, Smite);

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
