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
        StealthWhenPoisoned = false;
        SprintWhenAvail = true;
        UseBlindBandage = true;
        UseGarrote = true;

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

    [Category("Fight engage")]
    [DefaultValue(true)]
    [DisplayName("Stealth approach")]
    [Description("Always try to approach enemies in Stealth (can be buggy)")]
    public bool StealthApproach { get; set; }

    [Category("Fight engage")]
    [DefaultValue(true)]
    [DisplayName("Use Garrote")]
    [Description("Use Garrote when opening behind the target")]
    public bool UseGarrote { get; set; }

    [Category("Fight engage")]
    [DefaultValue(false)]
    [DisplayName("Stealth even if poisoned")]
    [Description("Try going in stealth even if affected by poison")]
    public bool StealthWhenPoisoned { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Sprint when available")]
    [Description("Use Sprint when available")]
    public bool SprintWhenAvail { get; set; }

    [Category("Combat")]
    [DefaultValue(true)]
    [DisplayName("Use Blind + Bandage")]
    [Description("Use Blind followed by your best bandage in your bags during combat " +
        "(If true, you should avoid using poisons, as they will break Blind)")]
    public bool UseBlindBandage { get; set; }

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
