using System;
using System.Collections.Generic;
using System.Linq;
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
            List<Record> data;
            List<(int, int)> counts;
            try
            {
                (data, counts) = DataReader.ReadLogs(root);
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
                    // Matchday results
                    string matchdayDir = root + count.Item1 + "/MD#" + ++matchday;

                    Console.WriteLine(matchdayDir + "/MD#" + matchday + "_Details.txt");
                    List<Dictionary<ulong, Record>> results = Details.OrderedMapResults(mdToMapToSteamIdToRecord[matchday]);
                    Table details = Details.MakeDetailsTable(results, 4);
                    details.ToFile(matchdayDir + "/MD#" + matchday + "_Details.txt");

                    Console.WriteLine(matchdayDir + "/MD#" + matchday + "_Summary_Points.txt");
                    Dictionary<ulong, PlayerSummary> matchdaySummary = Summary.SingleMatchdaySummary(results);
                    Table summaryPoints = Summary.MakeSummaryTable(matchdaySummary, usePoints: true);
                    summaryPoints.ToFile(matchdayDir + "/MD#" + matchday + "_Summary_Points.txt");

                    Console.WriteLine(matchdayDir + "/MD#" + matchday + "_Summary_Positions.txt");
                    Table summaryPositions = Summary.MakeSummaryTable(matchdaySummary, usePoints: false);
                    summaryPositions.ToFile(matchdayDir + "/MD#" + matchday + "_Summary_Positions.txt");

                    // Yearly / all-time calculations
                    Summary.UpdateMuitiMatchdaySummary(yearSummary, matchdaySummary);
                    BestScores.UpdatePlayerBestScores(playerBestScores, results, calcOldRanks: matchday == nMatchdays, calcNewRanks: matchday == nMatchdays);
                    BestScores.UpdateBestScoreProgression(bestScoreProgression, results);
                    if (matchday == nMatchdays) // most recent matchday
                    {
                        lastMatchdaySteamIds = matchdaySummary.Keys;
                        Summary.CalculateOldRanks(yearSummary);
                        Summary.ShowDeltas(yearSummary, lastMatchdaySteamIds, pointDelta: false, rankDelta: true);
                    }
                    if (i == count.Item2 - 1) // last matchday of the year
                    {
                        Summary.CalculateRanks(yearSummary);
                    }
                }

                // Yearly results
                string yearDir = root + count.Item1;

                Console.WriteLine(yearDir + "/" + count.Item1 + "_Summary_Points.txt");
                Table yearSummaryPoints = Summary.MakeSummaryTable(yearSummary, frequencyData: true, summaryName: count.Item1 + " LEADERBOARD", usePoints: true);
                yearSummaryPoints.ToFile(yearDir + "/" + count.Item1 + "_Summary_Points.txt");

                Console.WriteLine(yearDir + "/" + count.Item1 + "_Summary_Positions.txt");
                Table yearSummaryPositions = Summary.MakeSummaryTable(yearSummary, frequencyData: true, summaryName: count.Item1 + " LEADERBOARD", usePoints: false);
                yearSummaryPositions.ToFile(yearDir + "/" + count.Item1 + "_Summary_Positions.txt");
            }

            // All-time results
            Console.WriteLine(root + "AllTime_BestScores_Details.txt");
            List<Dictionary<ulong, Record>> bestScoreResults = Details.OrderedMapResults(playerBestScores, updatePositions: false);
            Table bestScoresDetails = Details.MakeDetailsTable(bestScoreResults, eventsPerRow: 4, nResults: 20, showMatchdays: false, showPositions: true, showPosDeltas: true, showPoints: false);
            bestScoresDetails.ToFile(root + "AllTime_BestScores_Details.txt");
            
            Console.WriteLine(root + "AllTime_BestScores_Summary.txt");
            Dictionary<ulong, PlayerSummary> bestScoresSummary = BestScores.PlayerBestScoresSummary(bestScoreResults, nPositions: 20);
            Summary.CalculateRanks(bestScoresSummary);
            Summary.CalculateOldRanks(bestScoresSummary);
            Summary.ShowDeltas(bestScoresSummary, lastMatchdaySteamIds, pointDelta: true, rankDelta: true);
            Table bestScoresSummaryPositions = Summary.MakeSummaryTable(bestScoresSummary, "Overall Ranks", frequencyData: true, usePoints: false);
            bestScoresSummaryPositions.ToFile(root + "AllTime_BestScores_Summary.txt");
            
            Console.WriteLine(root + "AllTime_BestScores_Progress.txt");
            List<List<Record>> bestProgressionResults = Indexing.OrderedMapResults(bestScoreProgression, recordComp: Record.CompareScores);
            Table bestScoresProgressionDetails = Details.MakeDetailsTable(bestProgressionResults, eventsPerRow: 4, showMatchdays: true, showPositions: false, showPoints: false);
            bestScoresProgressionDetails.ToFile(root + "AllTime_BestScores_Progress.txt");

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
