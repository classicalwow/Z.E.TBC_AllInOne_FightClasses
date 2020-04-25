using System;
using System.Diagnostics;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Collections.Generic;
using System.Linq;


public static class Warrior
{
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEWarriorSettings _settings;
    private static uint currentWeapon;


    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEWarriorSettings.Load();
        _settings = ZEWarriorSettings.CurrentSetting;
        FightEvents.OnFightEnd += (ulong guid) =>
        {

        };

        Rotation();
    }

    public static void Dispose()
    {
        Main.Log("Stop in progress.");
    }

    internal static void Rotation()
    {
        Main.Log("Started");
        while (Main._isLaunched)
        {
            try
            {
                if (!Products.InPause && !ObjectManager.Me.IsDeadMe)
                {
                    if (_settings.fightType == "pvp")
                    {

                        pvp();
                    }
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError("ERROR: " + arg, true);
            }
            Thread.Sleep(ToolBox.GetLatency() + 10);
        }
        Main.Log("Stopped.");
    }

    private static void pvp()
    {
        var me = ObjectManager.Me;
        var target = ObjectManager.Target;

        

        if (Fight.InFight && ObjectManager.Me.Target > 0UL)
        {

            if(currentWeapon>0 && !me.HaveBuff("Spell Reflection"))
            {
                EquipItemById(currentWeapon);
                currentWeapon = 0;
                return;
            }

            //未进入战斗
            if (ObjectManager.GetNumberAttackPlayer() < 1 && !ObjectManager.Target.InCombatFlagOnly)
            {
                //检查怒气 如果怒气小于30 冲锋
                if (!InBattleStance() && me.Rage <= 30)
                {
                    if (Cast(BattleStance))
                        return;
                }
                if (Cast(Charge))
                    return;
            }


            //自动攻击
            ToolBox.CheckAutoAttack(Attack);

            //拦截
            if (ObjectManager.Target.GetDistance > 9f && ObjectManager.Target.GetDistance < 24f)
            {
                if (InBerserkStance())
                    if (Cast(Intercept))
                        return;
                    else if (me.Rage < 30)
                    {
                        if (Cast(BerserkerStance))
                            if (Cast(Intercept))
                                return;
                    }

            }



            //如果没有断筋 5-10码刺耳怒吼

            if (target.GetDistance >= 5f && target.GetDistance <= 10f && !target.HaveBuff("Piercing Howl") && !target.HaveBuff("Hamstring"))
            {
                if (Cast(PiercingHowl))
                    return;
            }

            //如果被冰环且有人寒冰箭就自动换上盾 且防御姿态 盾反
            
            if (_settings.shield > 0 && _settings.oneHandedWeapon > 0 && me.Rage >=25 && spellCoolDown("法术反射") && me.HaveBuff("Frost Nova") && target.GetDistance > 5f && target.CastingSpell.Name == "Frostbolt" && ObjectManager.Target.CastingTimeLeft > Usefuls.Latency)
            {
                
                currentWeapon = me.GetEquipedItemBySlot(wManager.Wow.Enums.InventorySlot.INVSLOT_MAINHAND);
                EquipItemById(_settings.oneHandedWeapon);
                EquipItemById(_settings.shield);
                if (!InDefensiveStance())
                {
                   
                    Cast(DefensiveStance);
                }
                Cast(SpellReflection);
                return;
            }

            //检查对方是否有断筋
            if (!target.HaveBuff("Hamstring"))
            {
                if (Cast(Hamstring))
                    return;
            }

            //斩杀
            if (target.HealthPercent < 20)
                if (Cast(Execute))
                    return;


            //乘胜追击
            if (VictoryRush.KnownSpell)
                if (Cast(VictoryRush))
                    return;

            //压制,如果压制好了看怒气 如果小于30切战斗姿态 释放压制
            if (Overpower.IsSpellUsable)
            {
                if (InBattleStance())
                    if (Cast(Overpower))
                        return;
                if (!InBattleStance() && me.Rage <= 30)
                    if (Cast(BattleStance))
                        if (Cast(Overpower))
                            return;

            }

           
            

            //致死打击
            if (Cast(MortalStrike))
                return;

            if (InBerserkStance() && target.GetDistance <= 8f && Cast(Whirlwind))
                return;

            if (target.HealthPercent > 50)
            {
                //挫志怒吼 判断10码内是否有物理职业 而且他们有人没有挫志buff
                if (physicals.Contains(target.WowClass.ToString()) && !target.HaveBuff("Demoralizing Shout"))
                {
                    if (Cast(DemoralizingShout))
                        return;
                }


                //缴械
                if (me.Rage >= 20 && me.Rage < 60 && new List<string> { "Warrior", "Rogue", "Paladin" }.Contains(target.WowClass.ToString()) && spellCoolDown("缴械"))
                {
                    if (!InDefensiveStance())
                    {
                        Cast(DefensiveStance);
                    }
                    if (Cast(Disarm) && Cast(BerserkerStance))
                        return;
                }
            }



            //血腥狂暴
            if (Me.HealthPercent > 70)
                if (Cast(BloodRage))
                    return;

            //如果在战斗姿态 怒气大于30 用撕裂和英勇泄怒
            if (InBattleStance() && me.Rage > 30)
            {
                if (!target.HaveBuff("Rend") && target.HealthPercent > 25)
                    if (Cast(Rend))
                        return;
                if (!HeroicStrikeOn() && Cast(HeroicStrike))
                    return;

            }


            //狂暴姿态
            if (!InBerserkStance() && Me.Rage < 30)
                if (Cast(BerserkerStance))
                    return;

            //在狂暴姿态下打断施法
            if (InBerserkStance() && ToolBox.EnemyCasting())
            {
                if (Cast(Pummel))
                    return;
            }



            //如果被恐惧了 用狂暴之怒解恐惧
            if (me.HaveBuff("Fear"))
            {
                if (Cast(BerserkerRage))
                    return;
                if (Cast(DeathWish))
                    return;
            }
            //如果怒气大于60泄怒
            if (!HeroicStrikeOn() && Me.Rage > 80)
                if (Cast(HeroicStrike))
                    return;

            //战斗怒吼
            if (!Me.HaveBuff("Battle Shout") && Cast(BattleShout))
                return;








        }
    }

    private static void EquipItemById(uint id)
    {
        ItemsManager.EquipItemByName(ItemsManager.GetNameById(id));
    }

    public static void ShowConfiguration()
    {
        ZEWarriorSettings.Load();
        ZEWarriorSettings.CurrentSetting.ToForm();
        ZEWarriorSettings.CurrentSetting.Save();
    }

    //攻击
    private static Spell Attack = new Spell("Attack");
    //英勇打击
    private static Spell HeroicStrike = new Spell("Heroic Strike");
    //战斗怒吼
    private static Spell BattleShout = new Spell("Battle Shout");
    //命令怒吼
    private static Spell CommandingShout = new Spell("Commanding Shout");
    //冲锋
    private static Spell Charge = new Spell("Charge");
    //撕裂
    private static Spell Rend = new Spell("Rend");
    //断筋
    private static Spell Hamstring = new Spell("Hamstring");
    //血性狂暴
    private static Spell BloodRage = new Spell("Bloodrage");
    //压制
    private static Spell Overpower = new Spell("Overpower");
    //挫志怒吼
    private static Spell DemoralizingShout = new Spell("Demoralizing Shout");
    //投掷
    private static Spell Throw = new Spell("Throw");
    //射击
    private static Spell Shoot = new Spell("Shoot");
    //反击风暴
    private static Spell Retaliation = new Spell("Retaliation");
    //顺劈斩
    private static Spell Cleave = new Spell("Cleave");
    //斩杀
    private static Spell Execute = new Spell("Execute");
    //横扫
    private static Spell SweepingStrikes = new Spell("Sweeping Strikes");
    //嗜血
    private static Spell Bloodthirst = new Spell("Bloodthirst");
    //致死打击
    private static Spell MortalStrike = new Spell("Mortal Strike");
    //狂暴姿态
    private static Spell BerserkerStance = new Spell("Berserker Stance");
    //战斗姿态
    private static Spell BattleStance = new Spell("Battle Stance");
    
    //防御姿态
    private static Spell DefensiveStance = new Spell("Defensive Stance");
    //拦截
    private static Spell Intercept = new Spell("Intercept");
    //拳击
    private static Spell Pummel = new Spell("Pummel");
    //狂暴之怒
    private static Spell BerserkerRage = new Spell("Berserker Rage");
    //暴怒
    private static Spell Rampage = new Spell("Rampage");

    //乘胜追击
    private static Spell VictoryRush = new Spell("Victory Rush");

    //刺耳怒吼
    private static Spell PiercingHowl = new Spell("Piercing Howl");

    //死亡之愿
    private static Spell DeathWish = new Spell("Death Wish");

    //缴械
    private static Spell Disarm = new Spell("Disarm");


    //旋风斩
    private static Spell Whirlwind = new Spell("Whirlwind");

    //法术反射

    private static Spell SpellReflection = new Spell("Spell Reflection");



    private static bool spellCoolDown(string spellName)
    {
        return Lua.LuaDoString<bool>("cooldown = false;local time = GetSpellCooldown('"+ spellName + "');if time == 0 then cooldown = true end", "cooldown");
    }


    internal static bool Cast(Spell s)
    {
        CombatDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;

        s.Launch();
        return true;
    }

    private static void CombatDebug(string s)
    {
        //Main.Log(s);
    }

    //是否是物理输出职业
    private static List<String> physicals = new List<string>
        {
            "Warrior",
            "Rogue",
            "Hunter",
            "Puladin"
        };

    private static bool HeroicStrikeOn()
    {
        return Lua.LuaDoString<bool>("hson = false; if IsCurrentSpell('英勇打击') then hson = true end", "hson");
    }

    private static bool InBattleStance()
    {
        return Lua.LuaDoString<bool>("bs = false; if GetShapeshiftForm() == 1 then bs = true end", "bs");
    }

    private static bool InDefensiveStance()
    {
        return Lua.LuaDoString<bool>("bs = false; if GetShapeshiftForm() == 2 then bs = true end", "bs");
    }

    private static bool InBerserkStance()
    {
        return Lua.LuaDoString<bool>("bs = false; if GetShapeshiftForm() == 3 then bs = true end", "bs");
    }
}
