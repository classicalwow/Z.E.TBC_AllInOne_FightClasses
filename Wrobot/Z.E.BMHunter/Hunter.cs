using System;
using System.ComponentModel;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class Hunter
{
    private static HunterFoodManager _foodManager = new HunterFoodManager();
    internal static ZEBMHunterSettings _settings;
    private static bool _isLaunched;
    public static bool _autoshotRepeating;
    public static bool RangeCheck;
    private static bool _isBackingUp = false;
    private static int _backupAttempts = 0;
    private static int _steadyShotSleep = 0;
    private static bool _canOnlyMelee = false;

    public static void Initialize()
    {
        _isLaunched = true;
        Main.Log("Initialized");
        ZEBMHunterSettings.Load();
        _settings = ZEBMHunterSettings.CurrentSetting;

        // Set Steady Shot delay
        if (_settings.RangedWeaponSpeed > 2000)
        {
            _steadyShotSleep = _settings.RangedWeaponSpeed - 1600;
        }
        else
        {
            _steadyShotSleep = 500;
        }
        Main.Log("Steady Shot delay set to : " + _steadyShotSleep.ToString() + "ms");

        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (ObjectManager.Target.GetDistance >= 13f && !AutoShot.IsSpellUsable && !_isBackingUp)
                _canOnlyMelee = true;
            else
                _canOnlyMelee = false;
        };

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _isBackingUp = false;
            _backupAttempts = 0;
            _autoshotRepeating = false;
            _canOnlyMelee = false;
        };

        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (ObjectManager.Target.GetDistance < 13f && ObjectManager.Target.IsTargetingMyPet && _backupAttempts < _settings.MaxBackupAttempts
            && !MovementManager.InMovement && ObjectManager.Me.IsAlive && !ObjectManager.Pet.HaveBuff("Pacifying Dust") && !_canOnlyMelee
            && !ObjectManager.Pet.IsStunned && !_isBackingUp && !ObjectManager.Me.IsCast && _settings.BackupFromMelee)
            {
                _isBackingUp = true;
                Move.Backward(Move.MoveAction.DownKey, 700);
                Thread.Sleep(700 + Usefuls.Latency);
                _isBackingUp = false;
                _backupAttempts++;
                _autoshotRepeating = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(75); " +
                       "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
                if (!_autoshotRepeating)
                {
                    Main.LogFight("Re-enabling auto shot");
                    AutoShot.Launch();
                }
                Main.LogFight("Backup attempt : " + _backupAttempts);
                if (_backupAttempts >= _settings.MaxBackupAttempts)
                {
                    _canOnlyMelee = true;
                }
            }
        };

        Rotation();
    }


    public static void Dispose()
    {
        _isLaunched = false;
        Main.Log("Stop in progress.");
    }

    internal static void Rotation()
	{
        Main.Log("Started");
		while (_isLaunched)
		{
			try
			{
				if (!Products.InPause && !ObjectManager.Me.IsDeadMe)
				{
                    Main.settingRange = _canOnlyMelee ? 4.5f : 33f;
                    PetManager();

                    // Feed
					if (Lua.LuaDoString<int>("happiness, damagePercentage, loyaltyRate = GetPetHappiness() return happiness", "") < 3 
                        && !Fight.InFight && _settings.FeedPet)
						Feed();

                    // Pet attack
					if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable 
                        && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
						Lua.LuaDoString("PetAttack();", false);

                    // Aspect of the Cheetah
                    if (!ObjectManager.Me.IsMounted && !Fight.InFight && !ObjectManager.Me.HaveBuff("Aspect of the Cheetah") 
                        && MovementManager.InMoveTo && AspectCheetah.IsSpellUsable && AspectCheetah.KnownSpell 
                        && ObjectManager.Me.ManaPercentage > 60f)
                        AspectCheetah.Launch();

					if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable)
						CombatRotation();
				}
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(10 + Usefuls.Latency);
		}
        Main.Log("Stopped.");
	}

    internal static void CombatRotation()
    {
        // Aspect of the viper
        if (AspectViper.KnownSpell && AspectViper.IsSpellUsable && !ObjectManager.Me.HaveBuff("Aspect of the Viper")
            && ObjectManager.Me.ManaPercentage < 30)
            AspectViper.Launch();

        // Aspect of the Hawk
        if (AspectHawk.KnownSpell && AspectHawk.IsSpellUsable && !ObjectManager.Me.HaveBuff("Aspect of the Hawk")
            && (ObjectManager.Me.ManaPercentage > 90 || ObjectManager.Me.HaveBuff("Aspect of the Cheetah")))
            AspectHawk.Launch();

        // Aspect of the Monkey
        if (AspectMonkey.KnownSpell && AspectMonkey.IsSpellUsable && !ObjectManager.Me.HaveBuff("Aspect of the Monkey")
            && !AspectHawk.KnownSpell)
            AspectMonkey.Launch();

        // Bestial Wrath
        if (BestialWrath.KnownSpell && BestialWrath.IsSpellUsable && ObjectManager.Target.GetDistance < 34f
            && ObjectManager.Target.HealthPercent >= 60.0 && ObjectManager.Me.ManaPercentage > 10u && BestialWrath.IsSpellUsable
            && ((_settings.BestialWrathOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1) || !_settings.BestialWrathOnMulti))
            BestialWrath.Launch();

        // Rapid Fire
        if (RapidFire.KnownSpell && RapidFire.IsSpellUsable && ObjectManager.Target.GetDistance < 34f
            && ObjectManager.Target.HealthPercent >= 80.0
            && ((_settings.RapidFireOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1) || !_settings.RapidFireOnMulti))
            RapidFire.Launch();

        // Kill Command
        if (KillCommand.KnownSpell && KillCommand.IsSpellUsable)
            KillCommand.Launch();
        
        // Raptor Strike
        if (RaptorStrike.KnownSpell && RaptorStrike.IsSpellUsable && ObjectManager.Target.GetDistance < 6u && !RaptorStrikeOn())
            RaptorStrike.Launch();
        
        // Mongoose Bite
        if (MongooseBite.KnownSpell && MongooseBite.IsSpellUsable && ObjectManager.Target.GetDistance < 6u)
            MongooseBite.Launch();
        
        // Feign Death
        if (FeignDeath.KnownSpell && FeignDeath.IsSpellUsable && ObjectManager.Me.HealthPercent < 20u)
        {
            FeignDeath.Launch();
            Fight.StopFight();
        }

        // Freezing Trap
        if (FreezingTrap.KnownSpell && FreezingTrap.IsSpellUsable && ObjectManager.Pet.HaveBuff("Mend Pet")
            && ObjectManager.GetUnitAttackPlayer().Count > 1 && _settings.UseFreezingTrap)
            FreezingTrap.Launch();
        
        // Mend Pet
        if (ObjectManager.Pet.IsValid && MendPet.KnownSpell && MendPet.IsSpellUsable && ObjectManager.Pet.HealthPercent <= 30.0 
            && !ObjectManager.Pet.HaveBuff("Mend Pet"))
			MendPet.Launch();
		
        // Hunter's Mark
		if (HuntersMark.KnownSpell && HuntersMark.IsSpellUsable && ObjectManager.Pet.IsValid && !HuntersMark.TargetHaveBuff 
            && ObjectManager.Target.GetDistance > 13f && ObjectManager.Target.IsAlive)
			HuntersMark.Launch();
        
        // Steady Shot
        if (SteadyShot.KnownSpell && SteadyShot.IsSpellUsable && ObjectManager.Me.ManaPercentage > 30 && SteadyShot.IsDistanceGood && !_isBackingUp)
        {
            SteadyShot.Launch();
            Thread.Sleep(_steadyShotSleep);
        }

        // Serpent Sting
        if (SerpentSting.KnownSpell && SerpentSting.IsSpellUsable && !ObjectManager.Target.HaveBuff("Serpent Sting") 
            && ObjectManager.Target.GetDistance < 34f && Canpoison(ObjectManager.Me.TargetObject) 
            && ObjectManager.Target.HealthPercent >= 80.0 && ObjectManager.Me.ManaPercentage > 50u && !SteadyShot.KnownSpell
            && ObjectManager.Target.GetDistance > 13f)
			SerpentSting.Launch();
		
        // Intimidation
		if (Intimidation.KnownSpell && Intimidation.IsSpellUsable && ObjectManager.Target.GetDistance < 34f 
            && ObjectManager.Target.GetDistance > 10f && ObjectManager.Target.HealthPercent >= 20.0 && ObjectManager.Me.ManaPercentage > 10u 
            && Intimidation.IsSpellUsable)
			Intimidation.Launch();
		
        // Arcane Shot
		if (ArcaneShot.KnownSpell && ArcaneShot.IsSpellUsable && ObjectManager.Target.GetDistance < 34f 
            && ObjectManager.Target.HealthPercent >= 30.0 && ObjectManager.Me.ManaPercentage > 80u
            && !SteadyShot.KnownSpell)
			ArcaneShot.Launch();
    }

    public static void Feed()
    {
        if (ObjectManager.Pet.IsAlive && !ObjectManager.Me.IsCast && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
        {
            _foodManager.FeedPet();
            Thread.Sleep(400);
        }
    }

    internal static void PetManager()
    {
        if (!ObjectManager.Me.IsDeadMe || !ObjectManager.Me.IsMounted)
        {
            if (!ObjectManager.Pet.IsValid && CallPet.KnownSpell && !ObjectManager.Me.IsMounted && CallPet.IsSpellUsable)
            {
                CallPet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
            }

            if (ObjectManager.Pet.IsDead && RevivePet.KnownSpell && !ObjectManager.Me.IsMounted && RevivePet.IsSpellUsable)
            {
                RevivePet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
                Usefuls.WaitIsCasting();
            }

            if (ObjectManager.Pet.IsAlive && ObjectManager.Pet.IsValid && !ObjectManager.Pet.HaveBuff("Mend Pet")
                && ObjectManager.Me.IsAlive && MendPet.KnownSpell && MendPet.IsDistanceGood && ObjectManager.Pet.HealthPercent <= 60.0
                && MendPet.IsSpellUsable)
            {
                MendPet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
            }
        }
    }

    private static bool Canpoison(WoWUnit unit)
    {
        return unit.CreatureTypeTarget != "Elemental" && unit.CreatureTypeTarget != "Mechanical";
    }

    private static bool RaptorStrikeOn()
    {
        return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Raptor Strike') then isAutoRepeat = true end", "isAutoRepeat");
    }

    public static void ShowConfiguration()
    {
        ZEBMHunterSettings.Load();
        ZEBMHunterSettings.CurrentSetting.ToForm();
        ZEBMHunterSettings.CurrentSetting.Save();
    }

    private static Spell RevivePet = new Spell("Revive Pet");
    private static Spell CallPet = new Spell("Call Pet");
    private static Spell MendPet = new Spell("Mend Pet");
    private static Spell AspectHawk = new Spell("Aspect of the Hawk");
    private static Spell AspectCheetah = new Spell("Aspect of the Cheetah");
    private static Spell AspectMonkey = new Spell("Aspect of the Monkey");
    private static Spell AspectViper = new Spell("Aspect of the Viper");
    private static Spell HuntersMark = new Spell("Hunter's Mark");
    private static Spell ConcussiveShot = new Spell("Concussive Shot");
    private static Spell RaptorStrike = new Spell("Raptor Strike");
    private static Spell MongooseBite = new Spell("Mongoose Bite");
    private static Spell WingClip = new Spell("Wing Clip");
    private static Spell SerpentSting = new Spell("Serpent Sting");
    private static Spell ArcaneShot = new Spell("Arcane Shot");
    private static Spell AutoShot = new Spell("Auto Shot");
    private static Spell RapidFire = new Spell("Rapid Fire");
    private static Spell Intimidation = new Spell("Intimidation");
    private static Spell BestialWrath = new Spell("Bestial Wrath");
    private static Spell FeignDeath = new Spell("Feign Death");
    private static Spell FreezingTrap = new Spell("Freezing Trap");
    private static Spell SteadyShot = new Spell("Steady Shot");
    private static Spell KillCommand = new Spell("Kill Command");
}
