using System.Collections.Generic;
using wManager.Wow.Helpers;

public static class PetAndConsumables
{
    // Healthstones list
    public static List<string> HealthStones()
    {
        return new List<string>
        {
            "Minor Healthstone",
            "Lesser Healthstone",
            "Healthstone",
            "Greater Healthstone",
            "Major Healthstone"
        };
    }

    // Checks if we have a Healthstone
    public static bool HaveHealthstone()
    {
        if (ToolBox.HaveOneInList(HealthStones()))
            return true;
        return false;
    }

    // Use Healthstone
    public static void UseHealthstone()
    {
        ToolBox.UseFirstMatchingItem(HealthStones());
    }

    // Soulstones list
    public static List<string> SoulStones()
    {
        return new List<string>
        {
            "Minor Soulstone",
            "Lesser Soulstone",
            "Soulstone",
            "Major Soulstone",
            "Master Soulstone"
        };
    }

    // Checks if we have a Soulstone
    public static bool HaveSoulstone()
    {
        if (ToolBox.HaveOneInList(SoulStones()))
            return true;
        return false;
    }

    // Use Soulstone
    public static void UseSoulstone()
    {
        ToolBox.UseFirstMatchingItem(SoulStones());
    }

    // Returns which pet the warlock has summoned
    public static string MyWarlockPet()
    {
        return Lua.LuaDoString<string>
            ($"for i=1,10 do " +
                "local name, _, _, _, _, _, _ = GetPetActionInfo(i); " +
                "if name == 'Firebolt' then " +
                "return 'Imp' " +
                "end " +
                "if name == 'Torment' then " +
                "return 'Voidwalker' " +
                "end " +
            "end");
    }
}
