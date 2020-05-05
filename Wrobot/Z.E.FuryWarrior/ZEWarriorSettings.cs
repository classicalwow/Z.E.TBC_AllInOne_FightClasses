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
        pullKey = "xbtn1";
        baomingKey = "F1";
        fankongKey = "F2";
        mainHandedWeapon = 0;
        dc = "xbtn2";
        baomingItems = new List<uint>();
        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarrior "
            + Translate.Get("Settings")
        );
    }

    [Category("Performance")]
    [DefaultValue("pvp")]
    [DisplayName("战斗类型")]
    [Description("pvp pve pull")]
    public string fightType { get; set; }

    [Category("pvp")]
    [DefaultValue(0)]
    [DisplayName("单手武器")]
    [Description("写id 用于切盾换武器的时候")]
    public uint oneHandedWeapon { get; set; }

    [Category("pvp")]
    [DefaultValue(0)]
    [DisplayName("双手武器")]
    [Description("写id 用于切盾换武器的时候")]
    public uint mainHandedWeapon { get; set; }

    [Category("pvp")]
    [DefaultValue(0)]
    [DisplayName("盾")]
    [Description("写id 用于切盾换武器的时候")]
    public uint shield { get; set; }


    [Category("pull")]
    [DefaultValue("xbtn1")]
    [DisplayName("嘲讽")]
    [Description("按住会一直尝试嘲讽,惩戒痛击 xbtn或者a-z或者0-9")]
    public string pullKey { get; set; }

    [Category("pull")]
    [DefaultValue("F1")]
    [DisplayName("保命")]
    [Description("按一次使用一次保命技能 默认饰品->破斧->盾墙")]
    public string baomingKey { get; set; }

    [Category("pull")]
    [DefaultValue("F2")]
    [DisplayName("切反恐")]
    [Description("按一次尝试反恐 狂暴之怒")]
    public string fankongKey { get; set; }

    [Category("pull")]
    [DisplayName("保命物品id")]
    [Description("保命的物品id当使用保命快捷键的时候")]
    public List<uint> baomingItems { get; set; }

    [Category("pull")]
    [DefaultValue("xbtn2")]
    [DisplayName("追")]
    [Description("追过去 如果是怪 冲锋 拦截 如果是队友援护")]
    public string dc { get; set; }



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
