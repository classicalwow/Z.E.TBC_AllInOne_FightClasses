using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class ZEShadowPriest : ICustomClass
{
    private WoWLocalPlayer Me = ObjectManager.Me;
    private bool _isLaunched;
    private float _maxRange = 28f;
    private bool _usingWand = false;
    private int _innerManaSaveThreshold = 20;
    private int _wandThreshold;
    private bool _goInMFRange = false;

    public float Range
	{
		get
        {
            float result = _goInMFRange ? 17f : _maxRange - 2;
            return result;
        }
    }

    public void Initialize()
    {
        _isLaunched = true;
        Log("Initialized");
        ZEPriestSettings.Load();
        _wandThreshold = ZEPriestSettings.CurrentSetting.WandThreshold > 100 ? 50 : ZEPriestSettings.CurrentSetting.WandThreshold;
        
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _usingWand = false;
            _goInMFRange = false;
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
        Log("Stopped.");
    }

    internal void BuffRotation()
    {
        if (!Me.IsMounted)
        {
            if (HasDisease() && CureDisease.KnownSpell && CureDisease.IsSpellUsable)
                Cast(CureDisease, true, true);

            if (Me.HealthPercent < 60 && !Me.HaveBuff("Renew") && Renew.KnownSpell && Renew.IsSpellUsable)
                Cast(Renew, true, true);

            if (Me.HealthPercent < 50 && !Me.HaveBuff("Power Word: Shield") && !HaskWeakenedSoul()
                && ObjectManager.GetNumberAttackPlayer() > 0 && PowerWordShield.KnownSpell && PowerWordShield.IsSpellUsable)
                Cast(PowerWordShield, true, true);

            if (Me.HealthPercent < 30 && ObjectManager.GetNumberAttackPlayer() > 1 && PsychicScream.KnownSpell 
                && PsychicScream.IsSpellUsable)
                Cast(PsychicScream, true, true);

            if (!Me.HaveBuff("Power Word: Fortitude") && PowerWordFortitude.KnownSpell && PowerWordFortitude.IsSpellUsable)
                Cast(PowerWordFortitude, true, true);

            if (!Me.HaveBuff("Inner Fire") && ZEPriestSettings.CurrentSetting.UseInnerFire && InnerFire.KnownSpell)
                Cast(InnerFire, true, true);

            if (!Me.HaveBuff("Shadowguard") && Shadowguard.KnownSpell && Shadowguard.IsSpellUsable && ZEPriestSettings.CurrentSetting.UseShadowGuard)
                Cast(Shadowguard, true, true);

            if (!Me.HaveBuff("Shadow Protection") && ShadowProtection.KnownSpell && ZEPriestSettings.CurrentSetting.UseShadowProtection)
                Cast(ShadowProtection, true, true);

            if (!Me.HaveBuff("ShadowForm") && Shadowform.KnownSpell && Shadowform.IsSpellUsable && ObjectManager.GetNumberAttackPlayer() < 1)
                Cast(Shadowform, true, true);
        }
    }

    internal void Pull()
    {
        if (!HaskWeakenedSoul() && ZEPriestSettings.CurrentSetting.UseShieldOnPull && PowerWordShield.KnownSpell && PowerWordShield.IsSpellUsable
            && !Me.HaveBuff("Power Word: Shield"))
            Cast(PowerWordShield, true, true);

        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange && VampiricTouch.KnownSpell && VampiricTouch.IsSpellUsable)
        {
            Cast(VampiricTouch, true, false);
            return;
        }

        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange && MindBlast.KnownSpell && MindBlast.IsSpellUsable
            && !VampiricTouch.KnownSpell)
        {
            Cast(MindBlast, true, false);
            return;
        }

        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange && (!MindBlast.KnownSpell || !MindBlast.IsSpellUsable)
            && ShadowWordPain.KnownSpell && ShadowWordPain.IsSpellUsable)
        {
            Cast(ShadowWordPain, true, true);
            return;
        }

        if (ObjectManager.Target.GetDistance <= _maxRange && HolyFire.KnownSpell && HolyFire.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
        {
            Cast(HolyFire, true, false);
            return;
        }

        if (ObjectManager.Target.GetDistance <= _maxRange && Smite.KnownSpell && !HolyFire.KnownSpell && Smite.IsSpellUsable 
            && !Me.HaveBuff("ShadowForm"))
        {
            Cast(Smite, false, false);
            return;
        }
    }

    internal void CombatRotation()
    {
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
        bool _hasMagicDebuff = HasMagicDebuff();
        bool _hasDisease = HasDisease();
        bool _hasWeakenedSoul = HaskWeakenedSoul();
        double _myManaPC = Me.ManaPercentage;
        bool _inShadowForm = Me.HaveBuff("ShadowForm");
        int _mindBlastCD = Lua.LuaDoString<int>("local start, duration, enabled = GetSpellCooldown(\"Mind Blast\"); return start + duration - GetTime();");
        int _innerFocusCD = Lua.LuaDoString<int>("local start, duration, enabled = GetSpellCooldown(\"Inner Focus\"); return start + duration - GetTime();");

        if (Me.HealthPercent < 70 && !Me.HaveBuff("Power Word: Shield") && !_hasWeakenedSoul && PowerWordShield.KnownSpell)
        {
            if (Cast(PowerWordShield, true, true))
                return;
        }

        if (Me.HealthPercent < 60 && !Me.HaveBuff("Renew") && Renew.KnownSpell && !_inShadowForm)
        {
            if (Cast(Renew, true, true))
                return;
        }

        if (Me.HealthPercent < 50 && ObjectManager.GetNumberAttackPlayer() > 1 && PsychicScream.KnownSpell)
        {
            if (Cast(PsychicScream, true, true))
                return;
        }

        if (Me.HealthPercent < 50 && FlashHeal.KnownSpell)
        {
            if (Cast(FlashHeal, true, false))
                return;
        }

        if (Me.HealthPercent < 50 && Heal.KnownSpell)
        {
            if (Cast(Heal, true, false))
                return;
        }

        if (Me.HealthPercent < 50 && LesserHeal.KnownSpell && !FlashHeal.KnownSpell)
        {
            if (Cast(LesserHeal, true, false))
                return;
        }
        
        if (_hasDisease && CureDisease.KnownSpell && !_inShadowForm)
        {
            if (Cast(CureDisease, true, true))
                return;
        }

        if (_hasMagicDebuff && DispelMagic.KnownSpell && _myManaPC > 10)
        {
            if (_usingWand)
                StopWandWaitGCD();
            Lua.RunMacroText("/target player");
            Lua.RunMacroText("/cast Dispel Magic");
            WaitGlobalCoolDown();
            return;
        }

        if (ObjectManager.Target.GetDistance <= _maxRange && VampiricTouch.KnownSpell && VampiricTouch.IsSpellUsable
            && !ObjectManager.Target.HaveBuff("Vampiric Touch") && _myManaPC > _innerManaSaveThreshold 
            && ObjectManager.Target.HealthPercent > _wandThreshold)
        {
            Cast(VampiricTouch, true, false);
            return;
        }

        if (!ObjectManager.Target.HaveBuff("Vampiric Embrace") && VampiricEmbrace.KnownSpell && _myManaPC > _innerManaSaveThreshold)
        {
            if (Cast(VampiricEmbrace, true, true))
                return;
        }

        if (Shadowfiend.KnownSpell && ObjectManager.GetNumberAttackPlayer() > 1)
        {
            if (Cast(Shadowfiend, true, true))
                return;
        }

        if (_myManaPC > 10 && ObjectManager.Target.GetDistance < _maxRange && ObjectManager.Target.HealthPercent > 15
            && !ObjectManager.Target.HaveBuff("Shadow Word: Pain") && ShadowWordPain.KnownSpell)
        {
            if (Cast(ShadowWordPain, true, true))
                return;
        }

        if (!Me.HaveBuff("Inner Fire") && ZEPriestSettings.CurrentSetting.UseInnerFire && InnerFire.KnownSpell
            && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.HealthPercent > _wandThreshold)
        {
            if (Cast(InnerFire, true, true))
                return;
        }

        if (!Me.HaveBuff("Shadowguard") && Shadowguard.KnownSpell && _myManaPC > _innerManaSaveThreshold
            && ZEPriestSettings.CurrentSetting.UseShadowGuard && ObjectManager.Target.HealthPercent > _wandThreshold)
        {
            Cast(Shadowguard, true, true);
                return;
        }

        if (!Me.HaveBuff("Shadow Protection") && ShadowProtection.KnownSpell && _myManaPC > 70
            && ZEPriestSettings.CurrentSetting.UseShadowProtection)
        {
            Cast(ShadowProtection, true, true);
                return;
        }

        if ((ObjectManager.Target.HealthPercent < _wandThreshold || _myManaPC <= _innerManaSaveThreshold) && 
            !_usingWand && UseWand.IsSpellUsable)
        {
            UseWand.Launch();
            return;
        }

        if (_myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange && ZEPriestSettings.CurrentSetting.UseShadowWordDeath
            && ShadowWordDeath.KnownSpell && ObjectManager.Target.HealthPercent < 15)
        {
            if (Cast(ShadowWordDeath, true, false))
                return;
        }

        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange
            && MindBlast.KnownSpell && ObjectManager.Target.HealthPercent > 50 && !Me.HaveBuff("Power Word: Shield")
            && _mindBlastCD <= 0)
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus, true, true);

            if (Cast(MindBlast, true, false))
                return;
        }

        if (_inShadowForm && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange
            && MindBlast.KnownSpell && _mindBlastCD <= 0 && ObjectManager.Target.HealthPercent > _wandThreshold)
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus, true, true);

            if (Cast(MindBlast, true, false))
                return;
        }

        if (_inShadowForm && !MindFlay.IsDistanceGood && MindFlay.KnownSpell && Me.HaveBuff("Power Word: Shield"))
        {
            Log("Approaching to be in Mind Flay range");
            _goInMFRange = true;
            return;
        }

        if (Me.HaveBuff("Power Word: Shield") && MindFlay.KnownSpell && MindFlay.IsDistanceGood 
            && _myManaPC > _innerManaSaveThreshold)
        {
            if (Cast(MindFlay, false, false))
                return;
        }

        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && ObjectManager.Target.GetDistance < _maxRange && Smite.KnownSpell)
        {
            if (Cast(Smite, false, false))
                return;
        }

        if (!_usingWand && UseWand.IsSpellUsable)
        {
            UseWand.Launch();
            return;
        }
    }

    public void ShowConfiguration()
    {
        ZEPriestSettings.Load();
        ZEPriestSettings.CurrentSetting.ToForm();
        ZEPriestSettings.CurrentSetting.Save();
    }

    private Spell Smite = new Spell("Smite");
    private Spell LesserHeal = new Spell("Lesser Heal");
    private Spell PowerWordFortitude = new Spell("Power Word: Fortitude");
    private Spell PowerWordShield = new Spell("Power Word: Shield");
    private Spell ShadowWordPain = new Spell("Shadow Word: Pain");
    private Spell ShadowWordDeath = new Spell("Shadow Word: Death");
    private Spell UseWand = new Spell("Shoot");
    private Spell Renew = new Spell("Renew");
    private Spell MindBlast = new Spell("Mind Blast");
    private Spell InnerFire = new Spell("Inner Fire");
    private Spell CureDisease = new Spell("Cure Disease");
    private Spell PsychicScream = new Spell("Psychic Scream");
    private Spell Heal = new Spell("Heal");
    private Spell MindFlay = new Spell("Mind Flay");
    private Spell HolyFire = new Spell("Holy Fire");
    private Spell DispelMagic = new Spell("Dispel Magic");
    private Spell FlashHeal = new Spell("Flash Heal");
    private Spell VampiricEmbrace = new Spell("Vampiric Embrace");
    private Spell Shadowguard = new Spell("Shadowguard");
    private Spell ShadowProtection = new Spell("Shadow Protection");
    private Spell Shadowform = new Spell("Shadowform");
    private Spell VampiricTouch = new Spell("Vampiric Touch");
    private Spell InnerFocus = new Spell("Inner Focus");
    private Spell Shadowfiend = new Spell("Shadowfiend");

    private bool Cast(Spell s, bool castEvenIfWanding, bool waitGCD)
    {
        Log("Into Cast for " + s.Name);

        if (_usingWand && !castEvenIfWanding)
            return false;

        if (_usingWand && castEvenIfWanding)
            StopWandWaitGCD();

        if (!s.IsSpellUsable)
            return false;
        
        s.Launch();

        if (waitGCD)
            WaitGlobalCoolDown();
        return true;
    }

    private bool HaskWeakenedSoul()
    {
        bool weakenedSoul = Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local n, _, _, _, _  = UnitDebuff('player',i);
	            if n == 'Weakened Soul' then
                return true
                end
            end");
        return weakenedSoul;
    }

    private bool HasDisease()
    {
        bool hasDisease = Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Disease' then
                return true
                end
            end");
        return hasDisease;
    }

    private bool HasMagicDebuff()
    {
        bool hasMagicDebuff = Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Magic' then
                return true
                end
            end");
        return hasMagicDebuff;
    }

    private void StopWandWaitGCD()
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
            Log("Waited for GCD : " + c);
            if (c >= 1500)
                UseWand.Launch();
        }
    }

    private void WaitGlobalCoolDown()
    {
        int c = 0;
        while (!Smite.IsSpellUsable)
        {
            c += 50;
            Thread.Sleep(50);
            if (c >= 2000)
                return;
        }
        Log("Waited for GCD : " + c);
    }

    public void Log(string s)
    {
        Logging.WriteDebug("[Z.E.Priest] " + s);
    }
}
