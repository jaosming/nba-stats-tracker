﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using LeftosCommonLibrary;
using Microsoft.Win32;

namespace NBA_2K12_Correct_Team_Stats
{
    class StatsTracker
    {
        public static string AppDocsPath = MainWindow.AppDocsPath;
        public static string SavesPath = MainWindow.SavesPath;
        public static string AppTempPath = MainWindow.AppTempPath;
        public static string mode = "Mode 0";
        public static bool errorRealStats = false;

        public static PlayoffTree tempPT;

        public const int M = 0, PF = 1, PA = 2, FGM = 4, FGA = 5, TPM = 6, TPA = 7,
            FTM = 8, FTA = 9, OREB = 10, DREB = 11, STL = 12, TO = 13, BLK = 14, AST = 15,
            FOUL = 16;
        public const int PPG = 0, PAPG = 1, FGp = 2, FGeff = 3, TPp = 4, TPeff = 5,
            FTp = 6, FTeff = 7, RPG = 8, ORPG = 9, DRPG = 10, SPG = 11, BPG = 12,
            TPG = 13, APG = 14, FPG = 15, Wp = 16, Weff = 17;

        public static SortedDictionary<string, int> setTeamOrder(string mode)
        {
            SortedDictionary<string, int> TeamOrder; 

            switch (mode)
            {
                case "Mode 0":
                default:
                    TeamOrder = new SortedDictionary<string, int>
                    {
                        {"76ers", 20},
                        {"Bobcats", 22},
                        {"Bucks", 9},
                        {"Bulls", 28},
                        {"Cavaliers", 11},
                        {"Celtics", 12},
                        {"Clippers", 7},
                        {"Grizzlies", 6},
                        {"Hawks", 16},
                        {"Heat", 4},
                        {"Hornets", 15},
                        {"Jazz", 27},
                        {"Kings", 13},
                        {"Knicks", 5},
                        {"Lakers", 25},
                        {"Magic", 23},
                        {"Mavericks", 29},
                        {"Nets", 18},
                        {"Nuggets", 0},
                        {"Pacers", 2},
                        {"Pistons", 3},
                        {"Raptors", 21},
                        {"Rockets", 26},
                        {"Spurs", 10},
                        {"Suns", 14},
                        {"Thunder", 24},
                        {"Timberwolves", 17},
                        {"Trail Blazers", 1},
                        {"Warriors", 8},
                        {"Wizards", 19}
                    };
                    break;

                case "Mode 1":
                    TeamOrder = new SortedDictionary<string, int>
                    {
                        {"76ers", 20},
                        {"Bobcats", 22},
                        {"Bucks", 2},
                        {"Bulls", 28},
                        {"Cavaliers", 11},
                        {"Celtics", 12},
                        {"Clippers", 7},
                        {"Grizzlies", 6},
                        {"Hawks", 16},
                        {"Heat", 4},
                        {"Hornets", 15},
                        {"Jazz", 27},
                        {"Kings", 13},
                        {"Knicks", 5},
                        {"Lakers", 25},
                        {"Magic", 23},
                        {"Mavericks", 29},
                        {"Nets", 18},
                        {"Nuggets", 0},
                        {"Pacers", 9},
                        {"Pistons", 10},
                        {"Raptors", 21},
                        {"Rockets", 26},
                        {"Spurs", 3},
                        {"Suns", 14},
                        {"Thunder", 24},
                        {"Timberwolves", 17},
                        {"Trail Blazers", 1},
                        {"Warriors", 8},
                        {"Wizards", 19}
                    };
                    break;

                case "Mode 2":
                    TeamOrder = new SortedDictionary<string, int>
                    {
                        {"76ers", 20},
                        {"Bobcats", 22},
                        {"Bucks", 8},
                        {"Bulls", 28},
                        {"Cavaliers", 12},
                        {"Celtics", 13},
                        {"Clippers", 6},
                        {"Grizzlies", 5},
                        {"Hawks", 16},
                        {"Heat", 3},
                        {"Hornets", 15},
                        {"Jazz", 27},
                        {"Kings", 2},
                        {"Knicks", 4},
                        {"Lakers", 25},
                        {"Magic", 23},
                        {"Mavericks", 29},
                        {"Nets", 18},
                        {"Nuggets", 0},
                        {"Pacers", 10},
                        {"Pistons", 11},
                        {"Raptors", 21},
                        {"Rockets", 26},
                        {"Spurs", 9},
                        {"Suns", 14},
                        {"Thunder", 24},
                        {"Timberwolves", 17},
                        {"Trail Blazers", 1},
                        {"Warriors", 7},
                        {"Wizards", 19}
                    };
                    break;

                case "Mode 3":
                    TeamOrder = new SortedDictionary<string, int>
                    {
                        {"76ers", 20},
                        {"Bobcats", 22},
                        {"Bucks", 7},
                        {"Bulls", 28},
                        {"Cavaliers", 11},
                        {"Celtics", 12},
                        {"Clippers", 5},
                        {"Grizzlies", 4},
                        {"Hawks", 16},
                        {"Heat", 2},
                        {"Hornets", 15},
                        {"Jazz", 27},
                        {"Kings", 13},
                        {"Knicks", 3},
                        {"Lakers", 25},
                        {"Magic", 23},
                        {"Mavericks", 29},
                        {"Nets", 18},
                        {"Nuggets", 0},
                        {"Pacers", 9},
                        {"Pistons", 10},
                        {"Raptors", 21},
                        {"Rockets", 26},
                        {"Spurs", 8},
                        {"Suns", 14},
                        {"Thunder", 24},
                        {"Timberwolves", 17},
                        {"Trail Blazers", 1},
                        {"Warriors", 6},
                        {"Wizards", 19}
                    };
                    break;

                case "Mode 4":
                    TeamOrder = new SortedDictionary<string, int>
                    {
                        {"76ers", 20},
                        {"Bobcats", 22},
                        {"Bucks", 7},
                        {"Bulls", 24},
                        {"Cavaliers", 11},
                        {"Celtics", 12},
                        {"Clippers", 5},
                        {"Grizzlies", 4},
                        {"Hawks", 16},
                        {"Heat", 2},
                        {"Hornets", 15},
                        {"Jazz", 29},
                        {"Kings", 13},
                        {"Knicks", 3},
                        {"Lakers", 27},
                        {"Magic", 23},
                        {"Mavericks", 25},
                        {"Nets", 18},
                        {"Nuggets", 0},
                        {"Pacers", 9},
                        {"Pistons", 10},
                        {"Raptors", 21},
                        {"Rockets", 28},
                        {"Spurs", 8},
                        {"Suns", 14},
                        {"Thunder", 26},
                        {"Timberwolves", 17},
                        {"Trail Blazers", 1},
                        {"Warriors", 6},
                        {"Wizards", 19}
                    };
                    break;

                case "Mode 5":
                    TeamOrder = new SortedDictionary<string, int>
                    {
                        {"76ers", 13},
                        {"Bobcats", 10},
                        {"Bucks", 0},
                        {"Bulls", 4},
                        {"Cavaliers", 20},
                        {"Celtics", 14},
                        {"Clippers", 5},
                        {"Grizzlies", 16},
                        {"Hawks", 22},
                        {"Heat", 1},
                        {"Hornets", 9},
                        {"Jazz", 11},
                        {"Kings", 29},
                        {"Knicks", 17},
                        {"Lakers", 28},
                        {"Magic", 8},
                        {"Mavericks", 26},
                        {"Nets", 3},
                        {"Nuggets", 27},
                        {"Pacers", 19},
                        {"Pistons", 25},
                        {"Raptors", 21},
                        {"Rockets", 24},
                        {"Spurs", 12},
                        {"Suns", 23},
                        {"Thunder", 7},
                        {"Timberwolves", 18},
                        {"Trail Blazers", 2},
                        {"Warriors", 6},
                        {"Wizards", 15}
                    };
                    break;
            }

            List<int> checklist = new List<int>();
            foreach (KeyValuePair<string, int> kvp in TeamOrder)
            {
                if (checklist.Contains(kvp.Value) == false)
                {
                    checklist.Add(kvp.Value);
                }
                else
                {
                    MessageBox.Show("Conflict for " + mode + " TeamOrder on ID " + kvp.Value);
                    Environment.Exit(-1);
                }
            }

            return TeamOrder;
        }

