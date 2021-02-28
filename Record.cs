using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRT_BoostLeagueAssistant
{
    public class Record
    {
        public int Index;
        public int Year;
        public int MatchDay;
        public string LobbyName;
        public int EventNum;
        public DateTime EventTime;
        public EventType EventType;
        public Map Map;
        public string Name;
        public ulong SteamID;
        public Character Character;
        public Completion Completion;
        public float Score;
        public int Position;
        public float Points;
        public bool UsedExploit;


        public string ScoreString()
        {
            string s;
            if (EventType == EventType.CaptureTheChao)
            {
                s = Score.ToString();
            }
            else if (EventType != EventType.BattleRace)
            {
                if (Completion != Completion.DNF)
                {
                    s = TimeSpan.FromSeconds(Score).ToString(@"m\:ss\.fff");
                }
                else if (Score <= 100 && Score >= 0)
                {
                    s = TruncatedFloatString(Score, 1) + "% (DNF)";
                }
                else
                {
                    s = "DNF";
                }
            }
            else
            {
                s = TimeSpan.FromSeconds(Score).ToString(@"m\:ss\.fff");
                if (Completion == Completion.Finished)
                {
                    s += " (finished)";
                }
            }
            return s;
        }

        public static string TruncatedFloatString(float n, int decimals)
        {
            string num = n.ToString();
            int dp = num.IndexOf('.');
            if (dp == -1)
            {
                return num;
            }
            else if (decimals == 0)
            {
                return num.Substring(0, dp);
            }
            else
            {
                return num.Substring(0, dp + decimals + 1);
            }
        }
    }
}
