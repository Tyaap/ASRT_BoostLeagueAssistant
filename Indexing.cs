using System;
using System.Collections.Generic;
using System.Linq;

namespace ASRT_BoostLeagueAssistant
{
    class Indexing
    {
        public static readonly Map[] mapOrder = {
            Map.OceanView, Map.SambaStudios, Map.CarrierZone, Map.DragonCanyon,
            Map.TempleTrouble, Map.GalacticParade, Map.SeasonalShrines, Map.RoguesLanding,
            Map.DreamValley, Map.ChillyCastle, Map.GraffitiCity, Map.SanctuaryFalls,
            Map.GraveyardGig, Map.AddersLair, Map.BurningDepths, Map.RaceOfAges,
            Map.SunshineTour, Map.ShibuyaDowntown, Map.RouletteRoad, Map.EggHangar,
            Map.OutrunBay };

        public static Dictionary<int, Dictionary<Map, List<Record>>> MdToMapToRecords(List<Record> data, EventType eventType = EventType.BoostRace, bool onlyValidTimes = true)
        {
            Dictionary<int, Dictionary<Map, List<Record>>> mdGroups = new Dictionary<int, Dictionary<Map, List<Record>>>();
            foreach (Record rec in data)
            {
                if (rec.EventType != eventType || (onlyValidTimes && (rec.UsedExploit || rec.Completion != Completion.Finished))) // filter out unwanted records
                {
                    continue;
                }
                AddRecord(mdGroups, rec);
            }
            return mdGroups;
        }

        public static Dictionary<int, Dictionary<Map, Dictionary<ulong, Record>>> MdToMapToSteamIdToRecord(List<Record> data, EventType eventType = EventType.BoostRace,
            bool onlyValidTimes = false, bool mergeRoAs = true)
        {
            Dictionary<int, Dictionary<Map, Dictionary<ulong, Record>>> mdGroups = new Dictionary<int, Dictionary<Map, Dictionary<ulong, Record>>>();
            foreach (Record rec in data)
            {
                if (rec.EventType != eventType || (onlyValidTimes && (rec.UsedExploit || rec.Completion != Completion.Finished))) // filter out unwanted records
                {
                    continue;
                }
                if (mergeRoAs && rec.Map == Map.RaceOfAgesAlt1 || rec.Map == Map.RaceOfAgesAlt2 || rec.Map == Map.RaceOfAgesAlt3)
                {
                    rec.Map = Map.RaceOfAges; // caution - lose distinction between RoAs - only do this after calculating points
                }
                AddRecord(mdGroups, rec);
            }
            return mdGroups;
        }

        public static Dictionary<int, Dictionary<Map, List<Record>>> MdToMapToMdIntegral(List<Record> data, EventType eventType = EventType.BoostRace, bool onlyValidTimes = true)
        {
            int nMatchdays = data.Last().MatchDay;
            Dictionary<int, Dictionary<Map, List<Record>>> mdIntegral = new Dictionary<int, Dictionary<Map, List<Record>>>();
            foreach (Record rec in data)
            {
                if (rec.EventType != eventType || (onlyValidTimes && (rec.UsedExploit || rec.Completion != Completion.Finished))) // filter out unwanted records
                {
                    continue;
                }
                for (int i = rec.MatchDay; i <= nMatchdays; i++)
                {
                    AddRecord(mdIntegral, rec, matchday: i);
                }
            }
            return mdIntegral;
        }

        public static Record[] RecordDictToList(Dictionary<ulong, Record> recDict) // list is sorted by the record positions
        {
            Record[] recList = new Record[recDict.Count];
            foreach (Record rec in recDict.Values)
            {
                recList[rec.Position - 1] = rec;
            }
            return recList;
        }


        public static List<List<Record>> GetOrderedMapResults(Dictionary<Map, List<Record>> mapToSteamIdToRecord, IEnumerable<Map> mapOrder = null, bool updatePositions = true, Comparison<Record> recordComp = null)
        {
            if (mapOrder == null)
            {
                mapOrder = Indexing.mapOrder;
            }
            if (recordComp == null)
            {
                recordComp = Record.ComparePoints;
            }
            List<List<Record>> orderedResults = new List<List<Record>>();
            foreach (Map map in mapOrder)
            {
                if (mapToSteamIdToRecord.TryGetValue(map, out var mapResults))
                {
                    if (updatePositions)
                    {
                        UpdatePositions(mapResults, recordComp); // Caution - we lose the old positions
                    }
                    orderedResults.Add(mapResults);
                }
                else
                {
                    orderedResults.Add(new List<Record>());
                }
            }
            return orderedResults;
        }