        public static int askGamesInSeason(int gamesInSeason)
        {
            MessageBoxResult r = MessageBox.Show("How many games does each season have in this save?\n\n82 Games: Yes\n58 Games: No\n29 Games: Cancel", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) gamesInSeason = 82;
            else if (r == MessageBoxResult.No) gamesInSeason = 58;
            else if (r == MessageBoxResult.Cancel) gamesInSeason = 28;
            return gamesInSeason;
        }

        public static int[][] calculateRankings(TeamStats[] _teamStats)
        {
            int[][] rating = new int[30][];
            for (int i = 0; i < 30; i++)
            {
                rating[i] = new int[19];
            }
            for (int k = 0; k < 30; k++)
            {
                for (int i = 0; i < 18; i++)
                {
                    rating[k][i] = 1;
                    for (int j = 0; j < 30; j++)
                    {
                        if (j != k)
                        {
                            if (_teamStats[j].averages[i] > _teamStats[k].averages[i])
                            {
                                rating[k][i]++;
                            }
                        }
                    }
                }
                rating[k][18] = _teamStats[k].getGames();
            }
            return rating;
        }

        public static TeamStats[] GetStats(string fn, ref SortedDictionary<string, int> TeamOrder, ref PlayoffTree pt, bool havePT = false)
        {
            TeamStats[] _teamStats = new TeamStats[30];
            for (int i = 0; i < 30; i++)
            {
                _teamStats[i] = new TeamStats();
            }
            if (!havePT) pt = null;

            string ext = Tools.getExtension(fn);

            if (ext.ToUpperInvariant() == "PMG")
            {
                if (!havePT)
                {
                    pt = new PlayoffTree();
                    MessageBoxResult r = MessageBox.Show("Do you have a saved Playoff Tree you want to load for this save file?", "NBA Stats Tracker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (r == MessageBoxResult.No)
                    {
                        playoffTreeW ptw = new playoffTreeW();
                        ptw.ShowDialog();
                        if (!pt.done) return new TeamStats[1];

                        SaveFileDialog spt = new SaveFileDialog();
                        spt.Title = "Please select a file to save the Playoff Tree to...";
                        spt.InitialDirectory = AppDocsPath;
                        spt.Filter = "Playoff Tree files (*.ptr)|*.ptr";
                        spt.ShowDialog();

                        if (spt.FileName == "") return new TeamStats[1];

                        try
                        {
                            FileStream stream = File.Open(spt.FileName, FileMode.Create);
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

                            bf.Serialize(stream, pt);
                            stream.Close();
                        }
                        catch (Exception ex)
                        {
                            App.errorReport(ex, "Trying to save playoff tree");
                        }
                    }
                    else if (r == MessageBoxResult.Yes)
                    {
                        OpenFileDialog ofd = new OpenFileDialog();
                        ofd.Filter = "Playoff Tree files (*.ptr)|*.ptr";
                        ofd.InitialDirectory = AppDocsPath;
                        ofd.Title = "Please select the file you saved the Playoff Tree to for " + Tools.getSafeFilename(fn) + "...";
                        ofd.ShowDialog();

                        if (ofd.FileName == "") return new TeamStats[1];

                        FileStream stream = File.Open(ofd.FileName, FileMode.Open);
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

                        pt = (PlayoffTree)bf.Deserialize(stream);
                        stream.Close();
                    }
                    else return new TeamStats[1];
                }
            }
            prepareOffsets(fn, _teamStats, ref TeamOrder, ref pt);

            BinaryReader br = new BinaryReader(File.OpenRead(fn));
            MemoryStream ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true);
            br.Close();
            byte[] buf = new byte[2];

            if ((pt == null) || (pt.teams[0] == "Invalid"))
            {
                foreach (KeyValuePair<string, int> kvp in TeamOrder)
                {
                    if (kvp.Key != "")
                    {
                        _teamStats[kvp.Value].name = kvp.Key;
                    }
                }
                for (int i = 0; i < 30; i++)
                {
                    ms.Seek(_teamStats[i].offset, SeekOrigin.Begin);
                    ms.Read(buf, 0, 2);
                    _teamStats[i].winloss[0] = buf[0];
                    _teamStats[i].winloss[1] = buf[1];
                    for (int j = 0; j < 18; j++)
                    {
                        ms.Read(buf, 0, 2);
                        _teamStats[i].stats[j] = BitConverter.ToUInt16(buf, 0);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    int id = TeamOrder[pt.teams[i]];
                    ms.Seek(_teamStats[id].offset, SeekOrigin.Begin);
                    ms.Read(buf, 0, 2);
                    _teamStats[id].name = pt.teams[i];
                    _teamStats[id].winloss[0] = buf[0];
                    _teamStats[id].winloss[1] = buf[1];
                    for (int j = 0; j < 18; j++)
                    {
                        ms.Read(buf, 0, 2);
                        _teamStats[id].stats[j] = BitConverter.ToUInt16(buf, 0);
                    }
                }
            }
            int temp;
            for (int i = 0; i < 30; i++)
            {
                temp = _teamStats[i].calcAvg();
            }

            return _teamStats;
        }

        public static void prepareOffsets(string fn, TeamStats[] _teamStats, ref SortedDictionary<string, int> TeamOrder, ref PlayoffTree pt)
        {
            // Stage 1
            string ext = Tools.getExtension(fn);
            if (ext.ToUpperInvariant() == "FXG" || ext.ToUpperInvariant() == "RFG")
            {
                _teamStats[0].offset = 3240532;
            }
            else if (ext.ToUpperInvariant() == "CMG")
            {
                _teamStats[0].offset = 5722996;
            }
            else if (ext.ToUpperInvariant() == "PMG")
            {
                _teamStats[TeamOrder[pt.teams[0]]].offset = 1813028;
            }

            // Stage 2
            if (ext.ToUpperInvariant() != "PMG")
            {
                for (int i = 1; i < 30; i++)
                {
                    _teamStats[i].offset = _teamStats[i - 1].offset + 40;
                }
                int inPlayoffs = checkIfIntoPlayoffs(fn, _teamStats, ref TeamOrder, ref pt);
                if (inPlayoffs == 1)
                {
                    _teamStats[TeamOrder[pt.teams[0]]].offset = _teamStats[0].offset - 1440;
                    for (int i = 1; i < 16; i++)
                    {
                        _teamStats[TeamOrder[pt.teams[i]]].offset = _teamStats[TeamOrder[pt.teams[i - 1]]].offset + 40;
                    }
                }
                else if (inPlayoffs == -1) return;
            }
            else
            {
                for (int i = 1; i < 16; i++)
                {
                    _teamStats[TeamOrder[pt.teams[i]]].offset = _teamStats[TeamOrder[pt.teams[i - 1]]].offset + 40;
                }
            }
        }

        public static int checkIfIntoPlayoffs(string fn, TeamStats[] _teamStats, ref SortedDictionary<string, int> TeamOrder, ref PlayoffTree pt)
        {
            int gamesInSeason = -1;
            string ptFile = "";
            string safefn = Tools.getSafeFilename(fn);
            string SettingsFile = AppDocsPath + safefn + ".cfg";
            string team = "";

            if (File.Exists(SettingsFile))
            {
                StreamReader sr = new StreamReader(SettingsFile);
                while (sr.Peek() > -1)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split('\t');
                    if (parts[0] == fn)
                    {
                        try
                        {
                            gamesInSeason = Convert.ToInt32(parts[1]);
                            ptFile = parts[2];
                            team = parts[3];

                            TeamOrder = StatsTracker.setTeamOrder(team);
                        }
                        catch
                        {
                            gamesInSeason = -1;
                        }
                        break;
                    }
                }
                sr.Close();
            }
            if (gamesInSeason == -1)
            {
                gamesInSeason = StatsTracker.askGamesInSeason(gamesInSeason);

                mode = StatsTracker.askMode();

                TeamOrder = StatsTracker.setTeamOrder(mode);

                saveSettingsForFile(fn, gamesInSeason, "", team, SettingsFile);
            }

            BinaryReader br = new BinaryReader(File.OpenRead(fn));
            MemoryStream ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true);
            br.Close();

