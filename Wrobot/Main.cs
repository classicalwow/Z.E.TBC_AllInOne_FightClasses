using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Drawing;

public class Main : ICustomClass
{
    private static string wowClass = ObjectManager.Me.WowClass.ToString();
    public static float settingRange = 5f;
    public static bool _isLaunched;
    private static bool _debug = true;
    private static bool _saveCalcuCombatRangeSetting = wManager.wManagerSetting.CurrentSetting.CalcuCombatRange;

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
        {
            wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = false;
            _isLaunched = true;
            type.GetMethod("Initialize").Invoke(null, null);
        }
        else
        {
            LogError("Class not supported.");
            Products.ProductStop();
        }
    }

    public void Dispose()
    {
        var type = Type.GetType(wowClass);
        if (type != null)
            type.GetMethod("Dispose").Invoke(null, null);
        _isLaunched = false;
        wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = _saveCalcuCombatRangeSetting;
    }

    public void ShowConfiguration()
    {
        var type = Type.GetType(wowClass);

        if (type != null)
            type.GetMethod("ShowConfiguration").Invoke(null, null);
        else
            LogError("Class not supported.");
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

    public static void LogDebug(string message)
    {
        if (_debug)
            Logging.WriteDebug($"[WholesomeFCTBC - {wowClass}]: { message}");
    }
}
