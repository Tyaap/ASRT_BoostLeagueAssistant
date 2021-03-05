Boost League Assistant by Tyapp

The folder structure:

--> Boost League
     |--> Boost League Assistant.exe [1]
     |--> Config
     |     |-> Names.txt [2]
     |     |-> RoA.txt [3]
     |--> 2020 [4]
     |     |-> MD#1 [5]
     |     |    |-> Lobby A [6]
     |     |    |    |-> SessionEvents(...).txt [7]
     |     |    |    |-> Players(...).txt [8]
     |     |    |-> Lobby B
     |     |    |-> Lobby C
     |     |    |-> ...
     |     |-> MD#2
     |     |-> MD#3
     |     |-> ...
     |--> 2021
     |--> 2022
     |--> ...

[1]: Boost League Assistant.exe
     The boost league assistant. 
     You run it when you have prepared all data in the correct folders.

     The tool will calculate various results/statisticts and store them in the following files:
          (A) Matchday details (e.g. MD#1_Details.txt) containing full results for each track.
          (B) Matchday points summary (e.g. MD#1_Summary_Points.txt)
          (C) Matchday positions summary (e.g. MD#1_Summary_Positions.txt)
          (D) Yearly points summary (e.g. 2020_Summary_Points.txt)
          (E) Yearly positions summary (e.g. 2020_Summary_Positions.txt)
          (F) All-time best scores for each track (AllTime_BestScores_Details.txt)
          (G) All-time best scores summary (AllTime_BestScores_Summary.txt)
          (H) All-time progression of the top score on each track (AllTime_BestScores_Progress.txt)

[2]: Names.txt
     Each line of the file contains a Steam ID (SteamID64) and a player name.
     If the assistant finds a player matching the Steam ID, it will change their
     name to the one given in the file.

[3]: RoA.txt
     Each line of the file contains a matchday (e.g. MD#2), a lobby (e.g. Lobby A), and a number (between 1 and 3)
     If a lobby uses the plane path on Race of Ages, use this file to specify how many plane laps were taken.
     This ensures all players are scored fairly on Race of Ages.

[4]: Year folder
     The assistant searches inside the Boost League folder for year folders (e.g. 2021)
     Each folder contains all data for the corresponding year.
     The year folders start at 2020, and with each new folder the number increases by one.

[5]: Matchday folder
     The assistant searches inside a year folder for matchday folders (e.g. MD#2)
     Each folder contains all data for the corresponding matchday.
     The first folder is MD#1, and with each new folder the number increases by one.
     The first matchday number in each year must continue on from the last matchday number in the previous year. 
     e.g. if the last matchday in 2020 is MD#8 then the first matchday in 2021 must be MD#9.

[6]: Lobby folder
     The assistant searches inside a matchday folder for lobby folders.
     Lobby folders start with the word "Lobby" (e.g. "Lobby A", "Lobby 1", or "Lobby Tyapp")
     Each folder contains all data for the corresponding lobby.
     The assistant can handle any number of matchday lobbies (not just one or two!)

[7]: SessionEvents(...).txt
     The (...) part can be anything, e.g. "SessionEvents_2020-11-10--20-15.txt"
     This contains a log of all boost races that took place in the lobby.
     These files can be created automatically by using the EventLogger tool.
     The assistant can handle any track order, any number of repeated tracks, and any number of missing tracks.
     If a lobby repeats a track, the assistant will merge the results from the races and use each players best score.

[8]: Players(...).txt
     The (...) part can be anything, e.g. "Players_2020-11-10--20-15.txt"
     Each line of the file contains a Steam ID (SteamID64) and a player name.
     The assistant matches the names in this file with the names used in the SessionEvents(...).txt file.
     It will assign the given Steam ID to matching names, and uses this to track the player across multiple matchdays.
     These files can be created automatically by using the EventLogger tool.