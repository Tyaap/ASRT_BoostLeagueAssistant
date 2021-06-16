using System.Collections.Generic;
using System.Linq;

namespace ASRT_BoostLeagueAssistant.Results
{
    public class Summary
    {
        static readonly string[] summaryHeadingsPoints = { "10", "10>x≥9", "9>x≥8", "8>x≥7", "7>x≥6", "6>x≥5", "5>x≥4", "4>x≥3", "3>x≥2", "2>x≥1" };

        public static Dictionary<ulong, PlayerSummary> SingleMatchdaySummary(IEnumerable<Dictionary<ulong, Record>> results)
        {
            Dictionary<ulong, PlayerSummary> summary = new Dictionary<ulong, PlayerSummary>();
            int nEvents = results.Count();
            int i = 0;
            foreach (Dictionary<ulong, Record> eventResults in results)
            {
                foreach (Record rec in eventResults.Values)
                {
                    if (!summary.TryGetValue(rec.SteamID, out PlayerSummary playerSummary))
                    {
                        playerSummary = new PlayerSummary()
                        {
                            name = rec.Name,
                            points = new decimal[nEvents],
                            positions = new int[nEvents],
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
            CalculateRanks(summary);
            return summary;
        }

        public static void UpdateMuitiMatchdaySummary(Dictionary<ulong, PlayerSummary> mmdSummary, Dictionary<ulong, PlayerSummary> mdSummary, int nPositions = 20, int nWins = 10)
        {
            foreach (var pair in mdSummary)
            {
                if (!mmdSummary.TryGetValue(pair.Key, out PlayerSummary playerSummary))
                {
                    playerSummary = new PlayerSummary()
                    {
                        name = pair.Value.name,
                        points = new decimal[10],
                        positions = new int[nPositions],
                        wins = new int[nWins],
                    };
                    mmdSummary[pair.Key] = playerSummary;
                }
                foreach (int position in pair.Value.positions)
                {
                    if (position >= 1 && position <= nPositions)
                    {
                        playerSummary.positions[position - 1]++;
                    }
                }
                foreach (decimal points in pair.Value.points)
                {
                    if (points > 0)
                    {
                        playerSummary.points[10 - (int)points]++;
                    }
                }
                playerSummary.totalPointsOld = playerSummary.totalPoints;
                playerSummary.totalPoints += pair.Value.totalPoints;
                playerSummary.totalPositions += pair.Value.totalPositions;
                playerSummary.nTracks += pair.Value.nTracks;
                if (pair.Value.rank <= nWins)
                {
                    playerSummary.wins[pair.Value.rank - 1]++;
                }
                playerSummary.parts++;
            }
        }


        public static void CalculateRanks(Dictionary<ulong, PlayerSummary> summary)
        {
            List<PlayerSummary> summaryList = new List<PlayerSummary>(summary.Values);
            summaryList.Sort(PlayerSummary.Compare);
            int nPlayers = summaryList.Count;
            for (int i = 0; i < nPlayers; i++)
            {
                summaryList[i].rank = i + 1;
            }
        }

        public static void CalculateOldRanks(Dictionary<ulong, PlayerSummary> summary)
        {
            List<PlayerSummary> summaryList = new List<PlayerSummary>(summary.Values);
            summaryList.Sort(PlayerSummary.CompareOld);
            int nPlayers = summaryList.Count;
            for (int i = 0; i < nPlayers; i++)
            {
                summaryList[i].rankOld = i + 1;
            }
        }

        public static Table MakeSummaryTable(Dictionary<ulong, PlayerSummary> summary, string summaryName = "RESULTS",
            IEnumerable<string> headings = null, bool frequencyData = false, bool usePoints = true)
        {
            Table table = new Table();
            if (!summary.Any())
            {
                return table;
            }

            // Check what information is available in the summary
            PlayerSummary firstSummary = summary.First().Value;
            bool hasTotalPoints = firstSummary.totalPoints > 0;
            bool hasTracks = firstSummary.nTracks > 0;
            bool hasParts = firstSummary.parts > 0;
            bool hasWins = firstSummary.wins != null && firstSummary.wins.Length > 0;
            bool hasPoints = firstSummary.points != null && firstSummary.points.Length > 0;
            bool hasPositions = firstSummary.positions != null && firstSummary.positions.Length > 0;
            bool hasOldRanks = false;
            bool hasOldTotalPoints = false;
            foreach (PlayerSummary playerSummary in summary.Values)
            {
                if (playerSummary.showRankDelta && playerSummary.rankOld != 0)
                {
                    hasOldRanks = true;
                }
                if (playerSummary.showPointDelta && playerSummary.totalPointsOld != 0)
                {
                    hasOldTotalPoints = true;
                }
                if (hasOldRanks && hasOldTotalPoints)
                {
                    break;
                }
            }

            // Headings
            int r = 0;
            int c = 0;
            table[r, c++] = "#";
            if (hasOldRanks)
            {
                c++;
            }
            table[r, c++] = summaryName;
            if (headings == null && frequencyData && usePoints)
            {
                // By default use point ranges
                headings = summaryHeadingsPoints;
            }
            if ((hasPoints && usePoints) || (hasPositions && !usePoints))
            {
                if (headings != null)
                {
                    foreach (string heading in headings)
                    {
                        table[r, c++] = heading;
                    }
                }
                else if (frequencyData)
                {
                    // By default use positions (maximum given in the summary)
                    int nPositions = firstSummary.positions.Length;
                    for (int i = 0; i < nPositions; i++)
                    {
                        table[r, c++] = (i + 1) + "°";
                    }
                }
                else
                {
                    // By default use the acronyms for all 21 tracks
                    foreach (Map map in Indexing.mapOrder)
                    {
                        table[r, c++] = ((MapAcronym)(uint)map).ToString();
                    }
                }
                c++;
            }
            if (hasTotalPoints)
            {
                table[r, c++] = "PTS";
            }
            if (hasOldTotalPoints)
            {
                c++;
            }
            if (hasTracks)
            {
                table[r, c++] = "Tracks";
            }
            if (hasTotalPoints && hasTracks)
            {
                table[r, c++] = "AVERAGE";
            }
            if (hasParts)
            {
                table[r, c++] = "PARTS";
            }
            if (hasWins)
            {
                int nWins = firstSummary.wins.Length;
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
                string rank = 
                table[r, c++] = playerSummary.rank + "°";
                if (hasOldRanks)
                {
                    if (playerSummary.showRankDelta && playerSummary.rankOld != 0 && (playerSummary.totalPoints > 0 || playerSummary.totalPointsOld > 0))
                    {
                        table[r, c] = Details.DeltaIndicator(playerSummary.rankOld, playerSummary.rank, inverseArrow: true);
                    }
                    c++;
                }
                table[r, c++] = playerSummary.name;
                if (usePoints && hasPoints)
                {
                    foreach (decimal points in playerSummary.points)
                    {
                        if (points != 0)
                        {
                            table[r, c] = Record.TruncatedNumString(points.ToString(), -1);
                        }
                        c++;
                    }
                    c++;
                }
                if (!usePoints && hasPositions)
                {
                    foreach (int position in playerSummary.positions)
                    {
                        if (position != 0)
                        {
                            table[r, c] = position.ToString();
                        }
                        c++;
                    }
                    c++;
                }
                if (hasTotalPoints)
                {
                    table[r, c++] = Record.TruncatedNumString(playerSummary.totalPoints.ToString(), -1);
                }
                if (hasOldTotalPoints)
                {
                    if (playerSummary.showPointDelta && playerSummary.totalPointsOld != 0)
                    {
                        table[r, c] = Details.DeltaIndicator(playerSummary.totalPointsOld, playerSummary.totalPoints, inverseArrow: false);
                    }
                    c++;
                }
                if (hasTracks)
                {
                    table[r, c++] = playerSummary.nTracks.ToString();
                }
                if (hasTotalPoints && hasTracks)
                {
                    table[r, c++] = Record.TruncatedNumString((playerSummary.totalPoints / playerSummary.nTracks * 10).ToString(), -1);
                }
                if (hasParts)
                {
                    table[r, c++] = playerSummary.parts.ToString();
                }
                if (hasWins)
                {
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

        public static void ShowDeltas(Dictionary<ulong, PlayerSummary> summary, IEnumerable<ulong> steamIds, bool pointDelta = true, bool rankDelta = true)
        {
            foreach(ulong id in steamIds)
            {
                if (summary.TryGetValue(id, out var playerSummary))
                {
                    playerSummary.showPointDelta = pointDelta;
                    playerSummary.showRankDelta = rankDelta;
                }
            }
        }
    }
}
