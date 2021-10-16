using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ASRT_BoostLeagueAssistant
{
    public class DataReader
    {
        public static (List<Record>, int, List<(int, int)>) ReadLogs(string path)
        {
            Dictionary<(int, string), int> roa = ReadRoA(path + "Config/RoA.txt");
            Dictionary<ulong, string> names = ReadPlayersBySteamID(path + "Config/Names.txt");
            List<Record> allData = new();
            List<(int, int)> yearMdCounts = new();
            int year = 2020;
            int matchday = 1;
            while (Directory.Exists(path + year))
            {
                int count = 0;
                while (Directory.Exists(path + year + "/MD#" + matchday))
                {
                    string[] lobbyDirs = Directory.GetDirectories(path + year + "/MD#" + matchday);
                    foreach (string lobbyDir in lobbyDirs)
                    {
                        string[] logFiles = Directory.GetFiles(lobbyDir, "SessionEvents*.txt");
                        if (logFiles.Length == 0)
                        {
                            continue;
                        }
                        Dictionary<string, ulong> players = null;
                        string[] playerFiles = Directory.GetFiles(lobbyDir, "Players*.txt");
                        if (playerFiles.Length > 0)
                        {
                            players = ReadPlayersByName(playerFiles[0].Replace('\\', '/'));
                        }
                        string lobbyName = Path.GetFileName(lobbyDir);
                        if (roa.TryGetValue((matchday, lobbyName), out int roaPlaneLaps))
                        {
                            roa.Remove((matchday, lobbyName));
                        }
                        ReadLog(logFiles[0].Replace('\\', '/'), year, matchday, lobbyName, players, names, roaPlaneLaps, allData);
                    }
                    matchday++;
                    count++;
                }
                yearMdCounts.Add((year, count));
                year++;
            }
            int i = 0;
            foreach (Record r in allData)
            {
                r.Index = i++;
            }
            foreach (var pair in roa)
            {
                Console.WriteLine("Warning! " + path + "Config/RoA.txt contains MD#" + pair.Key.Item1 + " - " + pair.Key.Item2 + ", but no data was found!");
            }
            return (allData, matchday - 1, yearMdCounts);
        }

        public static Dictionary<string, ulong> ReadPlayersByName(string path)
        {
            Dictionary<string, ulong> players = new();
            foreach (string line in File.ReadAllLines(path))
            {
                string[] elements = line.Split('\t');
                if (elements.Length < 2)
                {
                    Console.WriteLine("Warning! " + path + " contains line with less than two elements: \"" + line + "\" (missing tab?)");
                    continue;
                }
                if (!ulong.TryParse(elements[1], out ulong steamId))
                {
                    if (elements[1] != "Steam ID") // This is the column name, so don't show a warning 
                    {
                        Console.WriteLine("Warning! " + path + " contains invalid Steam ID: \"" + elements[1] + "\"");
                    }
                    continue;
                }
                players[elements[0]] = steamId;
            }
            return players;
        }

        public static Dictionary<ulong, string> ReadPlayersBySteamID(string path)
        {
            Dictionary<ulong, string> players = new();
            foreach (string line in File.ReadAllLines(path))
            {
                string[] elements = line.Split('\t');
                if (elements.Length < 2)
                {
                    Console.WriteLine("Warning! " + path + " contains line with less than two elements: \"" + line + "\" (missing tab?)");
                    continue;
                }
                if (!ulong.TryParse(elements[1], out ulong steamId))
                {
                    if (elements[1] != "Steam ID") // This is the column name, so don't show a warning 
                    {
                        Console.WriteLine("Warning! " + path + " contains invalid Steam ID: \"" + elements[1] + "\"");
                    }
                    continue;
                }
                players[steamId] = elements[0];
            }
            return players;
        }

        public static Dictionary<(int, string), int> ReadRoA(string path)
        {
            Dictionary<(int, string), int> roa = new();
            foreach (string line in File.ReadAllLines(path))
            {
                string[] elements = line.Split('\t');
                if (elements.Length < 3)
                {
                    Console.WriteLine("Warning! " + path + " contains a line with less than three elements: \"" + line + "\" (missing tab?)");
                    continue;
                }
                if (!int.TryParse(elements[0], out int matchDay))
                {
                    if (elements[0] != "Matchday") // This is the column name, so don't show a warning 
                    {
                        Console.WriteLine("Warning! " + path + " contains invalid Steam ID: \"" + elements[1] + "\"");
                    }
                    continue;
                }
                if (!int.TryParse(elements[2], out int planeLaps))
                {
                    Console.WriteLine("Warning! " + path + " contains an invalid number of plane laps: \"" + elements[2] + "\"");
                    continue;
                }
                roa[(matchDay, elements[1])] = planeLaps;
            }
            return roa;
        }

        public static void ReadLog(string path, int year, int matchday, string lobbyName, Dictionary<string, ulong> players, Dictionary<ulong, string> names, int roaPlaneLaps, List<Record> allData, bool checkMapData = true)
        {
            HashSet<Map> mapsWithData = new();
            string[] lines = File.ReadAllLines(path);
            int nLines = lines.Length;
            Map map = 0;
            EventType eventType = 0;
            DateTime eventTime = DateTime.MinValue;
            int eventNum = 0;
            for (int i = 0; i < nLines; i++)
            {
                string line = lines[i];
                if (line.StartsWith("Position"))
                {
                    // Found a new event description
                    string[] eventElements = lines[i - 1].Split('\t');
                    int nElements = eventElements.Length;
                    map = nElements >= 1 ? StringToMap(eventElements[0], path) : 0;
                    if (map == Map.RaceOfAges && roaPlaneLaps > 0)
                    {
                        map = (Map)((uint)Map.RaceOfAges + roaPlaneLaps);
                    }
                    eventType = nElements >= 2 ? StringToEvent(eventElements[1], path) : 0;
                    eventTime = nElements >= 3 ? StringToDateTime(eventElements[2], path) : DateTime.MinValue;
                    eventNum++;
                }
                else
                {
                    string[] elements = line.Split('\t');
                    int nElements = elements.Length;
                    if (nElements >= 4 && elements[3] != "")
                    {
                        // Found a new result
                        int position = StringToPosition(elements[0], path);
                        decimal score;
                        Completion completion;
                        bool usedExploit = false;
                        if (nElements >= 6 && int.TryParse(elements[5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex))
                        {
                            Console.WriteLine("Got hex score!");
                            score = (decimal)hex.ToFloat();
                            Console.WriteLine("Score:" + score);
                            completion = Completion.Finished;
                        }
                        else
                        {
                            string scoreStr = elements[2];
                            if (scoreStr.Contains(':'))
                            {
                                if (scoreStr.EndsWith('*'))
                                {
                                    usedExploit = true;
                                    scoreStr = scoreStr.TrimEnd('*');
                                }
                                score = StringToTime(scoreStr, path);
                                completion = Completion.Finished;
                            }
                            else if (scoreStr == "DNF")
                            {
                                score = decimal.MaxValue;
                                completion = Completion.DNF;
                            }
                            else if (scoreStr.Contains('%'))
                            {
                                score = StringToDNFPercent(scoreStr, path);
                                completion = Completion.DNF;
                            }
                            else
                            {
                                score = StringToGeneralScore(scoreStr, path);
                                completion = Completion.Finished;
                            }
                        }

                        Character character = StringToCharacter(elements[3], path);
                        decimal points = nElements >= 5 ? StringToPoints(elements[4], path) : 0;
                        string name = elements[1];
                        if (players == null || !players.TryGetValue(name, out ulong steamId))
                        {
                            steamId = StringHash(name);
                        }
                        else if (names != null && names.TryGetValue(steamId, out string newName))
                        {
                            name = newName;
                        }
                        allData.Add(new Record
                        {
                            Year = year,
                            MatchDay = matchday,
                            LobbyName = lobbyName,
                            EventNum = eventNum,
                            EventTime = eventTime,
                            EventType = eventType,
                            Map = map,
                            Name = name,
                            SteamID = steamId,
                            Character = character,
                            Completion = completion,
                            Score = score,
                            Position = position,
                            Points = points,
                            UsedExploit = usedExploit
                        });
                        if (checkMapData)
                        {
                            mapsWithData.Add(map);
                        }
                    }
                }
            }
            if (checkMapData)
            {
                foreach (Map m in Indexing.mapOrder)
                {
                    if ((m != Map.RaceOfAges && !mapsWithData.Contains(m)) ||
                        !mapsWithData.Contains((Map)((uint)Map.RaceOfAges + roaPlaneLaps)))
                    {
                        Console.WriteLine("Warning! " + path + " is missing data for: " + m.GetDescription());
                    }
                }
            }
        }

        public static Map StringToMap(string s, string sessionName = "Session Log")
        {
            try
            {
                return Extensions.GetValueFromDescription<Map>(s);
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " contains invalid map name: " + s);
                /*
                MessageBox.Show(
                    "Invalid map name in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The available options are:\n" +
                    EnumExtensions.GetDescriptionList<Map>(),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static EventType StringToEvent(string s, string sessionName = "Session Log")
        {
            try
            {
                return Extensions.GetValueFromDescription<EventType>(s);
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " contains invalid event type: " + s);
                /*
                MessageBox.Show(
                    "Invalid event type in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The available options are:\n" +
                    EnumExtensions.GetDescriptionList<EventType>(),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static DateTime StringToDateTime(string s, string sessionName = "Session Log")
        {
            try
            {
                return DateTime.ParseExact(s, "yy/MM/dd HH:mm", CultureInfo.InvariantCulture);
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " containt invalid event date/time: " + s);
                /*
                MessageBox.Show(
                    "Invalid event date/time in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The correct format is:\n" +
                    "yy/MM/dd HH:mm",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return DateTime.MinValue;
            }
        }

        public static int StringToPosition(string s, string sessionName = "Session Log")
        {
            try
            {
                return int.Parse(s.TrimEnd('°'));
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + "contains invalid player position: " + s);
                /*
                MessageBox.Show(
                    "Invalid player position in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The position must be an integer, and can have the ° symbol after it.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static decimal StringToTime(string s, string sessionName = "Session Log")
        {
            try
            {
                string[] timeParts = s.Split(':');
                int minutes = int.Parse(timeParts[0]);
                decimal seconds = decimal.Parse(timeParts[1]);
                if (seconds > 60 || minutes > 60)
                {
                    Console.WriteLine("Warning! " + sessionName + "contains time with minutes/seconds larger than sixty: " + s);
                    /*
                    MessageBox.Show(
                        "Player time with minutes/seconds larger than sixty: " + s,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    */
                }
                return minutes * 60 + seconds;
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " contains invalid time: " + s);
                /*
                MessageBox.Show(
                    "Invalid player time in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The correct format is:\n" +
                    "mm:ss.fff",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static decimal StringToDNFPercent(string s, string sessionName = "Session Log")
        {
            try
            {
                decimal percent = Math.Min(100, decimal.Parse(s.Split('%')[0]));
                if (percent < 0 || percent > 100)
                {
                    Console.WriteLine("Warning! " + sessionName + " contains DNF percentage outside normal range: " + s);
                    /*
                    MessageBox.Show(
                        "Player DNF percentage outside expected range in  " + s,
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    */
                }
                return percent;
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " contains invalid DNF percentage : " + s);
                /*
                MessageBox.Show(
                    "Invalid player DNF percentage in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The correct format is:\n" +
                    "{number}% (DNF)",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static decimal StringToGeneralScore(string s, string sessionName = "Session Log")
        {
            try
            {
                return decimal.Parse(s);
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " contains invalid player score: " + s);
                /*
                MessageBox.Show(
                    "Invalid player score in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The score must be a number, time, or percentage.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static Character StringToCharacter(string s, string sessionName = "Session Log")
        {
            try
            {
                return Extensions.GetValueFromDescription<Character>(s);
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " contains invalid character name: " + s);
                /*
                MessageBox.Show(
                    "Invalid character name in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The available options are:\n" +
                    EnumExtensions.GetDescriptionList<Character>(),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static decimal StringToPoints(string s, string sessionName = "Session Log")
        {
            try
            {
                return decimal.Parse(s);
            }
            catch
            {
                Console.WriteLine("Warning! " + sessionName + " contains invalid points: " + s);
                /*
                MessageBox.Show(
                    "Invalid player points in " + sessionName + ":\n" +
                    s + "\n\n" +
                    "The points must be a number.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                */
                return 0;
            }
        }

        public static uint StringHash(string s)
        {
            uint x = 0x811C9DC5;
            foreach (char c in s)
            {
                x *= 0x1000193;
                x ^= c;
            }
            return x;
        }
    }
}