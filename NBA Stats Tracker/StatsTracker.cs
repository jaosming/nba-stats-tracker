﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Documents;
using HtmlAgilityPack;
using LeftosCommonLibrary;
using Microsoft.Win32;

namespace NBA_Stats_Tracker
{
    internal class NSTHelper
    {
        public const int tMINS = 0, tPF = 1, tPA = 2, tFGM = 4, tFGA = 5, tTPM = 6, tTPA = 7, tFTM = 8, tFTA = 9, tOREB = 10, tDREB = 11, tSTL = 12, tTO = 13, tBLK = 14, tAST = 15, tFOUL = 16;

        public const int tPPG = 0, tPAPG = 1, tFGp = 2, tFGeff = 3, tTPp = 4, tTPeff = 5, tFTp = 6, tFTeff = 7, tRPG = 8, tORPG = 9, tDRPG = 10, tSPG = 11, tBPG = 12, tTPG = 13, tAPG = 14, tFPG = 15, tWp = 16, tWeff = 17, tPD = 18;

        public const int pGP = 0, pGS = 1, pMINS = 2, pPTS = 3, pDREB = 4, pOREB = 5, pAST = 6, pSTL = 7, pBLK = 8, pTO = 9, pFOUL = 10, pFGM = 11, pFGA = 12, pTPM = 13, pTPA = 14, pFTM = 15, pFTA = 16;

        public const int pMPG = 0, pPPG = 1, pDRPG = 2, pORPG = 3, pAPG = 4, pSPG = 5, pBPG = 6, pTPG = 7, pFPG = 8, pFGp = 9, pFGeff = 10, pTPp = 11, pTPeff = 12, pFTp = 13, pFTeff = 14, pRPG = 15;

        public static string AppDocsPath = MainWindow.AppDocsPath;
        public static string SavesPath = MainWindow.SavesPath;
        public static string AppTempPath = MainWindow.AppTempPath;
        public static string mode = "Mode 0";
        public static bool errorRealStats;

        public static PlayoffTree tempPT;

        public static void CalculateAllMetrics(ref Dictionary<int, PlayerStats> playerStats, TeamStats[] teamStats, TeamStats[] oppStats, bool leagueOv = false)
        {
            int tCount = teamStats.Length;

            TeamStats ls = new TeamStats();
            TeamStats lsopp = new TeamStats();
            TeamStats[] tst = new TeamStats[tCount];
            TeamStats[] tstopp = new TeamStats[tCount];
            for (int i = 0; i < tCount; i++)
            {
                ls.AddTeamStats(teamStats[i], "All");
                tst[i] = new TeamStats();
                tst[i].AddTeamStats(teamStats[i], "All");
                lsopp.AddTeamStats(oppStats[i], "All");
                tstopp[i] = new TeamStats();
                tstopp[i].AddTeamStats(oppStats[i], "All");
                tst[i].CalcMetrics(tstopp[i]);
            }
            ls.CalcMetrics(lsopp);

            double lg_aPER = 0;
            double totalMins = 0;

            foreach (var playerid in playerStats.Keys.ToList())
            {
                int teamid = MainWindow.TeamOrder[playerStats[playerid].TeamF];
                TeamStats ts = tst[teamid];
                TeamStats tsopp = tstopp[teamid];

                playerStats[playerid].CalcMetrics(ts, tsopp, ls, leagueOv);
                if (!(Double.IsNaN(playerStats[playerid].metrics["aPER"])))
                {
                    lg_aPER += playerStats[playerid].metrics["aPER"]*playerStats[playerid].stats[pMINS];
                    totalMins += playerStats[playerid].stats[pMINS];
                }
            }
            lg_aPER /= totalMins;

            foreach (var playerid in playerStats.Keys.ToList())
            {
                playerStats[playerid].CalcPER(lg_aPER);
            }
        }

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

                case "Mode 6":
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
                                        {"Kings", 14},
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
                                        {"Suns", 2},
                                        {"Thunder", 24},
                                        {"Timberwolves", 17},
                                        {"Trail Blazers", 1},
                                        {"Warriors", 7},
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

            var checklist = new List<int>();
            foreach (var kvp in TeamOrder)
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
            MessageBoxResult r =
                MessageBox.Show(
                    "How many games does each season have in this save?\n\n82 Games: Yes\n58 Games: No\n29 Games: Cancel",
                    "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) gamesInSeason = 82;
            else if (r == MessageBoxResult.No) gamesInSeason = 58;
            else if (r == MessageBoxResult.Cancel) gamesInSeason = 28;
            return gamesInSeason;
        }

        public static int[][] calculateRankings(TeamStats[] _teamStats, bool playoffs = false)
        {
            int len = _teamStats.GetLength(0);
            var rating = new int[len][];
            for (int i = 0; i < len; i++)
            {
                rating[i] = new int[20];
            }
            for (int k = 0; k < len; k++)
            {
                for (int i = 0; i < 19; i++)
                {
                    rating[k][i] = 1;
                    for (int j = 0; j < len; j++)
                    {
                        if (j != k)
                        {
                            if (!playoffs)
                            {
                                if (_teamStats[j].averages[i] > _teamStats[k].averages[i])
                                {
                                    rating[k][i]++;
                                }
                            }
                            else
                            {
                                if (_teamStats[j].pl_averages[i] > _teamStats[k].pl_averages[i])
                                {
                                    rating[k][i]++;
                                }
                            }
                        }
                    }
                }
                rating[k][19] = _teamStats[k].getGames();
            }
            return rating;
        }

        public static void GetStatsFrom2K12Save(string fn, ref TeamStats[] tst, ref TeamStats[] tstopp, ref SortedDictionary<string, int> TeamOrder, ref PlayoffTree pt,
                                           bool havePT = false)
        {
            var _teamStats = new TeamStats[30];
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
                    MessageBoxResult r =
                        MessageBox.Show("Do you have a saved Playoff Tree you want to load for this save file?",
                                        "NBA Stats Tracker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (r == MessageBoxResult.No)
                    {
                        var ptw = new playoffTreeW();
                        ptw.ShowDialog();
                        if (!pt.done)
                        {
                            tst = new TeamStats[1];
                            return;
                        }

                        var spt = new SaveFileDialog();
                        spt.Title = "Please select a file to save the Playoff Tree to...";
                        spt.InitialDirectory = AppDocsPath;
                        spt.Filter = "Playoff Tree files (*.ptr)|*.ptr";
                        spt.ShowDialog();

                        if (spt.FileName == "")
                        {
                            tst = new TeamStats[1];
                            return;
                        }

                        try
                        {
                            FileStream stream = File.Open(spt.FileName, FileMode.Create);
                            var bf = new BinaryFormatter();
                            bf.AssemblyFormat = FormatterAssemblyStyle.Simple;

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
                        var ofd = new OpenFileDialog();
                        ofd.Filter = "Playoff Tree files (*.ptr)|*.ptr";
                        ofd.InitialDirectory = AppDocsPath;
                        ofd.Title = "Please select the file you saved the Playoff Tree to for " +
                                    Tools.getSafeFilename(fn) + "...";
                        ofd.ShowDialog();

                        if (ofd.FileName == "")
                        {
                            tst = new TeamStats[1];
                            return;
                        }

                        FileStream stream = File.Open(ofd.FileName, FileMode.Open);
                        var bf = new BinaryFormatter();
                        bf.AssemblyFormat = FormatterAssemblyStyle.Simple;

                        pt = (PlayoffTree) bf.Deserialize(stream);
                        stream.Close();
                    }
                    else
                    {
                        tst = new TeamStats[1];
                        return;
                    }
                }
            }
            prepareOffsets(fn, _teamStats, ref TeamOrder, ref pt);

            var br = new BinaryReader(File.OpenRead(fn));
            var ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true);
            br.Close();
            var buf = new byte[2];

            foreach (var kvp in TeamOrder)
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

            if (pt != null && pt.teams[0] != "Invalid")
            {
                for (int i = 0; i < 16; i++)
                {
                    int id = TeamOrder[pt.teams[i]];
                    ms.Seek(_teamStats[id].pl_offset, SeekOrigin.Begin);
                    ms.Read(buf, 0, 2);
                    _teamStats[id].name = pt.teams[i];
                    _teamStats[id].pl_winloss[0] = buf[0];
                    _teamStats[id].pl_winloss[1] = buf[1];
                    for (int j = 0; j < 18; j++)
                    {
                        ms.Read(buf, 0, 2);
                        _teamStats[id].pl_stats[j] = BitConverter.ToUInt16(buf, 0);
                    }
                }
            }

            for (int i = 0; i < _teamStats.Length; i++)
            {
                _teamStats[i].calcAvg();
            }

            tst = _teamStats;
            
            //TODO: Implement loading opponents stats from 2K12 save here
            /*
            tstopp = new TeamStats[tst.Length];
            for (int i = 0; i < tst.Length; i++)
            {
                tstopp[i] = new TeamStats(tst[i].name);
            }
            */
        }