            bool done = true;

            if (ptFile == "")
            {
                for (int i = 0; i < 30; i++)
                {
                    ms.Seek(_teamStats[i].offset, SeekOrigin.Begin);
                    byte w = (byte)ms.ReadByte();
                    byte l = (byte)ms.ReadByte();
                    uint total = Convert.ToUInt32(w + l);
                    if (total < gamesInSeason)
                    {
                        done = false;
                        break;
                    }
                }
            }

            if (done == true)
            {
                if (ptFile == "")
                {
                    pt = null;
                    pt = new PlayoffTree();
                    tempPT = new PlayoffTree();
                    playoffTreeW ptW = new playoffTreeW();
                    ptW.ShowDialog();
                    pt = tempPT;

                    if (!pt.done) return -1;

                    SaveFileDialog spt = new SaveFileDialog();
                    spt.Title = "Please select a file to save the Playoff Tree to...";
                    spt.InitialDirectory = AppDocsPath;
                    spt.Filter = "Playoff Tree files (*.ptr)|*.ptr";
                    spt.ShowDialog();

                    if (spt.FileName == "") return -1;

                    ptFile = spt.FileName;

                    try
                    {
                        FileStream stream = File.Open(spt.FileName, FileMode.Create);
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

                        bf.Serialize(stream, pt);
                        stream.Close();
                    }
                    catch (Exception ex)
                    {
                        App.errorReport(ex, "Trying to save playoff tree");
                    }
                }
                else
                {
                    FileStream stream = File.Open(ptFile, FileMode.Open);
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

                    pt = (PlayoffTree)bf.Deserialize(stream);
                    stream.Close();
                }
            }

