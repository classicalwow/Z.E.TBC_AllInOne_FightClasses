using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class ZERetPaladin : ICustomClass
{
    private bool _isLaunched;
    private int _manaSavePercent;

    public float Range
	{
		get
		{
            return 4.5f;
		}
    }

    public void Initialize()
    {
        _isLaunched = true;
        Log("Initialized");
        ZEPaladinSettings.Load();
        _manaSavePercent = ZEPaladinSettings.CurrentSetting.ManaSaveLimitPercent;
        if (_manaSavePercent < 20)
            _manaSavePercent = 20;
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
                    BuffRotation();

                    if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable)
                    {
                        CombatRotation();
                    }
				}
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(20);
		}
        Log("Stopped.");
    }

    internal void BuffRotation()
    {
        if (ObjectManager.Me.HealthPercent < 50 && !Fight.InFight
            && !ObjectManager.Me.IsMounted)
            Cast(HolyLight);

        if (ObjectManager.Me.HealthPercent < 75 && !Fight.InFight && ZEPaladinSettings.CurrentSetting.FlashHealBetweenFights
            && !ObjectManager.Me.IsMounted)
            Cast(FlashOfLight);

        if (ObjectManager.Me.IsMounted && CrusaderAura.KnownSpell && !ObjectManager.Me.HaveBuff("Crusader Aura") && !Fight.InFight)
            Cast(CrusaderAura);

        if (ZEPaladinSettings.CurrentSetting.UseBlessingOfWisdom && !ObjectManager.Me.HaveBuff("Blessing of Wisdom")
            && !ObjectManager.Me.IsMounted)
            Cast(BlessingOfWisdom);

        if (!ZEPaladinSettings.CurrentSetting.UseBlessingOfWisdom && !ObjectManager.Me.HaveBuff("Blessing of Might")
            && !ObjectManager.Me.IsMounted)
            Cast(BlessingOfMight);
    }


    internal void CombatRotation()
    {
        CheckAutoAttack();
        string _debuff = GetDebuffType();

        if (_debuff == "Poison" || _debuff == "Disease")
            Cast(Purify);

        if (_debuff == "Magic")
            Cast(Cleanse);

        if ((ObjectManager.GetNumberAttackPlayer() > 1 && !ObjectManager.Me.HaveBuff("Devotion Aura")) || (!ObjectManager.Me.HaveBuff("Devotion Aura") && !SanctityAura.KnownSpell))
            Cast(DevotionAura);

        if (!ObjectManager.Me.HaveBuff("Sanctity Aura") && SanctityAura.KnownSpell && ObjectManager.GetNumberAttackPlayer() <= 1)
            Cast(SanctityAura);

        if (ObjectManager.Me.HealthPercent < 10)
            Cast(LayOnHands);

        if (ObjectManager.Me.ManaPercentage > _manaSavePercent && ObjectManager.GetNumberAttackPlayer() > 1)
            Cast(AvengingWrath);

        if (ObjectManager.Me.HealthPercent < 50 && ObjectManager.Me.ManaPercentage > _manaSavePercent)
            Cast(HammerOfJustice);
        
        if (ObjectManager.Target.CreatureTypeTarget == "Undead" || ObjectManager.Target.CreatureTypeTarget == "Demon"
            && ZEPaladinSettings.CurrentSetting.UseExorcism)
            Cast(Exorcism);
            
        if (ObjectManager.Me.HaveBuff("Seal of the Crusader") && ObjectManager.Target.GetDistance < 10)
        {
            Cast(Judgement);
            Thread.Sleep(200);
        }

        if ((ObjectManager.Me.HaveBuff("Seal of Righteousness") || ObjectManager.Me.HaveBuff("Seal of Command")) 
            && ObjectManager.Target.GetDistance < 10  
            && (ObjectManager.Me.ManaPercentage >= _manaSavePercent || ObjectManager.Me.HaveBuff("Seal of the Crusader")))
            Cast(Judgement);

        if (!ObjectManager.Target.HaveBuff("Judgement of the Crusader") && !ObjectManager.Me.HaveBuff("Seal of the Crusader")
            && ObjectManager.Me.ManaPercentage > _manaSavePercent - 20 && ObjectManager.Target.IsAlive)
            Cast(SealOfTheCrusader);

        if (!ObjectManager.Me.HaveBuff("Seal of Righteousness") && !ObjectManager.Me.HaveBuff("Seal of the Crusader") && ObjectManager.Target.IsAlive &&
            (ObjectManager.Target.HaveBuff("Judgement of the Crusader") || ObjectManager.Me.ManaPercentage < _manaSavePercent)
            && (!ZEPaladinSettings.CurrentSetting.UseSealOfCommand || !SealOfCommand.KnownSpell))
            Cast(SealOfRighteousness);

        if (!ObjectManager.Me.HaveBuff("Seal of Command") && !ObjectManager.Me.HaveBuff("Seal of the Crusader") && ObjectManager.Target.IsAlive &&
            (ObjectManager.Target.HaveBuff("Judgement of the Crusader") || ObjectManager.Me.ManaPercentage < _manaSavePercent)
            && ZEPaladinSettings.CurrentSetting.UseSealOfCommand && SealOfCommand.KnownSpell)
            Cast(SealOfCommand);

        if (!ObjectManager.Me.HaveBuff("Seal of Righteousness") && !ObjectManager.Me.HaveBuff("Seal of the Crusader") &&
            !ObjectManager.Me.HaveBuff("Seal of Command") && !SealOfCommand.IsSpellUsable && !SealOfRighteousness.IsSpellUsable
            && SealOfCommand.KnownSpell && ObjectManager.Me.Mana >= 55)
            Lua.RunMacroText("/cast Seal of Command(Rank 1)");

        if (ObjectManager.Me.HealthPercent < 50)
        {
            if (!HolyLight.IsSpellUsable)
            {
                if (ObjectManager.Me.HealthPercent < 20)
                    Cast(DivineShield);
                Cast(FlashOfLight);
            }
            Cast(HolyLight);
        }

        if (ObjectManager.Me.ManaPercentage > 10)
            Cast(CrusaderStrike);

        if (ZEPaladinSettings.CurrentSetting.UseHammerOfWrath)
            Cast(HammerOfWrath);
    }

    public void ShowConfiguration()
    {
        ZEPaladinSettings.Load();
        ZEPaladinSettings.CurrentSetting.ToForm();
        ZEPaladinSettings.CurrentSetting.Save();
    }

    private Spell SealOfRighteousness = new Spell("Seal of Righteousness");
    private Spell SealOfTheCrusader = new Spell("Seal of the Crusader");
    private Spell SealOfCommand = new Spell("Seal of Command");
    private Spell HolyLight = new Spell("Holy Light");
    private Spell DevotionAura = new Spell("Devotion Aura");
    private Spell BlessingOfMight = new Spell("Blessing of Might");
    private Spell Judgement = new Spell("Judgement");
    private Spell LayOnHands = new Spell("Lay on Hands");
    private Spell HammerOfJustice = new Spell("Hammer of Justice");
    private Spell RetributionAura = new Spell("Retribution Aura");
    private Spell Exorcism = new Spell("Exorcism");
    private Spell ConcentrationAura = new Spell("Concentration Aura");
    private Spell SanctityAura = new Spell("Sanctity Aura");
    private Spell FlashOfLight = new Spell("Flash of Light");
    private Spell BlessingOfWisdom = new Spell("Blessing of Wisdom");
    private Spell DivineShield = new Spell("Divine Shield");
    private Spell Cleanse = new Spell("Cleanse");
    private Spell Purify = new Spell("Purify");
    private Spell CrusaderStrike = new Spell("Crusader Strike");
    private Spell HammerOfWrath = new Spell("Hammer of Wrath");
    private Spell Attack = new Spell("Attack");
    private Spell CrusaderAura = new Spell("Crusader Aura");
    private Spell AvengingWrath = new Spell("Avenging Wrath");

    private void Cast(Spell s)
    {
        if (s.IsSpellUsable && s.KnownSpell)
            s.Launch();
    }

    private void CheckAutoAttack()
    {
        bool _autoAttacking = Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Attack') then isAutoRepeat = true end", "isAutoRepeat");
        if (!_autoAttacking)
        {
            Log("Re-activating attack");
            Attack.Launch();
        }
    }

    private string GetDebuffType()
    {
        string debuffType = Lua.LuaDoString<string>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if (d == 'Poison' or d == 'Magic' or d == 'Curse' or d == 'Disease') then
                return d
                end
            end");
        return debuffType;
    }

    public void Log(string s)
    {
        Logging.WriteDebug("[Z.E.Paladin] " + s);
    }
}
