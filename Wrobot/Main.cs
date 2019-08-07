using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : ICustomClass
{
    string wowClass = ObjectManager.Me.WowClass.ToString();
    private ZEBMHunter _hunterclass = new ZEBMHunter();

    public float Range
	{
		get
        {
            switch (wowClass)
            {
                case "Hunter":
                    return _hunterclass.Range;

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
        }
    }


    public void Dispose()
    {
        switch (wowClass)
        {
            case "Hunter":
                _hunterclass.Dispose();
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
        }
    }
}
