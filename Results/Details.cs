using System;
using System.Collections.Generic;
using System.Linq;

namespace ASRT_BoostLeagueAssistant.Results
{
    class Details
    {
        public static List<Dictionary<ulong, Record>> OrderedMapResults(Dictionary<Map, Dictionary<ulong, Record>> mapToSteamIdToRecord, IEnumerable<Map> mapOrder = null, bool updatePositions = true, Comparison<Record> recordComp = null)
        {
            if (mapOrder == null)
            {
                mapOrder = Indexing.mapOrder;
            }
            if (recordComp == null)
            {
                recordComp = Record.ComparePoints;
            }
            List<Dictionary<ulong, Record>> orderedResults = new List<Dictionary<ulong, Record>>();
            foreach (Map map in mapOrder)
            {
                if (mapToSteamIdToRecord.TryGetValue(map, out Dictionary<ulong, Record> mapResults))
                {
                    if (updatePositions)
                    {
                        Indexing.UpdatePositions(mapResults, recordComp); // Caution - we lose the old positions
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

        public static List<List<Record>> OrderedMapResults(Dictionary<Map, List<Record>> mapToSteamIdToRecord, IEnumerable<Map> mapOrder = null, bool updatePositions = true, Comparison<Record> recordComp = null)
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
                        Indexing.UpdatePositions(mapResults, recordComp); // Caution - we lose the old positions
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


        public static Table MakeDetailsTable(IEnumerable<Dictionary<ulong, Record>> results, int eventsPerRow = 1,
            int nResults = -1, bool showHeadings = true, bool showMatchdays = false, bool showPositions = true, bool showPosDeltas = false, bool showPoints = true)
        {
            List<IEnumerable<Record>> resultsList = new List<IEnumerable<Record>>();
            foreach (var result in results)
            {
                resultsList.Add(Indexing.RecordDictToList(result));
            }
            return MakeDetailsTable(resultsList, eventsPerRow, nResults, showHeadings, showMatchdays, showPositions, showPosDeltas, showPoints);
        }

        public static Table MakeDetailsTable(IEnumerable<IEnumerable<Record>> results, int eventsPerRow = 1,
            int nResults = -1, bool showHeadings = true, bool showMatchdays = false, bool showPositions = true, bool showPosDeltas = false, bool showPoints = true)
        {
            Table table = new Table();
            int r = 0;
            int c = 0;
            int er = 0; // events on current row
            foreach (var eventResults in results)
            {
                InsertEventDetails(r, c, eventResults, table, nResults, showHeadings, showMatchdays, showPositions, showPosDeltas, showPoints);
                er++;
                if (er == eventsPerRow)
                {
                    r = table.RowCount + 2;
                    c = 0;
                    er = 0;
                }
                else
                {
                    c += 4 + (showMatchdays ? 1 : 0) + (showPositions ? 1 : 0) + (showPosDeltas ? 1 : 0) + (showPoints ? 1 : 0);
                }
            }
            return table;
        }

        public static void InsertEventDetails(int r, int c, IEnumerable<Record> eventResults, Table table, 
            int nResults = -1, bool showHeadings = true, bool showMatchdays = false, bool showPositions = true, bool showPosDeltas = false, bool showPoints = true)
        {
            if (!eventResults.Any())
            {
                return;
            }
            int nColumns = 3 + (showMatchdays ? 1 : 0) + (showPositions ? 1 : 0) + (showPosDeltas ? 1 : 0) + (showPoints ? 1 : 0);

            // Event info
            table[r, c++] = eventResults.First().Map.GetDescription();
            //table[r, c] = eventResults.First().EventType.GetDescription();
            c--;
            r++;

            // Headings
            if (showHeadings)
            {
                if (showMatchdays)
                {
                    table[r, c++] = "Matchday";
                }
                if (showPositions)
                {
                    table[r, c++] = "Position";
                }
                if (showPosDeltas)
                {
                    c++;
                }
                table[r, c++] = "Name";
                table[r, c++] = "Time";
                table[r, c++] = "Character";
                if (showPoints)
                {
                    table[r, c++] = "Points";
                }
                c -= nColumns;
            }

            // Data
            int i = 0;
            foreach(Record rec in eventResults)
            {
                if (nResults >= 0 && nResults < ++i)
                {
                    break;
                }
                r++;
                if (showMatchdays)
                {
                    table[r, c++] = "#" + rec.MatchDay;
                }
                if (showPositions)
                {
                    table[r, c++] = rec.Position + "°";
                }
                if (showPosDeltas)
                {
                    if (rec.ShowPosDelta && rec.OldPosition != 0)
                    {
                        table[r, c] = DeltaIndicator(rec.OldPosition, rec.Position, inverseArrow: true);
                    }
                    c++;
                }
                table[r, c++] = rec.Name;
                table[r, c++] = rec.ScoreString();
                table[r, c++] = rec.Character.GetDescription();
                if (showPoints)
                {
                    table[r, c++] = Record.TruncatedNumString(rec.Points.ToString(), 3);
                }
                c -= nColumns;
            }
        }

        public static string DeltaIndicator(decimal a, decimal b, bool inverseArrow = false)
        {
            if (a == b)
            {
                return "●";
            }
            else if (a < b)
            {
                return (inverseArrow ? "▼" : "▲") + (b - a);
            }
            else
            {
                return (inverseArrow ? "▲" : "▼") + (a - b);
            }
        }
    }
}
