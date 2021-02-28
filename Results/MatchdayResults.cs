using System;
using System.Collections.Generic;
using System.Linq;

namespace ASRT_BoostLeagueAssistant
{
    class MatchdayResults
    {
        static readonly Map[] mapOrder = {
            Map.OceanView, Map.SambaStudios, Map.CarrierZone, Map.DragonCanyon,
            Map.TempleTrouble, Map.GalacticParade, Map.SeasonalShrines, Map.RoguesLanding,
            Map.DreamValley, Map.ChillyCastle, Map.GraffitiCity, Map.SanctuaryFalls,
            Map.GraveyardGig, Map.AddersLair, Map.BurningDepths, Map.RaceOfAges,
            Map.EggHangar, Map.RouletteRoad, Map.ShibuyaDowntown, Map.SunshineTour,
            Map.OutrunBay };


        public static List<PlayerSummary> GetBreakdown(IEnumerable<IEnumerable<Record>> results)
        {
            Dictionary<ulong, PlayerSummary> summaryDict = new Dictionary<ulong, PlayerSummary>();
            List<PlayerSummary> summaryList = new List<PlayerSummary>();
            int nEvents = results.Count();
            int i = 0;
            foreach (IEnumerable<Record> eventResults in results)
            {
                foreach (Record rec in eventResults)
                {
                    if (!summaryDict.TryGetValue(rec.SteamID, out PlayerSummary playerSummary))
                    {
                        playerSummary = new PlayerSummary() { 
                            name = rec.Name,
                            points = new double[nEvents], 
                            positions = new int[nEvents]
                        };
                        summaryDict[rec.SteamID] = playerSummary;
                        summaryList.Add(playerSummary);
                    }
                    playerSummary.points[i] = rec.Points;
                    playerSummary.positions[i] = rec.Position;
                    playerSummary.totalPoints += rec.Points;
                    playerSummary.totalPositions += rec.Position;
                    playerSummary.nTracks++;
                }
                i++;
            }
            summaryList.Sort((x, y) => y.totalPoints.CompareTo(x.totalPoints));
            return summaryList;
        }

        public static Dictionary<(int, Map), List<Record>> MatchdayMapToResults(List<Record> data)
        {
            Dictionary<(int, Map), Dictionary<ulong, Record>> matchdayMapToIdDict = Indexing.MatchdayMapToSteamIdToRecord(data);
            Dictionary<(int, Map), List<Record>> matchdayMapToResults = new Dictionary<(int, Map), List<Record>>();
            foreach (var key in matchdayMapToIdDict.Keys)
            {
                List<Record> recs;
                if (key.Item2 == Map.RaceOfAges || key.Item2 == Map.RaceOfAgesAlt1 || key.Item2 == Map.RaceOfAgesAlt2 || key.Item2 == Map.RaceOfAgesAlt3)
                {
                    if (matchdayMapToResults.TryGetValue((key.Item1, Map.RaceOfAges), out recs))
                    {
                        recs.AddRange(matchdayMapToIdDict[key].Values);
                    }
                    else
                    {
                        recs = new List<Record>(matchdayMapToIdDict[key].Values);
                        matchdayMapToResults[(key.Item1, Map.RaceOfAges)] = recs;
                    }
                }
                else
                {
                    recs = new List<Record>(matchdayMapToIdDict[key].Values);
                    matchdayMapToResults[key] = recs;
                }
                recs.Sort((x, y) => y.Points.CompareTo(x.Points));
                int nRecs = recs.Count;
                for (int i = 0; i < nRecs; i++)
                {
                    recs[i].Position = i + 1;
                }
            }
            return matchdayMapToResults;
        }



        public static List<List<Record>> GetOrderedMatchdayResults(int matchday, Dictionary<(int, Map), List<Record>> matchdayMapToResults, IEnumerable<Map> mapOrder = null)
        {
            if (mapOrder == null)
            {
                mapOrder = MatchdayResults.mapOrder;
            }
            List<List<Record>> orderedResults = new List<List<Record>>();
            foreach (Map map in mapOrder)
            {
                if (!matchdayMapToResults.TryGetValue((matchday, map), out List<Record> results))
                {
                    results = new List<Record>();
                }
                orderedResults.Add(results);
            }
            return orderedResults;
        }

        public static int GetMatchdayYear(int matchday, IEnumerable<IEnumerable<Record>> orderedResults)
        {
            foreach(List<Record> eventResults in orderedResults)
            {
                foreach(Record rec in eventResults)
                {
                    return rec.Year;
                }
            }
            return 0;
        }

        public static Table MakeSummaryTable(IEnumerable<PlayerSummary> breakdown, IEnumerable<Map> mapOrder = null, bool usePoints = true)
        {
            // Headings
            if (mapOrder == null)
            {
                mapOrder = MatchdayResults.mapOrder;
            }
            Table table = new Table();
            int r = 0;
            int c = 1;

            table[r, c++] = "RESULTS";
            foreach (Map map in mapOrder)
            {
                table[r, c++] = ((MapAcronym)(uint)map).ToString();
            }
            c++;
            table[r, c++] = "PTS";
            table[r, c++] = "Tracks";
            table[r, c] = "AVERAGE";

            // Data
            foreach (PlayerSummary summary in breakdown)
            {
                r++;
                c = 0;
                table[r, c++] = r + "°";
                table[r, c++] = summary.name;
                if (usePoints)
                {
                    foreach(double points in summary.points)
                    {
                        if (points != 0)
                        {
                            table[r, c] = points.ToString();
                        }
                        c++;
                    }
                }
                else
                {
                    foreach (int position in summary.positions)
                    {
                        if (position != 0)
                        {
                            table[r, c] = position.ToString();
                        }
                        c++;
                    }
                }
                c++;
                table[r, c++] = summary.totalPoints.ToString();
                table[r, c++] = summary.nTracks.ToString();
                table[r, c] = (summary.totalPoints / summary.nTracks * 10).ToString();
            }

            return table;
        }

        public static Table MakeDetailsTable(IEnumerable<List<Record>> results, int eventsPerRow = 1)
        {
            Table table = new Table();
            int r = 0;
            int c = 0;
            int er = 0;
            foreach(List<Record> eventResults in results)
            {
                InsertEventDetails(r, c, eventResults, table);
                er++;
                if (er == eventsPerRow)
                {
                    r = table.RowCount + 2;
                    c = 0;
                    er = 0;
                }
                else
                {
                    c += 6;
                }
            }
            return table;
        }

        public static void InsertEventDetails(int r, int c, IEnumerable<Record> eventResults, Table table)
        {
            if (!eventResults.Any())
            {
                return;
            }

            // Event info
            table[r, c++] = eventResults.First().Map.GetDescription();
            table[r++, c] = eventResults.First().EventType.GetDescription();

            // Headings
            c--;
            table[r, c++] = "Position";
            table[r, c++] = "Name";
            table[r, c++] = "Time";
            table[r, c++] = "Character";
            table[r, c] = "Points";

            // Data
            c -= 4;
            foreach(Record rec in eventResults)
            {
                r++;
                table[r, c++] = rec.Position + "°";
                table[r, c++] = rec.Name;
                table[r, c++] = rec.ScoreString();
                table[r, c++] = rec.Character.GetDescription();
                table[r, c] = rec.Points.ToString();
                c -= 4;
            }
        }
    }
}
