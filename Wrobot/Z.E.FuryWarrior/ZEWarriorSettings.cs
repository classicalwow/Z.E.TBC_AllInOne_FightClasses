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
        oneHandedWeapon = 0;
        shield = 0;
        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarrior "
            + Translate.Get("Settings")
        );
    }

    [Category("Performance")]
    [DefaultValue("pvp")]
    [DisplayName("战斗类型")]
    [Description("pvp pve 拉怪")]
    public string fightType { get; set; }

    [Category("Performance")]
    [DefaultValue(0)]
    [DisplayName("单手武器")]
    [Description("写id 用于切盾换武器的时候")]
    public uint oneHandedWeapon { get; set; }

    [Category("Performance")]
    [DefaultValue(0)]
    [DisplayName("盾")]
    [Description("写id 用于切盾换武器的时候")]
    public uint shield { get; set; }



    


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
