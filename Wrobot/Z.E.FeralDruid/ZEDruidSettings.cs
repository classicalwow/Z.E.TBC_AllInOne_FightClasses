using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEDruidSettings : Settings
{
    public static ZEDruidSettings CurrentSetting { get; set; }

    private ZEDruidSettings()
    {
        AlwaysPull = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCDruid "
            + Translate.Get("Settings")
        );
    }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Always range pull")]
    [Description("Always pull with a range weapon")]
    public bool AlwaysPull { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCDruid",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCDruid > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCDruid",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEDruidSettings>(
                    AdviserFilePathAndName("WholesomeTBCDruid",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEDruidSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCDruid > Load(): " + e);
        }
        return false;
    }
}
