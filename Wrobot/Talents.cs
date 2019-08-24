using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Talents
{
    private static bool _isAssigning = false;
    private static bool _isInitialized = false;
    public static string[] _talentsCodes = new string[] { };
    private static int _talentTimer = 60000 * 5; // 5 minutes

    // Talent initialization
    public static void InitTalents(bool assignTalents, bool useDefaultTalents, string[] customTalentsCodes)
    {
        if (assignTalents)
        {
            if (useDefaultTalents)
            {
                SetTalentCodes();
                Main.Log("Your are using the following default talents build:");
            }
            else
            {
                SetTalentCodes(customTalentsCodes);
                Main.Log("Your are using the following custom talents build:");
            }
            
            if (_talentsCodes.Count() > 0)
                Main.Log(_talentsCodes.Last());
            else
                Main.LogError("No talent code");

            _isInitialized = true;
        }
    }

    // Set the default talents codes to use
    public static void SetTalentCodes()
    {
        switch (Main.wowClass)
        {
            // FURY WARRIOR
            case "Warrior":
                _talentsCodes = new string[]
                {
                    "000000000000000000000000505000500501000000000000000000000000000000",
                    "000000000000000000000000505000540501005010000000000000000000000000",
                    "000000000000000000000000505000540501005310000000000000000000000000",
                    "000000000000000000000000505000550501005310510000000000000000000000",
                    "323200113020000000000002505000551501005310510000000000000000000000"
                };
                break;

            // AFFLICTION WARLOCK
            case "Warlock":
                _talentsCodes = new string[]
                {
                    "0502210010000000000000000000000000000000000000000000000000000000",
                    "0502222110230100000000000000000000000000000000000000000000000000",
                    "0502222110234105500000000000000000000000000000000000000000000000",
                    "0502222510234105510010000000000000000000000000000000000000000000",
                    "0502222510234105510010052300100000000000000000000000000000000000",
                    "0502222510234105510010052300100000000000000000000000000000000000",
                    "1502222510235105510010052330130100000000000000000000000000000000"
                };
                break;

            // BM HUNTER
            case "Hunter":
                _talentsCodes = new string[]
                {
                    "5020122120501000000000000000000000000000000000000000000000000000",
                    "5020122122501205010000000000000000000000000000000000000000000000",
                    "5020122142501225010510000000000000000000000000000000000000000000",
                    "5020122142501225010510550200000000000000000000000000000000000000",
                    "5020322152501225010510555200000000000000000000000000000000000000"

                };
                break;

            // COMBAT ROGUE
            case "Rogue":
                _talentsCodes = new string[]
                {
                    "0000000000000000000000230050000000000000000000000000000000000000000",
                    "0000000000000000000000230550100040100000000000000000000000000000000",
                    "0000000000000000000000230550100050130020000000000000000000000000000",
                    "0000000000000000000000230550100050150021010000000000000000000000000",
                    "0000000000000000000000230550100050150023210510000000000000000000000",
                    "0000000000000000000000230550100050150023210515000300000000000000000",
                    "3053001000000000000000230550100050150023210515000300000000000000000"

                };
                break;

            // ENHANCEMENT SHAMAN
            case "Shaman":
                _talentsCodes = new string[]
                {
                    "0000000000000000000050052301040010000000000000000000000000000",
                    "0000000000000000000050052301050013050110000000000000000000000",
                    "0000000000000000000050052301050013053115100000000000000000000",
                    "2500310000000000000050052321450013353115100000000000000000000"
                };
                break;

            // FERAL DRUID
            case "Druid":
                _talentsCodes = new string[]
                {
                    "00000000000000000000050302203002000000000000000000000000000000",
                    "00000000000000000000050302203032010000000000000000000000000000",
                    "00000000000000000000050302203032212500120000000000000000000000",
                    "00000000000000000000050302203032212520125100000000000000000000",
                    "00000000000000000000050302203032212520125105050001000000000000",
                    "00000000000000000000052303203232212530125105053001000000000000"
                };
                break;

            // FROST MAGE
            case "Mage":
                _talentsCodes = new string[]
                {
                    "0000000000000000000000000000000000000000000000500320010000000000000",
                    "0000000000000000000000000000000000000000000000504320010005010000000",
                    "0000000000000000000000000000000000000000000000505323010005010051000",
                    "0000000000000000000000000000000000000000000000505323310005010051241",
                    "0000000000000000000000000000000000000000000000535323310035013251551",
                    "0000000000000000000000005000000000000000000000535323310035013251551"
                };
                break;

            // RETRIBUTION PALADIN
            case "Paladin":
                _talentsCodes = new string[]
                {
                    "0000000000000000000000000000000000000000000523005130000000000000",
                    "0000000000000000000000000000000000000000001523005130000100000000",
                    "0000000000000000000000000000000000000000001523005130003115321041",
                    "5500300000000000000000000000000000000000005523005130003125331051"
                };
                break;

            // SHADOW PRIEST
            case "Priest":
                _talentsCodes = new string[]
                {
                    "0000000000000000000000000000000000000000000500230010000000000000",
                    "0000000000000000000000000000000000000000000500232310041120000000",
                    "0000000000000000000000000000000000000000000500232310041121051451",
                    "0500320130000000000000000000000000000000000500232510051123051551"
                };
                break;

            default:
                break;
        }
    }

    // Set the custom talents codes to use
    public static void SetTalentCodes(string[] talentsCodes)
    {
        _talentsCodes = talentsCodes;
    }

    // Talent pulse
    public static void DoTalentPulse(object sender, DoWorkEventArgs args)
    {
        while (Main._isLaunched)
        {
            Thread.Sleep(3000);
            try
            {
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause && !ObjectManager.Me.InCombatFlagOnly 
                    && ObjectManager.Me.IsAlive && Main._isLaunched && !_isAssigning && _isInitialized)
                {
                    Main.LogDebug("Assigning Talents");
                    _isAssigning = true;
                    AssignTalents(_talentsCodes);
                    _isAssigning = false;
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError(string.Concat(arg), true);
            }
            Thread.Sleep(_talentTimer);
        }
    }

    // Talent assignation 
    public static void AssignTalents(string[] TalentCodes)
    {
        // Number of talents in each tree
        List<int> NumTalentsInTrees = new List<int>()
        {
            Lua.LuaDoString<int>("return GetNumTalents(1)"),
            Lua.LuaDoString<int>("return GetNumTalents(2)"),
            Lua.LuaDoString<int>("return GetNumTalents(3)")
        };

        if (!_isInitialized)
        {
            Thread.Sleep(500);
        }
        else if (TalentCodes.Count() <= 0)
        {
            Main.LogError("No talent code");
        }
        else if (Lua.LuaDoString<int>("local unspentTalentPoints, _ = UnitCharacterPoints('player'); return unspentTalentPoints;") <= 0)
        {
            Main.LogDebug("No talent point to spend");
        }
        else
        {
            bool _stop = false;

            // Loop for each TalentCode in list
            foreach (string talentsCode in TalentCodes)
            {
                if (_stop)
                    break;

                // check if talent code length is correct
                if ((NumTalentsInTrees[0] + NumTalentsInTrees[1] + NumTalentsInTrees[2]) != talentsCode.Length)
                {
                    Main.LogError("WARNING: Your talents code length is incorrect. Please use " +
                        "http://armory.twinstar.cz/talent-calc.php to generate valid codes.");
                    Main.LogError("Talents code : " + talentsCode);
                    _stop = true;
                    break;
                }

                // TalentCode per tree
                List<string> TalentCodeTrees = new List<string>()
                {
                    talentsCode.Substring(0, NumTalentsInTrees[0]),
                    talentsCode.Substring(NumTalentsInTrees[0], NumTalentsInTrees[1]),
                    talentsCode.Substring(NumTalentsInTrees[0] + NumTalentsInTrees[1], NumTalentsInTrees[2])
                };

                // loop in 3 trees
                for (int k = 1; k <= 3; k++)
                {
                    if (_stop)
                        break;

                    // loop for each talent
                    for (int i = 0; i < NumTalentsInTrees[k - 1]; i++)
                    {
                        if (_stop)
                            break;

                        int _talentNumber = i + 1;
                        string _talentName = Lua.LuaDoString<string>("local name, _, _, _, _, _, _, _ = GetTalentInfo(" + k + ", " + _talentNumber + "); return name;");
                        int _currentRank = Lua.LuaDoString<int>("_, _, _, _, currentRank, _, _, _ = GetTalentInfo(" + k + ", " + _talentNumber + "); return currentRank;");
                        int _realMaxRank = Lua.LuaDoString<int>("_, _, _, _, _, maxRank, _, _ = GetTalentInfo(" + k + ", " + _talentNumber + "); return maxRank;");

                        int _pointsToAssignInTalent = System.Convert.ToInt16(TalentCodeTrees[k - 1].Substring(i, 1));

                        if (_currentRank > _pointsToAssignInTalent && TalentCodes.Last().Equals(talentsCode))
                        {
                            Main.LogError("WARNING: Your assigned talent points don't match your talent code. Please reset your talents or review your talents code." +
                                " You have " + _currentRank + " point(s) in " + _talentName + " where you should have " + _pointsToAssignInTalent + " point(s)");
                            Main.LogError("Talents code : " + talentsCode);
                            _stop = true;
                        }
                        else if (_pointsToAssignInTalent > _realMaxRank)
                        {
                            Main.LogError($"WARNING : You're trying to assign {_pointsToAssignInTalent} points into {_talentName}," +
                                $" maximum is {_realMaxRank} points for this talent. Please check your talent code.");
                            Main.LogError("Talents code : " + talentsCode);
                            _stop = true;
                        }
                        else if (_currentRank != _pointsToAssignInTalent)
                        {
                            // loop for individual talent rank
                            for (int j = 0; j < _pointsToAssignInTalent - _currentRank; j++)
                            {
                                if (!Main._isLaunched)
                                    _stop = true;
                                if (_stop)
                                    break;
                                Lua.LuaDoString("LearnTalent(" + k + ", " + _talentNumber + ")");
                                Thread.Sleep(500 + Usefuls.Latency);
                                int _newRank = Lua.LuaDoString<int>("_, _, _, _, currentRank, _, _, _ = GetTalentInfo(" + k + ", " + _talentNumber + "); return currentRank;");
                                Main.Log("Assigned talent: " + _talentName + " : " + _newRank + "/" + _pointsToAssignInTalent, Color.SteelBlue);
                                if (Lua.LuaDoString<int>("local unspentTalentPoints, _ = UnitCharacterPoints('player'); return unspentTalentPoints;") <= 0)
                                    _stop = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
