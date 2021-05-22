
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ASRT_BoostLeagueAssistant
{
    public class DataReader
    {
        public static (List<Record>, List<(int, int)>) ReadLogs(string path)
        {
            Dictionary<(int, string), int> roa = ReadRoA(path + "Config/RoA.txt");
            Dictionary<ulong, string> names = ReadPlayersBySteamID(path + "Config/Names.txt");
            List<Record> allData = new List<Record>();
            List<(int, int)> counts = new List<(int, int)>();
            int year = 2020;
            int matchday = 1;
            while (Directory.Exists(path + year))
            {
                int count = 0;
                while (Directory.Exists(path + year + "/MD#" + matchday))
                {
                    string[] lobbyDirs = Directory.GetDirectories(path + year + "/MD#" + matchday, "Lobby*");
                    foreach(string lobbyDir in lobbyDirs)
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
                            players = ReadPlayersByName(playerFiles[0]);
                        }
                        string lobbyName = Path.GetFileName(lobbyDir);
                        if (roa.TryGetValue((matchday, lobbyName), out int roaPlaneLaps))
                        {
                            roa.Remove((matchday, lobbyName));
                        }
                        ReadLog(logFiles[0], year, matchday, lobbyName, players, names, roaPlaneLaps, allData);
                    }
                    matchday++;
                    count++;
                }
                counts.Add((year, count));
                year++;       
            }
            int i = 0;
            foreach(Record r in allData)
            {
                r.Index = i++;
            }
            foreach(var pair in roa)
            {
                Console.WriteLine("Warning: " + path + "Config/RoA.txt contains MD#" + pair.Key.Item1 + " - " + pair.Key.Item2 + ", but no data was found!");
            }
            return (allData, counts);
        }

        public static Dictionary<string,ulong> ReadPlayersByName(string path)
        {
            Dictionary<string, ulong> players = new Dictionary<string, ulong>();
            foreach (string line in File.ReadAllLines(path).Skip(1))
            {
                string[] elements = line.Split('\t');
                if (elements.Length < 2)
                {
                    Console.WriteLine("Warning: " + path + " contains line with less than two elements: \"" + line + "\" (missing tab?)");
                    continue;
                }
                if (!ulong.TryParse(elements[1], out ulong steamId))
                {
                    Console.WriteLine("Warning: " + path + " contains invalid Steam ID: \"" + elements[1] + "\"");
                    continue;
                }
                players[elements[0]] = steamId;
            }
            return players;
        }

        public static Dictionary<ulong, string> ReadPlayersBySteamID(string path)
        {
            Dictionary<ulong, string> players = new Dictionary<ulong, string>();
            foreach (string line in File.ReadAllLines(path).Skip(1))
            {
                string[] elements = line.Split('\t');
                if (elements.Length < 2)
                {
                    Console.WriteLine("Warning: " + path + " contains line with less than two elements: \"" + line + "\" (missing tab?)");
                    continue;
                }
                if (!ulong.TryParse(elements[1], out ulong steamId))
                {
                    Console.WriteLine("Warning: " + path + " contains invalid Steam ID: \"" + elements[1] + "\"");
                    continue;
                }
                players[steamId] = elements[0];
            }
            return players;
        }

        public static Dictionary<(int, string), int> ReadRoA(string path)
        {
            Dictionary<(int, string), int> roa = new Dictionary<(int, string), int>();
            foreach (string line in File.ReadAllLines(path).Skip(1))
            {
                string[] elements = line.Split('\t');
                if (elements.Length < 3)
                {
                    Console.WriteLine("Warning: " + path + " contains a line with less than three elements: \"" + line + "\" (missing tab?)");
                    continue;
                }
                if (!int.TryParse(elements[0], out int matchDay))
                {
                    Console.WriteLine("Warning: " + path + " contains an invalid matchday number: \"" + elements[0] + "\"");
                    continue;
                }
                if (!int.TryParse(elements[2], out int planeLaps))
                {
                    Console.WriteLine("Warning: " + path + " contains an invalid number of plane laps: \"" + elements[2] + "\"");
                    continue;
                }
                roa[(matchDay, elements[1])] = planeLaps;
            }
            return roa;
        }

        public static void ReadLog(string path, int year, int matchday, string lobbyName, Dictionary<string,ulong> players, Dictionary<ulong, string> names, int roaPlaneLaps, List<Record> allData)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string[] lines = File.ReadAllLines(path);
            int nLines = lines.Length;
            Map map = 0;
            EventType eventType = 0;
            DateTime eventTime = DateTime.MinValue;
            int eventNum = 0;
            for(int i = 0; i < nLines; i++)
            {
                string line = lines[i];
                if (line.StartsWith("Position"))
                {
                    // Found a new event description
                    string[] eventElements = lines[i - 1].Split('\t');
                    int nElements = eventElements.Length;
                    map = nElements >= 1 ? StringToMap(eventElements[0], fileName) : 0;
                    if (map == Map.RaceOfAges && roaPlaneLaps > 0)
                    {
                        map = (Map)((uint)Map.RaceOfAges + roaPlaneLaps);
                    }
                    eventType = nElements >= 2 ? StringToEvent(eventElements[1], fileName) : 0;
                    eventTime = nElements >= 3 ? StringToDateTime(eventElements[2], fileName) : DateTime.MinValue;
                    eventNum++;
                }
                else
                {
                    string[] elements = line.Split('\t');
                    int nElements = elements.Length;
                    if (nElements >= 4 && elements[3] != "")
                    {
                        // Found a new result
                        int position = StringToPosition(elements[0], fileName);
                        double score;
                        Completion completion;
                        bool usedExploit = false;
                        string scoreStr = elements[2];
                        if (scoreStr.Contains(':'))
                        {
                            if (scoreStr.EndsWith('*'))
                            {
                                usedExploit = true;
                                scoreStr = scoreStr.TrimEnd('*');
                            }
                            score = StringToTime(scoreStr, fileName);
                            completion = Completion.Finished;
                        }
                        else if (scoreStr == "DNF")
                        {
                            score = double.MaxValue;
                            completion = Completion.DNF;
                        }
                        else if (scoreStr.Contains('%'))
                        {
                            score = StringToDNFPercent(scoreStr, fileName);
                            completion = Completion.DNF;
                        }
                        else
                        {
                            score = StringToGeneralScore(scoreStr, fileName);
                            completion = Completion.Finished;
                        }
                        Character character = StringToCharacter(elements[3], fileName);
                        double points = nElements >= 5 ? StringToPoints(elements[4], fileName) : 0;
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
                    }
                }   
            }
        }

        public static Map StringToMap(string s, string sessionName = "Session Log")
        {
            try
            {
                return EnumExtensions.GetValueFromDescription<Map>(s);
            }
            catch
            {
                Console.WriteLine("Warning: Invalid map name in " + sessionName + ": " + s);
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
                return EnumExtensions.GetValueFromDescription<EventType>(s);
            }
            catch
            {
                Console.WriteLine("Warning: Invalid event type in " + sessionName + ": " + s);
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
                Console.WriteLine("Warning: Invalid event date/time in " + sessionName + ": " + s);
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
                Console.WriteLine("Warning: Invalid player position in " + sessionName + ": " + s);
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

        public static double StringToTime(string s, string sessionName = "Session Log")
        {
            try
            {
                string[] timeParts = s.Split(':');
                int minutes = int.Parse(timeParts[0]);
                double seconds = double.Parse(timeParts[1]);
                if (seconds > 60 || minutes > 60)
                {
                    Console.WriteLine("Warning: Player time with minutes/seconds larger than sixty in " + sessionName + ": " + s);
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
                Console.WriteLine("Warning: Invalid player time in " + sessionName + ": " + s);
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

        public static double StringToDNFPercent(string s, string sessionName = "Session Log")
        {
            try
            {
                double percent = Math.Min(100f, double.Parse(s.Split('%')[0]));
                if (percent < 0 || percent > 100)
                {
                    Console.WriteLine("Warning: Player DNF percentage outside expected range in " + sessionName + ": " + s);
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
                Console.WriteLine("Warning: Invalid player DNF percentage in " + sessionName + ": " + s);
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

        public static double StringToGeneralScore(string s, string sessionName = "Session Log")
        {
            try
            {
                return double.Parse(s);
            }
            catch
            {
                Console.WriteLine("Warning: Invalid player score in " + sessionName + ": " + s);
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
                return EnumExtensions.GetValueFromDescription<Character>(s);
            }
            catch
            {
                Console.WriteLine("Warning: Invalid character name in " + sessionName + ": " + s);
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

        public static double StringToPoints(string s, string sessionName = "Session Log")
        {
            try
            {
                return double.Parse(s);
            }
            catch
            {
                Console.WriteLine("Warning: Invalid player points in " + sessionName + ": " + s);
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