using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEWarriorSettings : Settings
{
    public static ZEWarriorSettings CurrentSetting { get; set; }

    private ZEWarriorSettings()
    {
        UseHamstring = true;
        UseBloodRage = true;
        UseDemoralizingShout = true;
        UseRend = true;
        UseCleave = true;
        PrioritizeBerserkStance = false;
        AlwaysPull = false;
        UseCommandingShout = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarrior "
            + Translate.Get("Settings")
        );
    }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Prioritize Berserker Stance")]
    [Description("Prioritize Berserker Stance over Battle Stance")]
    public bool PrioritizeBerserkStance { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Always pull")]
    [Description("Always pull with a range weapon")]
    public bool AlwaysPull { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Hamstring against humanoids")]
    [Description("Use Hamstring against humanoids to prevent them from fleeing too far")]
    public bool UseHamstring { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Bloodrage")]
    [Description("Use Bloodrage")]
    public bool UseBloodRage { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Demoralizing Shout")]
    [Description("Use Demoralizing Shout")]
    public bool UseDemoralizingShout { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Commanding Shout")]
    [Description("Use Commanding Shout instead of Battle Shout")]
    public bool UseCommandingShout { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Rend")]
    [Description("Use Rend")]
    public bool UseRend { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Cleave")]
    [Description("Use Cleave on multi aggro")]
    public bool UseCleave { get; set; }

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
