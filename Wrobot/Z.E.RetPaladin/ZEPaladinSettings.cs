using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEPaladinSettings : Settings
{
    public static ZEPaladinSettings CurrentSetting { get; set; }

    private ZEPaladinSettings()
    {
        ManaSaveLimitPercent = 50;
        FlashHealBetweenFights = true;
        UseBlessingOfWisdom = false;
        UseSealOfCommand = false;
        UseExorcism = false;
        UseHammerOfWrath = false;
        DevoAuraOnMulti = true;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCPaladin "
            + Translate.Get("Settings")
        );
    }

    [Category("Combat")]
    [DefaultValue(50)]
    [DisplayName("Mana Save Limit percent")]
    [Description("Try to save this percentage of mana")]
    public int ManaSaveLimitPercent { get; set; }

    [Category("Combat")]
    [DefaultValue(false)]
    [DisplayName("Use Hammer of Wrath")]
    [Description("Use Hammer of Wrath")]
    public bool UseHammerOfWrath { get; set; }

    [Category("Combat")]
    [DefaultValue(false)]
    [DisplayName("Use Exorcism")]
    [Description("Use Exorcism against Undead and Demon target")]
    public bool UseExorcism { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Flash Heal between fights")]
    [Description("Remain healed up between fights using Flash of Light")]
    public bool FlashHealBetweenFights { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Use Blessing od Wisdom")]
    [Description("Use Blessing od Wisdom instead of Blessing od Might")]
    public bool UseBlessingOfWisdom { get; set; }

    [Category("Combat")]
    [DefaultValue(false)]
    [DisplayName("Use Seal of Command")]
    [Description("Use Seal of Command instead of Seal of Righteousness")]
    public bool UseSealOfCommand { get; set; }

    [Category("Combat")]
    [DefaultValue(true)]
    [DisplayName("Devotion Aura on multi aggro")]
    [Description("Use Devotion Aura on multi aggro")]
    public bool DevoAuraOnMulti { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCPaladin",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCPaladin > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCPaladin",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEPaladinSettings>(
                    AdviserFilePathAndName("WholesomeTBCPaladin",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEPaladinSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCPaladin > Load(): " + e);
        }
        return false;
    }
}
