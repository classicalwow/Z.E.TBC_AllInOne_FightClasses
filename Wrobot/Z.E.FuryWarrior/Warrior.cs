using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Collections.Generic;
using static KeyBoardHook;
using System.Linq;

public static class Warrior
{
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEWarriorSettings _settings;
    private static int pullStatus = 1; //1 拉怪 2 嘲讽 3反恐 4保命 5追
    private static int pvpStatus = 1;//1 输出 2 盾反 3盾反中 4缴械

    public static void Initialize()
    {
        Main.Log("Initialized");
        KeyBoardHook.Initialize();
        KeyBoardHook.OnKeyDown += onKeyDown;
        KeyBoardHook.OnKeyUp += OnKeyUp;

        ZEWarriorSettings.Load();
        _settings = ZEWarriorSettings.CurrentSetting;
        FightEvents.OnFightEnd += (ulong guid) =>
        {

        };
        

        Rotation();
    }

    private static void onKeyDown(string key)
    {
        //Main.Log(key);
        if(_settings.fightType == "pull")
        {
            if (_settings.pullKey == key)
            {
                pullStatus = 2;
            }
        }else if(_settings.fightType == "pvp")
        {
            
        }
        
        
    }

    private static void OnKeyUp(string key)
    {

        if(_settings.fightType == "pull")
        {
            if (_settings.pullKey == key)
            {
                pullStatus = 1;
            }
            else if (_settings.fankongKey == key)
            {
                pullStatus = 3;
            }
            else if (_settings.baomingKey == key)
            {
                pullStatus = 4;
            }
            else if (_settings.dc == key)
            {
                pullStatus = 5;
            }
        }else if(_settings.fightType == "pvp")
        {
            if(key == "xbtn1")
            {
                if(pvpStatus == 1)
                {
                    pvpStatus = 2;
                }
                else
                {
                    EquipItemById(_settings.mainHandedWeapon);
                    pvpStatus = 1;
                }
                
            }else if(key == "xbtn2")
            {
                if (pvpStatus == 1)
                {
                    pvpStatus = 4;
                }
                else
                {
                    pvpStatus = 1;
                }
                
            }
        }
        
    }

    private static void baoming()
    {
        pullStatus = 1;
        var me = ObjectManager.Me;
        _settings.baomingItems.Add(me.GetEquipedItemBySlot(wManager.Wow.Enums.InventorySlot.INVSLOT_TRINKET1));
        _settings.baomingItems.Add(me.GetEquipedItemBySlot(wManager.Wow.Enums.InventorySlot.INVSLOT_TRINKET2));
        foreach(var itemId in _settings.baomingItems)
        {
            if (canUseItem(itemId))
            {
                ItemsManager.UseItem(itemId);
                return;
            }
        }
        if (Cast(LastStand))
            return;
        if (Cast(ShieldWall))
            return;

    }

    

    private static bool canUseItem(uint itemId)
    {
        return Lua.LuaDoString<bool>("canUse = false;local startTime, duration, enable = GetItemCooldown("+itemId+");if enable > 0 and startTime == 0 then canUse = true; end", "canUse");
    }

    private static void fankong()
    {
        pullStatus = 1;
        var me = ObjectManager.Me;
        var target = ObjectManager.Target;
        if (spellCoolDown("狂暴之怒"))
        {
            if (!InBerserkStance())
            {
                Cast(BerserkerStance);
            }
            Cast(BerserkerRage);
        }
        
    }

    public static void Dispose()
    {

        KeyBoardHook.Dispose();
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
                        switch (pvpStatus)
                        {
                            case 1:
                                pvp();
                                break;
                            case 2:
                                shieldReflection();
                                break;
                            case 3:
                                shieldReflectionEnd();
                                break;
                            case 4:
                                disarming();
                                break;
                           
                        }
                        
                    }else if(_settings.fightType == "pull")
                    {
                        if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable && ObjectManager.Target.IsAlive)
                        {
                            switch (pullStatus)
                            {
                                case 1:
                                    pull();
                                    break;
                                case 2:
                                    pull1();
                                    break;
                                case 3:
                                    fankong();
                                    break;
                                case 4:
                                    baoming();
                                    break;
                               
                            }
                            
                        }
                        if(pullStatus == 5)
                        {
                            dc();
                        }
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


    private static void disarming()
    {
        var me = ObjectManager.Me;
        //缴械
        if (me.Rage >= 20 && spellCoolDown("缴械"))
        {
            if (!InDefensiveStance())
            {
                Cast(DefensiveStance);
            }
            if (Cast(Disarm))
            {
                pvpStatus = 1;

            }
        }
    }