            saveSettingsForFile(fn, gamesInSeason, ptFile, team, SettingsFile);

            if (done) return 1;
            else return 0;
        }

        private static string askMode()
        {
            askTeamW at = new askTeamW(false);
            at.ShowDialog();
            return mode;
        }

        public static void saveSettingsForFile(string fn, int gamesInSeason, string ptFile, string team, string SettingsFile)
        {
            StreamWriter sw2 = new StreamWriter(SettingsFile, false);
            sw2.WriteLine("{0}\t{1}\t{2}\t{3}", fn, gamesInSeason, ptFile, team);
            sw2.Close();
        }

        public static void updateSavegame(string fn, TeamStats[] tst, SortedDictionary<string, int> TeamOrder, PlayoffTree pt)
        {
            BinaryReader br = new BinaryReader(File.OpenRead(fn));
            MemoryStream ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true);

            if ((pt != null) && (pt.teams[0] != "Invalid"))
            {
                for (int i = 0; i < 16; i++)
                {
                    ms.Seek(tst[TeamOrder[pt.teams[i]]].offset, SeekOrigin.Begin);
                    ms.Write(tst[TeamOrder[pt.teams[i]]].winloss, 0, 2);
                    for (int j = 0; j < 18; j++)
                    {
                        ms.Write(BitConverter.GetBytes(tst[TeamOrder[pt.teams[i]]].stats[j]), 0, 2);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 30; i++)
                {
                    ms.Seek(tst[i].offset, SeekOrigin.Begin);
                    ms.Write(tst[i].winloss, 0, 2);
                    for (int j = 0; j < 18; j++)
                    {
                        ms.Write(BitConverter.GetBytes(tst[i].stats[j]), 0, 2);
                    }
                }
            }

            BinaryWriter bw = new BinaryWriter(File.OpenWrite(AppTempPath + Tools.getSafeFilename(fn)));
            ms.Position = 4;
            byte[] t = new byte[1048576];
            int count;
            do
            {
                count = ms.Read(t, 0, 1048576);
                bw.Write(t, 0, count);
            } while (count > 0);

            br.Close();
            bw.Close();

            byte[] crc = Tools.ReverseByteOrder(Tools.StringToByteArray(Tools.getCRC(AppTempPath + Tools.getSafeFilename(fn))), 4);

            try
            {
                File.Delete(fn + ".bak");
            }
            catch
            {
            }
            File.Move(fn, fn + ".bak");
            BinaryReader br2 = new BinaryReader(File.OpenRead(AppTempPath + Tools.getSafeFilename(fn)));
            BinaryWriter bw2 = new BinaryWriter(File.OpenWrite(fn));
            bw2.Write(crc);
            do
            {
                t = br2.ReadBytes(1048576);
                bw2.Write(t);
            } while (t.Length > 0);
            br2.Close();
            bw2.Close();

            File.Delete(AppTempPath + Tools.getSafeFilename(fn));
        }

