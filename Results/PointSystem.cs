using System.Collections.Generic;

namespace ASRT_BoostLeagueAssistant.Results
{
    class PointSystem
    {
        public static void CalculatePoints(Record rec, List<Record> compRecs) // compRecs must be sorted by score from smallest to largest
        {
            if (rec.Completion == Completion.Finished)
            {
                int n = 0;
                foreach (Record compRec in compRecs)
                {
                    n++;
                    if (rec.Score <= compRec.Score)
                    {
                        break;
                    }
                }
                rec.Points = 10 - 9 * (double)(n - 1) / (compRecs.Count - 1);
            }
            else
            {
                rec.Points = 1;
            }
        }

        public static void CalculatePoints(List<Record> data)
        {
            Dictionary<int, Dictionary<Map, List<Record>>> mdGroups = Indexing.MdToMapToRecords(data, onlyValidTimes: false);
            Dictionary<int, Dictionary<Map, List<Record>>> mdIntegral = Indexing.MdToMapToMdIntegral(data, onlyValidTimes: true);

            foreach(int matchday in mdGroups.Keys)
            {
                foreach(Map map in mdGroups[matchday].Keys)
                {
                    List<Record> mapResults = mdGroups[matchday][map];
                    List<Record> integral = mdIntegral[matchday][map];
                    integral.Sort((x, y) => x.Score.CompareTo(y.Score)); // order times from smallest to largest
                    foreach (Record rec in mapResults)
                    {
                        CalculatePoints(rec, integral);
                    }
                }
            }
        }
    }
}
