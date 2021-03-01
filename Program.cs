using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            // Create results
            int nMatchdays = data.Last().MatchDay;
            List<Dictionary<ulong, PlayerSummary>> matchdaySummaries = new List<Dictionary<ulong, PlayerSummary>>(); // Summaries for the current year
            Dictionary<(int, Map), List<Record>> matchdayMapToResults = MatchdayResults.MatchdayMapToResults(data);
            int matchday = 0;
            foreach ((int, int) count in counts)
            {
                for (int i = 0; i < count.Item2; i++)
                {
                    List<List<Record>> results = MatchdayResults.GetOrderedMatchdayResults(++matchday, matchdayMapToResults);

                    // Matchday results
                    Dictionary<ulong, PlayerSummary> matchdaySummary = MatchdayResults.SingleMatchdaySummary(results);
                    matchdaySummaries.Add(matchdaySummary);
                    Table details = MatchdayResults.MakeDetailsTable(results, 4);
                    Table summaryPoints = MatchdayResults.MakeSummaryTable(matchdaySummary, usePoints: true);
                    Table summaryPositions = MatchdayResults.MakeSummaryTable(matchdaySummary, usePoints: false);

                    string matchdayDir = root + "\\" + count.Item1 + "\\MD#" + matchday;
                    details.ToFile(matchdayDir + "\\MD#" + matchday + "_Details.txt");
                    summaryPoints.ToFile(matchdayDir + "\\MD#" + matchday + "_Summary_Points.txt");
                    summaryPositions.ToFile(matchdayDir + "\\MD#" + matchday + "_Summary_Positions.txt");
                }

                // Yearly results
                Dictionary<ulong, PlayerSummary> yearSummary = MatchdayResults.MultiMatchdaySummary(matchdaySummaries, calculateOldRanks: matchday == nMatchdays);
                matchdaySummaries.Clear();
                Table yearSummaryPoints = MatchdayResults.MakeSummaryTable(yearSummary, singleMatchday: false, summaryName: count.Item1 + " LEADERBOARD", usePoints: true);
                Table yearSummaryPositions = MatchdayResults.MakeSummaryTable(yearSummary, singleMatchday: false, summaryName: count.Item1 + " LEADERBOARD", usePoints: false);

                string yearDir = root + "\\" + count.Item1;
                yearSummaryPoints.ToFile(yearDir + "\\" + count.Item1 + "_Summary_Points.txt");
                yearSummaryPositions.ToFile(yearDir + "\\" + count.Item1 + "_Summary_Positions.txt");
            }
        }
    }
}
