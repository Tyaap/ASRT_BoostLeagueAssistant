using System;
using System.Collections.Generic;

namespace ASRT_BoostLeagueAssistant.Results
{
    class PointSystem
    {
        public static void CalculatePoints(Record rec, List<Record> allRecs, List<Record> playerRecs) // compRecs must be sorted by score from smallest to largest
        {
            if (rec.Completion == Completion.Finished)
            {
                int nAll = CountRecsBeaten(rec, allRecs);          
                decimal f = allRecs.Count <= 1 ? 0 : 1 - (decimal)nAll / (allRecs.Count - 1); // generic track CDF           
                rec.Points = 1 + 9 * f;

                // bonus points
                /*
                if (!rec.UsedExploit) // no bonus for times that used exploits!
                {
                    int nPlayer = CountRecsBeaten(rec, playerRecs);
                    decimal g = playerRecs.Count <= 1 ? 0 : 1 - (decimal)nPlayer / (playerRecs.Count - 1);  // player track CDF
                    rec.Bonus = 8 * Math.Max(0, g + (f - g) / (decimal)Math.Sqrt(playerRecs.Count) - 0.75m);
                    rec.Bonus = (int)g + (int)f;
                }
                */
            }
            else
            {
                rec.Points = 1;
            }
            rec.Points += rec.Bonus;
        }

        public static int CountRecsBeaten(Record rec, List<Record> recsToBeat)
        {
            int n = 0;
            foreach (Record recToBeat in recsToBeat)
            {
                if (rec.Score <= recToBeat.Score)
                {
                    return n;
                }
                n++;
            }
            return n;
        }


        // adds the new matchday records to the cumulative records (and resorts)
        // then calculates points for new records
        public static void AddAndCalcPoints(Dictionary<Map, List<Record>> newRecs,
            Dictionary<Map, List<Record>> mapToRecs,                             // point calculations
            Dictionary<Map, Dictionary<ulong, List<Record>>> mapToSteamIdToRecs) // bonus point calculations
        {
            foreach (var pair in newRecs)
            {
                if (!mapToRecs.TryGetValue(pair.Key, out List<Record> recs))
                {
                    recs = new List<Record>();
                    mapToRecs[pair.Key] = recs;
                }
                if (!mapToSteamIdToRecs.TryGetValue(pair.Key, out Dictionary<ulong, List<Record>> steamIdToRecs))
                {
                    steamIdToRecs = new Dictionary<ulong, List<Record>>();
                    mapToSteamIdToRecs[pair.Key] = steamIdToRecs;
                }
                AddAndCalcPoints(pair.Value, recs, steamIdToRecs);
            }
        }

        public static void AddAndCalcPoints(List<Record> newRecs,
            List<Record> allRecs,
            Dictionary<ulong, List<Record>> playerGroupedRecs,
            bool onlyAddValidTimes = true)
        {
            // add time
            if (onlyAddValidTimes)
            {
                allRecs.AddRange(newRecs.FindAll(rec => !rec.UsedExploit && rec.Completion == Completion.Finished));
            }
            else
            {
                allRecs.AddRange(newRecs);
            }
            allRecs.Sort((x, y) => x.Score.CompareTo(y.Score)); // order times from smallest to largest

            foreach (Record rec in newRecs)
            {
                // add time to player group
                if (!playerGroupedRecs.TryGetValue(rec.SteamID, out List<Record> playerRecs))
                {
                    playerRecs = new List<Record>();
                    playerGroupedRecs[rec.SteamID] = playerRecs;
                }
                if (!onlyAddValidTimes || (!rec.UsedExploit && rec.Completion == Completion.Finished))
                {
                    playerRecs.Add(rec);
                    playerRecs.Sort((x, y) => x.Score.CompareTo(y.Score));
                }

                // calculate points
                CalculatePoints(rec, allRecs, playerRecs);
            }
        }
    }
}
