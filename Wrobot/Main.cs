using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

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
        switch(wowClass)
        {
            case "Hunter":
                _hunterclass.Initialize();
                break;

            case "Warrior":
                _warriorclass.Initialize();
                break;
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
        }
    }
}
