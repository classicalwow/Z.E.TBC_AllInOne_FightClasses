using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class ToolBox
{
    // Reactivates auto attack if it's off. Must pass the Attack spell as argument
    public static void CheckAutoAttack(Spell attack)
    {
        bool _autoAttacking = Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Attack') then isAutoRepeat = true end", "isAutoRepeat");
        if (!_autoAttacking && ObjectManager.GetNumberAttackPlayer() > 0)
        {
            Main.LogDebug("Re-activating attack");
            attack.Launch();
        }
    }

    // Returns whether units, hostile or not, are close to the player. Distance must be passed as argument
    public static bool CheckIfEnemiesClose(float distance)
    {
        List<WoWUnit> surroundingEnemies = ObjectManager.GetObjectWoWUnit();
        WoWUnit closestUnit = null;
        float closestUnitDistance = 100;

        foreach (WoWUnit unit in surroundingEnemies)
        {
            float distanceFromTarget = unit.Position.DistanceTo(ObjectManager.Me.Position);

            if (unit.IsAlive && !unit.IsTapDenied && unit.IsValid && !unit.IsTaggedByOther && !unit.PlayerControlled
                && unit.IsAttackable && distanceFromTarget < closestUnitDistance && unit.Guid != ObjectManager.Target.Guid)
            {
                closestUnit = unit;
                closestUnitDistance = distanceFromTarget;
            }
        }

        if (closestUnit != null && closestUnitDistance < distance)
        {
            Main.LogDebug("Enemy close: " + closestUnit.Name);
            return true;
        }
        return false;
    }

    // Returns whether hostile units are close to the target. Target and distance must be passed as argument
    public static bool CheckIfEnemiesOnPull(WoWUnit target, float distance)
    {
        List<WoWUnit> surroundingEnemies = ObjectManager.GetObjectWoWUnit();
        WoWUnit closestUnit = null;
        float closestUnitDistance = 100;

        foreach (WoWUnit unit in surroundingEnemies)
        {
            bool flagHostile = unit.Reaction.ToString().Equals("Hostile");
            float distanceFromTarget = unit.Position.DistanceTo(target.Position);

            if (unit.IsAlive && !unit.IsTapDenied && unit.IsValid && !unit.IsTaggedByOther && !unit.PlayerControlled
                && unit.IsAttackable && distanceFromTarget < closestUnitDistance && flagHostile && unit.Guid != target.Guid)
            {
                closestUnit = unit;
                closestUnitDistance = distanceFromTarget;
            }
        }

        if (closestUnit != null && closestUnitDistance < distance)
        {
            Main.Log("Enemy too close: " + closestUnit.Name + ", pulling with range weapon");
            return true;
        }
        return false;
    }

    // Returns whether the unit can bleed or be poisoned
    public static bool CanBleed(WoWUnit unit)
    {
        return unit.CreatureTypeTarget != "Elemental" && unit.CreatureTypeTarget != "Mechanical";
    }

    // Returns whether the player is poisoned
    public static bool HasPoisonDebuff()
    {
        return Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Poison' then
                return true
                end
            end");
    }

    // Returns whether the player has a disease
    public static bool HasDiseaseDebuff()
    {
        return Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Disease' then
                return true
                end
            end");
    }

    // Returns whether the player has a curse
    public static bool HasCurseDebuff()
    {
        return Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Curse' then
                return true
                end
            end");
    }

    // Returns whether the player has a magic debuff
    public static bool HasMagicDebuff()
    {
        return Lua.LuaDoString<bool>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Magic' then
                return true
                end
            end");
    }

    // Returns the type of debuff the player has as a string
    public static string GetDebuffType()
    {
        return Lua.LuaDoString<string>
            (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if (d == 'Poison' or d == 'Magic' or d == 'Curse' or d == 'Disease') then
                return d
                end
            end");
    }

    // Returns whether the player has the debuff passed as a string (ex: Weakened Soul)
    public static bool HasDebuff(string debuffName)
    {
        return Lua.LuaDoString<bool>
            ($"for i=1,25 do " +
	            "local n, _, _, _, _  = UnitDebuff('player',i); " +
                "if n == '" + debuffName + "' then " +
                "return true " +
                "end "+
            "end");
    }

    // Returns the time left on a buff in seconds, buff name is passed as string
    public static int BuffTimeLeft(string buffName)
    {
        return Lua.LuaDoString<int>
            ($"for i=1,25 do " +
                "local n, _, _, _, _, duration, _  = UnitBuff('player',i); " +
                "if n == '" + buffName + "' then " +
                "return duration " +
                "end " +
            "end");
    }

    // Returns true if the enemy is either casting or channeling (good for interrupts)
    public static bool EnemyCasting()
    {
        int channelTimeLeft = Lua.LuaDoString<int>(@"local spell, _, _, _, endTimeMS = UnitChannelInfo('target')
                                    if spell then
                                     local finish = endTimeMS / 1000 - GetTime()
                                     return finish
                                    end");
        if (channelTimeLeft < 0 || ObjectManager.Target.CastingTimeLeft > Usefuls.Latency)
            return true;
        return false;
    }

    // Waits for GlobalCooldown to be off, must pass the most basic spell avalailable at lvl1 (ex: Smite for priest)
    public static void WaitGlobalCoolDown(Spell s)
    {
        int c = 0;
        while (!s.IsSpellUsable)
        {
            c += 50;
            Thread.Sleep(50);
            if (c >= 2000)
                return;
        }
        Main.LogDebug("Waited for GCD : " + c);
    }

    // Gets Character's specialization (talents)
    public static string GetSpec()
    {
        var Talents = new Dictionary<string, int>();
        for (int i = 1; i <= 3; i++)
        {
            Talents.Add(
                Lua.LuaDoString<string>($"local name, iconTexture, pointsSpent = GetTalentTabInfo({i}); return name"),
                Lua.LuaDoString<int>($"local name, iconTexture, pointsSpent = GetTalentTabInfo({i}); return pointsSpent")
            );
        }
        var highestTalents = Talents.Max(x => x.Value);
        return Talents.Where(t => t.Value == highestTalents).FirstOrDefault().Key;
    }

    #region Items

    // Deletes item passed as string
    public static void LuaDeleteItem(string item)
    {
        Lua.LuaDoString("for bag = 0, 4, 1 do for slot = 1, 32, 1 do local name = GetContainerItemLink(bag, slot); " +
            "if name and string.find(name, \"" + item + "\") then PickupContainerItem(bag, slot); " +
            "DeleteCursorItem(); end; end; end", false);
    }

    // Count the amount of the specified item stacks in your bags
    public static int CountItemStacks(string itemArg)
    {
        return Lua.LuaDoString<int>("local count = GetItemCount('" + itemArg + "'); return count");
    }

    // Checks if you have any of the listed items in your bags
    public static bool HaveOneInList(List<string> list)
    {
        List<WoWItem> _bagItems = Bag.GetBagItem();
        bool _haveItem = false;
        foreach (WoWItem item in _bagItems)
        {
            if (list.Contains(item.Name))
                _haveItem = true;
        }
        return _haveItem;
    }

    // Get item ID in bag from a list passed as argument (good to check CD)
    public static int GetItemID(List<string> list)
    {
        List<WoWItem> _bagItems = Bag.GetBagItem();
        foreach (WoWItem item in _bagItems)
            if (list.Contains(item.Name))
                return item.Entry;

        return 0;
    }

    // Get item ID in bag from a string passed as argument (good to check CD)
    public static int GetItemID(string itemName)
    {
        List<WoWItem> _bagItems = Bag.GetBagItem();
        foreach (WoWItem item in _bagItems)
            if (itemName.Equals(item))
                return item.Entry;

        return 0;
    }

    // Get item Cooldown (must pass item string as arg)
    public static int GetItemCooldown(string itemName)
    {
        int entry = GetItemID(itemName);
        List<WoWItem> _bagItems = Bag.GetBagItem();
        foreach (WoWItem item in _bagItems)
            if (entry == item.Entry)
                return Lua.LuaDoString<int>("local startTime, duration, enable = GetItemCooldown(" + entry + "); " +
                    "return duration - (GetTime() - startTime)");

        Main.Log("Couldn't find item" + itemName);
        return 0;
    }

    // Get item Cooldown from list (must pass item list as arg)
    public static int GetItemCooldown(List<string> itemList)
    {
        int entry = GetItemID(itemList);
        List<WoWItem> _bagItems = Bag.GetBagItem();
        foreach (WoWItem item in _bagItems)
            if (entry == item.Entry)
                return Lua.LuaDoString<int>("local startTime, duration, enable = GetItemCooldown(" + entry + "); " +
                    "return duration - (GetTime() - startTime)");

        Main.Log("Couldn't find item");
        return 0;
    }

    // Uses the first item found in your bags that matches any element from the list
    public static void UseFirstMatchingItem(List<string> list)
    {
        List<WoWItem> _bagItems = Bag.GetBagItem();
        foreach (WoWItem item in _bagItems)
        {
            if (list.Contains(item.Name))
            {
                ItemsManager.UseItemByNameOrId(item.Name);
                Main.Log("Using " + item.Name);
                return;
            }
        }
    }

    #endregion
    
    #region Pet
    
    // Returns the index of the pet spell passed as argument
    public static int GetPetSpellIndex(string spellName)
    {
        int spellindex = Lua.LuaDoString<int>
            ($"for i=1,10 do " +
                "local name, _, _, _, _, _, _ = GetPetActionInfo(i); " +
                "if name == '" + spellName + "' then " +
                "return i " +
                "end " +
            "end");

        return spellindex;
    }

    // Returns the cooldown of the pet spell passed as argument
    public static int GetPetSpellCooldown(string spellName)
    {
        int _spellIndex = GetPetSpellIndex(spellName);
        return Lua.LuaDoString<int>("local startTime, duration, enable = GetPetActionCooldown(" + _spellIndex + "); return duration - (GetTime() - startTime)");
    }

    // Returns whether a pet spell is available (off cooldown)
    public static bool GetPetSpellReady(string spellName)
    {
        return GetPetSpellCooldown(spellName) <= 0;
    }

    // Casts the pet spell passed as argument
    public static void PetSpellCast(string spellName)
    {
        int spellIndex = GetPetSpellIndex(spellName);
        if (GetPetSpellReady(spellName))
            Lua.LuaDoString("CastPetAction(" + spellIndex + ");");
    }

    // Toggles Pet spell autocast (pass true as second argument to toggle on, or false to toggle off)
    public static void TogglePetSpellAuto(string spellName, bool toggle)
    {
        string spellIndex = GetPetSpellIndex(spellName).ToString();

        if (!spellIndex.Equals("0"))
        {
            bool autoCast = Lua.LuaDoString<bool>("local _, autostate = GetSpellAutocast(" + spellIndex + ", 'pet'); " +
                "return autostate == 1") || Lua.LuaDoString<bool>("local _, autostate = GetSpellAutocast('" + spellName + "', 'pet'); " +
                "return autostate == 1");

            if ((toggle && !autoCast) || (!toggle && autoCast))
            {
                Lua.LuaDoString("ToggleSpellAutocast(" + spellIndex + ", 'pet');");
                Lua.LuaDoString("ToggleSpellAutocast('" + spellName + "', 'pet');");
            }
        }
    }

    #endregion
}
