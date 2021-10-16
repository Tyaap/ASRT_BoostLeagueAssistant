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
        public decimal Score;
        public int Position;
        public decimal Points;
        public decimal Bonus;
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
                    s = TruncatedTimeString(Score, -1);
                }
                else if (Score <= 100 && Score >= 0)
                {
                    s = TruncatedNumString(Score.ToString(), 1) + "% (DNF)";
                }
                else
                {
                    s = "DNF";
                }
            }
            else
            {
                s = TruncatedTimeString(Score, -1);
                if (Completion == Completion.Finished)
                {
                    s += " (finished)";
                }
            }
            return s;
        }

        public static string TruncatedTimeString(decimal time, int dp)
        {
            string s = ((int)time / 60) + ":" + ((int)time % 60).ToString("00");
            if (dp == 0)
            {
                return s;
            }
            string tmp = TruncatedNumString(time.ToString(), dp);
            int point = tmp.IndexOf('.');
            if (point == -1)
            {
                return s;
            }
            return s + tmp[point..];
        }

        public static string TruncatedNumString(string s, int dp)
        {
            if (dp < 0)
            {
                return s;
            }
            int point = s.IndexOf('.');
            int numLen = point + dp + 1;
            if (point == -1 || numLen > s.Length)
            {
                return s;
            }
            else if (dp == 0)
            {
                return s.Substring(0, point);
            }
            else
            {
                return s.Substring(0, numLen).TrimEnd('0').TrimEnd('.');
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
