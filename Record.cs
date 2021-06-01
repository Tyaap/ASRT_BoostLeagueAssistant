using System;

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
        public double Score;
        public int Position;
        public double Points;
        public bool UsedExploit;


        // Best score tracking variables
        public bool ShowPosDelta; // selectively show position deltas (new PBs only)
        public int OldPosition; // position changes

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
                    s = TruncatedDecimalString(Score, 1) + "% (DNF)";
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

        public static string TruncatedDecimalString(double n, int dp)
        {
            string num = n.ToString();
            int point = num.IndexOf('.');
            if (point == -1 || point + dp + 1 > num.Length)
            {
                return num;
            }
            else if (dp == 0)
            {
                return num.Substring(0, point);
            }
            else
            {
                return num.Substring(0, point + dp + 1).TrimEnd('0').TrimEnd('.');
            }
        }

        public static int ComparePoints(Record x, Record y)
        {
            return x.Points != y.Points ? y.Points.CompareTo(x.Points) : CompareScores(x, y);
        }

        public static int CompareScores(Record x, Record y)
        {
            if (x.EventType != y.EventType || x.Map != y.Map)
            {
                return 0; // Cannot compare records from different maps/events
            }

            if (x.EventType == EventType.BattleArena || x.EventType == EventType.CaptureTheChao)
            {
                return y.Score.CompareTo(x.Score);
            }
            else if (x.EventType == EventType.BattleRace)
            {
                if (x.Completion == Completion.Eliminated && (y.Completion == Completion.DNF || y.Completion == Completion.Finished)
                    || x.Completion == Completion.DNF && y.Completion == Completion.Finished)
                {
                    return 1;
                }
                else if (y.Completion == Completion.Eliminated && (x.Completion == Completion.DNF || x.Completion == Completion.Finished)
                    || y.Completion == Completion.DNF && x.Completion == Completion.Finished)
                {
                    return -1;
                }
                else if (x.Completion == Completion.Finished) // x and y both finished
                {
                    int comp = x.Score.CompareTo(y.Score);
                    if (comp == 0)
                    {
                        return x.MatchDay.CompareTo(y.MatchDay);
                    }
                    return comp;
                }
                else // x and y both DNF or eliminated
                {
                    return y.Score.CompareTo(x.Score);
                }
            }
            else // Boost or normal race
            {
                if (x.Completion == Completion.DNF && y.Completion == Completion.Finished)
                {
                    return 1;
                }
                else if (y.Completion == Completion.DNF && x.Completion == Completion.Finished)
                {
                    return -1;
                }
                else if (x.Completion == Completion.Finished) // x and y both finished
                {
                    int comp = x.Score.CompareTo(y.Score);
                    if (comp == 0)
                    {
                        return x.MatchDay.CompareTo(y.MatchDay);
                    }
                    return comp;
                }
                else // x and y both DNF
                {
                    return y.Score.CompareTo(x.Score); // compare DNF percentages
                }
            }
        }
    }
}
