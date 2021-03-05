using System;
using System.Collections.Generic;
using System.Linq;
using ASRT_BoostLeagueAssistant.Results;

namespace ASRT_BoostLeagueAssistant
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            // Load data
            string root = "C:\\Users\\TomAlex\\Desktop\\Boost League";
            (List<Record> data, List<(int, int)> counts) = DataReader.ReadLogs(root);
            PointSystem.CalculatePoints(data);

            int matchday = 0;
            int nMatchdays = data.Last().MatchDay;
            Dictionary<int, Dictionary<Map, Dictionary<ulong, Record>>> mdToMapToSteamIdToRecord = Indexing.MdToMapToSteamIdToRecord(data);
            Dictionary<Map, Dictionary<ulong, Record>> playerBestScores = new Dictionary<Map, Dictionary<ulong, Record>>();
            Dictionary<Map, List<Record>> bestScoreProgression = new Dictionary<Map, List<Record>>();
            IEnumerable<ulong> lastMatchdaySteamIds = null;
            foreach ((int, int) count in counts)
            {
                Dictionary<ulong, PlayerSummary> yearSummary = new Dictionary<ulong, PlayerSummary>();
                for (int i = 0; i < count.Item2; i++)
                {
                    List<Dictionary<ulong, Record>> results = Details.OrderedMapResults(mdToMapToSteamIdToRecord[++matchday]);

                    // Matchday results
                    Dictionary<ulong, PlayerSummary>  matchdaySummary = Summary.SingleMatchdaySummary(results);
                    Table details = Details.MakeDetailsTable(results, 4);
                    Table summaryPoints = Summary.MakeSummaryTable(matchdaySummary, usePoints: true);
                    Table summaryPositions = Summary.MakeSummaryTable(matchdaySummary, usePoints: false);

                    string matchdayDir = root + "\\" + count.Item1 + "\\MD#" + matchday;
                    details.ToFile(matchdayDir + "\\MD#" + matchday + "_Details.txt");
                    summaryPoints.ToFile(matchdayDir + "\\MD#" + matchday + "_Summary_Points.txt");
                    summaryPositions.ToFile(matchdayDir + "\\MD#" + matchday + "_Summary_Positions.txt");

                    Summary.UpdateMuitiMatchdaySummary(yearSummary, matchdaySummary, calcOldRanks: matchday == nMatchdays, calcNewRanks: i == count.Item2 - 1);
                    BestScores.UpdatePlayerBestScores(playerBestScores, results, calcOldRanks: matchday == nMatchdays, calcNewRanks: matchday == nMatchdays);
                    BestScores.UpdateBestScoreProgression(bestScoreProgression, results);
                    if (matchday == nMatchdays)
                    {
                        lastMatchdaySteamIds = matchdaySummary.Keys;
                    }
                }

                Table yearSummaryPoints = Summary.MakeSummaryTable(yearSummary, frequencyData: true, summaryName: count.Item1 + " LEADERBOARD", usePoints: true);
                Table yearSummaryPositions = Summary.MakeSummaryTable(yearSummary, frequencyData: true, summaryName: count.Item1 + " LEADERBOARD", usePoints: false);

                string yearDir = root + "\\" + count.Item1;
                yearSummaryPoints.ToFile(yearDir + "\\" + count.Item1 + "_Summary_Points.txt");
                yearSummaryPositions.ToFile(yearDir + "\\" + count.Item1 + "_Summary_Positions.txt");
            }

            // All-time results
            List<Dictionary<ulong, Record>> bestScoreResults = Details.OrderedMapResults(playerBestScores, updatePositions: false);
            Table bestScoresDetails = Details.MakeDetailsTable(bestScoreResults, eventsPerRow: 4, nResults: 20, showMatchdays: false, showPositions: true, showPosDeltas: true, showPoints: false);

            Dictionary<ulong, PlayerSummary> bestScoresSummary = BestScores.PlayerBestScoresSummary(bestScoreResults, nPositions: 20);
            Summary.ShowDeltas(bestScoresSummary, lastMatchdaySteamIds);
            Table bestScoresSummaryPositions = Summary.MakeSummaryTable(bestScoresSummary, "Overall Ranks", frequencyData: true, usePoints: false);

            List <List<Record>> bestProgressionResults = Indexing.OrderedMapResults(bestScoreProgression, recordComp: Record.CompareScores);
            Table bestScoresProgressionDetails = Details.MakeDetailsTable(bestProgressionResults, eventsPerRow: 4, showMatchdays: true, showPositions: false, showPoints: false);

            bestScoresDetails.ToFile(root + "\\AllTime_BestScores_Details.txt");
            bestScoresSummaryPositions.ToFile(root + "\\AllTime_BestScores_Summary.txt");
            bestScoresProgressionDetails.ToFile(root + "\\AllTime_BestScores_Progress.txt");
        }
    }
}
