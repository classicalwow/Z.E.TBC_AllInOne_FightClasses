using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEMageSettings : Settings
{
    public static ZEMageSettings CurrentSetting { get; set; }

    private ZEMageSettings()
    {
        UseConeOfCold = true;
        WandThreshold = 30;
        IcyVeinMultiPull = true;
        BlinkWhenBackup = true;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCMage "
            + Translate.Get("Settings")
        );
    }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Cone of Cold")]
    [Description("Use Cone of Cold during the combat rotation")]
    public bool UseConeOfCold { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(30)]
    [DisplayName("Wand Threshold")]
    [Description("Enemy HP under which the wand should be used")]
    public int WandThreshold { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Only use Icy Veins on multipull")]
    [Description("Only use Icy Veins when 2 or more enemy are pulled")]
    public bool IcyVeinMultiPull { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Blink when backing up")]
    [Description("Use Blink when backing up from the target")]
    public bool BlinkWhenBackup { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCMage",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCMage > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCMage",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEMageSettings>(
                    AdviserFilePathAndName("WholesomeTBCMage",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEMageSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCMage > Load(): " + e);
        }
        return false;
    }
}