    private static void shieldReflectionEnd()
    {
        var me = ObjectManager.Me;
        if (!me.HaveBuff("Spell Reflection"))
        {
            EquipItemById(_settings.mainHandedWeapon);
            pvpStatus = 1;
            return;
        }
    }

    //盾反
    private static void shieldReflection()
    {
        //如果被冰环且有人寒冰箭就自动换上盾 且防御姿态 盾反
        var me = ObjectManager.Me;
        
        if (_settings.shield > 0 && _settings.oneHandedWeapon > 0 && _settings.mainHandedWeapon > 0 && me.Rage >= 25 && spellCoolDown("法术反射"))
        {
            EquipItemById(_settings.oneHandedWeapon);
            EquipItemById(_settings.shield);
            if (!InDefensiveStance())
            {

                Cast(DefensiveStance);
            }
            if (Cast(SpellReflection))
            {
                pvpStatus = 3;
            }
        }
        else
        {
            pvpStatus = 1;
        }
    }

    private static void dc()
    {
        pullStatus = 1;
        var target = ObjectManager.Target;
        var me = ObjectManager.Me;
        if (target.GetDistance > 9f)
        {
            if (target.IsPartyMember)
            {
                if (Cast(Intervene))
                    return;
            }
            else 
            {
                if (ObjectManager.GetNumberAttackPlayer() < 1 && !me.InCombatFlagOnly)
                {
                    //检查怒气 如果怒气小于30 冲锋
                    if (!InBattleStance())
                    {
                        if (Cast(BattleStance))
                            return;
                    }
                    if (Cast(Charge))
                        return;
                }
                else if(me.Rage>15 && spellCoolDown("拦截"))
                {
                    if (!InBerserkStance())
                        Cast(BerserkerStance);

                    if (Cast(Intercept))
                        return;
                }
            }
        }
        
    }