        public static string averagesAndRankings(string teamName, TeamStats[] tst, SortedDictionary<string, int> TeamOrder)
        {
            int id = -1;
            try
            {
                id = TeamOrder[teamName];
            }
            catch
            {
                return "";
            }
            int[][] rating = StatsTracker.calculateRankings(tst);
            string text = String.Format("Win %: {32:F3} ({33})\nWin eff: {34:F2} ({35})\n\nPPG: {0:F1} ({16})\nPAPG: {1:F1} ({17})\n\nFG%: {2:F3} ({18})\nFGeff: {3:F2} ({19})\n3P%: {4:F3} ({20})\n3Peff: {5:F2} ({21})\n"
                    + "FT%: {6:F3} ({22})\nFTeff: {7:F2} ({23})\n\nRPG: {8:F1} ({24})\nORPG: {9:F1} ({25})\nDRPG: {10:F1} ({26})\n\nSPG: {11:F1} ({27})\nBPG: {12:F1} ({28})\n"
                    + "TPG: {13:F1} ({29})\nAPG: {14:F1} ({30})\nFPG: {15:F1} ({31})",
                    tst[id].averages[PPG], tst[id].averages[PAPG], tst[id].averages[FGp],
                    tst[id].averages[FGeff], tst[id].averages[TPp], tst[id].averages[TPeff],
                    tst[id].averages[FTp], tst[id].averages[FTeff], tst[id].averages[RPG], tst[id].averages[ORPG], tst[id].averages[DRPG], tst[id].averages[SPG],
                    tst[id].averages[BPG], tst[id].averages[TPG], tst[id].averages[APG], tst[id].averages[FPG],
                    rating[id][0], 31 - rating[id][1], rating[id][2], rating[id][3], rating[id][4], rating[id][5], rating[id][6], rating[id][7], rating[id][8], rating[id][9],
                    rating[id][10], rating[id][11], rating[id][12], 31 - rating[id][13], rating[id][14], 31 - rating[id][15], tst[id].averages[Wp], rating[id][16], tst[id].averages[Weff], rating[id][Weff]);
            return text;
        }

