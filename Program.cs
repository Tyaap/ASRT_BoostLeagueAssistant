using System;
using System.Collections.Generic;
using ASRT_BoostLeagueAssistant.Results;

namespace ASRT_BoostLeagueAssistant
{
    static class Program
    {
        static void Main()
        {
            Console.WriteLine(@"

  ___  __ )____________________  /_    ___  /___________ _______ ____  ______ 
  __  __  |  __ \  __ \_  ___/  __/    __  / _  _ \  __ `/_  __ `/  / / /  _ \
  _  /_/ // /_/ / /_/ /(__  )/ /_      _  /__/  __/ /_/ /_  /_/ // /_/ //  __/
  /_____/ \____/\____//____/ \__/      /_____|___/\__,_/ _\__, / \__,_/ \___/ 
                                                         /____/               

                    @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                    @@@@@@@@@@@@@@@@@@%//////////@@@@@@@@@@@
                    @%))))))))@((((((((@@//////////%@@@@@@@@
                    @@@)))))))))@((((((((@@(/////////(&@@@@@
                    @@@@@&))))))))@@((((((((@@//////////#@@@
                    @@@@@@@@)))))))))@(((((((((@///////////&
                    @@@@@@%))))))))@@(((((((((@(//////////%@
                    @@@@)))))))))@@((((((((@@//////////(@@@@
                    @@%)))))))@@(((((((((@(//////////%@@@@@@
                    @@@@@@@@@#((((((((@@(/////////#@@@@@@@@@
                    @@@@@@@@@@@@@@@@@@@&&&&&&&&&&@@@@@@@@@@@

");

            // Load data
            string root = "";
            List<Record> allData;
            int allMdCount;
            List<(int, int)> yearMdCounts;
            try
            {
                (allData, allMdCount, yearMdCounts) = DataReader.ReadLogs(root);
            }
            catch(Exception e)
            {
                Console.WriteLine(
                    "Error loading data!\n" + e.Message + "\n" +
                    "Press any key to close this window...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine(
                "Data loaded successfully!\n" +
                "Press any key to continue...");
            Console.ReadKey();

            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Generate results
            Console.WriteLine("\nGenerating results...");

            int matchday = 0;
            Dictionary<int, Dictionary<Map, Dictionary<ulong, Record>>> mdToMapToSteamIdToRec = Indexing.MdToMapToSteamIdToRecord(allData);
            Dictionary<int, Dictionary<Map, List<Record>>> mdToMapToRecs = Indexing.MdToMapToRecords(allData, mergeRoAs: false);
            
            // cumulative data for calculating points
            Dictionary<Map, List<Record>> mapToCRecs = new Dictionary<Map, List<Record>>();
            Dictionary<Map, Dictionary<ulong, List<Record>>> mapToSteamIdToCRecs = new Dictionary<Map, Dictionary<ulong, List<Record>>>();
            
            // cumulative data for yearly/all-time summary
            Dictionary<Map, Dictionary<ulong, Record>> playerBestScores = new Dictionary<Map, Dictionary<ulong, Record>>();
            Dictionary<Map, List<Record>> bestScoreProgression = new Dictionary<Map, List<Record>>();
            IEnumerable<ulong> lastMatchdaySteamIds = null;
            
            foreach ((int, int) yearMdCount in yearMdCounts) // (year, matchday count)
            {
                Dictionary<ulong, PlayerSummary> yearSummary = new Dictionary<ulong, PlayerSummary>();
                for (int i = 0; i < yearMdCount.Item2; i++)
                {
                    string matchdayDir = root + yearMdCount.Item1 + "/MD#" + ++matchday;
                    
                    // Matchday points
                    PointSystem.AddAndCalcPoints(mdToMapToRecs[matchday], mapToCRecs, mapToSteamIdToCRecs);
                    
                    // Matchday details table
                    Console.WriteLine(matchdayDir + "/MD#" + matchday + "_Details.txt");
                    List<Dictionary<ulong, Record>> results = Details.OrderedMapResults(mdToMapToSteamIdToRec[matchday]);
                    Table details = Details.MakeDetailsTable(results, 4);
                    details.ToFile(matchdayDir + "/MD#" + matchday + "_Details.txt");

                    // Matchday summary table (points)
                    Console.WriteLine(matchdayDir + "/MD#" + matchday + "_Summary_Points.txt");
                    Dictionary<ulong, PlayerSummary> matchdaySummary = Summary.SingleMatchdaySummary(results);
                    Table summaryPoints = Summary.MakeSummaryTable(matchdaySummary, usePoints: true);
                    summaryPoints.ToFile(matchdayDir + "/MD#" + matchday + "_Summary_Points.txt");

                    // Matchday summary table (positions)
                    Console.WriteLine(matchdayDir + "/MD#" + matchday + "_Summary_Positions.txt");
                    Table summaryPositions = Summary.MakeSummaryTable(matchdaySummary, usePoints: false);
                    summaryPositions.ToFile(matchdayDir + "/MD#" + matchday + "_Summary_Positions.txt");

                    // Update yearly and all-time results
                    Summary.UpdateMuitiMatchdaySummary(yearSummary, matchdaySummary);
                    BestScores.UpdatePlayerBestScores(playerBestScores, results, calcOldRanks: matchday == allMdCount, calcNewRanks: matchday == allMdCount);
                    BestScores.UpdateBestScoreProgression(bestScoreProgression, results);
                    if (matchday == allMdCount) // most recent matchday
                    {
                        lastMatchdaySteamIds = matchdaySummary.Keys; // for the best scores summary
                        Summary.CalculateOldRanks(yearSummary);
                        Summary.ShowDeltas(yearSummary, lastMatchdaySteamIds, pointDelta: false, rankDelta: true);
                    }
                    if (i == yearMdCount.Item2 - 1) // last matchday of the year
                    {
                        Summary.CalculateRanks(yearSummary);
                    }
                }

                string yearDir = root + yearMdCount.Item1;

                // Yearly summary table (points)
                Console.WriteLine(yearDir + "/" + yearMdCount.Item1 + "_Summary_Points.txt");
                Table yearSummaryPoints = Summary.MakeSummaryTable(yearSummary, frequencyData: true, summaryName: yearMdCount.Item1 + " LEADERBOARD", usePoints: true);
                yearSummaryPoints.ToFile(yearDir + "/" + yearMdCount.Item1 + "_Summary_Points.txt");

                // Yearly summary table (positions)
                Console.WriteLine(yearDir + "/" + yearMdCount.Item1 + "_Summary_Positions.txt");
                Table yearSummaryPositions = Summary.MakeSummaryTable(yearSummary, frequencyData: true, summaryName: yearMdCount.Item1 + " LEADERBOARD", usePoints: false);
                yearSummaryPositions.ToFile(yearDir + "/" + yearMdCount.Item1 + "_Summary_Positions.txt");
            }

            // All-time best score details table
            Console.WriteLine(root + "AllTime_BestScores_Details.txt");
            List<Dictionary<ulong, Record>> bestScoreResults = Details.OrderedMapResults(playerBestScores, updatePositions: false);
            Table bestScoresDetails = Details.MakeDetailsTable(bestScoreResults, eventsPerRow: 4, nResults: 20, showMatchdays: false, showPositions: true, showPosDeltas: true, showPoints: false);
            bestScoresDetails.ToFile(root + "AllTime_BestScores_Details.txt");
            
            // All-time best score summary table
            Console.WriteLine(root + "AllTime_BestScores_Summary.txt");
            Dictionary<ulong, PlayerSummary> bestScoresSummary = BestScores.PlayerBestScoresSummary(bestScoreResults, nPositions: 20);
            Summary.CalculateRanks(bestScoresSummary);
            Summary.CalculateOldRanks(bestScoresSummary);
            Summary.ShowDeltas(bestScoresSummary, lastMatchdaySteamIds, pointDelta: true, rankDelta: true);
            Table bestScoresSummaryPositions = Summary.MakeSummaryTable(bestScoresSummary, "Overall Ranks", frequencyData: true, usePoints: false);
            bestScoresSummaryPositions.ToFile(root + "AllTime_BestScores_Summary.txt");
            
            // All-time best score progress table
            Console.WriteLine(root + "AllTime_BestScores_Progress.txt");
            List<List<Record>> bestProgressionResults = Details.OrderedMapResults(bestScoreProgression, recordComp: Record.CompareScores);
            Table bestScoresProgressionDetails = Details.MakeDetailsTable(bestProgressionResults, eventsPerRow: 4, showMatchdays: true, showPositions: false, showPoints: false);
            bestScoresProgressionDetails.ToFile(root + "AllTime_BestScores_Progress.txt");

            // Test tables
            //Dictionary<ulong, Dictionary<Map, List<Record>>> recsByPlayer = Indexing.SteamIdMapToRecords(allData, mergeRoAs: false);
            //Table testTable = Details.MakeDetailsTable(recsByPlayer[76561198347259925].Values, eventsPerRow: 23, showPositions: false);
            //testTable.ToFile("piero.txt");

            watch.Stop();
            Console.WriteLine(@"
______________       _____       ______      ______________
___  ____/__(_)_________(_)_________  /____________  /__  /
__  /_   __  /__  __ \_  /__  ___/_  __ \  _ \  __  /__  / 
_  __/   _  / _  / / /  / _(__  )_  / / /  __/ /_/ /  /_/  
/_/      /_/  /_/ /_//_/  /____/ /_/ /_/\___/\__,_/  (_)    

Time taken: " + watch.ElapsedMilliseconds / 1000d + " seconds!");
            Console.WriteLine("Press any key to close this window...");
            Console.ReadKey();
        }
    }
}