    //嘲讽
    private static void pull1()
    {
        var me = ObjectManager.Me;
        var target = ObjectManager.Target;
        
        if(target.HaveBuff("Mocking Blow") || target.HaveBuff("Taunt"))
        {
            return;
        }

        if (Taunt.IsSpellUsable)
        {
            if (InDefensiveStance())
            {
                Cast(DefensiveStance);
            }
            if (Cast(Taunt))
            {
                return;
            }
                
        }
        if(spellCoolDown("惩戒痛击") && me.Rage > 10)
        {
            if (!InBattleStance())
            {
                Cast(BattleStance);
            }
            if (Cast(MockingBlow))
                if (Cast(DefensiveStance))
                {
                    return;
                }
        }
        pull();
    }
    //拉怪
    private static void pull()
    {
        var me = ObjectManager.Me;
        var target = ObjectManager.Target;

        
        ToolBox.CheckAutoAttack(Attack);
        //切防御姿态
        if (!InDefensiveStance() && Cast(DefensiveStance))
            return;
        //英勇
        if(me.Rage > 50) {

            if (!HeroicStrikeOn() && Cast(HeroicStrike))
                return;

        }
        //复仇
        if (Cast(Revenge))
            return;
        //盾猛
        if (Cast(ShieldSlam))
            return;
        //格挡
        if (Cast(ShieldBlock))
            return;
        //破甲
        if(target.GetBuff("Sunder Armor").Stack < 5 && me.Rage>30)
        {
            if(target.GetBuff("Sunder Armor").TimeLeft < 5 * 1000)
            {
                if (Cast(SunderArmor))
                    return;
            }
            else if(Cast(Devastate))
            {
                return;
            }
                
        }
        //挫志
        if(!target.HaveBuff("Demoralizing Shout"))
        {
            if (Cast(DemoralizingShout))
                return;
        }

        
    }
    private static void pvp()
    {
        var me = ObjectManager.Me;
        var target = ObjectManager.Target;


        if (me.HealthPercent < 50 && canUseItem(25829))
        {
            ItemsManager.UseItem(25829);
        }

        if (!ObjectManager.Me.InCombatFlagOnly)
        {
            if (me.Rage < 20)
            {
                if (!InBattleStance() && Cast(BattleStance))
                    return;
            }

            if (!me.IsMounted && !me.HaveBuff("Battle Shout") && Cast(BattleShout))
                return;
        }

        if(!ObjectManager.Target.IsAttackable && me.InCombatFlagOnly)
        {
            if (me.Rage > 10 && target.GetDistance >=8 && target.GetDistance <= 25 && spellCoolDown("援护"))
            {
                if (!InDefensiveStance())
                {
                    Cast(DefensiveStance);
                }
                Cast(Intervene);
            }
            return;
        }

        if (ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable )
        {
            //自动攻击
            ToolBox.CheckAutoAttack(Attack);
            //未进入战斗
            if (!ObjectManager.Me.InCombatFlagOnly)
            {
                if (ObjectManager.Target.GetDistance >= 8f && ObjectManager.Target.GetDistance <= 25f && spellCoolDown("冲锋"))
                {
                    //检查怒气 如果怒气小于30 冲锋
                    if (!InBattleStance())
                    {
                        Cast(BattleStance);
                    }

                    if (Cast(Charge))
                        return;
                }
            }
            else
            {
                //拦截
                if (ObjectManager.Target.GetDistance > 9f )
                {
                    
                    if(ObjectManager.Target.GetDistance < 24f)
                    {
                        if (me.Rage > 10 && spellCoolDown("拦截"))
                        {
                            if (!InBerserkStance())
                            {
                                Cast(BerserkerStance);
                            }
                            Cast(Intercept);
                            return;
                        }
                    }
                    if (me.Rage < 20)
                    {
                        if (!InDefensiveStance())
                        {
                            if(Cast(DefensiveStance))
                                return;
                        }
                    }
                    return;
                }



                //如果没有断筋 5-10码刺耳怒吼

                if (target.GetDistance >= 5f && target.GetDistance <= 10f && !target.HaveBuff("Piercing Howl") && !target.HaveBuff("Hamstring"))
                {
                    if (Cast(PiercingHowl))
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

                //压制
                if (Cast(Overpower))
                    return;


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

                }



           
                //无蓝职业
                if(target.MaxMana == 0)
                {
                    if (InBerserkStance() && me.Rage > 30)
                    {
                        Cast(Slam);
                        if (!HeroicStrikeOn())
                            Cast(HeroicStrike);
                        return;
                    }
                    else
                    {
                        if (Cast(BattleStance))
                            return;
                    }
                }
                else
                {
                    //如果在战斗姿态 怒气大于30 用撕裂和英勇泄怒
                    if (InBattleStance() && me.Rage > 30)
                    {
                        if (!target.HaveBuff("Rend") && target.HealthPercent > 25)
                            if (Cast(Rend))
                                return;
                        if (!HeroicStrikeOn())
                            Cast(HeroicStrike);
                        return;

                    }else
                    {
                        if (Cast(BerserkerStance))
                            return;
                    }
                       
                }

                

                //血腥狂暴
                if (Me.HealthPercent > 70)
                    if (Cast(BloodRage))
                        return;



                //在狂暴姿态下打断施法
                if (InBerserkStance() && ToolBox.EnemyCasting())
                {
                    if (Cast(Pummel))
                        return;
                }

                if (canUseItem(30350) && me.HaveBuff("Polymorph") || me.HaveBuff("Frost Nova") || me.HaveBuff("Freezing Trap") || me.HaveBuff("Kidney Shot"))
                {
                    ItemsManager.UseItem(30350);
                }


                //如果被恐惧了 用狂暴之怒解恐惧
                if (me.HaveBuff("Fear") || me.HaveBuff("Intimidating Shout") || me.HaveBuff("Psychic Scream"))
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
            }


            

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

    //猛击
    private static Spell Slam = new Spell("Slam");
    
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

    //盾牌猛击
    private static Spell ShieldSlam = new Spell("Shield Slam");

    //盾牌格档
    private static Spell ShieldBlock = new Spell("Shield Block");

    //复仇
    private static Spell Revenge = new Spell("Revenge");


    //破甲攻击
    private static Spell SunderArmor = new Spell("Sunder Armor");

    //毁灭打击
    private static Spell Devastate = new Spell("Devastate");

    //嘲讽
    private static Spell Taunt = new Spell("Taunt");

    //惩戒痛击
    private static Spell MockingBlow = new Spell("Mocking Blow");

    //破釜沉舟
    private static Spell LastStand = new Spell("Last Stand");

    //盾墙
    private static Spell ShieldWall = new Spell("Shield Wall");

    //援护
    private static Spell Intervene = new Spell("Intervene");

    


    private static bool spellCoolDown(string spellName)
    {
        return Lua.LuaDoString<bool>("cooldown = false;local time = GetSpellCooldown('"+ spellName + "');if time == 0 then cooldown = true end", "cooldown");
    }


    internal static bool Cast(Spell s)
    {
      //  CombatDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;

        s.Launch();
        return true;
    }

    private static void CombatDebug(string s)
    {
        Main.Log(s);
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