        public static string scoutReport(int[][] rating, int teamID, string teamName)
        {
            //public const int PPG = 0, PAPG = 1, FGp = 2, FGeff = 3, TPp = 4, TPeff = 5,
            //FTp = 6, FTeff = 7, RPG = 8, ORPG = 9, DRPG = 10, SPG = 11, BPG = 12,
            //TPG = 13, APG = 14, FPG = 15, Wp = 16, Weff = 17;
            string msg;
            msg = String.Format("{0}, the {1}", teamName, rating[teamID][17]);
            switch (rating[teamID][17])
            {
                case 1:
                case 21:
                    msg += "st";
                    break;
                case 2:
                case 22:
                    msg += "nd";
                    break;
                case 3:
                case 23:
                    msg += "rd";
                    break;
                default:
                    msg += "th";
                    break;
            }
            msg += " strongest team in the league right now, after having played " + rating[teamID][18].ToString() + " games.\n\n";

            if ((rating[teamID][3] <= 5) && (rating[teamID][5] <= 5))
            {
                if (rating[teamID][7] <= 5)
                {
                    msg += "This team just can't be beaten offensively. One of the strongest in the league in all aspects.";
                }
                else
                {
                    msg += "Great team offensively. Even when they don't get to the line, they know how to raise the bar with "
                        + "efficiency in both 2 and 3 pointers.";
                }
            }
            else if ((rating[teamID][3] <= 10) && (rating[teamID][5] <= 10))
            {
                if (rating[teamID][7] <= 10)
                {
                    msg += "Top 10 in the league in everything offense, and they're one to worry about.";
                }
                else
                {
                    msg += "Although their free throwing is not on par with their other offensive qualities, you can't relax "
                        + "when playing against them. Top 10 in field goals and 3 pointers.";
                }
            }
            else if ((rating[teamID][3] <= 20) && (rating[teamID][5] <= 20))
            {
                if (rating[teamID][7] <= 10)
                {
                    msg += "Although an average offensive team (they can't seem to remain consistent from both inside and "
                    + "outside the arc), they can get back at you with their efficiency from the line.";
                }
                else
                {
                    msg += "Average offensive team. Not really efficient in anything they do when they bring the ball down "
                        + "the court.";
                }
            }
            else
            {
                if (rating[teamID][7] <= 10)
                {
                    msg += "They aren't consistent from the floor, but still manage to get to the line enough times and "
                        + "be good enough to make a difference.";
                }
                else
                {
                    msg += "One of the most inconsistent teams at the offensive end, and they aren't efficient enough from "
                        + "the line to make up for it.";
                }
            }
            msg += "\n\n";

            if (rating[teamID][3] <= 5)
                msg += "Top scoring team, one of the top 5 in field goal efficiency.";
            else if (rating[teamID][3] <= 10)
                msg += "You'll have to worry about their scoring efficiency, as they're one of the Top 10 in the league.";
            else if (rating[teamID][3] <= 20)
                msg += "Scoring is not their virtue, but they're not that bad either.";
            else if (rating[teamID][3] <= 30)
                msg += "You won't have to worry about their scoring, one of the least 10 efficient in the league.";

            int comp = rating[teamID][FGeff] - rating[teamID][FGp];
            if (comp < -15)
                msg += "\nThey score more baskets than their FG% would have you guess, but they need to work on getting more consistent.";
            else if (comp > 15)
                msg += "\nThey can be dangerous whenever they shoot the ball. Their offense just doesn't get them enough chances to shoot it, though.";

            msg += "\n";

            if (rating[teamID][5] <= 5)
                msg += "You'll need to always have an eye on the perimeter. They can turn a game around with their 3 pointers. "
                    + "They score well, they score a lot.";
            else if (rating[teamID][5] <= 10)
                msg += "Their 3pt shooting is bad news. They're in the top 10, and you can't relax playing against them.";
            else if (rating[teamID][5] <= 20)
                msg += "Not much to say about their 3pt shooting. Average, but it is there.";
            else if (rating[teamID][5] <= 30)
                msg += "Definitely not a threat from 3pt land, one of the worst in the league. They waste too many shots from there.";

            comp = rating[teamID][TPeff] - rating[teamID][TPp];
            if (comp < -15)
                msg += "\nThey'll get enough 3 pointers to go down each night, but not on a good enough percentage for that amount.";
            else if (comp > 15)
                msg += "\nWith their accuracy from the 3PT line, you'd think they'd shoot more of those.";

            msg += "\n";

            if (rating[teamID][7] <= 5)
                msg += "They tend to attack the lanes hard, getting to the line and making the most of it. They're one of the best "
                    + "teams in the league at it.";
            else if (rating[teamID][7] <= 10)
                msg += "One of the best teams in the league at getting to the line. They get enough free throws to punish the opposing team every night. Top 10.";
            else if (rating[teamID][7] <= 20)
                msg += "Average free throw efficiency, you don't have to worry about sending them to the line; at least as much as other aspects of their game.";
            else if (rating[teamID][7] <= 30)
                if (rating[teamID][FTp] < 15)
                    msg += "A team that you'll enjoy playing hard and aggressively against on defense. They don't know how to get to the line.";
                else
                    msg += "A team that doesn't know how to get to the line, or how to score from there. You don't have to worry about freebies against them.";

            comp = rating[teamID][FTeff] - rating[teamID][FTp];
            if (comp < -15)
                msg += "\nAlthough they get to the line a lot and make some free throws, they have to put up a lot to actually get that amount each night.";
            else if (comp > 15)
                msg += "\nThey're lethal when shooting free throws, but they need to play harder and get there more often.";

            msg += "\n";

            if (rating[teamID][14] <= 15)
                msg += "They know how to find the open man, and they get their offense going by getting it around the perimeter until a clean shot is there.";
            else if ((rating[teamID][14] > 15) && (rating[teamID][3] < 10))
                msg += "A team that prefers to run its offense through its core players in isolation. Not very good in assists, but they know how to get the job "
                    + "done more times than not.";
            else
                msg += "A team that seems to have some selfish players around, nobody really that efficient to carry the team into high percentages.";

            msg += "\n\n";

            if (31 - rating[teamID][PAPG] <= 5)
                msg += "Don't expect to get your score high against them. An elite defensive team, top 5 in points against them each night.";
            else if (31 - rating[teamID][PAPG] <= 10)
                msg += "One of the better defensive teams out there, limiting their opponents to low scores night in, night out.";
            else if (31 - rating[teamID][PAPG] <= 20)
                msg += "Average defensively, not much to show for it, but they're no blow-outs.";
            else if (31 - rating[teamID][PAPG] <= 30)
                msg += "This team has just forgotten what defense is. They're one of the 10 easiest teams to score against.";

            msg += "\n\n";

            if ((rating[teamID][9] <= 10) && (rating[teamID][11] <= 10) && (rating[teamID][12] <= 10))
                msg += "Hustle is their middle name. They attack the offensive glass, they block, they steal. Don't even dare to blink or get complacent.\n\n";
            else if ((rating[teamID][9] >= 20) && (rating[teamID][11] >= 20) && (rating[teamID][12] >= 20))
                msg += "This team just doesn't know what hustle means. You'll be doing circles around them if you're careful.\n\n";

            if (rating[teamID][8] <= 5)
                msg += "Sensational rebounding team, everybody jumps for the ball, no missed shot is left loose.";
            else if (rating[teamID][8] <= 10)
                msg += "You can't ignore their rebounding ability, they work together and are in the top 10 in rebounding.";
            else if (rating[teamID][8] <= 20)
                msg += "They crash the boards as much as the next guy, but they won't give up any freebies.";
            else if (rating[teamID][8] <= 30)
                msg += "Second chance points? One of their biggest fears. Low low LOW rebounding numbers; just jump for the ball and you'll keep your score high.";

            msg += " ";

            if ((rating[teamID][9] <= 10) && (rating[teamID][10] <= 10))
                msg += "The work they put on rebounding on both sides of the court is commendable. Both offensive and defensive rebounds, their bread and butter.";

            msg += "\n\n";

            if ((rating[teamID][11] <= 10) && (rating[teamID][12] <= 10))
                msg += "A team that knows how to play defense. They're one of the best in steals and blocks, and they make you work hard on offense.\n";
            else if (rating[teamID][11] <= 10)
                msg += "Be careful dribbling and passing. They won't be much trouble once you shoot the ball, but the trouble is getting there. Great in steals.\n";
            else if (rating[teamID][12] <= 10)
                msg += "Get that thing outta here! Great blocking team, they turn the lights off on any mismatched jumper or drive; sometimes even when you least expect it.\n";

            if ((rating[teamID][13] <= 10) && (rating[teamID][15] <= 10))
                msg += "Clumsy team to say the least. They're not careful with the ball, and they foul too much. Keep your eyes open and play hard.";
            else if (rating[teamID][13] < 10)
                msg += "Not good ball handlers, and that's being polite. Bottom 10 in turnovers, they have work to do until they get their offense going.";
            else if (rating[teamID][15] < 10)
                msg += "A team that's prone to fouling. You better drive the lanes as hard as you can, you'll get to the line a lot.";
            else
                msg += "This team is careful with and without the ball. They're good at keeping their turnovers down, and don't foul too much.\nDon't throw "
                    + "your players into steals or fouls against them, because they play smart, and you're probably going to see the opposite call than the "
                    + "one you expected.";

            return msg;
        }

