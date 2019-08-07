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

public class ZEEnhancementShaman : ICustomClass
{
    private static bool _debug = false;

    internal static Stopwatch _ghostWolfTimer = new Stopwatch();
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEShamanSettings _settings;
    private bool _isLaunched;
    private bool _goInMelee = false;
    private bool _fightingACaster = false;
    private float _pullRange = 28f;
    internal static int _lowManaThreshold = 20;
    internal static int _mediumManaThreshold = 50;
    internal static int _highManaThreshold = 80;
    List<string> _casterEnemies = new List<string>();
    TotemManager totemManager = new TotemManager();

    public float Range
	{
		get
        {
            return _goInMelee ? 5f : _pullRange;
        }
    }

    public void Initialize()
    {
        _isLaunched = true;
        Log("Initialized");
        ZEShamanSettings.Load();
        _settings = ZEShamanSettings.CurrentSetting;
        _ghostWolfTimer.Start();

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _goInMelee = false;
            _ghostWolfTimer.Restart();
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
        };

        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            _ghostWolfTimer.Reset();
        };

        robotManager.Events.FiniteStateMachineEvents.OnRunState += (Engine engine, State state, CancelEventArgs cancelable) =>
        {
            if (state.DisplayName == "Regeneration")
                _ghostWolfTimer.Reset();
        };

        robotManager.Events.FiniteStateMachineEvents.OnAfterRunState += (Engine engine, State state) =>
        {
            if (state.DisplayName == "Regeneration")
                _ghostWolfTimer.Restart();
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
                    CheckEnchantMainHand();
                    totemManager.CheckForTotemicCall();

                    // Lesser Healing Wave OOC
                    if (!Fight.InFight && Me.HealthPercent < 65 && LesserHealingWave.KnownSpell)
                    {
                        Cast(LesserHealingWave);
                    }

                    // Ghost Wolf
                    if (Me.ManaPercentage > 50 && !Me.IsIndoors && _ghostWolfTimer.ElapsedMilliseconds > 3000
                        && _settings.UseGhostWolf && !Me.IsMounted && !Fight.InFight && !Me.HaveBuff("Ghost Wolf"))
                    {
                        _ghostWolfTimer.Stop();
                        Cast(GhostWolf);
                    }

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
        if (!Me.IsMounted && !Me.HaveBuff("Ghost Wolf") && !Me.IsCast)
        {
            // OOC Healing Wave
            if (Me.HealthPercent < 65 && !LesserHealingWave.KnownSpell)
            {
                if (Cast(HealingWave))
                    return;
            }

            // Water Shield
            if (!Me.HaveBuff("Water Shield") && !Me.HaveBuff("Lightning Shield")
                && (_settings.UseWaterShield || !_settings.UseLightningShield || Me.ManaPercentage < 20))
            {
                if (Cast(WaterShield))
                    return;
            }
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

        // Water Shield
        if (!Me.HaveBuff("Water Shield") && !Me.HaveBuff("Lightning Shield")
            && (_settings.UseWaterShield || !_settings.UseLightningShield) || Me.ManaPercentage < _lowManaThreshold)
        {
            if (Cast(WaterShield))
                return;
        }

        // Ligntning Shield
        if (Me.ManaPercentage > _lowManaThreshold && !Me.HaveBuff("Lightning Shield") && !Me.HaveBuff("Water Shield") 
            && _settings.UseLightningShield && (!WaterShield.KnownSpell || !_settings.UseWaterShield))
        {
            if (Cast(LightningShield))
                return;
        }

        // Pull with Lightning Bolt
        if (ObjectManager.Target.GetDistance <= _pullRange + 1)
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
        }
    }

    internal void CombatRotation()
    {
        bool _lowMana = Me.ManaPercentage <= _lowManaThreshold;
        bool _mediumMana = Me.ManaPercentage >= _mediumManaThreshold;
        bool _highMana = Me.ManaPercentage >= _highManaThreshold;
        bool _isPoisoned = HasPoisonDebuff();
        bool _hasDisease = HasDiseaseDebuff();
        bool _shouldBeInterrupted = false;

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

        // Shamanistic Rage
        if (!_mediumMana && ((ObjectManager.Target.HealthPercent > 80 && !_settings.ShamanisticRageOnMultiOnly) || ObjectManager.GetNumberAttackPlayer() > 1))
        {
            if (Cast(ShamanisticRage))
                return;
        }

        // Lesser Healing Wave
        if (Me.HealthPercent < 50 && LesserHealingWave.KnownSpell)
        {
            if (Cast(LesserHealingWave))
                return;
        }

        // Healing Wave
        if (Me.HealthPercent < 50 && !LesserHealingWave.KnownSpell)
        {
            if (Cast(HealingWave))
                return;
        }

        // Cure Poison
        if (_isPoisoned && !_lowMana)
        {
            if (Cast(CurePoison))
                return;
        }

        // Cure Disease
        if (_hasDisease && !_lowMana)
        {
            if (Cast(CureDisease))
                return;
        }

        // Ligntning Shield
        if (!_lowMana && !Me.HaveBuff("Lightning Shield") && !Me.HaveBuff("Water Shield") && _settings.UseLightningShield 
            && (!WaterShield.KnownSpell || !_settings.UseWaterShield))
        {
            if (Cast(LightningShield))
                return;
        }

        // Earth Shock Interupt Rank 1
        if (_shouldBeInterrupted && ObjectManager.Target.GetDistance < 19f 
            && (_settings.InterruptWithRankOne || _lowMana))
        {
            _fightingACaster = true;
            if (!_casterEnemies.Contains(ObjectManager.Target.Name))
                _casterEnemies.Add(ObjectManager.Target.Name);
            Lua.RunMacroText("/cast Earth Shock(Rank 1)");
                return;
        }

        // Earth Shock Interupt
        if (_shouldBeInterrupted && ObjectManager.Target.GetDistance < 19f && !_settings.InterruptWithRankOne)
        {
            if (!_casterEnemies.Contains(ObjectManager.Target.Name))
                _casterEnemies.Add(ObjectManager.Target.Name);
            _fightingACaster = true;
            if (Cast(EarthShock))
                return;
        }

        // Water Shield
        if (!Me.HaveBuff("Water Shield") && !Me.HaveBuff("Lightning Shield")
            && (_settings.UseWaterShield || !_settings.UseLightningShield || _lowMana))
        {
            if (Cast(WaterShield))
                return;
        }

        // Flame Shock DPS
        if (!_lowMana && ObjectManager.Target.GetDistance < 19f && !ObjectManager.Target.HaveBuff("Flame Shock") 
            && ObjectManager.Target.HealthPercent > 20 && !_fightingACaster && _settings.UseFlameShock)
        {
            if (Cast(FlameShock))
                return;
        }

        // Totems
        if (!_lowMana && ObjectManager.Target.GetDistance < 20)
        {
            if (totemManager.CastTotems())
                return;
        }

        // Stormstrike
        if (!_lowMana && Stormstrike.IsDistanceGood)
        {
            if (Cast(Stormstrike))
                return;
        }

        // Earth Shock DPS
        if (_highMana && ObjectManager.Target.GetDistance < 19f && !FlameShock.KnownSpell)
        {
            if (Cast(EarthShock))
                return;
        }
    }

    public void ShowConfiguration()
    {
        ZEShamanSettings.Load();
        ZEShamanSettings.CurrentSetting.ToForm();
        ZEShamanSettings.CurrentSetting.Save();
    }

    private Spell LightningBolt = new Spell("Lightning Bolt");
    private Spell HealingWave = new Spell("Healing Wave");
    private Spell LesserHealingWave = new Spell("Lesser Healing Wave");
    private Spell RockbiterWeapon = new Spell("Rockbiter Weapon");
    private Spell EarthShock = new Spell("Earth Shock");
    private Spell FlameShock = new Spell("Flame Shock");
    private Spell LightningShield = new Spell("Lightning Shield");
    private Spell WaterShield = new Spell("Water Shield");
    private Spell GhostWolf = new Spell("Ghost Wolf");
    private Spell CurePoison = new Spell("Cure Poison");
    private Spell CureDisease = new Spell("Cure Disease");
    private Spell WindfuryWeapon = new Spell("Windfury Weapon");
    private Spell Stormstrike = new Spell("Stormstrike");
    private Spell ShamanisticRage = new Spell("Shamanistic Rage");
    private Spell Attack = new Spell("Attack");

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

    private void CheckEnchantMainHand()
    {
        bool hasMainHandEnchant = Lua.LuaDoString<bool>
            (@"local hasMainHandEnchant, _, _, _, _, _, _, _, _ = GetWeaponEnchantInfo()
            if (hasMainHandEnchant) then 
               return '1'
            else
               return '0'
            end");

        bool hasOffHandEnchant = Lua.LuaDoString<bool>
            (@"local _, _, _, _, hasOffHandEnchant, _, _, _, _ = GetWeaponEnchantInfo()
            if (hasOffHandEnchant) then 
               return '1'
            else
               return '0'
            end");

        bool hasoffHandWeapon = Lua.LuaDoString<bool>(@"local hasWeapon = OffhandHasWeapon()
            return hasWeapon");

        if (!hasMainHandEnchant || (hasoffHandWeapon && !hasOffHandEnchant))
        {
            if (!WindfuryWeapon.KnownSpell && RockbiterWeapon.KnownSpell)
                Cast(RockbiterWeapon);

            if (WindfuryWeapon.KnownSpell)
                Cast(WindfuryWeapon);
        }
    }

    private bool HasPoisonDebuff()
    {
        bool hasPoisonDebuff = Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Poison' then
                return true
                end
            end");
        return hasPoisonDebuff;
    }

    private bool HasDiseaseDebuff()
    {
        bool hasDiseaseDebuff = Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Disease' then
                return true
                end
            end");
        return hasDiseaseDebuff;
    }

    internal static void Log(string s)
    {
        Logging.WriteDebug("[Z.E.Shaman] " + s);
    }

    internal static void Debug(string s)
    {
        if (_debug)
            Logging.WriteDebug("[Z.E.Shaman DEBUG] " + s);
    }
}
