using System;
using System.ComponentModel;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class ZEBMHunter : ICustomClass
{
    private HunterFoodManager _foodManager = new HunterFoodManager();
    private bool _isLaunched;
    public bool _autoshotRepeating;
    public bool RangeCheck;
    private bool _isBackingUp = false;
    private int _backupAttempts = 0;
    int _steadyShotSleep = 0;
    private bool _canOnlyMelee = false;

    public float Range
	{
		get
		{
			float result = _canOnlyMelee ? 4.5f : 33f;
            return result;
		}
    }

    public void Initialize()
    {
        _isLaunched = true;
        Log("Initialized");
        ZEBMHunterSettings.Load();
        if (ZEBMHunterSettings.CurrentSetting.RangedWeaponSpeed > 2000)
        {
            _steadyShotSleep = ZEBMHunterSettings.CurrentSetting.RangedWeaponSpeed - 1600;
        }
        else
        {
            _steadyShotSleep = 500;
        }
        Log("Steady Shot delay set to : " + _steadyShotSleep.ToString() + "ms");

        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (ObjectManager.Target.GetDistance >= 13f && !AutoShot.IsSpellUsable && !_isBackingUp)
            {
                _canOnlyMelee = true;
            }
            else
            {
                _canOnlyMelee = false;
            }
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
            if (ObjectManager.Target.GetDistance < 13f && ObjectManager.Target.IsTargetingMyPet && _backupAttempts < ZEBMHunterSettings.CurrentSetting.MaxBackupAttempts
            && !MovementManager.InMovement && ObjectManager.Me.IsAlive && !ObjectManager.Pet.HaveBuff("Pacifying Dust") && !_canOnlyMelee
            && !ObjectManager.Pet.IsStunned && !_isBackingUp && !ObjectManager.Me.IsCast && ZEBMHunterSettings.CurrentSetting.BackupFromMelee)
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
                    Log("Re-enabling auto shot");
                    AutoShot.Launch();
                }
                Log("Backup attempt : " + _backupAttempts);
                if (_backupAttempts >= ZEBMHunterSettings.CurrentSetting.MaxBackupAttempts)
                {
                    _canOnlyMelee = true;
                }
            }
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
                    PetManager();
					if (Lua.LuaDoString<int>("happiness, damagePercentage, loyaltyRate = GetPetHappiness() return happiness", "") < 3 
                        && !Fight.InFight && ZEBMHunterSettings.CurrentSetting.FeedPet)
						Feed();

					if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable 
                        && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
						Lua.LuaDoString("PetAttack();", false);

                    if (!ObjectManager.Me.IsMounted && !Fight.InFight && !ObjectManager.Me.HaveBuff("Aspect of the Cheetah") 
                        && MovementManager.InMoveTo && AspectCheetah.IsSpellUsable && AspectCheetah.KnownSpell 
                        && ObjectManager.Me.ManaPercentage > 60f)
                        AspectCheetah.Launch();

					if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable)
                    {
                        if (AspectViper.KnownSpell && AspectViper.IsSpellUsable && !ObjectManager.Me.HaveBuff("Aspect of the Viper")
                            && ObjectManager.Me.ManaPercentage < 30)
                            AspectViper.Launch();

                        if (AspectHawk.KnownSpell && AspectHawk.IsSpellUsable && !ObjectManager.Me.HaveBuff("Aspect of the Hawk")
                            && (ObjectManager.Me.ManaPercentage > 90 || ObjectManager.Me.HaveBuff("Aspect of the Cheetah")))
							AspectHawk.Launch();

						if (AspectMonkey.KnownSpell && AspectMonkey.IsSpellUsable && !ObjectManager.Me.HaveBuff("Aspect of the Monkey") 
                            && !AspectHawk.KnownSpell)
							AspectMonkey.Launch();
                        
						CombatRotation();
					}
				}
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(10);
		}
        Log("Stopped.");
	}
    
	internal void PetManager()
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
    
	public void Feed()
	{
		if (ObjectManager.Pet.IsAlive && !ObjectManager.Me.IsCast && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
		{
            _foodManager.FeedPet();
			Thread.Sleep(400);
		}
	}
    
	internal void CombatRotation()
    {
        if (BestialWrath.KnownSpell && BestialWrath.IsSpellUsable && ObjectManager.Target.GetDistance < 34f
            && ObjectManager.Target.HealthPercent >= 60.0 && ObjectManager.Me.ManaPercentage > 10u && BestialWrath.IsSpellUsable
            && ((ZEBMHunterSettings.CurrentSetting.BestialWrathOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1) || !ZEBMHunterSettings.CurrentSetting.BestialWrathOnMulti))
            BestialWrath.Launch();

        if (RapidFire.KnownSpell && RapidFire.IsSpellUsable && ObjectManager.Target.GetDistance < 34f
            && ObjectManager.Target.HealthPercent >= 80.0
            && ((ZEBMHunterSettings.CurrentSetting.RapidFireOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1) || !ZEBMHunterSettings.CurrentSetting.RapidFireOnMulti))
            RapidFire.Launch();

        if (KillCommand.KnownSpell && KillCommand.IsSpellUsable)
            KillCommand.Launch();
        
        if (RaptorStrike.KnownSpell && RaptorStrike.IsSpellUsable && ObjectManager.Target.GetDistance < 6u && !RaptorStrikeOn())
            RaptorStrike.Launch();
        
        if (MongooseBite.KnownSpell && MongooseBite.IsSpellUsable && ObjectManager.Target.GetDistance < 6u)
            MongooseBite.Launch();
        
        if (FeignDeath.KnownSpell && FeignDeath.IsSpellUsable && ObjectManager.Me.HealthPercent < 20u)
        {
            FeignDeath.Launch();
            Fight.StopFight();
        }

        if (FreezingTrap.KnownSpell && FreezingTrap.IsSpellUsable && ObjectManager.Pet.HaveBuff("Mend Pet")
            && ObjectManager.GetUnitAttackPlayer().Count > 1 && ZEBMHunterSettings.CurrentSetting.UseFreezingTrap)
            FreezingTrap.Launch();
        
        if (ObjectManager.Pet.IsValid && MendPet.KnownSpell && MendPet.IsSpellUsable && ObjectManager.Pet.HealthPercent <= 30.0 
            && !ObjectManager.Pet.HaveBuff("Mend Pet"))
			MendPet.Launch();
		
		if (HuntersMark.KnownSpell && HuntersMark.IsSpellUsable && ObjectManager.Pet.IsValid && !HuntersMark.TargetHaveBuff 
            && ObjectManager.Target.GetDistance > 13f && ObjectManager.Target.IsAlive)
			HuntersMark.Launch();
        
        if (SteadyShot.KnownSpell && SteadyShot.IsSpellUsable && ObjectManager.Me.ManaPercentage > 30 && SteadyShot.IsDistanceGood && !_isBackingUp)
        {
            SteadyShot.Launch();
            Thread.Sleep(_steadyShotSleep);
        }

        if (SerpentSting.KnownSpell && SerpentSting.IsSpellUsable && !ObjectManager.Target.HaveBuff("Serpent Sting") 
            && ObjectManager.Target.GetDistance < 34f && Canpoison(ObjectManager.Me.TargetObject) 
            && ObjectManager.Target.HealthPercent >= 80.0 && ObjectManager.Me.ManaPercentage > 50u && !SteadyShot.KnownSpell
            && ObjectManager.Target.GetDistance > 13f)
			SerpentSting.Launch();
		
		if (Intimidation.KnownSpell && Intimidation.IsSpellUsable && ObjectManager.Target.GetDistance < 34f 
            && ObjectManager.Target.GetDistance > 10f && ObjectManager.Target.HealthPercent >= 20.0 && ObjectManager.Me.ManaPercentage > 10u 
            && Intimidation.IsSpellUsable)
			Intimidation.Launch();
		
		if (ArcaneShot.KnownSpell && ArcaneShot.IsSpellUsable && ObjectManager.Target.GetDistance < 34f 
            && ObjectManager.Target.HealthPercent >= 30.0 && ObjectManager.Me.ManaPercentage > 80u
            && !SteadyShot.KnownSpell)
			ArcaneShot.Launch();
    }

    private bool Canpoison(WoWUnit unit)
    {
        return unit.CreatureTypeTarget != "Elemental" && unit.CreatureTypeTarget != "Mechanical";
    }

    private bool RaptorStrikeOn()
    {
        return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Raptor Strike') then isAutoRepeat = true end", "isAutoRepeat");
    }

    public void ShowConfiguration()
    {
        ZEBMHunterSettings.Load();
        ZEBMHunterSettings.CurrentSetting.ToForm();
        ZEBMHunterSettings.CurrentSetting.Save();
    }

    private Spell RevivePet = new Spell("Revive Pet");
	private Spell CallPet = new Spell("Call Pet");
	public Spell MendPet = new Spell("Mend Pet");
	public Spell AspectHawk = new Spell("Aspect of the Hawk");
	public Spell AspectCheetah = new Spell("Aspect of the Cheetah");
	public Spell AspectMonkey = new Spell("Aspect of the Monkey");
    public Spell AspectViper = new Spell("Aspect of the Viper");
    public Spell HuntersMark = new Spell("Hunter's Mark");
	public Spell ConcussiveShot = new Spell("Concussive Shot");
	public Spell RaptorStrike = new Spell("Raptor Strike");
    public Spell MongooseBite = new Spell("Mongoose Bite");
    public Spell WingClip = new Spell("Wing Clip");
	public Spell SerpentSting = new Spell("Serpent Sting");
	public Spell ArcaneShot = new Spell("Arcane Shot");
	public Spell AutoShot = new Spell("Auto Shot");
	public Spell RapidFire = new Spell("Rapid Fire");
	public Spell Intimidation = new Spell("Intimidation");
	public Spell BestialWrath = new Spell("Bestial Wrath");
    public Spell FeignDeath = new Spell("Feign Death");
    public Spell FreezingTrap = new Spell("Freezing Trap");
    public Spell SteadyShot = new Spell("Steady Shot");
    public Spell KillCommand = new Spell("Kill Command");

    public void Log(string s)
    {
        Logging.WriteDebug("[Z.E.Hunter] " + s);
    }
}