        public static TeamStats getRealStats(string team, bool useLocal = false)
        {
            TeamStats ts = new TeamStats();
            WebClient web = new WebClient();
            string file = AppDocsPath + team + ".rst";

            Dictionary<string, string> TeamNamesShort = new Dictionary<string, string>
            {
                {"76ers", "PHI"},
                {"Bobcats", "CHA"},
                {"Bucks", "MIL"},
                {"Bulls", "CHI"},
                {"Cavaliers", "CLE"},
                {"Celtics", "BOS"},
                {"Clippers", "LAC"},
                {"Grizzlies", "MEM"},
                {"Hawks", "ATL"},
                {"Heat", "MIA"},
                {"Hornets", "NOH"},
                {"Jazz", "UTA"},
                {"Kings", "SAC"},
                {"Knicks", "NYK"},
                {"Lakers", "LAL"},
                {"Magic", "ORL"},
                {"Mavericks", "DAL"},
                {"Nets", "NJN"},
                {"Nuggets", "DEN"},
                {"Pacers", "IND"},
                {"Pistons", "DET"},
                {"Raptors", "TOR"},
                {"Rockets", "HOU"},
                {"Spurs", "SAS"},
                {"Suns", "PHO"},
                {"Thunder", "OKC"},
                {"Timberwolves", "MIN"},
                {"Trail Blazers", "POR"},
                {"Warriors", "GSW"},
                {"Wizards", "WAS"}
            };

            ts.name = team;
            string tns = TeamNamesShort[team];
            if (!useLocal)
            {
                web.DownloadFile("http://www.basketball-reference.com/teams/" + tns + "/2012.html", file);
            }
            if (File.Exists(file))
            {
                grs_getStats(ref ts, file);

                if (errorRealStats)
                {
                    web.DownloadFile("http://www.basketball-reference.com/teams/" + tns + "/2012.html", file);
                    grs_getStats(ref ts, file);
                }

                ts.calcAvg();
            }
            else
            {
                ts.name = "Error";
            }
            return ts;
        }

