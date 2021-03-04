using System.Collections.Generic;

namespace ASRT_BoostLeagueAssistant.Results
{
    class BestScores
    {
        static readonly Map[] mapOrder = {
            Map.OceanView, Map.SambaStudios, Map.CarrierZone, Map.DragonCanyon,
            Map.TempleTrouble, Map.GalacticParade, Map.SeasonalShrines, Map.RoguesLanding,
            Map.DreamValley, Map.ChillyCastle, Map.GraffitiCity, Map.SanctuaryFalls,
            Map.GraveyardGig, Map.AddersLair, Map.BurningDepths, Map.RaceOfAges,
            Map.EggHangar, Map.RouletteRoad, Map.ShibuyaDowntown, Map.SunshineTour,
            Map.OutrunBay };


        public static Dictionary<Map, Dictionary<ulong, Record>> GetPlayerBestScores(IEnumerable<IEnumerable<Dictionary<ulong, Record>>> results) // results[matchday index][map index][steam ID] = record
        {
            Dictionary<Map, Dictionary<ulong, Record>> playerBestScores = new Dictionary<Map, Dictionary<ulong, Record>>();
            foreach (var matchdayResults in results)
            {
                UpdatePlayerBestScores(playerBestScores, matchdayResults);
            }      
            return playerBestScores;
        }

        public static void UpdatePlayerBestScores(Dictionary<Map, Dictionary<ulong, Record>> playerBestScores, IEnumerable<Dictionary<ulong, Record>> matchdayResults)
        {
            foreach (var mapResults in matchdayResults)
            {
                foreach (Record rec in mapResults.Values)
                {
                    if (rec.UsedExploit || rec.Completion != Completion.Finished) // Filter out unwanted scores
                    {
                        continue;
                    }
                    if (!playerBestScores.TryGetValue(rec.Map, out Dictionary<ulong, Record> mapScores))
                    {
                        playerBestScores[rec.Map] = new Dictionary<ulong, Record>() { { rec.SteamID, rec } };
                    }
                    else if (!mapScores.TryGetValue(rec.SteamID, out Record rec2) || Record.CompareScores(rec, rec2) < 0)
                    {
                        mapScores[rec.SteamID] = rec;
                    }
                }
            }
        }

        public static void UpdateBestScoreProgression(Dictionary<Map, List<Record>> bestScoreProgression, IEnumerable<Dictionary<ulong, Record>> matchdayResults)
        {
            foreach (Dictionary<ulong, Record> mapResults in matchdayResults)
            {
                Record rec = BestValidScore(mapResults);
                if (rec == null)
                {
                    continue;
                }
                if (!bestScoreProgression.TryGetValue(rec.Map, out List<Record> mapGroup))
                {
                    bestScoreProgression[rec.Map] = new List<Record>() { rec };
                }
                else if (Record.CompareScores(rec, mapGroup[0]) < 0)
                {
                    mapGroup.Insert(0, rec);
                }
            }
        }

        public static Record BestValidScore(Dictionary<ulong, Record> recs) // assumes records have updated positions
        {
            Record recBest = null;
            foreach (Record rec in recs.Values)
            {
                if(!rec.UsedExploit && rec.Completion == Completion.Finished && (recBest == null || recBest.Position < rec.Position))
                {
                    recBest = rec;
                    if (rec.Position == 1)
                    {
                        return rec;
                    }
                }
            }
            return recBest;
        }

        public static List<List<Record>> GetOrderedProgressionResults(Dictionary<Map, List<Record>> bestScoreProgression, IEnumerable<Map> mapOrder = null)
        {
            if (mapOrder == null)
            {
                mapOrder = BestScores.mapOrder;
            }
            List<List<Record>> orderedResults = new List<List<Record>>();
            foreach (Map map in mapOrder)
            {
                if (!bestScoreProgression.TryGetValue(map, out List<Record> results))
                {
                    results = new List<Record>();
                }
                orderedResults.Add(results);
            }
            return orderedResults;
        }
    }
}
