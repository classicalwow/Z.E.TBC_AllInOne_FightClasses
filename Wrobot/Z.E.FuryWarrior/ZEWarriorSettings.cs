using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;
using System.Collections.Generic;

[Serializable]
public class ZEWarriorSettings : Settings
{
    public static ZEWarriorSettings CurrentSetting { get; set; }

    private ZEWarriorSettings()
    {
        fightType = "pvp";

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarrior "
            + Translate.Get("Settings")
        );
    }

    [Category("Performance")]
    [DefaultValue(10)]
    [DisplayName("战斗类型")]
    [Description("pvp pve 拉怪")]
    public string fightType { get; set; }




    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCWarrior",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarrior > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCWarrior",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEWarriorSettings>(
                    AdviserFilePathAndName("WholesomeTBCWarrior",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEWarriorSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarrior > Load(): " + e);
        }
        return false;
    }
}
