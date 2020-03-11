using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZETBCFCSettings : Settings
{
    public static ZETBCFCSettings CurrentSetting { get; set; }

    private ZETBCFCSettings()
    {
        LastUpdateDate = 0;
    }

    public double LastUpdateDate { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("ZETBCFCSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Main.LogError("ZETBCFCSettings > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("ZETBCFCSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZETBCFCSettings>(
                    AdviserFilePathAndName("ZETBCFCSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZETBCFCSettings();
        }
        catch (Exception e)
        {
            Main.LogError("ZETBCFCSettings > Load(): " + e);
        }
        return false;
    }
}