        public static List<Dictionary<ulong, Record>> GetOrderedMapResults(Dictionary<Map, Dictionary<ulong, Record>> mapToSteamIdToRecord, IEnumerable<Map> mapOrder = null, bool updatePositions = true, Comparison<Record> recordComp = null)
        {
            if (mapOrder == null)
            {
                mapOrder = Indexing.mapOrder;
            }
            if (recordComp == null)
            {
                recordComp = Record.ComparePoints;
            }
            List<Dictionary<ulong, Record>> orderedResults = new List<Dictionary<ulong,Record>>();
            foreach (Map map in mapOrder)
            {
                if (mapToSteamIdToRecord.TryGetValue(map, out Dictionary<ulong, Record> mapResults))
                {
                    if (updatePositions)
                    {
                        UpdatePositions(mapResults, recordComp); // Caution - we lose the old positions
                    }
                    orderedResults.Add(mapResults);
                }
                else
                {
                    orderedResults.Add(new Dictionary<ulong, Record>());
                }
            }
            return orderedResults;
        }

        public static void UpdatePositions(Dictionary<ulong, Record> recDict, Comparison<Record> comp = null)
        {
            List<Record> recList = new List<Record>(recDict.Values);
            recList.Sort(comp);
            UpdatePositions(recList);
        }

        public static void UpdatePositions(List<Record> recList, Comparison<Record> comp = null)
        {
            if (comp == null)
            {
                comp = Record.ComparePoints;
            }
            recList.Sort(comp);
            int pos = 1;
            foreach (Record rec in recList)
            {
                rec.Position = pos++;
            }
        }

        public static void AddRecord(Dictionary<int, Dictionary<Map, Dictionary<ulong, Record>>> mdGroups, Record rec, int matchday = 0, Map map = 0, ulong steamId = 0)
        {
            if (matchday == 0)
            {
                matchday = rec.MatchDay;
            }
            if (!mdGroups.TryGetValue(matchday, out Dictionary<Map, Dictionary<ulong, Record>> mdGroup))
            {
                mdGroup = new Dictionary<Map, Dictionary<ulong, Record>>();
                mdGroups[matchday] = mdGroup;
            }
            AddRecord(mdGroup, rec, map, steamId);
        }

        public static void AddRecord(Dictionary<Map, Dictionary<ulong, Record>> mdGroup, Record rec, Map map = 0, ulong steamId = 0)
        {
            if (map == 0)
            {
                map = rec.Map;
            }
            if (!mdGroup.TryGetValue(map, out Dictionary<ulong, Record> mapGroup))
            {
                mapGroup = new Dictionary<ulong, Record>();
                mdGroup[map] = mapGroup;
            }
            AddRecord(mapGroup, rec, steamId);
        }

        public static void AddRecord(Dictionary<ulong, Record> mapGroup, Record rec, ulong steamId = 0)
        {
            if (steamId == 0)
            {
                steamId = rec.SteamID;
            }
            if (!mapGroup.TryGetValue(steamId, out Record rec2) || Record.ComparePoints(rec, rec2) > 0)
            {
                mapGroup[steamId] = rec;
            }
        }

        public static void AddRecord(Dictionary<int, Dictionary<Map, List<Record>>> mdGroups, Record rec, int matchday = 0, Map map = 0)
        {
            if (matchday == 0)
            {
                matchday = rec.MatchDay;
            }
            if (!mdGroups.TryGetValue(matchday, out Dictionary<Map, List<Record>> mdGroup))
            {
                mdGroup = new Dictionary<Map, List<Record>>();
                mdGroups[matchday] = mdGroup;
            }
            AddRecord(mdGroup, rec, map);
        }

        public static void AddRecord(Dictionary<Map, List<Record>> mdGroup, Record rec, Map map = 0)
        {
            if (map == 0)
            {
                map = rec.Map;
            }
            if (!mdGroup.TryGetValue(map, out List<Record> mapGroup))
            {
                mapGroup = new List<Record>();
                mdGroup[map] = mapGroup;
            }
            mapGroup.Add(rec);
        }
    }
}
