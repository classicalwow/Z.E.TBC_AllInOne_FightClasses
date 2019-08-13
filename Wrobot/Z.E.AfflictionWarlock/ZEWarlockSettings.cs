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
        UseLifeTap = true;
        PetInPassiveWhenOOC = true;
        PrioritizeWandingOverSB = true;
        UseSiphonLife = false;
        UseImmolateHighLevel = false;
        UseUnendingBreath = true;
        UseDarkPact = true;
        UseSoulStone = true;
        AutoTorment = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarlock "
            + Translate.Get("Settings")
        );
    }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Life Tap")]
    [Description("Use Life Tap")]
    public bool UseLifeTap { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Prioritize wanding over Shadow Bolt")]
    [Description("Prioritize wanding over Shadow Bolt during combat to save mana")]
    public bool PrioritizeWandingOverSB { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Auto torment")]
    [Description("If true, will let Torment on autocast. If false, will let Z.E.Warlock manage Torment in order to save mana.")]
    public bool AutoTorment { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Immolate at high level")]
    [Description("Keep using Immmolate once Unstable Affliction is learnt")]
    public bool UseImmolateHighLevel { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Siphon Life")]
    [Description("Use Siphon Life")]
    public bool UseSiphonLife { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Put pet in passive when out of combat")]
    [Description("Put pet in passive when out of combat (can be useful if you wan to ignore fights when traveling)")]
    public bool PetInPassiveWhenOOC { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Unending Breath")]
    [Description("Make sure you have Unending Breath up at all time")]
    public bool UseUnendingBreath { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Dark Pact")]
    [Description("Use Dark Pact")]
    public bool UseDarkPact { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Soul Stone")]
    [Description("Use Soul Stone (needs a third party plugin to resurrect using the Soulstone)")]
    public bool UseSoulStone { get; set; }

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
