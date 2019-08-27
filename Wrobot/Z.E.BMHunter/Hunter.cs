using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class Hunter
{
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static HunterFoodManager _foodManager = new HunterFoodManager();
    private static readonly BackgroundWorker _petPulseThread = new BackgroundWorker();
    internal static ZEBMHunterSettings _settings;
    public static bool _autoshotRepeating;
    public static bool RangeCheck;
    private static bool _isBackingUp = false;
    private static int _backupAttempts = 0;
    private static int _steadyShotSleep = 0;
    private static bool _canOnlyMelee = false;

    public static void Initialize()
    {
        Main.Log("Initialized");
        _petPulseThread.DoWork += PetThread;
        _petPulseThread.RunWorkerAsync();
        ZEBMHunterSettings.Load();
        _settings = ZEBMHunterSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        // Set Steady Shot delay
        if (_settings.RangedWeaponSpeed > 2000)
        {
            _steadyShotSleep = _settings.RangedWeaponSpeed - 1600;
        }
        else
        {
            _steadyShotSleep = 500;
        }
        Main.LogDebug("Steady Shot delay set to : " + _steadyShotSleep.ToString() + "ms");

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

        Radar3D.OnDrawEvent += () =>
        {
            if (ObjectManager.Me.TargetObject != null)
                Radar3D.DrawCircle(ToolBox.BackofVector3(Me.Position, Me, 20f), 1f, Color.Cyan);
        };

        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (ObjectManager.Target.GetDistance < 13f && ObjectManager.Target.IsTargetingMyPet && _backupAttempts < _settings.MaxBackupAttempts
            && !MovementManager.InMovement && Me.IsAlive && !ObjectManager.Pet.HaveBuff("Pacifying Dust") && !_canOnlyMelee
            && !ObjectManager.Pet.IsStunned && !_isBackingUp && !Me.IsCast && _settings.BackupFromMelee)
            {
                _isBackingUp = true;
                var pos = 1;
                if (ObjectManager.Me.IsAlive && ObjectManager.Target.IsAlive && pos == 1)
                {
                    Vector3 position = ToolBox.BackofVector3(Me.Position, Me, 20f);
                    MovementManager.Go(PathFinder.FindPath(position), false);
                    
                    while (MovementManager.InMovement && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Target.GetDistance < 13f && _backupAttempts < _settings.MaxBackupAttempts && !_canOnlyMelee)
                    {
                        // Wait follow path
                        Thread.Sleep(2000);
                        pos = 0;
                        _backupAttempts++;
                    }
                }
                ReenableAutoshot();
                Main.LogDebug("Backup attempt : " + _backupAttempts);
                _isBackingUp = false;
                if (_backupAttempts >= _settings.MaxBackupAttempts)
                    _canOnlyMelee = true;
            }
        };

        Rotation();
    }

    // Pet thread
    private static void PetThread(object sender, DoWorkEventArgs args)
    {
        while (Main._isLaunched)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause && !Me.IsOnTaxi && Me.IsAlive
                    && ObjectManager.Pet.IsValid)
                {
                    // Pet Growl
                    if (ObjectManager.Target.Target == Me.Guid && Me.InCombatFlagOnly && !_settings.AutoGrowl
                        && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
                        ToolBox.PetSpellCast("Growl");
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError(string.Concat(arg), true);
            }
            Thread.Sleep(300);
        }
    }


    public static void Dispose()
    {
        Main.Log("Stop in progress.");
        _petPulseThread.DoWork -= PetThread;
        _petPulseThread.Dispose();
    }

    internal static void Rotation()
	{
        Main.Log("Started");
		while (Main._isLaunched)
		{
			try
			{
				if (!Products.InPause && !Me.IsDeadMe)
				{
                    Main.settingRange = _canOnlyMelee ? 4.5f : 33f;
                    PetManager();

                    // Switch Auto Growl
                    if (ObjectManager.Pet.IsValid)
                    {
                        ToolBox.TogglePetSpellAuto("Growl", _settings.AutoGrowl);
                    }

                    // Feed
                    if (Lua.LuaDoString<int>("happiness, damagePercentage, loyaltyRate = GetPetHappiness() return happiness", "") < 3 
                        && !Fight.InFight && _settings.FeedPet)
						Feed();

                    // Pet attack
					if (Fight.InFight && Me.Target > 0UL && ObjectManager.Target.IsAttackable 
                        && !ObjectManager.Pet.HaveBuff("Feed Pet Effect") && ObjectManager.Pet.Target != Me.Target)
						Lua.LuaDoString("PetAttack();", false);

                    // Aspect of the Cheetah
                    if (!Me.IsMounted && !Fight.InFight && !Me.HaveBuff("Aspect of the Cheetah") 
                        && MovementManager.InMoveTo && AspectCheetah.IsSpellUsable && AspectCheetah.KnownSpell 
                        && Me.ManaPercentage > 60f)
                        AspectCheetah.Launch();

					if (Fight.InFight && Me.Target > 0UL && ObjectManager.Target.IsAttackable)
						CombatRotation();
				}
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(ToolBox.GetLatency() + _settings.ThreadSleepCycle);
		}
        Main.Log("Stopped.");
	}

    internal static void CombatRotation()
    {
        if (ObjectManager.Target.GetDistance > 10f && !_isBackingUp)
            ReenableAutoshot();

        // Aspect of the viper
        if (AspectViper.KnownSpell && AspectViper.IsSpellUsable && !Me.HaveBuff("Aspect of the Viper")
            && Me.ManaPercentage < 30)
            AspectViper.Launch();

        // Aspect of the Hawk
        if (AspectHawk.KnownSpell && AspectHawk.IsSpellUsable && !Me.HaveBuff("Aspect of the Hawk")
            && (Me.ManaPercentage > 90 || Me.HaveBuff("Aspect of the Cheetah")))
            AspectHawk.Launch();

        // Aspect of the Monkey
        if (AspectMonkey.KnownSpell && AspectMonkey.IsSpellUsable && !Me.HaveBuff("Aspect of the Monkey")
            && !AspectHawk.KnownSpell)
            AspectMonkey.Launch();

        // Bestial Wrath
        if (BestialWrath.KnownSpell && BestialWrath.IsSpellUsable && ObjectManager.Target.GetDistance < 34f
            && ObjectManager.Target.HealthPercent >= 60 && Me.ManaPercentage > 10 && BestialWrath.IsSpellUsable
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
        if (RaptorStrike.KnownSpell && RaptorStrike.IsSpellUsable && ObjectManager.Target.GetDistance < 6f && !RaptorStrikeOn())
            RaptorStrike.Launch();
        
        // Mongoose Bite
        if (MongooseBite.KnownSpell && MongooseBite.IsSpellUsable && ObjectManager.Target.GetDistance < 6f)
            MongooseBite.Launch();
        
        // Feign Death
        if (FeignDeath.KnownSpell && FeignDeath.IsSpellUsable && Me.HealthPercent < 20)
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
        if (SteadyShot.KnownSpell && SteadyShot.IsSpellUsable && Me.ManaPercentage > 30 && SteadyShot.IsDistanceGood && !_isBackingUp)
        {
            SteadyShot.Launch();
            Thread.Sleep(_steadyShotSleep);
        }

        // Serpent Sting
        if (SerpentSting.KnownSpell && SerpentSting.IsSpellUsable && !ObjectManager.Target.HaveBuff("Serpent Sting") 
            && ObjectManager.Target.GetDistance < 34f && ToolBox.CanBleed(Me.TargetObject) 
            && ObjectManager.Target.HealthPercent >= 80 && Me.ManaPercentage > 50u && !SteadyShot.KnownSpell
            && ObjectManager.Target.GetDistance > 13f)
			SerpentSting.Launch();
		
        // Intimidation
		if (Intimidation.KnownSpell && Intimidation.IsSpellUsable && ObjectManager.Target.GetDistance < 34f 
            && ObjectManager.Target.GetDistance > 10f && ObjectManager.Target.HealthPercent >= 20 && Me.ManaPercentage > 10
            && Intimidation.IsSpellUsable)
			Intimidation.Launch();
		
        // Arcane Shot
		if (ArcaneShot.KnownSpell && ArcaneShot.IsSpellUsable && ObjectManager.Target.GetDistance < 34f 
            && ObjectManager.Target.HealthPercent >= 30 && Me.ManaPercentage > 80
            && !SteadyShot.KnownSpell)
			ArcaneShot.Launch();
    }

    public static void Feed()
    {
        if (ObjectManager.Pet.IsAlive && !Me.IsCast && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
        {
            _foodManager.FeedPet();
            Thread.Sleep(400);
        }
    }

    internal static void PetManager()
    {
        if (!Me.IsDeadMe || !Me.IsMounted)
        {
            // Call Pet
            if (!ObjectManager.Pet.IsValid && CallPet.KnownSpell && !Me.IsMounted && CallPet.IsSpellUsable)
            {
                CallPet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
            }

            // Revive Pet
            if (ObjectManager.Pet.IsDead && RevivePet.KnownSpell && !Me.IsMounted && RevivePet.IsSpellUsable)
            {
                RevivePet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
                Usefuls.WaitIsCasting();
            }

            // Mend Pet
            if (ObjectManager.Pet.IsAlive && ObjectManager.Pet.IsValid && !ObjectManager.Pet.HaveBuff("Mend Pet")
                && Me.IsAlive && MendPet.KnownSpell && MendPet.IsDistanceGood && ObjectManager.Pet.HealthPercent <= 60
                && MendPet.IsSpellUsable)
            {
                MendPet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
            }
        }
    }

    private static bool RaptorStrikeOn()
    {
        return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Raptor Strike') then isAutoRepeat = true end", "isAutoRepeat");
    }

    private static void ReenableAutoshot()
    {
        _autoshotRepeating = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(75); " +
               "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
        if (!_autoshotRepeating)
        {
            Main.LogDebug("Re-enabling auto shot");
            AutoShot.Launch();
        }
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
