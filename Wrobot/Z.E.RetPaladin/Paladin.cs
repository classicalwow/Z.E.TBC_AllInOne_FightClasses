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
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static ZEPaladinSettings _settings;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEPaladinSettings.Load();
        _settings = ZEPaladinSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        _manaSavePercent = _settings.ManaSaveLimitPercent;
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
				if (!Products.InPause && !Me.IsDeadMe)
                {
                    BuffRotation();

                    if (Fight.InFight && Me.Target > 0UL && ObjectManager.Target.IsAttackable)
                    {
                        CombatRotation();
                    }
				}
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(50);
		}
        Main.Log("Stopped.");
    }

    internal static void BuffRotation()
    {
        // Holy Light
        if (Me.HealthPercent < 50 && !Fight.InFight && !Me.IsMounted && HolyLight.IsSpellUsable)
        {
            Lua.RunMacroText("/target player");
            Cast(HolyLight);
            Lua.RunMacroText("/cleartarget");
        }

        // Flash of Light
        if (Me.HealthPercent < 75 && !Fight.InFight && _settings.FlashHealBetweenFights
            && !Me.IsMounted && FlashOfLight.IsSpellUsable)
        {
            Lua.RunMacroText("/target player");
            Cast(FlashOfLight);
            Lua.RunMacroText("/cleartarget");
        }

        // Crusader Aura
        if (Me.IsMounted && CrusaderAura.KnownSpell && !Me.HaveBuff("Crusader Aura") && !Fight.InFight)
            Cast(CrusaderAura);

        // Blessing of Wisdom
        if (_settings.UseBlessingOfWisdom && !Me.HaveBuff("Blessing of Wisdom")
            && !Me.IsMounted && BlessingOfWisdom.IsSpellUsable)
        {
            Lua.RunMacroText("/target player");
            Cast(BlessingOfWisdom);
            Lua.RunMacroText("/cleartarget");
        }

        // Blessing of Might
        if (!_settings.UseBlessingOfWisdom && !Me.HaveBuff("Blessing of Might")
            && !Me.IsMounted && BlessingOfMight.IsSpellUsable)
        {
            Lua.RunMacroText("/target player");
            Cast(BlessingOfMight);
            Lua.RunMacroText("/cleartarget");
        }
    }


    internal static void CombatRotation()
    {
        ToolBox.CheckAutoAttack(Attack);

        // Purify
        if ((ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff()) && Purify.IsSpellUsable &&
            (_purifyTimer.ElapsedMilliseconds > 10000 || _purifyTimer.ElapsedMilliseconds <= 0))
        {
            _purifyTimer.Restart();
            Lua.RunMacroText("/target player");
            Cast(Purify);
            Lua.RunMacroText("/cleartarget");
        }

        // Cleanse
        if (ToolBox.HasMagicDebuff() && (_cleanseTimer.ElapsedMilliseconds > 10000 || _cleanseTimer.ElapsedMilliseconds <= 0)
            && Cleanse.IsSpellUsable)
        {
            _cleanseTimer.Restart();
            Lua.RunMacroText("/target player");
            Cast(Cleanse);
            Lua.RunMacroText("/cleartarget");
        }

        // Devotion Aura multi
        if ((ObjectManager.GetNumberAttackPlayer() > 1 && _settings.DevoAuraOnMulti) && 
            !Me.HaveBuff("Devotion Aura"))
            Cast(DevotionAura);

        // Devotion Aura
        if (!Me.HaveBuff("Devotion Aura") && !SanctityAura.KnownSpell && !RetributionAura.KnownSpell)
            Cast(DevotionAura);

        // Sanctity Aura
        if (!Me.HaveBuff("Sanctity Aura") && SanctityAura.KnownSpell && ObjectManager.GetNumberAttackPlayer() <= 1)
            Cast(SanctityAura);

        // Retribution Aura
        if (!Me.HaveBuff("Retribution Aura") && !SanctityAura.KnownSpell && RetributionAura.KnownSpell 
            && ObjectManager.GetNumberAttackPlayer() <= 1)
            Cast(SanctityAura);

        // Lay on Hands
        if (Me.HealthPercent < 10)
            Cast(LayOnHands);

        // Avenging Wrath
        if (Me.ManaPercentage > _manaSavePercent && ObjectManager.GetNumberAttackPlayer() > 1)
            Cast(AvengingWrath);

        // Hammer of Justice
        if (Me.HealthPercent < 50 && Me.ManaPercentage > _manaSavePercent)
            Cast(HammerOfJustice);
        
        // Exorcism
        if (ObjectManager.Target.CreatureTypeTarget == "Undead" || ObjectManager.Target.CreatureTypeTarget == "Demon"
            && _settings.UseExorcism)
            Cast(Exorcism);
            
        // Judgement (Crusader)
        if (Me.HaveBuff("Seal of the Crusader") && ObjectManager.Target.GetDistance < 10)
        {
            Cast(Judgement);
            Thread.Sleep(200);
        }

        // Judgement
        if ((Me.HaveBuff("Seal of Righteousness") || Me.HaveBuff("Seal of Command")) 
            && ObjectManager.Target.GetDistance < 10  
            && (Me.ManaPercentage >= _manaSavePercent || Me.HaveBuff("Seal of the Crusader")))
            Cast(Judgement);

        // Seal of the Crusader
        if (!ObjectManager.Target.HaveBuff("Judgement of the Crusader") && !Me.HaveBuff("Seal of the Crusader")
            && Me.ManaPercentage > _manaSavePercent - 20 && ObjectManager.Target.IsAlive)
            Cast(SealOfTheCrusader);

        // Seal of Righteousness
        if (!Me.HaveBuff("Seal of Righteousness") && !Me.HaveBuff("Seal of the Crusader") && ObjectManager.Target.IsAlive &&
            (ObjectManager.Target.HaveBuff("Judgement of the Crusader") || Me.ManaPercentage > _manaSavePercent)
            && (!_settings.UseSealOfCommand || !SealOfCommand.KnownSpell))
            Cast(SealOfRighteousness);

        // Seal of Command
        if (!Me.HaveBuff("Seal of Command") && !Me.HaveBuff("Seal of the Crusader") && ObjectManager.Target.IsAlive &&
            (ObjectManager.Target.HaveBuff("Judgement of the Crusader") || Me.ManaPercentage > _manaSavePercent)
            && _settings.UseSealOfCommand && SealOfCommand.KnownSpell)
            Cast(SealOfCommand);

        // Seal of Command Rank 1
        if (!Me.HaveBuff("Seal of Righteousness") && !Me.HaveBuff("Seal of the Crusader") &&
            !Me.HaveBuff("Seal of Command") && !SealOfCommand.IsSpellUsable && !SealOfRighteousness.IsSpellUsable
            && SealOfCommand.KnownSpell && Me.Mana < _manaSavePercent)
            Lua.RunMacroText("/cast Seal of Command(Rank 1)");

        // Holy Light / Flash of Light
        if (Me.HealthPercent < 50)
        {
            if (!HolyLight.IsSpellUsable)
            {
                if (Me.HealthPercent < 20)
                    Cast(DivineShield);
                Cast(FlashOfLight);
            }
            Cast(HolyLight);
        }

        // Crusader Strike
        if (Me.ManaPercentage > 10)
            Cast(CrusaderStrike);

        // Hammer of Wrath
        if (_settings.UseHammerOfWrath)
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
