using System.Collections.Generic;
using System.Linq;

namespace ASRT_BoostLeagueAssistant
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
                rec.Points = 10 - 9 * (float)(n - 1) / (compRecs.Count - 1);
            }
            else
            {
                rec.Points = 1;
            }
        }

        public static void CalculatePoints(List<Record> data)
        {
            Dictionary<(int, Map), List<Record>> groupData = Indexing.MatchdayMapToRecords(data);
            Dictionary<(int, Map), List<Record>> integralData = Indexing.MatchdayMapToIntegral(data);

            foreach(var key in groupData.Keys)
            {
                List<Record> group = groupData[key];
                List<Record> integral = integralData[key];
                integral.Sort((x, y) => x.Score.CompareTo(y.Score));
                foreach(Record rec in group)
                {
                    CalculatePoints(rec, integral);
                }
            }
        }
    }
}
