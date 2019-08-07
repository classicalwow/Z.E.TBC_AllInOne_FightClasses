using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEBMHunterSettings : Settings
{
    public static ZEBMHunterSettings CurrentSetting { get; set; }

    private ZEBMHunterSettings()
    {
        RangedWeaponSpeed = 2500;
        BackupFromMelee = true;
        UseFreezingTrap = true;
        MaxBackupAttempts = 5;
        FeedPet = true;
        BestialWrathOnMulti = false;
        RapidFireOnMulti = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "Z.E.BMHunter "
            + Translate.Get("Settings")
        );
    }

    [Category("Misc")]
    [DefaultValue(2500)]
    [DisplayName("Ranged weapon speed")]
    [Description("Ranged weapon speed in milliseconds")]
    public int RangedWeaponSpeed { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Backup from melee")]
    [Description("Set to True is you want to backup from melee range when your pet has gained aggro")]
    public bool BackupFromMelee { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use freezing trap")]
    [Description("Set to True is you want to use freezing trap on multiple aggro (will trigger if Mend Pet is active on primary target)")]
    public bool UseFreezingTrap { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(5)]
    [DisplayName("Max backup attempts")]
    [Description("Maximum number of attempts after failing to backup to a valid distance (eg when back to a wall)")]
    public int MaxBackupAttempts { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Feed Pet")]
    [Description("Use Z.E.Hunter to manage pet feeding")]
    public bool FeedPet { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Bestial Wrath on multi aggro")]
    [Description("Only use Bestial Wrath on multi aggro. If set to False, Bestial Wrath will be used as soon at available")]
    public bool BestialWrathOnMulti { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Rapid Fire on multi aggro")]
    [Description("Only use Rapid Fire on multi aggro. If set to False, Rapid Fire will be used as soon at available")]
    public bool RapidFireOnMulti { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("ZEBMHunter",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("ZEBMHunter > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("ZEBMHunter",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEBMHunterSettings>(
                    AdviserFilePathAndName("ZEBMHunter",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEBMHunterSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("ZEBMHunter > Load(): " + e);
        }
        return false;
    }
}
