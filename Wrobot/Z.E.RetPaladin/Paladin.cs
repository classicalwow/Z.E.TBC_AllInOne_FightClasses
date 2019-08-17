using System;
using System.Diagnostics;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class Paladin
{
    private static int _manaSavePercent;
    private static Stopwatch _purifyTimer = new Stopwatch();
    private static Stopwatch _cleanseTimer = new Stopwatch();

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEPaladinSettings.Load();
        _manaSavePercent = ZEPaladinSettings.CurrentSetting.ManaSaveLimitPercent;
        if (_manaSavePercent < 20)
            _manaSavePercent = 20;

        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _purifyTimer.Reset();
            _cleanseTimer.Reset();
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
        Main.Log("Stopped.");
    }

    internal static void BuffRotation()
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


    internal static void CombatRotation()
    {
        ToolBox.CheckAutoAttack(Attack);

        if (ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff() && 
            (_purifyTimer.ElapsedMilliseconds > 10000 || _purifyTimer.ElapsedMilliseconds <= 0))
        {
            _purifyTimer.Restart();
            Cast(Purify);
        }

        if (ToolBox.HasMagicDebuff() && (_cleanseTimer.ElapsedMilliseconds > 10000 || _cleanseTimer.ElapsedMilliseconds <= 0))
        {
            _cleanseTimer.Restart();
            Cast(Cleanse);
        }

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
            (ObjectManager.Target.HaveBuff("Judgement of the Crusader") || ObjectManager.Me.ManaPercentage > _manaSavePercent)
            && (!ZEPaladinSettings.CurrentSetting.UseSealOfCommand || !SealOfCommand.KnownSpell))
            Cast(SealOfRighteousness);

        if (!ObjectManager.Me.HaveBuff("Seal of Command") && !ObjectManager.Me.HaveBuff("Seal of the Crusader") && ObjectManager.Target.IsAlive &&
            (ObjectManager.Target.HaveBuff("Judgement of the Crusader") || ObjectManager.Me.ManaPercentage > _manaSavePercent)
            && ZEPaladinSettings.CurrentSetting.UseSealOfCommand && SealOfCommand.KnownSpell)
            Cast(SealOfCommand);

        if (!ObjectManager.Me.HaveBuff("Seal of Righteousness") && !ObjectManager.Me.HaveBuff("Seal of the Crusader") &&
            !ObjectManager.Me.HaveBuff("Seal of Command") && !SealOfCommand.IsSpellUsable && !SealOfRighteousness.IsSpellUsable
            && SealOfCommand.KnownSpell && ObjectManager.Me.Mana < _manaSavePercent)
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

    public static void ShowConfiguration()
    {
        ZEPaladinSettings.Load();
        ZEPaladinSettings.CurrentSetting.ToForm();
        ZEPaladinSettings.CurrentSetting.Save();
    }

    private static Spell SealOfRighteousness = new Spell("Seal of Righteousness");
    private static Spell SealOfTheCrusader = new Spell("Seal of the Crusader");
    private static Spell SealOfCommand = new Spell("Seal of Command");
    private static Spell HolyLight = new Spell("Holy Light");
    private static Spell DevotionAura = new Spell("Devotion Aura");
    private static Spell BlessingOfMight = new Spell("Blessing of Might");
    private static Spell Judgement = new Spell("Judgement");
    private static Spell LayOnHands = new Spell("Lay on Hands");
    private static Spell HammerOfJustice = new Spell("Hammer of Justice");
    private static Spell RetributionAura = new Spell("Retribution Aura");
    private static Spell Exorcism = new Spell("Exorcism");
    private static Spell ConcentrationAura = new Spell("Concentration Aura");
    private static Spell SanctityAura = new Spell("Sanctity Aura");
    private static Spell FlashOfLight = new Spell("Flash of Light");
    private static Spell BlessingOfWisdom = new Spell("Blessing of Wisdom");
    private static Spell DivineShield = new Spell("Divine Shield");
    private static Spell Cleanse = new Spell("Cleanse");
    private static Spell Purify = new Spell("Purify");
    private static Spell CrusaderStrike = new Spell("Crusader Strike");
    private static Spell HammerOfWrath = new Spell("Hammer of Wrath");
    private static Spell Attack = new Spell("Attack");
    private static Spell CrusaderAura = new Spell("Crusader Aura");
    private static Spell AvengingWrath = new Spell("Avenging Wrath");

    private static void Cast(Spell s)
    {
        if (s.IsSpellUsable && s.KnownSpell)
            s.Launch();
    }
}
