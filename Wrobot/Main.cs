using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using robotManager.Helpful;
using robotManager.Products;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Drawing;
using System.Threading;

public class Main : ICustomClass
{
    private static string wowClass = ObjectManager.Me.WowClass.ToString();
    public static float settingRange = 5f;

    public float Range
	{
		get
        {
            return settingRange;
        }
    }

    public void Initialize()
    {
        Log("Started. Discovering class and finding rotation...");
        var type = Type.GetType(wowClass);

        if (type != null)
            type.GetMethod("Initialize").Invoke(null, null);
        else
        {
            LogError("Class not supported.");
            new Thread(() =>{ Products.ProductStop(); }).Start();
        }
    }


    public void Dispose()
    {
        var type = Type.GetType(wowClass);
        if (type != null)
            type.GetMethod("Dispose").Invoke(null, null);
    }

    public void ShowConfiguration()
    {
        var type = Type.GetType(wowClass);

        if (type != null)
            type.GetMethod("ShowConfiguration").Invoke(null, null);
        else
            LogError("Class not supported.");
    }

    private string GetSpec()
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

    public static void LogFight(string message)
    {
        Logging.Write($"[WholesomeFCTBC - {wowClass}]: { message}", Logging.LogType.Fight, Color.ForestGreen);
    }
    public static void LogError(string message)
    {
        Logging.Write($"[WholesomeFCTBC - {wowClass}]: {message}", Logging.LogType.Error, Color.DarkRed);
    }
    public static void Log(string message)
    {
        Logging.Write($"[WholesomeFCTBC - {wowClass}]: {message}");
    }
}