        public static void prepareOffsets(string fn, TeamStats[] _teamStats, ref SortedDictionary<string, int> TeamOrder,
                                          ref PlayoffTree pt)
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
                    _teamStats[TeamOrder[pt.teams[0]]].pl_offset = _teamStats[0].offset - 1440;
                    for (int i = 1; i < 16; i++)
                    {
                        _teamStats[TeamOrder[pt.teams[i]]].pl_offset =
                            _teamStats[TeamOrder[pt.teams[i - 1]]].pl_offset + 40;
                    }
                }
                else if (inPlayoffs == -1) return;
            }
            else
            {
                for (int i = 1; i < 16; i++)
                {
                    _teamStats[TeamOrder[pt.teams[i]]].pl_offset = _teamStats[TeamOrder[pt.teams[i - 1]]].pl_offset + 40;
                }
            }
        }

        public static int checkIfIntoPlayoffs(string fn, TeamStats[] _teamStats,
                                              ref SortedDictionary<string, int> TeamOrder, ref PlayoffTree pt)
        {
            int gamesInSeason = -1;
            string ptFile = "";
            string safefn = Tools.getSafeFilename(fn);
            string SettingsFile = AppDocsPath + safefn + ".cfg";
            string mode = "";

            if (File.Exists(SettingsFile))
            {
                var sr = new StreamReader(SettingsFile);
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
                            mode = parts[3];

                            TeamOrder = setTeamOrder(mode);
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
                gamesInSeason = askGamesInSeason(gamesInSeason);

                mode = askMode();

                TeamOrder = setTeamOrder(mode);

                saveSettingsForFile(fn, gamesInSeason, "", mode, SettingsFile);
            }

            var br = new BinaryReader(File.OpenRead(fn));
            var ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true);
            br.Close();

            bool done = true;

            if (ptFile == "")
            {
                for (int i = 0; i < 30; i++)
                {
                    ms.Seek(_teamStats[i].offset, SeekOrigin.Begin);
                    var w = (byte) ms.ReadByte();
                    var l = (byte) ms.ReadByte();
                    uint total = Convert.ToUInt32(w + l);
                    if (total < gamesInSeason)
                    {
                        done = false;
                        break;
                    }
                }
            }

            if (done)
            {
                if (ptFile == "")
                {
                    pt = null;
                    pt = new PlayoffTree();
                    tempPT = new PlayoffTree();
                    var ptW = new playoffTreeW();
                    ptW.ShowDialog();
                    pt = tempPT;

                    if (!pt.done) return -1;

                    var spt = new SaveFileDialog();
                    spt.Title = "Please select a file to save the Playoff Tree to...";
                    spt.InitialDirectory = AppDocsPath;
                    spt.Filter = "Playoff Tree files (*.ptr)|*.ptr";
                    spt.ShowDialog();

                    if (spt.FileName == "") return -1;

                    ptFile = spt.FileName;

                    try
                    {
                        FileStream stream = File.Open(spt.FileName, FileMode.Create);
                        var bf = new BinaryFormatter();
                        bf.AssemblyFormat = FormatterAssemblyStyle.Simple;

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
                    var bf = new BinaryFormatter();
                    bf.AssemblyFormat = FormatterAssemblyStyle.Simple;

                    pt = (PlayoffTree) bf.Deserialize(stream);
                    stream.Close();
                }
            }

            saveSettingsForFile(fn, gamesInSeason, ptFile, mode, SettingsFile);

            if (done) return 1;
            else return 0;
        }

        private static string askMode()
        {
            var at = new askTeamW(false);
            at.ShowDialog();
            return mode;
        }

        public static void saveSettingsForFile(string fn, int gamesInSeason, string ptFile, string mode,
                                               string SettingsFile)
        {
            var sw2 = new StreamWriter(SettingsFile, false);
            sw2.WriteLine("{0}\t{1}\t{2}\t{3}", fn, gamesInSeason, ptFile, mode);
            sw2.Close();
        }

        public static void updateSavegame(string fn, TeamStats[] tst, SortedDictionary<string, int> TeamOrder,
                                          PlayoffTree pt)
        {
            var br = new BinaryReader(File.OpenRead(fn));
            var ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true);

            if ((pt != null) && (pt.teams[0] != "Invalid"))
            {
                for (int i = 0; i < 16; i++)
                {
                    ms.Seek(tst[TeamOrder[pt.teams[i]]].pl_offset, SeekOrigin.Begin);
                    ms.Write(tst[TeamOrder[pt.teams[i]]].pl_winloss, 0, 2);
                    for (int j = 0; j < 18; j++)
                    {
                        ms.Write(BitConverter.GetBytes(tst[TeamOrder[pt.teams[i]]].pl_stats[j]), 0, 2);
                    }
                }
            }

            for (int i = 0; i < 30; i++)
            {
                ms.Seek(tst[i].offset, SeekOrigin.Begin);
                ms.Write(tst[i].winloss, 0, 2);
                for (int j = 0; j < 18; j++)
                {
                    ms.Write(BitConverter.GetBytes(tst[i].stats[j]), 0, 2);
                }
            }

            var bw = new BinaryWriter(File.OpenWrite(AppTempPath + Tools.getSafeFilename(fn)));
            ms.Position = 4;
            var t = new byte[1048576];
            int count;
            do
            {
                count = ms.Read(t, 0, 1048576);
                bw.Write(t, 0, count);
            } while (count > 0);

            br.Close();
            bw.Close();

            byte[] crc =
                Tools.ReverseByteOrder(Tools.StringToByteArray(Tools.getCRC(AppTempPath + Tools.getSafeFilename(fn))), 4);

            try
            {
                File.Delete(fn + ".bak");
            }
            catch
            {
            }
            File.Move(fn, fn + ".bak");
            var br2 = new BinaryReader(File.OpenRead(AppTempPath + Tools.getSafeFilename(fn)));
            var bw2 = new BinaryWriter(File.OpenWrite(fn));
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

        public static string averagesAndRankings(string teamName, TeamStats[] tst,
                                                 SortedDictionary<string, int> TeamOrder)
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
            int[][] rating = calculateRankings(tst);
            string text =
                String.Format(
                    "Win %: {32:F3} ({33})\nWin eff: {34:F2} ({35})\n\nPPG: {0:F1} ({16})\nPAPG: {1:F1} ({17})\n\nFG%: {2:F3} ({18})\nFGeff: {3:F2} ({19})\n3P%: {4:F3} ({20})\n3Peff: {5:F2} ({21})\n"
                    +
                    "FT%: {6:F3} ({22})\nFTeff: {7:F2} ({23})\n\nRPG: {8:F1} ({24})\nORPG: {9:F1} ({25})\nDRPG: {10:F1} ({26})\n\nSPG: {11:F1} ({27})\nBPG: {12:F1} ({28})\n"
                    + "TPG: {13:F1} ({29})\nAPG: {14:F1} ({30})\nFPG: {15:F1} ({31})",
                    tst[id].averages[tPPG], tst[id].averages[tPAPG], tst[id].averages[tFGp],
                    tst[id].averages[tFGeff], tst[id].averages[tTPp], tst[id].averages[tTPeff],
                    tst[id].averages[tFTp], tst[id].averages[tFTeff], tst[id].averages[tRPG], tst[id].averages[tORPG],
                    tst[id].averages[tDRPG], tst[id].averages[tSPG],
                    tst[id].averages[tBPG], tst[id].averages[tTPG], tst[id].averages[tAPG], tst[id].averages[tFPG],
                    rating[id][0], tst.GetLength(0) + 1 - rating[id][1], rating[id][2], rating[id][3], rating[id][4],
                    rating[id][5], rating[id][6], rating[id][7], rating[id][8], rating[id][9],
                    rating[id][10], rating[id][11], rating[id][12], tst.GetLength(0) + 1 - rating[id][13],
                    rating[id][14], tst.GetLength(0) + 1 - rating[id][15], tst[id].averages[tWp], rating[id][16],
                    tst[id].averages[tWeff], rating[id][tWeff]);
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
            msg += " strongest team in the league right now, after having played " + rating[teamID][19].ToString() +
                   " games.\n\n";

            if ((rating[teamID][3] <= 5) && (rating[teamID][5] <= 5))
            {
                if (rating[teamID][7] <= 5)
                {
                    msg +=
                        "This team just can't be beaten offensively. One of the strongest in the league in all aspects.";
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
                msg +=
                    "You'll have to worry about their scoring efficiency, as they're one of the Top 10 in the league.";
            else if (rating[teamID][3] <= 20)
                msg += "Scoring is not their virtue, but they're not that bad either.";
            else if (rating[teamID][3] <= 30)
                msg += "You won't have to worry about their scoring, one of the least 10 efficient in the league.";

            int comp = rating[teamID][tFGeff] - rating[teamID][tFGp];
            if (comp < -15)
                msg +=
                    "\nThey score more baskets than their FG% would have you guess, but they need to work on getting more consistent.";
            else if (comp > 15)
                msg +=
                    "\nThey can be dangerous whenever they shoot the ball. Their offense just doesn't get them enough chances to shoot it, though.";

            msg += "\n";

            if (rating[teamID][5] <= 5)
                msg += "You'll need to always have an eye on the perimeter. They can turn a game around with their 3 pointers. "
                       + "They score well, they score a lot.";
            else if (rating[teamID][5] <= 10)
                msg +=
                    "Their 3pt shooting is bad news. They're in the top 10, and you can't relax playing against them.";
            else if (rating[teamID][5] <= 20)
                msg += "Not much to say about their 3pt shooting. Average, but it is there.";
            else if (rating[teamID][5] <= 30)
                msg +=
                    "Definitely not a threat from 3pt land, one of the worst in the league. They waste too many shots from there.";

            comp = rating[teamID][tTPeff] - rating[teamID][tTPp];
            if (comp < -15)
                msg +=
                    "\nThey'll get enough 3 pointers to go down each night, but not on a good enough percentage for that amount.";
            else if (comp > 15)
                msg += "\nWith their accuracy from the 3PT line, you'd think they'd shoot more of those.";

            msg += "\n";

            if (rating[teamID][7] <= 5)
                msg += "They tend to attack the lanes hard, getting to the line and making the most of it. They're one of the best "
                       + "teams in the league at it.";
            else if (rating[teamID][7] <= 10)
                msg +=
                    "One of the best teams in the league at getting to the line. They get enough free throws to punish the opposing team every night. Top 10.";
            else if (rating[teamID][7] <= 20)
                msg +=
                    "Average free throw efficiency, you don't have to worry about sending them to the line; at least as much as other aspects of their game.";
            else if (rating[teamID][7] <= 30)
                if (rating[teamID][tFTp] < 15)
                    msg +=
                        "A team that you'll enjoy playing hard and aggressively against on defense. They don't know how to get to the line.";
                else
                    msg +=
                        "A team that doesn't know how to get to the line, or how to score from there. You don't have to worry about freebies against them.";

            comp = rating[teamID][tFTeff] - rating[teamID][tFTp];
            if (comp < -15)
                msg +=
                    "\nAlthough they get to the line a lot and make some free throws, they have to put up a lot to actually get that amount each night.";
            else if (comp > 15)
                msg +=
                    "\nThey're lethal when shooting free throws, but they need to play harder and get there more often.";

            msg += "\n";

            if (rating[teamID][14] <= 15)
                msg +=
                    "They know how to find the open man, and they get their offense going by getting it around the perimeter until a clean shot is there.";
            else if ((rating[teamID][14] > 15) && (rating[teamID][3] < 10))
                msg += "A team that prefers to run its offense through its core players in isolation. Not very good in assists, but they know how to get the job "
                       + "done more times than not.";
            else
                msg +=
                    "A team that seems to have some selfish players around, nobody really that efficient to carry the team into high percentages.";

            msg += "\n\n";

            if (31 - rating[teamID][tPAPG] <= 5)
                msg +=
                    "Don't expect to get your score high against them. An elite defensive team, top 5 in points against them each night.";
            else if (31 - rating[teamID][tPAPG] <= 10)
                msg +=
                    "One of the better defensive teams out there, limiting their opponents to low scores night in, night out.";
            else if (31 - rating[teamID][tPAPG] <= 20)
                msg += "Average defensively, not much to show for it, but they're no blow-outs.";
            else if (31 - rating[teamID][tPAPG] <= 30)
                msg +=
                    "This team has just forgotten what defense is. They're one of the 10 easiest teams to score against.";

            msg += "\n\n";

            if ((rating[teamID][9] <= 10) && (rating[teamID][11] <= 10) && (rating[teamID][12] <= 10))
                msg +=
                    "Hustle is their middle name. They attack the offensive glass, they block, they steal. Don't even dare to blink or get complacent.\n\n";
            else if ((rating[teamID][9] >= 20) && (rating[teamID][11] >= 20) && (rating[teamID][12] >= 20))
                msg +=
                    "This team just doesn't know what hustle means. You'll be doing circles around them if you're careful.\n\n";

            if (rating[teamID][8] <= 5)
                msg += "Sensational rebounding team, everybody jumps for the ball, no missed shot is left loose.";
            else if (rating[teamID][8] <= 10)
                msg +=
                    "You can't ignore their rebounding ability, they work together and are in the top 10 in rebounding.";
            else if (rating[teamID][8] <= 20)
                msg += "They crash the boards as much as the next guy, but they won't give up any freebies.";
            else if (rating[teamID][8] <= 30)
                msg +=
                    "Second chance points? One of their biggest fears. Low low LOW rebounding numbers; just jump for the ball and you'll keep your score high.";

            msg += " ";

            if ((rating[teamID][9] <= 10) && (rating[teamID][10] <= 10))
                msg +=
                    "The work they put on rebounding on both sides of the court is commendable. Both offensive and defensive rebounds, their bread and butter.";

            msg += "\n\n";

            if ((rating[teamID][11] <= 10) && (rating[teamID][12] <= 10))
                msg +=
                    "A team that knows how to play defense. They're one of the best in steals and blocks, and they make you work hard on offense.\n";
            else if (rating[teamID][11] <= 10)
                msg +=
                    "Be careful dribbling and passing. They won't be much trouble once you shoot the ball, but the trouble is getting there. Great in steals.\n";
            else if (rating[teamID][12] <= 10)
                msg +=
                    "Get that thing outta here! Great blocking team, they turn the lights off on any mismatched jumper or drive; sometimes even when you least expect it.\n";

            if ((rating[teamID][13] <= 10) && (rating[teamID][15] <= 10))
                msg +=
                    "Clumsy team to say the least. They're not careful with the ball, and they foul too much. Keep your eyes open and play hard.";
            else if (rating[teamID][13] < 10)
                msg +=
                    "Not good ball handlers, and that's being polite. Bottom 10 in turnovers, they have work to do until they get their offense going.";
            else if (rating[teamID][15] < 10)
                msg +=
                    "A team that's prone to fouling. You better drive the lanes as hard as you can, you'll get to the line a lot.";
            else
                msg += "This team is careful with and without the ball. They're good at keeping their turnovers down, and don't foul too much.\nDon't throw "
                       +
                       "your players into steals or fouls against them, because they play smart, and you're probably going to see the opposite call than the "
                       + "one you expected.";

            return msg;
        }

        public static TeamStats getRealStats(string team, bool useLocal = false)
        {
            var ts = new TeamStats();
            var web = new WebClient();
            string file = AppDocsPath + team + ".rst";

            var TeamNamesShort = new Dictionary<string, string>
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
            var sr = new StreamReader(file);
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
            ts.stats[tMINS] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tFGM] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tFGA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip FG%
            ts.stats[tTPM] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tTPA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip 3G%
            ts.stats[tFTM] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tFTA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip FT%
            ts.stats[tOREB] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tDREB] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            grs_GetNextStat(ref sr); // Skip Total Rebounds
            ts.stats[tAST] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tSTL] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tBLK] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tTO] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tFOUL] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            ts.stats[tPF] = Convert.ToUInt16(grs_GetNextStat(ref sr));

            do
            {
                line = sr.ReadLine();
            } while (line.Contains("<td align=\"left\" >Opponent</td>") == false);

            for (int i = 0; i < 19; i++)
                line = sr.ReadLine();

            ts.stats[tPA] = Convert.ToUInt16(grs_GetNextStat(ref sr));
            sr.Close();
        }

        private static string grs_GetNextStat(ref StreamReader sr)
        {
            string line = sr.ReadLine();
            string[] parts1 = line.Split('>');
            string[] parts2 = parts1[1].Split('<');
            return parts2[0];
        }

        public static UInt16 getUShort(DataRow r, string ColumnName)
        {
            return Convert.ToUInt16(r[ColumnName].ToString());
        }

        public static int getInt(DataRow r, string ColumnName)
        {
            return Convert.ToInt32(r[ColumnName].ToString());
        }

        public static Boolean getBoolean(DataRow r, string ColumnName)
        {
            string s = r[ColumnName].ToString();
            s = s.ToLower();
            return Convert.ToBoolean(s);
        }

        public static string getString(DataRow r, string ColumnName)
        {
            return r[ColumnName].ToString();
        }
    }

    public static class RealStats
    {
        private const int tMINS = 0, tPF = 1, tPA = 2, tFGM = 4, tFGA = 5, tTPM = 6, tTPA = 7, tFTM = 8, tFTA = 9, tOREB = 10, tDREB = 11, tSTL = 12, tTO = 13, tBLK = 14, tAST = 15, tFOUL = 16;
        private const int pGP = 0, pGS = 1, pMINS = 2, pPTS = 3, pDREB = 4, pOREB = 5, pAST = 6, pSTL = 7, pBLK = 8, pTO = 9, pFOUL = 10, pFGM = 11, pFGA = 12, pTPM = 13, pTPA = 14, pFTM = 15, pFTA = 16; 

        private static DataSet GetBoxScore(string url)
        {
            var dataset = new DataSet();

            var htmlweb = new HtmlWeb();
            var doc = htmlweb.Load(url);

            var tables = doc.DocumentNode.SelectNodes("//table");
            foreach (var cur in tables)
            {
                try
                {
                    if (!cur.Attributes["id"].Value.EndsWith("_basic")) continue;
                }
                catch (Exception)
                {
                    continue;
                }
                var table = new DataTable(cur.Attributes["id"].Value);

                var thead = cur.SelectSingleNode("thead");
                var theadrows = thead.SelectNodes("tr");
                var header = theadrows[1].SelectNodes("th");

                var headers = theadrows[1]
                    .Elements("th")
                    .Select(th => th.InnerText.Trim());
                foreach (var colheader in headers)
                {
                    table.Columns.Add(colheader);
                }

                var tbody = cur.SelectSingleNode("tbody");
                var tbodyrows = tbody.SelectNodes("tr");
                var rows = tbodyrows.Select(tr => tr.Elements("td")
                                                          .Select(td => td.InnerText.Trim())
                                                          .ToArray());
                foreach (var row in rows)
                {
                    table.Rows.Add(row);
                }

                dataset.Tables.Add(table);
            }
            return dataset;
        }

        private static DataSet GetSeasonTeamStats(string url, out string[] recordparts)
        {
            var dataset = new DataSet();

            var htmlweb = new HtmlWeb();
            var doc = htmlweb.Load(url);

            var infobox = doc.DocumentNode.SelectSingleNode("//div[@id='info_box']");
            var infoboxps = infobox.SelectNodes("p");
            var infoboxp = infoboxps[2];
            var infoboxpstrong = infoboxp.NextSibling;
            var record = infoboxpstrong.InnerText;
            recordparts = record.Split('-');

            var tables = doc.DocumentNode.SelectNodes("//table");
            foreach (var cur in tables)
            {
                try
                {
                    if (cur.Attributes["id"].Value != "team") continue;
                }
                catch (Exception)
                {
                    continue;
                }
                var table = new DataTable(cur.Attributes["id"].Value);

                var thead = cur.SelectSingleNode("thead");
                var theadrows = thead.SelectNodes("tr");

                var headers = theadrows[0]
                    .Elements("th")
                    .Select(th => th.InnerText.Trim());
                foreach (var colheader in headers)
                {
                    table.Columns.Add(colheader);
                }

                var tbody = cur.SelectSingleNode("tbody");
                var tbodyrows = tbody.SelectNodes("tr");
                var rows = tbodyrows.Select(tr => tr.Elements("td")
                                                          .Select(td => td.InnerText.Trim())
                                                          .ToArray());
                foreach (var row in rows)
                {
                    table.Rows.Add(row);
                }

                dataset.Tables.Add(table);
            }
            return dataset;
        }

        private static DataSet GetPlayerStats(string url)
        {
            var dataset = new DataSet();

            var htmlweb = new HtmlWeb();
            var doc = htmlweb.Load(url);

            var tables = doc.DocumentNode.SelectNodes("//table");
            foreach (var cur in tables)
            {
                try
                {
                    if (!(cur.Attributes["id"].Value == "totals" || cur.Attributes["id"].Value == "playoffs" || cur.Attributes["id"].Value == "roster")) continue;
                }
                catch (Exception)
                {
                    continue;
                }
                var table = new DataTable(cur.Attributes["id"].Value);

                var thead = cur.SelectSingleNode("thead");
                var theadrows = thead.SelectNodes("tr");

                HtmlNode theadrow;
                if (cur.Attributes["id"].Value == "playoffs") theadrow = theadrows[1];
                else theadrow = theadrows[0];

                var headers = theadrow
                    .Elements("th")
                    .Select(th => th.InnerText.Trim());
                foreach (var colheader in headers)
                {
                    try
                    {
                        table.Columns.Add(colheader);
                    }
                    catch (Exception)
                    {
                        table.Columns.Add(colheader + "2");
                    }
                }

                var tbody = cur.SelectSingleNode("tbody");
                var tbodyrows = tbody.SelectNodes("tr");
                var rows = tbodyrows.Select(tr => tr.Elements("td")
                                                          .Select(td => td.InnerText.Trim())
                                                          .ToArray());
                foreach (var row in rows)
                {
                    table.Rows.Add(row);
                }

                dataset.Tables.Add(table);
            }
            return dataset;
        }

        private static DataSet GetPlayoffTeamStats(string url)
        {
            var dataset = new DataSet();

            var htmlweb = new HtmlWeb();
            var doc = htmlweb.Load(url);
            
            var tables = doc.DocumentNode.SelectNodes("//table");
            foreach (var cur in tables)
            {
                try
                {
                    if (!(cur.Attributes["id"].Value == "team" || cur.Attributes["id"].Value == "opponent" || cur.Attributes["id"].Value == "misc")) continue;
                }
                catch (Exception)
                {
                    continue;
                }
                var table = new DataTable(cur.Attributes["id"].Value);

                var thead = cur.SelectSingleNode("thead");
                var theadrows = thead.SelectNodes("tr");
                HtmlNode theadrow;

                if (cur.Attributes["id"].Value == "misc") theadrow = theadrows[1];
                else theadrow = theadrows[0];

                var headers = theadrow
                    .Elements("th")
                    .Select(th => th.InnerText.Trim());
                foreach (var colheader in headers)
                {
                    try
                    {
                        table.Columns.Add(colheader);
                    }
                    catch (Exception)
                    {
                        table.Columns.Add(colheader + "2");
                    }
                }

                var tbody = cur.SelectSingleNode("tbody");
                var tbodyrows = tbody.SelectNodes("tr");
                var rows = tbodyrows.Select(tr => tr.Elements("td")
                                                          .Select(td => td.InnerText.Trim())
                                                          .ToArray());
                foreach (var row in rows)
                {
                    table.Rows.Add(row);
                }

                dataset.Tables.Add(table);
            }
            return dataset;
        }

        private static void TeamStatsFromDataTable(DataTable dt, string name, string[] recordparts, out TeamStats ts, out TeamStats tsopp)
        {
            ts = new TeamStats(name);
            tsopp = new TeamStats(name);

            tsopp.winloss[1] = ts.winloss[0] = Convert.ToByte(recordparts[0]);
            tsopp.winloss[0] = ts.winloss[1] = Convert.ToByte(recordparts[1]);

            DataRow tr = dt.Rows[0];
            DataRow toppr = dt.Rows[2];

            ts.stats[tMINS] = (ushort)(NSTHelper.getUShort(tr, "MP") / 5);
            ts.stats[tFGM] = NSTHelper.getUShort(tr, "FG");
            ts.stats[tFGA] = NSTHelper.getUShort(tr, "FGA");
            ts.stats[tTPM] = NSTHelper.getUShort(tr, "3P");
            ts.stats[tTPA] = NSTHelper.getUShort(tr, "3PA");
            ts.stats[tFTM] = NSTHelper.getUShort(tr, "FT");
            ts.stats[tFTA] = NSTHelper.getUShort(tr, "FTA");
            ts.stats[tOREB] = NSTHelper.getUShort(tr, "ORB");
            ts.stats[tDREB] = NSTHelper.getUShort(tr, "DRB");
            ts.stats[tAST] = NSTHelper.getUShort(tr, "AST");
            ts.stats[tSTL] = NSTHelper.getUShort(tr, "STL");
            ts.stats[tBLK] = NSTHelper.getUShort(tr, "BLK");
            ts.stats[tTO] = NSTHelper.getUShort(tr, "TOV");
            ts.stats[tFOUL] = NSTHelper.getUShort(tr, "PF");
            ts.stats[tPF] = NSTHelper.getUShort(tr, "PTS");
            ts.stats[tPA] = NSTHelper.getUShort(toppr, "PTS");

            ts.calcAvg();

            tsopp.stats[tMINS] = (ushort)(NSTHelper.getUShort(toppr, "MP") / 5);
            tsopp.stats[tFGM] = NSTHelper.getUShort(toppr, "FG");
            tsopp.stats[tFGA] = NSTHelper.getUShort(toppr, "FGA");
            tsopp.stats[tTPM] = NSTHelper.getUShort(toppr, "3P");
            tsopp.stats[tTPA] = NSTHelper.getUShort(toppr, "3PA");
            tsopp.stats[tFTM] = NSTHelper.getUShort(toppr, "FT");
            tsopp.stats[tFTA] = NSTHelper.getUShort(toppr, "FTA");
            tsopp.stats[tOREB] = NSTHelper.getUShort(toppr, "ORB");
            tsopp.stats[tDREB] = NSTHelper.getUShort(toppr, "DRB");
            tsopp.stats[tAST] = NSTHelper.getUShort(toppr, "AST");
            tsopp.stats[tSTL] = NSTHelper.getUShort(toppr, "STL");
            tsopp.stats[tBLK] = NSTHelper.getUShort(toppr, "BLK");
            tsopp.stats[tTO] = NSTHelper.getUShort(toppr, "TOV");
            tsopp.stats[tFOUL] = NSTHelper.getUShort(toppr, "PF");
            tsopp.stats[tPF] = NSTHelper.getUShort(toppr, "PTS");
            tsopp.stats[tPA] = NSTHelper.getUShort(tr, "PTS");

            tsopp.calcAvg();
        }

        private static void PlayoffTeamStatsFromDataSet(DataSet ds, ref TeamStats[] tst, ref TeamStats[] tstopp)
        {
            DataTable dt = ds.Tables["team"];
            DataTable dtopp = ds.Tables["opponent"];
            DataTable dtmisc = ds.Tables["misc"];

            for (int i = 0; i < tst.Length; i++)
            {
                DataRow tr = dt.Rows[0];
                DataRow toppr = dtopp.Rows[0];
                DataRow tmiscr = dtmisc.Rows[0];

                bool found = false;

                for (int j = 0; j < dt.Rows.Count; j++)
                {
                    if (dt.Rows[j]["Team"].ToString().EndsWith(tst[i].name))
                    {
                        tr = dt.Rows[j];
                        found = true;
                        break;
                    }
                }

                if (!found) continue;

                for (int j = 0; j < dtopp.Rows.Count; j++)
                {
                    if (dtopp.Rows[j]["Team"].ToString().EndsWith(tst[i].name))
                    {
                        toppr = dtopp.Rows[j];
                        break;
                    }
                }

                for (int j = 0; j < dtmisc.Rows.Count; j++)
                {
                    if (dtmisc.Rows[j]["Team"].ToString().EndsWith(tst[i].name))
                    {
                        tmiscr = dtmisc.Rows[j];
                        break;
                    }
                }

                tst[i].pl_winloss[0] = (byte) NSTHelper.getUShort(tmiscr, "W");
                tst[i].pl_winloss[1] = (byte) NSTHelper.getUShort(tmiscr, "L");
                tst[i].pl_stats[tMINS] = (ushort) (NSTHelper.getUShort(tr, "MP")/5);
                tst[i].pl_stats[tFGM] = NSTHelper.getUShort(tr, "FG");
                tst[i].pl_stats[tFGA] = NSTHelper.getUShort(tr, "FGA");
                tst[i].pl_stats[tTPM] = NSTHelper.getUShort(tr, "3P");
                tst[i].pl_stats[tTPA] = NSTHelper.getUShort(tr, "3PA");
                tst[i].pl_stats[tFTM] = NSTHelper.getUShort(tr, "FT");
                tst[i].pl_stats[tFTA] = NSTHelper.getUShort(tr, "FTA");
                tst[i].pl_stats[tOREB] = NSTHelper.getUShort(tr, "ORB");
                tst[i].pl_stats[tDREB] = NSTHelper.getUShort(tr, "DRB");
                tst[i].pl_stats[tAST] = NSTHelper.getUShort(tr, "AST");
                tst[i].pl_stats[tSTL] = NSTHelper.getUShort(tr, "STL");
                tst[i].pl_stats[tBLK] = NSTHelper.getUShort(tr, "BLK");
                tst[i].pl_stats[tTO] = NSTHelper.getUShort(tr, "TOV");
                tst[i].pl_stats[tFOUL] = NSTHelper.getUShort(tr, "PF");
                tst[i].pl_stats[tPF] = NSTHelper.getUShort(tr, "PTS");
                tst[i].pl_stats[tPA] = NSTHelper.getUShort(toppr, "PTS");

                tstopp[i].pl_winloss[0] = (byte)NSTHelper.getUShort(tmiscr, "L");
                tstopp[i].pl_winloss[1] = (byte)NSTHelper.getUShort(tmiscr, "W");
                tstopp[i].pl_stats[tMINS] = (ushort)(NSTHelper.getUShort(toppr, "MP") / 5);
                tstopp[i].pl_stats[tFGM] = NSTHelper.getUShort(toppr, "FG");
                tstopp[i].pl_stats[tFGA] = NSTHelper.getUShort(toppr, "FGA");
                tstopp[i].pl_stats[tTPM] = NSTHelper.getUShort(toppr, "3P");
                tstopp[i].pl_stats[tTPA] = NSTHelper.getUShort(toppr, "3PA");
                tstopp[i].pl_stats[tFTM] = NSTHelper.getUShort(toppr, "FT");
                tstopp[i].pl_stats[tFTA] = NSTHelper.getUShort(toppr, "FTA");
                tstopp[i].pl_stats[tOREB] = NSTHelper.getUShort(toppr, "ORB");
                tstopp[i].pl_stats[tDREB] = NSTHelper.getUShort(toppr, "DRB");
                tstopp[i].pl_stats[tAST] = NSTHelper.getUShort(toppr, "AST");
                tstopp[i].pl_stats[tSTL] = NSTHelper.getUShort(toppr, "STL");
                tstopp[i].pl_stats[tBLK] = NSTHelper.getUShort(toppr, "BLK");
                tstopp[i].pl_stats[tTO] = NSTHelper.getUShort(toppr, "TOV");
                tstopp[i].pl_stats[tFOUL] = NSTHelper.getUShort(toppr, "PF");
                tstopp[i].pl_stats[tPF] = NSTHelper.getUShort(toppr, "PTS");
                tstopp[i].pl_stats[tPA] = NSTHelper.getUShort(tr, "PTS");
            }
        }

        private static void PlayerStatsFromDataSet(DataSet ds, string team, out Dictionary<int, PlayerStats> pst)
        {
            var pstnames = new Dictionary<string, PlayerStats>();

            DataTable dt;
            dt = ds.Tables["roster"];

            foreach (DataRow r in dt.Rows)
            {
                string Position1, Position2;
                switch (r["Pos"].ToString())
                {
                    case "C":
                        Position1 = "C";
                        Position2 = " ";
                        break;

                    case "G":
                        Position1 = "PG";
                        Position2 = "SG";
                        break;

                    case "F":
                        Position1 = "SF";
                        Position2 = "PF";
                        break;

                    case "G-F":
                        Position1 = "SG";
                        Position2 = "SF";
                        break;

                    case "F-G":
                        Position1 = "SF";
                        Position2 = "SG";
                        break;

                    case "F-C":
                        Position1 = "PF";
                        Position2 = "C";
                        break;

                    case "C-F":
                        Position1 = "C";
                        Position2 = "PF";
                        break;

                    default:
                        throw(new Exception("Don't recognize the position " + r["Pos"].ToString()));
                }
                PlayerStats ps = new PlayerStats(new Player(pstnames.Count, team, r["Player"].ToString().Split(' ')[1],
                                                            r["Player"].ToString().Split(' ')[0], Position1, Position2));

                pstnames.Add(r["Player"].ToString(), ps);
            }

            dt = ds.Tables["totals"];

            foreach (DataRow r in dt.Rows)
            {
                string name = r["Player"].ToString();
                pstnames[name].stats[pGP] = NSTHelper.getUShort(r, "G");
                pstnames[name].stats[pGS] = NSTHelper.getUShort(r, "GS");
                pstnames[name].stats[pMINS] = NSTHelper.getUShort(r, "MP");
                pstnames[name].stats[pFGM] = NSTHelper.getUShort(r, "FG");
                pstnames[name].stats[pFGA] = NSTHelper.getUShort(r, "FGA");
                pstnames[name].stats[pTPM] = NSTHelper.getUShort(r, "3P");
                pstnames[name].stats[pTPA] = NSTHelper.getUShort(r, "3PA");
                pstnames[name].stats[pFTM] = NSTHelper.getUShort(r, "FT");
                pstnames[name].stats[pFTA] = NSTHelper.getUShort(r, "FTA");
                pstnames[name].stats[pOREB] = NSTHelper.getUShort(r, "ORB");
                pstnames[name].stats[pDREB] = NSTHelper.getUShort(r, "DRB");
                pstnames[name].stats[pAST] = NSTHelper.getUShort(r, "AST");
                pstnames[name].stats[pSTL] = NSTHelper.getUShort(r, "STL");
                pstnames[name].stats[pBLK] = NSTHelper.getUShort(r, "BLK");
                pstnames[name].stats[pTO] = NSTHelper.getUShort(r, "TOV");
                pstnames[name].stats[pFOUL] = NSTHelper.getUShort(r, "PF");
                pstnames[name].stats[pPTS] = NSTHelper.getUShort(r, "PTS");
            }

            dt = ds.Tables["playoffs"];

            try
            {
                foreach (DataRow r in dt.Rows)
                {
                    string name = r["Player"].ToString();
                    pstnames[name].stats[pGP] += NSTHelper.getUShort(r, "G");
                    //pstnames[name].stats[pGS] += NSTHelper.getUShort(r, "GS");
                    pstnames[name].stats[pMINS] += NSTHelper.getUShort(r, "MP");
                    pstnames[name].stats[pFGM] += NSTHelper.getUShort(r, "FG");
                    pstnames[name].stats[pFGA] += NSTHelper.getUShort(r, "FGA");
                    pstnames[name].stats[pTPM] += NSTHelper.getUShort(r, "3P");
                    pstnames[name].stats[pTPA] += NSTHelper.getUShort(r, "3PA");
                    pstnames[name].stats[pFTM] += NSTHelper.getUShort(r, "FT");
                    pstnames[name].stats[pFTA] += NSTHelper.getUShort(r, "FTA");
                    pstnames[name].stats[pOREB] += NSTHelper.getUShort(r, "ORB");
                    pstnames[name].stats[pDREB] += (ushort)(NSTHelper.getUShort(r, "TRB") - NSTHelper.getUShort(r, "ORB"));
                    pstnames[name].stats[pAST] += NSTHelper.getUShort(r, "AST");
                    pstnames[name].stats[pSTL] += NSTHelper.getUShort(r, "STL");
                    pstnames[name].stats[pBLK] += NSTHelper.getUShort(r, "BLK");
                    pstnames[name].stats[pTO] += NSTHelper.getUShort(r, "TOV");
                    pstnames[name].stats[pFOUL] += NSTHelper.getUShort(r, "PF");
                    pstnames[name].stats[pPTS] += NSTHelper.getUShort(r, "PTS");

                    pstnames[name].CalcAvg();
                }
            }
            catch (Exception)
            { }

            pst = new Dictionary<int, PlayerStats>();
            foreach (var kvp in pstnames)
            {
                kvp.Value.ID = pst.Count;
                pst.Add(pst.Count, kvp.Value);
            }
        }

        public static void ImportRealStats(KeyValuePair<string, string> teamAbbr, out TeamStats ts, out TeamStats tsopp, out Dictionary<int, PlayerStats> pst)
        {
            string[] recordparts;
            DataSet ds = GetSeasonTeamStats(@"http://www.basketball-reference.com/teams/" + teamAbbr.Value + @"/2012.html", out recordparts);
            TeamStatsFromDataTable(ds.Tables[0], teamAbbr.Key, recordparts, out ts, out tsopp);

            ds = GetPlayerStats(@"http://www.basketball-reference.com/teams/" + teamAbbr.Value + @"/2012.html");
            PlayerStatsFromDataSet(ds, teamAbbr.Key, out pst);
        }

        public static void AddPlayoffTeamStats(ref TeamStats[] tst, ref TeamStats[] tstopp)
        {
            DataSet ds = GetPlayoffTeamStats("http://www.basketball-reference.com/playoffs/NBA_2012.html");
            PlayoffTeamStatsFromDataSet(ds, ref tst, ref tstopp);
        }
    }

    // Unlike TeamStats which was designed before REditor implemented such stats,
    // PlayerStats were made according to REditor's standards, to make life 
    // easier when importing/exporting from REditor's CSV
    public class PlayerStats
    {
        // TODO: Metric Stats here

        public const int pGP = 0, pGS = 1, pMINS = 2, pPTS = 3,pDREB = 4, pOREB = 5, pAST = 6, pSTL = 7, pBLK = 8, pTO = 9, pFOUL = 10, pFGM = 11, pFGA = 12, pTPM = 13, pTPA = 14, pFTM = 15, pFTA = 16; 

        public const int pMPG = 0,pPPG = 1, pDRPG = 2, pORPG = 3, pAPG = 4, pSPG = 5, pBPG = 6, pTPG = 7, pFPG = 8, pFGp = 9, pFGeff = 10, pTPp = 11, pTPeff = 12, pFTp = 13, pFTeff = 14, pRPG = 15;

        public const int tMINS = 0, tPF = 1, tPA = 2, tFGM = 4, tFGA = 5, tTPM = 6, tTPA = 7, tFTM = 8, tFTA = 9, tOREB = 10, tDREB = 11, tSTL = 12, tTO = 13, tBLK = 14, tAST = 15, tFOUL = 16;

        public const int tPPG = 0, tPAPG = 1, tFGp = 2, tFGeff = 3, tTPp = 4, tTPeff = 5, tFTp = 6, tFTeff = 7, tRPG = 8, tORPG = 9, tDRPG = 10, tSPG = 11, tBPG = 12, tTPG = 13, tAPG = 14, tFPG = 15, tWp = 16, tWeff = 17, tPD = 18;

        public string FirstName;
        public int ID;
        public string LastName;
        public string Position1;
        public string Position2;
        public string TeamF;
        public string TeamS = "";
        public float[] averages = new float[16];
        public bool isActive;
        public bool isAllStar;
        public bool isInjured;
        public bool isNBAChampion;
        public UInt16[] stats = new UInt16[17];
        public Dictionary<string, double> metrics = new Dictionary<string, double>();

        public PlayerStats(Player player)
        {
            ID = player.ID;
            LastName = player.LastName;
            FirstName = player.FirstName;
            Position1 = player.Position;
            Position2 = player.Position2;
            TeamF = player.Team;
            isActive = true;
            isInjured = false;
            isAllStar = false;
            isNBAChampion = false;

            for (int i = 0; i < stats.Length; i++)
            {
                stats[i] = 0;
            }

            for (int i = 0; i < averages.Length; i++)
            {
                averages[i] = 0;
            }
        }

        public PlayerStats(DataRow dataRow)
        {
            ID = NSTHelper.getInt(dataRow, "ID");
            LastName = NSTHelper.getString(dataRow, "LastName");
            FirstName = NSTHelper.getString(dataRow, "FirstName");
            Position1 = NSTHelper.getString(dataRow, "Position1");
            Position2 = NSTHelper.getString(dataRow, "Position2");
            TeamF = NSTHelper.getString(dataRow, "TeamFin");
            TeamS = NSTHelper.getString(dataRow, "TeamSta");
            isActive = NSTHelper.getBoolean(dataRow, "isActive");
            isInjured = NSTHelper.getBoolean(dataRow, "isInjured");
            isAllStar = NSTHelper.getBoolean(dataRow, "isAllStar");
            isNBAChampion = NSTHelper.getBoolean(dataRow, "isNBAChampion");

            stats[pGP] = NSTHelper.getUShort(dataRow, "GP");
            stats[pGS] = NSTHelper.getUShort(dataRow, "GS");
            stats[pMINS] = NSTHelper.getUShort(dataRow, "MINS");
            stats[pPTS] = NSTHelper.getUShort(dataRow, "PTS");
            stats[pFGM] = NSTHelper.getUShort(dataRow, "FGM");
            stats[pFGA] = NSTHelper.getUShort(dataRow, "FGA");
            stats[pTPM] = NSTHelper.getUShort(dataRow, "TPM");
            stats[pTPA] = NSTHelper.getUShort(dataRow, "TPA");
            stats[pFTM] = NSTHelper.getUShort(dataRow, "FTM");
            stats[pFTA] = NSTHelper.getUShort(dataRow, "FTA");
            stats[pOREB] = NSTHelper.getUShort(dataRow, "OREB");
            stats[pDREB] = NSTHelper.getUShort(dataRow, "DREB");
            stats[pSTL] = NSTHelper.getUShort(dataRow, "STL");
            stats[pTO] = NSTHelper.getUShort(dataRow, "TOS");
            stats[pBLK] = NSTHelper.getUShort(dataRow, "BLK");
            stats[pAST] = NSTHelper.getUShort(dataRow, "AST");
            stats[pFOUL] = NSTHelper.getUShort(dataRow, "FOUL");

            CalcAvg();
        }

        public PlayerStats(int ID, string LastName, string FirstName, string Position1, string Position2, string TeamF,
                           string TeamS,
                           bool isActive, bool isInjured, bool isAllStar, bool isNBAChampion, DataRow dataRow)
        {
            this.ID = ID;
            this.LastName = LastName;
            this.FirstName = FirstName;
            this.Position1 = Position1;
            this.Position2 = Position2;
            this.TeamF = TeamF;
            this.TeamS = TeamS;
            this.isActive = isActive;
            this.isAllStar = isAllStar;
            this.isInjured = isInjured;
            this.isNBAChampion = isNBAChampion;

            stats[pGP] = NSTHelper.getUShort(dataRow, "GP");
            stats[pGS] = NSTHelper.getUShort(dataRow, "GS");
            stats[pMINS] = NSTHelper.getUShort(dataRow, "MINS");
            stats[pPTS] = NSTHelper.getUShort(dataRow, "PTS");

            string[] parts = NSTHelper.getString(dataRow, "FG").Split('-');

            stats[pFGM] = Convert.ToUInt16(parts[0]);
            stats[pFGA] = Convert.ToUInt16(parts[1]);

            parts = NSTHelper.getString(dataRow, "3PT").Split('-');

            stats[pTPM] = Convert.ToUInt16(parts[0]);
            stats[pTPA] = Convert.ToUInt16(parts[1]);

            parts = NSTHelper.getString(dataRow, "FT").Split('-');

            stats[pFTM] = Convert.ToUInt16(parts[0]);
            stats[pFTA] = Convert.ToUInt16(parts[1]);

            stats[pOREB] = NSTHelper.getUShort(dataRow, "OREB");
            stats[pDREB] = NSTHelper.getUShort(dataRow, "DREB");
            stats[pSTL] = NSTHelper.getUShort(dataRow, "STL");
            stats[pTO] = NSTHelper.getUShort(dataRow, "TO");
            stats[pBLK] = NSTHelper.getUShort(dataRow, "BLK");
            stats[pAST] = NSTHelper.getUShort(dataRow, "AST");
            stats[pFOUL] = NSTHelper.getUShort(dataRow, "FOUL");

            CalcAvg();
        }

        public PlayerStats(PlayerStatsRow playerStatsRow)
        {
            LastName = playerStatsRow.LastName;
            FirstName = playerStatsRow.FirstName;

            stats[pGP] = playerStatsRow.GP;
            stats[pGS] = playerStatsRow.GS;
            stats[pMINS] = playerStatsRow.MINS;
            stats[pPTS] = playerStatsRow.PTS;
            stats[pFGM] = playerStatsRow.FGM;
            stats[pFGA] = playerStatsRow.FGA;
            stats[pTPM] = playerStatsRow.TPM;
            stats[pTPA] = playerStatsRow.TPA;
            stats[pFTM] = playerStatsRow.FTM;
            stats[pFTA] = playerStatsRow.FTA;
            stats[pOREB] = playerStatsRow.OREB;
            stats[pDREB] = playerStatsRow.DREB;
            stats[pSTL] = playerStatsRow.STL;
            stats[pTO] = playerStatsRow.TOS;
            stats[pBLK] = playerStatsRow.BLK;
            stats[pAST] = playerStatsRow.AST;
            stats[pFOUL] = playerStatsRow.FOUL;

            ID = playerStatsRow.ID;
            Position1 = playerStatsRow.Position1;
            Position2 = playerStatsRow.Position2;
            TeamF = playerStatsRow.TeamF;
            TeamS = playerStatsRow.TeamS;
            isActive = playerStatsRow.isActive;
            isAllStar = playerStatsRow.isAllStar;
            isInjured = playerStatsRow.isInjured;
            isNBAChampion = playerStatsRow.isNBAChampion;

            CalcAvg();
        }

        public void CalcAvg()
        {
            int games = stats[pGP];
            averages[pMPG] = (float) stats[pMINS]/games;
            averages[pPPG] = (float) stats[pPTS]/games;
            averages[pFGp] = (float) stats[pFGM]/stats[pFGA];
            averages[pFGeff] = averages[pFGp]*((float) stats[pFGM]/games);
            averages[pTPp] = (float) stats[pTPM]/stats[pTPA];
            averages[pTPeff] = averages[pTPp]*((float) stats[pTPM]/games);
            averages[pFTp] = (float) stats[pFTM]/stats[pFTA];
            averages[pFTeff] = averages[pFTp]*((float) stats[pFTM]/games);
            averages[pRPG] = (float) (stats[pOREB] + stats[pDREB])/games;
            averages[pORPG] = (float) stats[pOREB]/games;
            averages[pDRPG] = (float) stats[pDREB]/games;
            averages[pSPG] = (float) stats[pSTL]/games;
            averages[pBPG] = (float) stats[pBLK]/games;
            averages[pTPG] = (float) stats[pTO]/games;
            averages[pAPG] = (float) stats[pAST]/games;
            averages[pFPG] = (float) stats[pFOUL]/games;
        }

        /// <summary>
        /// Calculates the Metric Stats for this Player
        /// </summary>
        /// <param name="ts">The player's team's stats</param>
        /// <param name="ls">The total league stats</param>
        public void CalcMetrics(TeamStats ts, TeamStats tsopp, TeamStats ls, bool leagueOv = false)
        {
            double[] pstats = new double[stats.Length];
            for (int i = 0; i < stats.Length; i++)
            {
                pstats[i] = stats[i];
            }

            double[] tstats = new double[ts.stats.Length];
            for (int i = 0; i < ts.stats.Length; i++)
            {
                tstats[i] = ts.stats[i];
            }

            double[] toppstats = new double[tsopp.stats.Length];
            for (int i = 0; i < tsopp.stats.Length; i++)
            {
                toppstats[i] = tsopp.stats[i];
            }

            double[] lstats = new double[ls.stats.Length];
            for (int i = 0; i < ls.stats.Length; i++)
            {
                lstats[i] = ls.stats[i];
            }

            double pREB = pstats[pOREB] + pstats[pDREB];
            double tREB = tstats[tOREB] + tstats[tDREB];

            metrics = new Dictionary<string, double>();

            
            #region Metrics that do not require Opponent Stats

            double ASTp = 100*pstats[pAST]/(((pstats[pMINS]/(tstats[tMINS]))*tstats[tFGM]) - pstats[pFGM]);
            metrics.Add("AST%", ASTp);

            double EFGp = (pstats[pFGM] + 0.5 * pstats[pTPM]) / pstats[pFGA];
            metrics.Add("EFG%", EFGp); 
            
            double GmSc = pstats[pPTS] + 0.4 * pstats[pFGM] - 0.7 * pstats[pFGA] - 0.4 * (pstats[pFTA] - pstats[pFTM]) +
                           0.7 * pstats[pOREB] + 0.3 * pstats[pDREB] + pstats[pSTL] + 0.7 * pstats[pAST] + 0.7 * pstats[pBLK] -
                           0.4 * pstats[pFOUL] - pstats[pTO];
            metrics.Add("GmSc", GmSc/pstats[pGP]);

            double STLp = 100 * (pstats[pSTL] * (tstats[tMINS])) / (pstats[pMINS] * tsopp.metrics["Poss"]);
            metrics.Add("STL%", STLp);

            double TOp = 100 * pstats[pTO] / (pstats[pFGA] + 0.44 * pstats[pFTA] + pstats[pTO]);
            metrics.Add("TO%", TOp);

            double TSp = pstats[pPTS] / (2 * (pstats[pFGA] + 0.44 * pstats[pFTA]));
            metrics.Add("TS%", TSp);

            double USGp = 100 *
                          ((pstats[pFGA] + 0.44 * pstats[pFTA] + pstats[pTO]) *
                           (tstats[tMINS])) / (pstats[pMINS] * (tstats[tFGA] + 0.44 * tstats[tFTA] + tstats[tTO]));
            metrics.Add("USG%", USGp);

            // Rates, stat per 49 minutes played
            double PTSR = (pstats[pPTS] / pstats[pMINS]) * 48;
            metrics.Add("PTSR", PTSR);

            double REBR = (pREB / pstats[pMINS]) * 48;
            metrics.Add("REBR", REBR);

            double ASTR = (pstats[pAST] / pstats[pMINS]) * 48;
            metrics.Add("ASTR", ASTR);

            double BLKR = (pstats[pBLK] / pstats[pMINS]) * 48;
            metrics.Add("BLKR", BLKR);

            double STLR = (pstats[pSTL] / pstats[pMINS]) * 48;
            metrics.Add("STLR", STLR);

            double TOR = (pstats[pTO] / pstats[pMINS]) * 48;
            metrics.Add("TOR", TOR);

            double FTR = (pstats[pFTM]/pstats[pFGA]);
            metrics.Add("FTR", FTR);
            //
            // PER preparations
            double lREB = lstats[tOREB] + lstats[tDREB];
            double factor = (2 / 3) - (0.5 * (lstats[tAST] / lstats[tFGM])) / (2 * (lstats[tFGM] / lstats[tFTM]));
            double VOP = lstats[tPF] / (lstats[tFGA] - lstats[tOREB] + lstats[tTO] + 0.44 * lstats[tFTA]);
            double lDRBp = lstats[tDREB] / lREB;

            double uPER = (1 / pstats[pMINS]) *
                          (pstats[pTPM]
                           + (2 / 3) * pstats[pAST]
                           + (2 - factor * (tstats[tAST] / tstats[tFGM])) * pstats[pFGM]
                           +
                           (pstats[pFTM] * 0.5 * (1 + (1 - (tstats[tAST] / tstats[tFGM])) + (2 / 3) * (tstats[tAST] / tstats[tFGM])))
                           - VOP * pstats[pTO]
                           - VOP * lDRBp * (pstats[pFGA] - pstats[pFGM])
                           - VOP * 0.44 * (0.44 + (0.56 * lDRBp)) * (pstats[pFTA] - pstats[pFTM])
                           + VOP * (1 - lDRBp) * (pREB - pstats[pOREB])
                           + VOP * lDRBp * pstats[pOREB]
                           + VOP * pstats[pSTL]
                           + VOP * lDRBp * pstats[pBLK]
                           - pstats[pFOUL] * ((lstats[tFTM] / lstats[tFOUL]) - 0.44 * (lstats[tFTA] / lstats[tFOUL]) * VOP));
            metrics.Add("EFF", uPER * 100);

            #endregion

            #region Metrics that require Opponents stats

            if (ts.getGames() == tsopp.getGames())
            {
                double BLKp = 100 * (pstats[pBLK] * (tstats[tMINS])) / (pstats[pMINS] * (toppstats[tFGA] - toppstats[tTPA]));

                double DRBp = 100 * (pstats[pDREB] * (tstats[tMINS])) / (pstats[pMINS] * (tstats[tDREB] + toppstats[tOREB]));

                double ORBp = 100 * (pstats[pOREB] * (tstats[tMINS])) / (pstats[pMINS] * (tstats[tOREB] + toppstats[tDREB]));

                double toppREB = toppstats[tOREB] + toppstats[tDREB];

                double REBp = 100 * (pREB * (tstats[tMINS])) / (pstats[pMINS] * (tREB + toppREB));

                #region Metrics that require league stats

                double aPER;
                double PPR;

                if (ls.name != "$$Empty")
                {
                    double paceAdj = ls.metrics["Pace"] / ts.metrics["Pace"];
                    double estPaceAdj = 2 * ls.averages[tPPG] / (ts.averages[tPPG] + tsopp.averages[tPPG]);

                    aPER = estPaceAdj * uPER;

                    PPR = 100 * estPaceAdj * (((pstats[pAST] * 2 / 3) - pstats[pTO]) / pstats[pMINS]);
                }
                else
                {
                    aPER = double.NaN;
                    PPR = double.NaN;
                }

                #endregion

                metrics.Add("aPER", aPER);
                metrics.Add("BLK%", BLKp);
                metrics.Add("DREB%", DRBp);
                metrics.Add("OREB%", ORBp);
                metrics.Add("REB%", REBp);
                metrics.Add("PPR", PPR);
            }
            else
            {
                metrics.Add("aPER", double.NaN);
                metrics.Add("BLK%", double.NaN);
                metrics.Add("DREB%", double.NaN);
                metrics.Add("OREB%", double.NaN);
                metrics.Add("REB%", double.NaN);
                metrics.Add("PPR", double.NaN);
            }
            #endregion

            var gamesRequired = (int)Math.Ceiling(0.8522 * ts.getGames());
            if (leagueOv)
            {
                if (stats[pGP] < gamesRequired)
                {
                    foreach (var name in metrics.Keys.ToList())
                        metrics[name] = double.NaN;
                }
            }
        }

        public void CalcPER(double lg_aPER)
        {
            metrics.Add("PER", metrics["aPER"]*(15/lg_aPER));
        }

        public void AddBoxScore(PlayerBoxScore pbs)
        {
            if (ID != pbs.PlayerID)
                throw new Exception("Tried to update PlayerStats " + ID + " with PlayerBoxScore " + pbs.PlayerID);

            if (pbs.isStarter) stats[pGS]++;
            if (pbs.MINS > 0)
            {
                stats[pGP]++;
                stats[pMINS] += pbs.MINS;
            }
            stats[pPTS] += pbs.PTS;
            stats[pFGM] += pbs.FGM;
            stats[pFGA] += pbs.FGA;
            stats[pTPM] += pbs.TPM;
            stats[pTPA] += pbs.TPA;
            stats[pFTM] += pbs.FTM;
            stats[pFTA] += pbs.FTA;
            stats[pOREB] += pbs.OREB;
            stats[pDREB] += pbs.DREB;
            stats[pSTL] += pbs.STL;
            stats[pTO] += pbs.TOS;
            stats[pBLK] += pbs.BLK;
            stats[pAST] += pbs.AST;
            stats[pFOUL] += pbs.FOUL;

            CalcAvg();
        }

        public void AddPlayerStats(PlayerStats ps)
        {
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i] += ps.stats[i];
            }

            CalcAvg();
        }

        public void ResetStats()
        {
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i] = 0;
            }

            CalcAvg();
        }
    }

    public class PlayerBoxScore : INotifyPropertyChanged
    {
        private UInt16 _FGM, _FGA, _TPM, _TPA, _FTM, _FTA;
        //public ObservableCollection<KeyValuePair<int, string>> PlayersList { get; set; }
        public PlayerBoxScore()
        {
            PlayerID = -1;
            Team = "";
            isStarter = false;
            playedInjured = false;
            isOut = false;
            ResetStats();
        }

        public PlayerBoxScore(DataRow r)
        {
            PlayerID = NSTHelper.getInt(r, "PlayerID");
            GameID = NSTHelper.getInt(r, "GameID");
            Team = r["Team"].ToString();
            isStarter = NSTHelper.getBoolean(r, "isStarter");
            playedInjured = NSTHelper.getBoolean(r, "playedInjured");
            isOut = NSTHelper.getBoolean(r, "isOut");
            MINS = Convert.ToUInt16(r["MINS"].ToString());
            PTS = Convert.ToUInt16(r["PTS"].ToString());
            REB = Convert.ToUInt16(r["REB"].ToString());
            AST = Convert.ToUInt16(r["AST"].ToString());
            STL = Convert.ToUInt16(r["STL"].ToString());
            BLK = Convert.ToUInt16(r["BLK"].ToString());
            TOS = Convert.ToUInt16(r["TOS"].ToString());
            FGM = Convert.ToUInt16(r["FGM"].ToString());
            FGA = Convert.ToUInt16(r["FGA"].ToString());
            TPM = Convert.ToUInt16(r["TPM"].ToString());
            TPA = Convert.ToUInt16(r["TPA"].ToString());
            FTM = Convert.ToUInt16(r["FTM"].ToString());
            FTA = Convert.ToUInt16(r["FTA"].ToString());
            OREB = Convert.ToUInt16(r["OREB"].ToString());
            FOUL = Convert.ToUInt16(r["FOUL"].ToString());
            DREB = (UInt16) (REB - OREB);
            FGp = (float) FGM/FGA;
            TPp = (float) TPM/TPA;
            FTp = (float) FTM/FTA;

            // Let's try to get the result and date of the game
            // Only works for INNER JOIN'ed rows
            try
            {
                int T1PTS = NSTHelper.getInt(r, "T1PTS");
                int T2PTS = NSTHelper.getInt(r, "T2PTS");

                string Team1 = NSTHelper.getString(r, "T1Name");
                string Team2 = NSTHelper.getString(r, "T2Name");

                if (Team == Team1)
                {
                    if (T1PTS > T2PTS)
                        Result = "W " + T1PTS.ToString() + "-" + T2PTS.ToString();
                    else
                        Result = "L " + T1PTS.ToString() + "-" + T2PTS.ToString();

                    TeamPTS = T1PTS;
                    OppTeam = Team2;
                    OppTeamPTS = T2PTS;
                }
                else
                {
                    if (T2PTS > T1PTS)
                        Result = "W " + T2PTS.ToString() + "-" + T1PTS.ToString();
                    else
                        Result = "L " + T2PTS.ToString() + "-" + T1PTS.ToString();

                    TeamPTS = T2PTS;
                    OppTeam = Team1;
                    OppTeamPTS = T1PTS;
                }

                Date = NSTHelper.getString(r, "Date").Split(' ')[0];
            }
            catch (Exception)
            {
            }
        }

        public int PlayerID { get; set; }
        public string Team { get; set; }
        public int TeamPTS { get; set; }
        public string OppTeam { get; set; }
        public int OppTeamPTS { get; set; }
        public bool isStarter { get; set; }
        public bool playedInjured { get; set; }
        public bool isOut { get; set; }
        public UInt16 MINS { get; set; }
        public UInt16 PTS { get; set; }
        public UInt16 FGM
        {
            get { return _FGM; }
            set
            {
                _FGM = value; 
                FGp = (float)_FGM / _FGA;
                CalculatePoints();
                NotifyPropertyChanged("FGp");
                NotifyPropertyChanged("PTS");
            }
        }
        public UInt16 FGA
        {
            get { return _FGA; }
            set
            {
                _FGA = value;
                FGp = (float)_FGM / _FGA;
                CalculatePoints();
                NotifyPropertyChanged("FGp");
                NotifyPropertyChanged("PTS");
            }
        }
        public float FGp { get; set; }
        public UInt16 TPM
        {
            get { return _TPM; }
            set
            {
                _TPM = value;
                TPp = (float)_TPM / _TPA;
                CalculatePoints();
                NotifyPropertyChanged("TPp");
                NotifyPropertyChanged("PTS");
            }
        }
        public UInt16 TPA
        {
            get { return _TPA; }
            set
            {
                _TPA = value;
                TPp = (float)_TPM / _TPA;
                CalculatePoints();
                NotifyPropertyChanged("TPp");
                NotifyPropertyChanged("PTS");
            }
        }
        public float TPp { get; set; }
        public UInt16 FTM 
        {
            get { return _FTM; }
            set
            {
                _FTM = value;
                FTp = (float)FTM / FTA;
                CalculatePoints();
                NotifyPropertyChanged("FTp");
                NotifyPropertyChanged("PTS");
            }
        }
        public UInt16 FTA
        {
            get { return _FTA; }
            set
            {
                _FTA = value;
                FTp = (float)FTM / FTA;
                CalculatePoints();
                NotifyPropertyChanged("FTp");
                NotifyPropertyChanged("PTS");
            }
        }
        public float FTp { get; set; }
        public UInt16 REB { get; set; }
        public UInt16 OREB { get; set; }
        public UInt16 DREB { get; set; }
        public UInt16 STL { get; set; }
        public UInt16 TOS { get; set; }
        public UInt16 BLK { get; set; }
        public UInt16 AST { get; set; }
        public UInt16 FOUL { get; set; }

        public string Result { get; set; }
        public string Date { get; set; }
        public int GameID { get; set; }

        private void CalculatePoints()
        {
            PTS = (ushort)((_FGM - _TPM) * 2 + _TPM * 3 + _FTM); //(fgm - tpm)*2 + tpm*3 + ftm
        }

        public string GetBestStats(int count, string position)
        {
            double fgn = 0, tpn = 0, ftn = 0, orebn, rebn, astn, stln, blkn, ptsn, ftrn = 0;
            Dictionary<string, double> statsn = new Dictionary<string, double>();

            double fgfactor, tpfactor, ftfactor, orebfactor, rebfactor, astfactor, stlfactor, blkfactor, ptsfactor, ftrfactor;

            if (position.EndsWith("G"))
            {
                fgfactor = 0.4871;
                tpfactor = 0.39302;
                ftfactor = 0.86278;
                orebfactor = 1.242;
                rebfactor = 4.153;
                astfactor = 6.324;
                stlfactor = 1.619;
                blkfactor = 0.424;
                ptsfactor = 17.16;
                ftrfactor = 0.271417;
            }
            else if (position.EndsWith("F"))
            {
                fgfactor = 0.52792;
                tpfactor = 0.38034;
                ftfactor = 0.82656;
                orebfactor = 2.671;
                rebfactor = 8.145;
                astfactor = 3.037;
                stlfactor = 1.209;
                blkfactor = 1.24;
                ptsfactor = 17.731;
                ftrfactor = 0.307167;
            }
            else
            {
                fgfactor = 0.52862;
                tpfactor = 0.23014;
                ftfactor = 0.75321;
                orebfactor = 2.328;
                rebfactor = 7.431;
                astfactor = 1.688;
                stlfactor = 0.68;
                blkfactor = 1.536;
                ptsfactor = 11.616;
                ftrfactor = 0.302868;
            }

            if (FGM > 4)
            {
                fgn = FGp/fgfactor;
            }
            statsn.Add("fgn", fgn);

            if (TPM > 2)
            {
                tpn = TPp/tpfactor;
            }
            statsn.Add("tpn", tpn);

            if (FTM > 4)
            {
                ftn = FTp/ftfactor;
            }
            statsn.Add("ftn", ftn);

            orebn = OREB/orebfactor;
            statsn.Add("orebn", orebn);

            /*
            drebn = (REB-OREB)/6.348;
            statsn.Add("drebn", drebn);
            */

            rebn = REB/rebfactor;
            statsn.Add("rebn", rebn);

            astn = AST/astfactor;
            statsn.Add("astn", astn);

            stln = STL/stlfactor;
            statsn.Add("stln", stln);

            blkn = BLK/blkfactor;
            statsn.Add("blkn", blkn);

            ptsn = PTS/ptsfactor;
            statsn.Add("ptsn", ptsn);

            if (FTM > 3)
            {
                ftrn = (FTM/FGA)/ftrfactor;
            }
            statsn.Add("ftrn", ftrn);

            var items = from k in statsn.Keys
                        orderby statsn[k] descending
                        select k;

            string s = "";
            int i = 0;
            foreach (var item in items)
            {
                if (i == count) break;

                switch (item)
                {
                    case "fgn":
                        s += String.Format("FG: {0}-{1} ({2:F3})\n", FGM, FGA, FGp);
                        break;

                    case "tpn":
                        s += String.Format("3P: {0}-{1} ({2:F3})\n", TPM, TPA, TPp);
                        break;

                    case "ftn":
                        s += String.Format("FT: {0}-{1} ({2:F3})\n", FTM, FTA, FTp);
                        break;

                    case "orebn":
                        s += String.Format("OREB: {0}\n", OREB);
                        break;

                    /*
                    case "drebn":
                        s += String.Format("DREB: {0}\n", REB - OREB);
                        break;
                    */

                    case "rebn":
                        s += String.Format("REB: {0}\n", REB);
                        break;

                    case "astn":
                        s += String.Format("AST: {0}\n", AST);
                        break;

                    case "stln":
                        s += String.Format("STL: {0}\n", STL);
                        break;

                    case "blkn":
                        s += String.Format("BLK: {0}\n", BLK);
                        break;

                    case "ptsn":
                        s += String.Format("PTS: {0}\n", PTS);
                        break;

                    case "ftrn":
                        s += String.Format("FTM/FGA: {0}-{1} ({2:F3})\n", FTM, FGA, FTM/FGA);
                        break;
                }

                i++;
            }
            return s;
        }

        public void ResetStats()
        {
            MINS = 0;
            PTS = 0;
            FGM = 0;
            FGA = 0;
            TPM = 0;
            TPA = 0;
            FTM = 0;
            FTA = 0;
            REB = 0;
            OREB = 0;
            DREB = 0;
            STL = 0;
            TOS = 0;
            BLK = 0;
            AST = 0;
            FOUL = 0;
            FGp = 0;
            FTp = 0;
            TPp = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }

    public class PlayerRankings
    {
        public const int pMPG = 0,
                         pPPG = 1,
                         pDRPG = 2,
                         pORPG = 3,
                         pAPG = 4,
                         pSPG = 5,
                         pBPG = 6,
                         pTPG = 7,
                         pFPG = 8,
                         pFGp = 9,
                         pFGeff = 10,
                         pTPp = 11,
                         pTPeff = 12,
                         pFTp = 13,
                         pFTeff = 14,
                         pRPG = 15;

        private readonly int avgcount = (new PlayerStats(new Player(-1, "", "", "", "", ""))).averages.Length;

        private readonly Dictionary<int, int[]> rankings = new Dictionary<int, int[]>();
        public Dictionary<int, int[]> list = new Dictionary<int, int[]>();

        public PlayerRankings(Dictionary<int, PlayerStats> pst)
        {
            foreach (var kvp in pst)
            {
                rankings.Add(kvp.Key, new int[avgcount]);
            }
            for (int j = 0; j < avgcount; j++)
            {
                var averages = new Dictionary<int, float>();
                foreach (var kvp in pst)
                {
                    averages.Add(kvp.Key, kvp.Value.averages[j]);
                }

                var tempList = new List<KeyValuePair<int, float>>(averages);
                tempList.Sort((x, y) => x.Value.CompareTo(y.Value));
                tempList.Reverse();

                int k = 1;
                foreach (var kvp in tempList)
                {
                    rankings[kvp.Key][j] = k;
                    k++;
                }
            }

            /*
            list = new Dictionary<int, int[]>();
            for (int i = 0; i<pst.Count; i++)
                list.Add(pst[i].ID, rankings[i]);
            */
            list = rankings;
        }
    }

    public class TeamStats
    {
        public const int tMINS = 0, tPF = 1, tPA = 2, tFGM = 4, tFGA = 5, tTPM = 6, tTPA = 7, tFTM = 8, tFTA = 9, tOREB = 10, tDREB = 11, tSTL = 12, tTO = 13, tBLK = 14, tAST = 15, tFOUL = 16;

        public const int tPPG = 0, tPAPG = 1, tFGp = 2, tFGeff = 3, tTPp = 4, tTPeff = 5, tFTp = 6, tFTeff = 7, tRPG = 8, tORPG = 9, tDRPG = 10, tSPG = 11, tBPG = 12, tTPG = 13, tAPG = 14, tFPG = 15, tWp = 16, tWeff = 17, tPD = 18;

        /// <summary>
        /// Averages for each team.
        /// 0: PPG, 1: PAPG, 2: FG%, 3: FGEff, 4: 3P%, 5: 3PEff, 6: FT%, 7:FTEff,
        /// 8: RPG, 9: ORPG, 10: DRPG, 11: SPG, 12: BPG, 13: TPG, 14: APG, 15: FPG, 16: W%,
        /// 17: Weff, 18: PD
        /// </summary>
        public float[] averages = new float[19];

        public string name;
        public Int32 offset;

        public float[] pl_averages = new float[19];
        public Int32 pl_offset;
        public UInt16[] pl_stats = new UInt16[18];
        public byte[] pl_winloss = new byte[2];

        /// <summary>
        /// Stats for each team.
        /// 0: M, 1: PF, 2: PA, 3: 0x0000, 4: FGM, 5: FGA, 6: 3PM, 7: 3PA, 8: FTM, 9: FTA,
        /// 10: OREB, 11: DREB, 12: STL, 13: TO, 14: BLK, 15: AST,
        /// 16: FOUL
        /// </summary>
        public UInt16[] stats = new UInt16[18];

        public byte[] winloss = new byte[2];

        public Dictionary<string, double> metrics = new Dictionary<string, double>(); 

        public TeamStats()
        {
            prepareEmpty();
        }

        public TeamStats(string name)
        {
            this.name = name;
            prepareEmpty();
        }

        private void prepareEmpty()
        {
            winloss[0] = Convert.ToByte(0);
            winloss[1] = Convert.ToByte(0);
            pl_winloss[0] = Convert.ToByte(0);
            pl_winloss[1] = Convert.ToByte(0);
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i] = 0;
                pl_stats[i] = 0;
            }
            for (int i = 0; i < averages.Length; i++)
            {
                averages[i] = 0;
                pl_averages[i] = 0;
            }
        }

        public void calcAvg()
        {
            int games = winloss[0] + winloss[1];
            int pl_games = pl_winloss[0] + pl_winloss[1];

            averages[tWp] = (float) winloss[0]/games;
            averages[tWeff] = averages[tWp]*winloss[0];
            averages[tPPG] = (float) stats[tPF]/games;
            averages[tPAPG] = (float) stats[tPA]/games;
            averages[tFGp] = (float) stats[tFGM]/stats[tFGA];
            averages[tFGeff] = averages[tFGp]*((float) stats[tFGM]/games);
            averages[tTPp] = (float) stats[tTPM]/stats[tTPA];
            averages[tTPeff] = averages[tTPp]*((float) stats[tTPM]/games);
            averages[tFTp] = (float) stats[tFTM]/stats[tFTA];
            averages[tFTeff] = averages[tFTp]*((float) stats[tFTM]/games);
            averages[tRPG] = (float) (stats[tOREB] + stats[tDREB])/games;
            averages[tORPG] = (float) stats[tOREB]/games;
            averages[tDRPG] = (float) stats[tDREB]/games;
            averages[tSPG] = (float) stats[tSTL]/games;
            averages[tBPG] = (float) stats[tBLK]/games;
            averages[tTPG] = (float) stats[tTO]/games;
            averages[tAPG] = (float) stats[tAST]/games;
            averages[tFPG] = (float) stats[tFOUL]/games;
            averages[tPD] = averages[tPPG] - averages[tPAPG];

            pl_averages[tWp] = (float) pl_winloss[0]/pl_games;
            pl_averages[tWeff] = pl_averages[tWp]*pl_winloss[0];
            pl_averages[tPPG] = (float) pl_stats[tPF]/pl_games;
            pl_averages[tPAPG] = (float)pl_stats[tPA] / pl_games;
            pl_averages[tFGp] = (float) pl_stats[tFGM]/pl_stats[tFGA];
            pl_averages[tFGeff] = pl_averages[tFGp]*((float) pl_stats[tFGM]/pl_games);
            pl_averages[tTPp] = (float) pl_stats[tTPM]/pl_stats[tTPA];
            pl_averages[tTPeff] = pl_averages[tTPp]*((float) pl_stats[tTPM]/pl_games);
            pl_averages[tFTp] = (float) pl_stats[tFTM]/pl_stats[tFTA];
            pl_averages[tFTeff] = pl_averages[tFTp]*((float) pl_stats[tFTM]/pl_games);
            pl_averages[tRPG] = (float) (pl_stats[tOREB] + pl_stats[tDREB])/pl_games;
            pl_averages[tORPG] = (float) pl_stats[tOREB]/pl_games;
            pl_averages[tDRPG] = (float) pl_stats[tDREB]/pl_games;
            pl_averages[tSPG] = (float) pl_stats[tSTL]/pl_games;
            pl_averages[tBPG] = (float) pl_stats[tBLK]/pl_games;
            pl_averages[tTPG] = (float) pl_stats[tTO]/pl_games;
            pl_averages[tAPG] = (float) pl_stats[tAST]/pl_games;
            pl_averages[tFPG] = (float) pl_stats[tFOUL]/pl_games;
            pl_averages[tPD] = pl_averages[tPPG] - pl_averages[tPAPG];
        }

        public void CalcMetrics(TeamStats tsopp)
        {
            metrics = new Dictionary<string, double>();

            double[] tstats = new double[stats.Length];
            for (int i = 0; i < stats.Length; i++)
            {
                tstats[i] = stats[i];
            }

            double[] toppstats = new double[tsopp.stats.Length];
            for (int i = 0; i < tsopp.stats.Length; i++)
            {
                toppstats[i] = stats[i];
            }

            var Poss = GetPossMetric(tstats, toppstats);
            metrics.Add("Poss", Poss);

            Poss = GetPossMetric(toppstats, tstats);
            tsopp.metrics.Add("Poss", Poss);

            double Pace = 48*((metrics["Poss"] + tsopp.metrics["Poss"])/(2*(stats[tMINS])));
            metrics.Add("Pace", Pace);
        }

        private static double GetPossMetric( double[] tstats, double[] toppstats)
        {
            double Poss = 0.5*
                          ((tstats[tFGA] + 0.4*tstats[tFTA] -
                            1.07*(tstats[tOREB]/(tstats[tOREB] + toppstats[tDREB]))*
                            (tstats[tFGA] - tstats[tFGM]) + tstats[tTO]) +
                           (toppstats[tFGA] + 0.4*toppstats[tFTA] -
                            1.07*(toppstats[tOREB]/(toppstats[tOREB] + tstats[tDREB]))*
                            (toppstats[tFGA] - toppstats[tFGM]) + toppstats[tTO]));
            return Poss;
        }

        internal int getGames()
        {
            int games = winloss[0] + winloss[1];
            return games;
        }

        internal int getPlayoffGames()
        {
            int pl_games = pl_winloss[0] + pl_winloss[1];
            return pl_games;
        }

        public void AddTeamStats(TeamStats ts, string mode)
        {
            switch (mode)
            {
                case "Season":
                    {
                        winloss[0] += ts.winloss[0];
                        winloss[1] += ts.winloss[1];

                        for (int i = 0; i < stats.Length; i++)
                        {
                            stats[i] += ts.stats[i];
                        }

                        calcAvg();
                        break;
                    }
                case "Playoffs":
                    {
                        pl_winloss[0] += ts.pl_winloss[0];
                        pl_winloss[1] += ts.pl_winloss[1];

                        for (int i = 0; i < pl_stats.Length; i++)
                        {
                            pl_stats[i] += ts.pl_stats[i];
                        }

                        calcAvg();
                        break;
                    }
                case "All":
                    {
                        winloss[0] += ts.winloss[0];
                        winloss[1] += ts.winloss[1];

                        for (int i = 0; i < stats.Length; i++)
                        {
                            stats[i] += ts.stats[i];
                        }

                        winloss[0] += ts.pl_winloss[0];
                        winloss[1] += ts.pl_winloss[1];

                        for (int i = 0; i < pl_stats.Length; i++)
                        {
                            stats[i] += ts.pl_stats[i];
                        }

                        calcAvg();
                        break;
                    }
            }
        }
    }

    public class Rankings
    {
        public const int tPPG = 0, tPAPG = 1, tFGp = 2, tFGeff = 3, tTPp = 4, tTPeff = 5, tFTp = 6, tFTeff = 7, tRPG = 8, tORPG = 9, tDRPG = 10, tSPG = 11, tBPG = 12, tTPG = 13, tAPG = 14, tFPG = 15, tWp = 16, tWeff = 17, tPD = 18;

        public int[][] rankings;

        public Rankings(TeamStats[] _tst)
        {
            rankings = new int[_tst.Length][];
            for (int i = 0; i < _tst.Length; i++)
            {
                rankings[i] = new int[_tst[i].averages.Length];
            }
            for (int j = 0; j < _tst[0].averages.Length; j++)
            {
                var averages = new Dictionary<int, float>();
                for (int i = 0; i < _tst.Length; i++)
                {
                    averages.Add(i, _tst[i].averages[j]);
                }

                var tempList = new List<KeyValuePair<int, float>>(averages);
                tempList.Sort((x, y) => x.Value.CompareTo(y.Value));
                tempList.Reverse();

                int k = 1;
                foreach (var kvp in tempList)
                {
                    rankings[kvp.Key][j] = k;
                    k++;
                }
            }
        }
    }

    public class BoxScore
    {
        public UInt16 MINS1;
        public UInt16 MINS2;
        public UInt16 AST1;
        public UInt16 AST2;
        public UInt16 BLK1;
        public UInt16 BLK2;
        public UInt16 FGA1;
        public UInt16 FGA2;
        public UInt16 FGM1;
        public UInt16 FGM2;
        public UInt16 FTA1;
        public UInt16 FTA2;
        public UInt16 FTM1;
        public UInt16 FTM2;
        public UInt16 OFF1;
        public UInt16 OFF2;
        public UInt16 PF1;
        public UInt16 PF2;
        public UInt16 PTS1;
        public UInt16 PTS2;
        public UInt16 REB1;
        public UInt16 REB2;
        public UInt16 STL1;
        public UInt16 STL2;
        public int SeasonNum;
        public UInt16 TO1;
        public UInt16 TO2;
        public UInt16 TPA1;
        public UInt16 TPA2;
        public UInt16 TPM1;
        public UInt16 TPM2;
        public string Team1;
        public string Team2;
        public int bshistid = -1;
        public bool doNotUpdate;
        public bool done;
        public DateTime gamedate;
        public int id = -1;
        public bool isPlayoff;
    }

    public class BoxScoreEntry
    {
        public BoxScore bs;
        public DateTime date;
        public bool mustUpdate;
        public List<PlayerBoxScore> pbsList;

        public BoxScoreEntry(BoxScore bs)
        {
            this.bs = bs;
            date = DateTime.Now;
        }

        public BoxScoreEntry(BoxScore bs, DateTime date, List<PlayerBoxScore> pbsList)
        {
            this.bs = bs;
            this.date = date;
            this.pbsList = pbsList;
        }
    }

    [Serializable]
    public class PlayoffTree : ISerializable
    {
        public bool done;
        public string[] teams = new string[16];

        public PlayoffTree()
        {
            teams[0] = "Invalid";
        }

        public PlayoffTree(SerializationInfo info, StreamingContext ctxt)
        {
            teams = (string[]) info.GetValue("teams", typeof (string[]));
            done = (bool) info.GetValue("done", typeof (bool));
        }

        #region ISerializable Members

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("teams", teams);
            info.AddValue("done", done);
        }

        #endregion
    }

    public class Player
    {
        public Player()
        {
        }

        public Player(int ID, string Team, string LastName, string FirstName, string Position1, string Position2)
        {
            this.ID = ID;
            this.Team = Team;
            this.LastName = LastName;
            this.FirstName = FirstName;
            Position = Position1;
            this.Position2 = Position2;
        }

        public Player(DataRow dataRow)
        {
            ID = NSTHelper.getInt(dataRow, "ID");
            Team = NSTHelper.getString(dataRow, "TeamFin");
            LastName = NSTHelper.getString(dataRow, "LastName");
            FirstName = NSTHelper.getString(dataRow, "FirstName");
            Position = NSTHelper.getString(dataRow, "Position1");
            Position2 = NSTHelper.getString(dataRow, "Position2");
        }

        public int ID { get; set; }
        public string Team { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Position { get; set; }
        public string Position2 { get; set; }
    }

    public class PlayerStatsRow
    {
        public const int pGP = 0,
                         pGS = 1,
                         pMINS = 2,
                         pPTS = 3,
                         pDREB = 4,
                         pOREB = 5,
                         pAST = 6,
                         pSTL = 7,
                         pBLK = 8,
                         pTO = 9,
                         pFOUL = 10,
                         pFGM = 11,
                         pFGA = 12,
                         pTPM = 13,
                         pTPA = 14,
                         pFTM = 15,
                         pFTA = 16;

        public const int pMPG = 0,
                         pPPG = 1,
                         pDRPG = 2,
                         pORPG = 3,
                         pAPG = 4,
                         pSPG = 5,
                         pBPG = 6,
                         pTPG = 7,
                         pFPG = 8,
                         pFGp = 9,
                         pFGeff = 10,
                         pTPp = 11,
                         pTPeff = 12,
                         pFTp = 13,
                         pFTeff = 14,
                         pRPG = 15;

        public PlayerStatsRow(PlayerStats ps)
        {
            LastName = ps.LastName;
            FirstName = ps.FirstName;

            GP = ps.stats[pGP];
            GS = ps.stats[pGS];
            MINS = ps.stats[pMINS];
            PTS = ps.stats[pPTS];
            FGM = ps.stats[pFGM];
            FGA = ps.stats[pFGA];
            TPM = ps.stats[pTPM];
            TPA = ps.stats[pTPA];
            FTM = ps.stats[pFTM];
            FTA = ps.stats[pFTA];
            OREB = ps.stats[pOREB];
            DREB = ps.stats[pDREB];
            REB = (UInt16) (OREB + DREB);
            STL = ps.stats[pSTL];
            TOS = ps.stats[pTO];
            BLK = ps.stats[pBLK];
            AST = ps.stats[pAST];
            FOUL = ps.stats[pFOUL];

            MPG = ps.averages[pMPG];
            PPG = ps.averages[pPPG];
            FGp = ps.averages[pFGp];
            FGeff = ps.averages[pFGeff];
            TPp = ps.averages[pTPp];
            TPeff = ps.averages[pTPeff];
            FTp = ps.averages[pFTp];
            FTeff = ps.averages[pFTeff];
            RPG = ps.averages[pRPG];
            ORPG = ps.averages[pORPG];
            DRPG = ps.averages[pDRPG];
            SPG = ps.averages[pSPG];
            TPG = ps.averages[pTPG];
            BPG = ps.averages[pBPG];
            APG = ps.averages[pAPG];
            FPG = ps.averages[pFPG];

            ID = ps.ID;
            Position1 = ps.Position1;
            Position2 = ps.Position2;
            TeamF = ps.TeamF;
            TeamS = ps.TeamS;
            isActive = ps.isActive;
            isAllStar = ps.isAllStar;
            isInjured = ps.isInjured;
            isNBAChampion = ps.isNBAChampion;
        }

        public PlayerStatsRow(PlayerStats ps, string type) : this(ps)
        {
            Type = type;
        }

        public PlayerStatsRow(PlayerStats ps, string type, string group) : this(ps, type)
        {
            Type = type;
            Group = group;
        }

        public UInt16 GP { get; set; }
        public UInt16 GS { get; set; }

        public UInt16 MINS { get; set; }
        public UInt16 PTS { get; set; }
        public UInt16 FGM { get; set; }
        public UInt16 FGA { get; set; }
        public UInt16 TPM { get; set; }
        public UInt16 TPA { get; set; }
        public UInt16 FTM { get; set; }
        public UInt16 FTA { get; set; }
        public UInt16 REB { get; set; }
        public UInt16 OREB { get; set; }
        public UInt16 DREB { get; set; }
        public UInt16 STL { get; set; }
        public UInt16 TOS { get; set; }
        public UInt16 BLK { get; set; }
        public UInt16 AST { get; set; }
        public UInt16 FOUL { get; set; }

        public float MPG { get; set; }
        public float PPG { get; set; }
        public float FGp { get; set; }
        public float FGeff { get; set; }
        public float TPp { get; set; }
        public float TPeff { get; set; }
        public float FTp { get; set; }
        public float FTeff { get; set; }
        public float RPG { get; set; }
        public float ORPG { get; set; }
        public float DRPG { get; set; }
        public float SPG { get; set; }
        public float TPG { get; set; }
        public float BPG { get; set; }
        public float APG { get; set; }
        public float FPG { get; set; }

        public int ID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Position1 { get; set; }
        public string Position2 { get; set; }
        public string TeamF { get; set; }
        public string TeamS { get; set; }
        public bool isActive { get; set; }
        public bool isAllStar { get; set; }
        public bool isInjured { get; set; }
        public bool isNBAChampion { get; set; }

        public string Type { get; set; }
        public string Group { get; set; }
    }

    public class PlayerMetricStatsRow
    {
        public int ID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string TeamF { get; set; }

        public double EFF { get; set; }
        public double GmSc { get; set; }
        public double EFGp { get; set;}
        public double TSp { get; set; }
        public double ASTp { get; set; }
        public double STLp { get; set; }
        public double TOp { get; set; }
        public double USGp { get; set; }
        public double PTSR { get; set; }
        public double REBR { get; set; }
        public double ASTR { get; set; }
        public double BLKR { get; set; }
        public double STLR { get; set; }
        public double TOR { get; set; }
        public double FTR { get; set; }

        #region Metrics that require opponents' stats

        public double PER { get; set; }
        public double BLKp { get; set; }
        public double DREBp { get; set; }
        public double OREBp { get; set; }
        public double REBp { get; set; }
        public double PPR { get; set; }

        #endregion

        public PlayerMetricStatsRow(PlayerStats ps)
        {
            //ps.CalcMetrics();

            ID = ps.ID;
            LastName = ps.LastName;
            FirstName = ps.FirstName;
            TeamF = ps.TeamF;

            EFF = ps.metrics["EFF"];
            GmSc = ps.metrics["GmSc"];
            EFGp = ps.metrics["EFG%"];
            TSp = ps.metrics["TS%"];
            ASTp = ps.metrics["AST%"];
            STLp = ps.metrics["STL%"];
            TOp = ps.metrics["TO%"];
            USGp = ps.metrics["USG%"];
            PTSR = ps.metrics["PTSR"];
            REBR = ps.metrics["REBR"];
            ASTR = ps.metrics["ASTR"];
            BLKR = ps.metrics["BLKR"];
            STLR = ps.metrics["STLR"];
            TOR = ps.metrics["TOR"];
            FTR = ps.metrics["FTR"];

            try
            {
                PER = ps.metrics["PER"];
            }
            catch (Exception)
            {
                PER = double.NaN;
            }

            BLKp = ps.metrics["BLK%"];
            DREBp = ps.metrics["DREB%"];
            OREBp = ps.metrics["OREB%"];
            REBp = ps.metrics["REB%"];
            PPR = ps.metrics["PPR"];
        }   
    }
}