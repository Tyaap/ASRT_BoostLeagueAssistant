using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRT_BoostLeagueAssistant
{
    class Indexing
    {
        public static Dictionary<(int, Map), List<Record>> MatchdayMapToRecords(List<Record> data)
        {
            Dictionary<(int, Map), List<Record>> groupData = new Dictionary<(int, Map), List<Record>>();
            foreach (Record rec in data)
            {
                if (!groupData.TryGetValue((rec.MatchDay, rec.Map), out List<Record> group))
                {
                    group = new List<Record>();
                    groupData.Add((rec.MatchDay, rec.Map), group);
                }
                group.Add(rec);
            }
            return groupData;
        }

        public static Dictionary<(int, Map), Dictionary<ulong, Record>> MatchdayMapToSteamIdToRecord(List<Record> data)
        {
            Dictionary<(int, Map), Dictionary<ulong, Record>> groupData = new Dictionary<(int, Map), Dictionary<ulong, Record>>();
            foreach (Record rec in data)
            {
                if (!groupData.TryGetValue((rec.MatchDay, rec.Map), out Dictionary<ulong, Record> group))
                {
                    group = new Dictionary<ulong, Record>();
                    groupData[(rec.MatchDay, rec.Map)] = group;
                }
                if (!group.TryGetValue(rec.SteamID, out Record rec2) || rec2.Points > rec.Points)
                {
                    group[rec.SteamID] = rec;
                }
            }
            return groupData;
        }

        public static Dictionary<ulong, Dictionary<(int, Map), Record>> SteamIdToMatchdayMapToRecord(List<Record> data)
        {
            Dictionary<ulong, Dictionary<(int, Map), Record>> groupData = new Dictionary<ulong, Dictionary<(int, Map), Record>>();
            foreach (Record rec in data)
            {
                if (!groupData.TryGetValue(rec.SteamID, out Dictionary<(int, Map), Record> group))
                {
                    group = new Dictionary<(int, Map), Record>();
                    groupData[rec.SteamID] = group;
                }
                if (!group.TryGetValue((rec.MatchDay, rec.Map), out Record rec2) || rec2.Points > rec.Points)
                {
                    group[(rec.MatchDay, rec.Map)] = rec;
                }
            }
            return groupData;
        }

        public static Dictionary<(int, Map), List<Record>> MatchdayMapToIntegral(List<Record> data)
        {
            int nMatchdays = data.Last().MatchDay;
            Dictionary<(int, Map), List<Record>> integralData = new Dictionary<(int, Map), List<Record>>();
            foreach (Record rec in data)
            {
                if (rec.UsedExploit || rec.Completion != Completion.Finished) // filter out records with unusable scores
                {
                    continue;
                }
                for (int i = rec.MatchDay; i <= nMatchdays; i++)
                {
                    if (!integralData.TryGetValue((i, rec.Map), out List<Record> integral))
                    {
                        integral = new List<Record>();
                        integralData.Add((i, rec.Map), integral);
                    }
                    integral.Add(rec);
                }
            }
            return integralData;
        }
    }
}
