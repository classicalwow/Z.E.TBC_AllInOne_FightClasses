using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEWarlockSettings : Settings
{
    public static ZEWarlockSettings CurrentSetting { get; set; }

    private ZEWarlockSettings()
    {
        WandThreshold = 40;
        UseLifeTap = true;
        PetInPassiveWhenOOC = true;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarlock "
            + Translate.Get("Settings")
        );
    }

    [Category("Combat Rotation")]
    [DefaultValue(40)]
    [DisplayName("Wand Threshold")]
    [Description("Enemy HP under which the wand should be used")]
    public int WandThreshold { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Life Tap")]
    [Description("Use Life Tap")]
    public bool UseLifeTap { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Put pet in passive when out of combat")]
    [Description("Put pet in passive when out of combat (can be useful if you wan to ignore fights when traveling)")]
    public bool PetInPassiveWhenOOC { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCWarlock",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarlock > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCWarlock",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEWarlockSettings>(
                    AdviserFilePathAndName("WholesomeTBCWarlock",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEWarlockSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarlock > Load(): " + e);
        }
        return false;
    }
}
