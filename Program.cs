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
            string root = "C:\\Users\\TomAlex\\Desktop\\Boost League";
            List<Record> data = DataReader.ReadLogs(root);
            int nMatchdays = data.Last().MatchDay;
            PointSystem.CalculatePoints(data);
            Dictionary<(int, Map), List<Record>> matchdayMapToResults = MatchdayResults.MatchdayMapToResults(data);    
            for (int i = 1; i <= nMatchdays; i++)
            {
                List<List<Record>> results = MatchdayResults.GetOrderedMatchdayResults(i, matchdayMapToResults);
                List<PlayerSummary> summary = MatchdayResults.GetBreakdown(results);
                Table details = MatchdayResults.MakeDetailsTable(results, 4);
                Table summaryPoints = MatchdayResults.MakeSummaryTable(summary, usePoints: true);
                Table summaryPositions = MatchdayResults.MakeSummaryTable(summary, usePoints: false);
                string resultsDir = root + "\\" + MatchdayResults.GetMatchdayYear(i, results) + "\\MD#" + i + "\\Results";
                if (!Directory.Exists(resultsDir))
                {
                    Directory.CreateDirectory(resultsDir);
                }
                
                details.ToFile(resultsDir + "\\MD#" + i + "_Details.txt");
                summaryPoints.ToFile(resultsDir + "\\MD#" + i + "_Summary_Points.txt");
                summaryPositions.ToFile(resultsDir + "\\MD#" + i + "_Summary_Positions.txt");       
            }
        }
    }
}
