using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using robotManager.Helpful;
using robotManager.Products;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;

public class Main : ICustomClass
{
    string wowClass = ObjectManager.Me.WowClass.ToString();
    private ZEBMHunter _hunterclass = new ZEBMHunter();
    private ZEFuryWarrior _warriorclass = new ZEFuryWarrior();

    public float Range
	{
		get
        {
            switch (wowClass)
            {
                case "Hunter":
                    return _hunterclass.Range;

                case "Warrior":
                    return _warriorclass.Range;

                default:
                    return 5f;
            }
        }
    }

    public void Initialize()
    {
        switch (wowClass)
        {
            case "Hunter":
                _hunterclass.Initialize();
                break;

            case "Warrior":
                _warriorclass.Initialize();
                break;

            default:
                Logging.WriteError("Your class is not supported by TBC_ZE_AllInOne-FightClasses");
                new Thread(() => { Products.ProductStop(); }).Start();
                return;
        }
    }


    public void Dispose()
    {
        switch (wowClass)
        {
            case "Hunter":
                _hunterclass.Dispose();
                break;

            case "Warrior":
                _warriorclass.Dispose();
                break;

            default:
                return;
        }
    }

    public void ShowConfiguration()
    {
        switch (wowClass)
        {
            case "Hunter":
                _hunterclass.ShowConfiguration();
                break;

            case "Warrior":
                _warriorclass.ShowConfiguration();
                break;

            default:
                return;
        }
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
}
