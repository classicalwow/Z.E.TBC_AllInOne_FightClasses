using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZERogueSettings : Settings
{
    public static ZERogueSettings CurrentSetting { get; set; }

    private ZERogueSettings()
    {
        AlwaysPull = false;
        StealthApproach = true;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCRogue "
            + Translate.Get("Settings")
        );
    }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Always range pull")]
    [Description("Always pull with a range weapon")]
    public bool AlwaysPull { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Stealth approach")]
    [Description("Always try to approach enemies in Stealth (can be buggy)")]
    public bool StealthApproach { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCRogue",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCRogue > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCRogue",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZERogueSettings>(
                    AdviserFilePathAndName("WholesomeTBCRogue",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZERogueSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCRogue > Load(): " + e);
        }
        return false;
    }
}
