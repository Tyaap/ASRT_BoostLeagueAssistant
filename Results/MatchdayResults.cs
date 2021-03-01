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

        static readonly string[] summaryHeadingsPoints = { "10", "10>x≥9", "9>x≥8", "8>x≥7", "7>x≥6", "6>x≥5", "5>x≥4", "4>x≥3", "3>x≥2", "2>x≥1" };

        public static Dictionary<ulong, PlayerSummary> SingleMatchdaySummary(IEnumerable<IEnumerable<Record>> results)
        {
            Dictionary<ulong, PlayerSummary> summary = new Dictionary<ulong, PlayerSummary>();
            int nEvents = results.Count();
            int i = 0;
            foreach (IEnumerable<Record> eventResults in results)
            {
                foreach (Record rec in eventResults)
                {
                    if (!summary.TryGetValue(rec.SteamID, out PlayerSummary playerSummary))
                    {
                        playerSummary = new PlayerSummary() { 
                            name = rec.Name,
                            points = new double[nEvents], 
                            positions = new int[nEvents]
                        };
                        summary[rec.SteamID] = playerSummary;
                    }
                    playerSummary.points[i] = rec.Points;
                    playerSummary.positions[i] = rec.Position;
                    playerSummary.totalPoints += rec.Points;
                    playerSummary.totalPositions += rec.Position;
                    playerSummary.nTracks++;
                }
                i++;
            }
            // Calculate rankings
            CalculateSummaryRanks(summary);
            return summary;
        }

        public static Dictionary<ulong, PlayerSummary> MultiMatchdaySummary(IEnumerable<Dictionary<ulong, PlayerSummary>> matchdaySummarys, int nPositions = 20, int nWins = 10, bool calculateOldRanks = true)
        {
            Dictionary<ulong, PlayerSummary> summary = new Dictionary<ulong, PlayerSummary>();
            List<PlayerSummary> summaryList = new List<PlayerSummary>();
            int nMatchdays = matchdaySummarys.Count();
            int i = 0;
            foreach (Dictionary<ulong, PlayerSummary> mdSummary in matchdaySummarys)
            {
                if (calculateOldRanks && i == nMatchdays - 2)
                {
                    CalculateSummaryRanks(summary); // calculate old rankings
                }

                foreach (var pair in mdSummary)
                {
                    if (!summary.TryGetValue(pair.Key, out PlayerSummary playerSummary))
                    {
                        playerSummary = new PlayerSummary()
                        {
                            name = pair.Value.name,
                            points = new double[10],
                            positions = new int[nPositions],
                            wins = new int[nWins],
                        };
                        summary[pair.Key] = playerSummary;
                        summaryList.Add(playerSummary);
                    }
                    foreach (int position in pair.Value.positions)
                    {
                        if (position >= 1 && position <= nPositions)
                        {
                            playerSummary.positions[position - 1]++;
                        }
                    }
                    foreach (double points in pair.Value.points)
                    {
                        if (points > 0)
                        {
                            playerSummary.points[10 - (int)points]++;
                        }
                    }
                    playerSummary.totalPoints += pair.Value.totalPoints;
                    playerSummary.totalPositions += pair.Value.totalPositions;
                    playerSummary.nTracks += pair.Value.nTracks;
                    if (pair.Value.rank <= nWins)
                    {
                        playerSummary.wins[pair.Value.rank - 1]++;
                    }
                    playerSummary.parts++;
                    if (calculateOldRanks && i == nMatchdays - 2)
                    {
                        playerSummary.oldRank = playerSummary.rank;
                    }
                }
                i++;
            }
            // Calculate current rankings
            CalculateSummaryRanks(summary);
            return summary;
        }

        public static void CalculateSummaryRanks(Dictionary<ulong, PlayerSummary> summary)
        {
            List<PlayerSummary> summaryList = new List<PlayerSummary>(summary.Values);
            summaryList.Sort(PlayerSummary.Compare);
            int nPlayers = summaryList.Count;
            for (int i = 0; i < nPlayers; i++)
            {
                summaryList[i].rank = i + 1;
            }
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
                recs.Sort(Record.ComparePoints);
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

        public static Table MakeSummaryTable(Dictionary<ulong, PlayerSummary> summary,
            bool singleMatchday = true, string summaryName = "RESULTS", IEnumerable<string> headings = null, bool usePoints = true)
        {
            
            Table table = new Table();

            // Headings
            int r = 0;
            int c = 1;
            table[r, c++] = summaryName;
            if (headings == null && !singleMatchday && usePoints)
            {
                // By default use point ranges
                headings = summaryHeadingsPoints;
            }
            if (headings != null)
            {
                foreach (string heading in headings)
                {
                    table[r, c++] = heading;
                }
            }
            else if (singleMatchday)
            {
                // By default use the acronyms for all 21 tracks
                foreach (Map map in mapOrder)
                {
                    table[r, c++] = ((MapAcronym)(uint)map).ToString();
                }
            }
            else
            {
                // By default use positions (maximum given in the summary)
                int nPositions = summary.Any() ? summary.First().Value.positions.Length : 0;
                for (int i = 0; i < nPositions; i++)
                {
                    table[r, c++] = (i + 1) + "°";
                }
            }
            c++;
            table[r, c++] = "PTS";
            table[r, c++] = "Tracks";
            table[r, c++] = "AVERAGE";
            if (!singleMatchday)
            {
                table[r, c++] = "PARTS";
                int nWins = summary.Any() ? summary.First().Value.wins.Length : 0;
                for (int i = 0; i < nWins; i++)
                {
                    table[r, c++] = (i + 1) + "°";
                }
            }

            // Data
            foreach (PlayerSummary playerSummary in summary.Values)
            {
                r = playerSummary.rank;
                c = 0;
                string rank = playerSummary.rank + "°";
                if (!singleMatchday && playerSummary.oldRank != 0)
                {
                    if (playerSummary.rank == playerSummary.oldRank)
                    {
                        rank += " ●";
                    }
                    else if (playerSummary.rank < playerSummary.oldRank)
                    {
                        rank += " ▲" + (playerSummary.oldRank - playerSummary.rank);
                    }
                    else
                    {
                        rank += " ▼" + (playerSummary.rank - playerSummary.oldRank);
                    }
                }
                table[r, c++] = rank;
                table[r, c++] = playerSummary.name;
                if (usePoints)
                {
                    foreach(double points in playerSummary.points)
                    {
                        if (points != 0)
                        {
                            table[r, c] = Record.TruncatedDecimalString(points, 3);
                        }
                        c++;
                    }
                }
                else
                {
                    foreach (int position in playerSummary.positions)
                    {
                        if (position != 0)
                        {
                            table[r, c] = position.ToString();
                        }
                        c++;
                    }
                }
                c++;
                table[r, c++] = Record.TruncatedDecimalString(playerSummary.totalPoints, 3);
                table[r, c++] = playerSummary.nTracks.ToString();
                table[r, c++] = Record.TruncatedDecimalString((playerSummary.totalPoints / playerSummary.nTracks * 10), 3);
                if (!singleMatchday)
                {
                    table[r, c++] = playerSummary.parts.ToString();
                    foreach (int win in playerSummary.wins)
                    {
                        if (win != 0)
                        {
                            table[r, c] = win.ToString();
                        }
                        c++;
                    }
                }
            }

            return table;
        }

        public static Table MakeDetailsTable(IEnumerable<List<Record>> results, int eventsPerRow = 1)
        {
            Table table = new Table();
            int r = 0;
            int c = 0;
            int er = 0; // events on current row
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
                table[r, c] = Record.TruncatedDecimalString(rec.Points, 3);
                c -= 4;
            }
        }
    }
}