        private static void grs_getStats(ref TeamStats ts, string file)
        {
            errorRealStats = false;
            StreamReader sr = new StreamReader(file);
            string line;
            try
            {
                do
                {
                    line = sr.ReadLine();
                } while (line.Contains("Team Splits") == false);
            }
            catch
            {
                errorRealStats = true;
                sr.Close();
                return;
            }

            for (int i = 0; i < 3; i++)
                line = sr.ReadLine();

            // <p><strong>3-10
            string[] parts1 = line.Split('>');
            string[] parts2 = parts1[2].Split('<');
            string[] _winloss = parts2[0].Split('-');
            ts.winloss[0] = Convert.ToByte(_winloss[0]);
            ts.winloss[1] = Convert.ToByte(_winloss[1]);

            do
            {
                line = sr.ReadLine();
            } while (line.Contains("<div class=\"table_container\" id=\"div_team\">") == false);
            do
            {
                line = sr.ReadLine();
            } while (line.Contains("<td align=\"left\" >Team</td>") == false);

            grs_GetNextStat(ref sr); // Skip games played
            ts.stats[M] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[FGM] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[FGA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip FG%
            ts.stats[TPM] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[TPA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip 3G%
            ts.stats[FTM] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[FTA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip FT%
            ts.stats[OREB] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[DREB] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip Total Rebounds
            ts.stats[AST] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[STL] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[BLK] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[TO] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[FOUL] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[PF] = Convert.ToUInt16(grs_GetNextStat(ref sr));

            do
            {
                line = sr.ReadLine();
            } while (line.Contains("<td align=\"left\" >Opponent</td>") == false);

            for (int i = 0; i < 19; i++)
                line = sr.ReadLine();

            ts.stats[PA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            sr.Close();
        }

        private static string grs_GetNextStat(ref StreamReader sr)
        {
            string line = sr.ReadLine();
            string[] parts1 = line.Split('>');
            string[] parts2 = parts1[1].Split('<');
            return parts2[0];
        }
    }

    [Serializable()]
    public class TeamStats : ISerializable
    {
        public string name;
        public Int32 offset = 0;
        public const int M = 0, PF = 1, PA = 2, FGM = 4, FGA = 5, TPM = 6, TPA = 7,
            FTM = 8, FTA = 9, OREB = 10, DREB = 11, STL = 12, TO = 13, BLK = 14, AST = 15,
            FOUL = 16;
        public const int PPG = 0, PAPG = 1, FGp = 2, FGeff = 3, TPp = 4, TPeff = 5,
            FTp = 6, FTeff = 7, RPG = 8, ORPG = 9, DRPG = 10, SPG = 11, BPG = 12,
            TPG = 13, APG = 14, FPG = 15, Wp = 16, Weff = 17;
        /// <summary>
        /// Stats for each team.
        /// 0: M, 1: PF, 2: PA, 3: 0x0000, 4: FGM, 5: FGA, 6: 3PM, 7: 3PA, 8: FTM, 9: FTA,
        /// 10: OREB, 11: DREB, 12: STL, 13: TO, 14: BLK, 15: AST,
        /// 16: FOUL
        /// </summary>
        public UInt16[] stats = new UInt16[18];
        /// <summary>
        /// Averages for each team.
        /// 0: PPG, 1: PAPG, 2: FG%, 3: FGEff, 4: 3P%, 5: 3PEff, 6: FT%, 7:FTEff,
        /// 8: RPG, 9: ORPG, 10: DRPG, 11: SPG, 12: BPG, 13: TPG, 14: APG, 15: FPG, 16: W%
        /// </summary>
        public float[] averages = new float[18];
        public byte[] winloss = new byte[2];

        public TeamStats()
        {
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("name", name);
            info.AddValue("stats", stats);
            info.AddValue("winloss", winloss);
        }

        public TeamStats(SerializationInfo info, StreamingContext ctxt)
        {
            name = (string)info.GetValue("name", typeof(string));
            stats = (UInt16[])info.GetValue("stats", typeof(UInt16[]));
            winloss = (byte[])info.GetValue("winloss", typeof(byte[]));
        }

        public int calcAvg()
        {
            int games = winloss[0] + winloss[1];
            if (games == 0) games = -1;
            averages[Wp] = (float)winloss[0] / games;
            averages[Weff] = averages[Wp] * winloss[0];
            averages[PPG] = (float)stats[PF] / games;
            averages[PAPG] = (float)stats[PA] / games;
            averages[FGp] = (float)stats[FGM] / stats[FGA];
            averages[FGeff] = averages[FGp] * ((float)stats[FGM] / games);
            averages[TPp] = (float)stats[TPM] / stats[TPA];
            averages[TPeff] = averages[TPp] * ((float)stats[TPM] / games);
            averages[FTp] = (float)stats[FTM] / stats[FTA];
            averages[FTeff] = averages[FTp] * ((float)stats[FTM] / games);
            averages[RPG] = (float)(stats[OREB] + stats[DREB]) / games;
            averages[ORPG] = (float)stats[OREB] / games;
            averages[DRPG] = (float)stats[DREB] / games;
            averages[SPG] = (float)stats[STL] / games;
            averages[BPG] = (float)stats[BLK] / games;
            averages[TPG] = (float)stats[TO] / games;
            averages[APG] = (float)stats[AST] / games;
            averages[FPG] = (float)stats[FOUL] / games;
            return games;
        }

        internal int getGames()
        {
            int games = winloss[0] + winloss[1];
            return games;
        }
    }

    public class Rankings
    {
        public const int PPG = 0, PAPG = 1, FGp = 2, FGeff = 3, TPp = 4, TPeff = 5,
            FTp = 6, FTeff = 7, RPG = 8, ORPG = 9, DRPG = 10, SPG = 11, BPG = 12,
            TPG = 13, APG = 14, FPG = 15, Wp = 16, Weff = 17;

        public int[][] rankings = new int[30][];

        public Rankings(TeamStats[] _tst)
        {
            for (int i = 0; i < 30; i++)
            {
                rankings[i] = new int[18];
            }
            for (int j = 0; j < 18; j++)
            {
                Dictionary<int, float> averages = new Dictionary<int,float>();
                for (int i = 0; i<30;i++)
                {
                    averages.Add(i, _tst[i].averages[j]);
                }

                List<KeyValuePair<int, float>> tempList = new List<KeyValuePair<int,float>>(averages);
                tempList.Sort((x,y) => x.Value.CompareTo(y.Value));
                tempList.Reverse();

                int k = 1;
                foreach (KeyValuePair<int, float> kvp in tempList)
                {
                    rankings[kvp.Key][j] = k;
                    k++;
                }
            }
        }
    }

    public class BoxScore
    {
        public string Team1;
        public string Team2;
        public UInt16 PTS1, REB1, AST1, STL1, BLK1, TO1, FGM1, FGA1, TPM1, TPA1, FTM1, FTA1, OFF1, PF1;
        public UInt16 PTS2, REB2, AST2, STL2, BLK2, TO2, FGM2, FGA2, TPM2, TPA2, FTM2, FTA2, OFF2, PF2;
        public bool done = false;
    }

    public class BoxScoreEntry
    {
        public BoxScore bs;
        public DateTime date;

        public BoxScoreEntry(BoxScore bs)
        {
            this.bs = bs;
            this.date = DateTime.Now;
        }

        public BoxScoreEntry(BoxScore bs, DateTime date)
        {
            this.bs = bs;
            this.date = date;
        }
    }

    [Serializable()]
    public class PlayoffTree : ISerializable
    {
        public string[] teams = new string[16];
        public bool done = false;

        public PlayoffTree()
        {
            this.teams[0] = "Invalid";
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("teams", teams);
            info.AddValue("done", done);
        }

        public PlayoffTree(SerializationInfo info, StreamingContext ctxt)
        {
            teams = (string[])info.GetValue("teams", typeof(string[]));
            done = (bool)info.GetValue("done", typeof(bool));
        }
    }
}
