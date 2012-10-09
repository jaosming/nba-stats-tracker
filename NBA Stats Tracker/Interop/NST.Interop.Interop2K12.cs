#region Copyright Notice

// Created by Lefteris Aslanoglou, (c) 2011-2012
// 
// Implementation of thesis
// "Application Development for Basketball Statistical Analysis in Natural Language"
// under the supervision of Prof. Athanasios Tsakalidis & MSc Alexandros Georgiou,
// Computer Engineering & Informatics Department, University of Patras, Greece.
// 
// All rights reserved. Unless specifically stated otherwise, the code in this file should 
// not be reproduced, edited and/or republished without explicit permission from the 
// author.

#endregion

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using LeftosCommonLibrary;
using Microsoft.Win32;
using NBA_Stats_Tracker.Data;
using NBA_Stats_Tracker.Windows;

#endregion

namespace NBA_Stats_Tracker.Interop
{
    /// <summary>
    /// Implements methods for importing and exporting NBA 2K12 saves using BinaryReader and BinaryWriter.
    /// This method of interoperability has multiple issues regarding the team order detection and only worked for the first season, 
    /// so it's preferable to use <see cref="InteropREditor"/> instead.
    /// </summary>
    public static class Interop2K12
    {
        /// <summary>
        /// Gets the stats from an NBA 2K12 save.
        /// </summary>
        /// <param name="fn">The path to the save.</param>
        /// <param name="tst">The resulting team stats dictionary.</param>
        /// <param name="tstopp">The resulting opposing team stats dictionary.</param>
        /// <param name="TeamOrder">The team order.</param>
        /// <param name="pt">The playoff tree.</param>
        /// <param name="havePT">if set to <c>true</c>, the pre-existing PlayoffTree will be used.</param>
        public static void GetStatsFrom2K12Save(string fn, out Dictionary<int, TeamStats> tst, ref Dictionary<int, TeamStats> tstopp,
                                                ref SortedDictionary<string, int> TeamOrder, ref PlayoffTree pt, bool havePT = false)
        {
            var _teamStats = new Dictionary<int, TeamStats>();
            for (int i = 0; i < 30; i++)
            {
                _teamStats[i] = new TeamStats();
            }
            if (!havePT)
                pt = null;

            string ext = Tools.getExtension(fn);

            if (ext.ToUpperInvariant() == "PMG")
            {
                if (!havePT)
                {
                    pt = new PlayoffTree();
                    MessageBoxResult r = MessageBox.Show("Do you have a saved Playoff Tree you want to load for this save file?",
                                                         "NBA Stats Tracker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (r == MessageBoxResult.No)
                    {
                        var ptw = new PlayoffTreeWindow();
                        ptw.ShowDialog();
                        if (!pt.done)
                        {
                            tst = new Dictionary<int, TeamStats>();
                            return;
                        }

                        var spt = new SaveFileDialog
                                  {
                                      Title = "Please select a file to save the Playoff Tree to...",
                                      InitialDirectory = App.AppDocsPath,
                                      Filter = "Playoff Tree files (*.ptr)|*.ptr"
                                  };
                        spt.ShowDialog();

                        if (spt.FileName == "")
                        {
                            tst = new Dictionary<int, TeamStats>();
                            return;
                        }

                        try
                        {
                            using (FileStream stream = File.Open(spt.FileName, FileMode.Create))
                            {
                                var bf = new BinaryFormatter {AssemblyFormat = FormatterAssemblyStyle.Simple};

                                bf.Serialize(stream, pt);
                            }
                        }
                        catch (Exception ex)
                        {
                            App.errorReport(ex, "Trying to save playoff tree");
                        }
                    }
                    else if (r == MessageBoxResult.Yes)
                    {
                        var ofd = new OpenFileDialog
                                  {
                                      Filter = "Playoff Tree files (*.ptr)|*.ptr",
                                      InitialDirectory = App.AppDocsPath,
                                      Title = "Please select the file you saved the Playoff Tree to for " + Tools.getSafeFilename(fn) + "..."
                                  };
                        ofd.ShowDialog();

                        if (ofd.FileName == "")
                        {
                            tst = new Dictionary<int, TeamStats>();
                            return;
                        }

                        using (FileStream stream = File.Open(ofd.FileName, FileMode.Open))
                        {
                            var bf = new BinaryFormatter {AssemblyFormat = FormatterAssemblyStyle.Simple};

                            pt = (PlayoffTree) bf.Deserialize(stream);
                        }
                    }
                    else
                    {
                        tst = new Dictionary<int, TeamStats>();
                        return;
                    }
                }
            }
            prepareOffsets(fn, _teamStats, ref TeamOrder, ref pt);

            using (FileStream fileStream = File.OpenRead(fn))
            {
                var br = new BinaryReader(fileStream);

                using (var ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true))
                {
                    var buf = new byte[2];

                    foreach (var kvp in TeamOrder)
                    {
                        if (kvp.Key != "")
                        {
                            _teamStats[kvp.Value].name = kvp.Key;
                            _teamStats[kvp.Value].displayName = kvp.Key;
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
                }
            }

            foreach (int key in _teamStats.Keys)
            {
                _teamStats[key].calcAvg();
            }

            tst = new Dictionary<int, TeamStats>(_teamStats);

            //TODO: Implement loading opponents stats from 2K12 save here
            /*
            tstopp = new TeamStats[tst.Count];
            for (int i = 0; i < tst.Count; i++)
            {
                tstopp[i] = new TeamStats(tst[i].name);
            }
            */
        }

        /// <summary>
        /// Calculates the decimal offsets for each team inside the save file.
        /// </summary>
        /// <param name="fn">The path to the save file.</param>
        /// <param name="_teamStats">The team stats dictionary.</param>
        /// <param name="TeamOrder">The team order.</param>
        /// <param name="pt">The playoff tree.</param>
        public static void prepareOffsets(string fn, Dictionary<int, TeamStats> _teamStats, ref SortedDictionary<string, int> TeamOrder,
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
                        _teamStats[TeamOrder[pt.teams[i]]].pl_offset = _teamStats[TeamOrder[pt.teams[i - 1]]].pl_offset + 40;
                    }
                }
                //else if (inPlayoffs == -1) return;
            }
            else
            {
                for (int i = 1; i < 16; i++)
                {
                    _teamStats[TeamOrder[pt.teams[i]]].pl_offset = _teamStats[TeamOrder[pt.teams[i - 1]]].pl_offset + 40;
                }
            }
        }

        /// <summary>
        /// Checks if the specified save file is in the playoffs, by checking if all teams have played the required number of games.
        /// </summary>
        /// <param name="fn">The path to the save file.</param>
        /// <param name="_teamStats">The team stats dictionary.</param>
        /// <param name="TeamOrder">The team order.</param>
        /// <param name="pt">The playoff tree.</param>
        /// <returns></returns>
        private static int checkIfIntoPlayoffs(string fn, Dictionary<int, TeamStats> _teamStats, ref SortedDictionary<string, int> TeamOrder,
                                               ref PlayoffTree pt)
        {
            int gamesInSeason = -1;
            string ptFile = "";
            string safefn = Tools.getSafeFilename(fn);
            string SettingsFile = App.AppDocsPath + safefn + ".cfg";
            string mode = "";

            if (File.Exists(SettingsFile))
            {
                using (var sr = new StreamReader(SettingsFile))
                {
                    while (sr.Peek() > -1)
                    {
                        string line = sr.ReadLine();
                        Debug.Assert(line != null, "line != null");
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
                }
            }
            if (gamesInSeason == -1)
            {
                gamesInSeason = askGamesInSeason();

                mode = askMode();

                TeamOrder = setTeamOrder(mode);

                saveSettingsForFile(fn, gamesInSeason, "", mode, SettingsFile);
            }

            bool done;
            using (FileStream fileStream = File.OpenRead(fn))
            {
                var br = new BinaryReader(fileStream);
                MemoryStream ms;
                using (ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true))
                {
                    done = true;

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
                }
            }

            if (done)
            {
                if (ptFile == "")
                {
/*
                    pt = null;
                    pt = new PlayoffTree();
*/
                    App.tempPT = new PlayoffTree();
                    var ptW = new PlayoffTreeWindow();
                    ptW.ShowDialog();
                    pt = App.tempPT;

                    if (!pt.done)
                        return -1;

                    var spt = new SaveFileDialog
                              {
                                  Title = "Please select a file to save the Playoff Tree to...",
                                  InitialDirectory = App.AppDocsPath,
                                  Filter = "Playoff Tree files (*.ptr)|*.ptr"
                              };
                    spt.ShowDialog();

                    if (spt.FileName == "")
                        return -1;

                    ptFile = spt.FileName;

                    try
                    {
                        using (FileStream stream = File.Open(spt.FileName, FileMode.Create))
                        {
                            var bf = new BinaryFormatter {AssemblyFormat = FormatterAssemblyStyle.Simple};

                            bf.Serialize(stream, pt);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.errorReport(ex, "Trying to save playoff tree");
                    }
                }
                else
                {
                    using (FileStream stream = File.Open(ptFile, FileMode.Open))
                    {
                        var bf = new BinaryFormatter {AssemblyFormat = FormatterAssemblyStyle.Simple};

                        pt = (PlayoffTree) bf.Deserialize(stream);
                    }
                }
            }

            saveSettingsForFile(fn, gamesInSeason, ptFile, mode, SettingsFile);

            if (done)
                return 1;

            return 0;
        }

        /// <summary>
        /// Asks the user which compatibility mode to use.
        /// </summary>
        /// <returns></returns>
        private static string askMode()
        {
            var at = new ComboChoiceWindow(ComboChoiceWindow.Mode.ImportCompatibility);
            at.ShowDialog();
            return App.mode;
        }

        /// <summary>
        /// Saves the settings for the specified NBA 2K12 save.
        /// </summary>
        /// <param name="fn">The save file path.</param>
        /// <param name="gamesInSeason">The games in season.</param>
        /// <param name="ptFile">The playoff tree file.</param>
        /// <param name="mode">The compatibility mode.</param>
        /// <param name="SettingsFile">The settings file path.</param>
        private static void saveSettingsForFile(string fn, int gamesInSeason, string ptFile, string mode, string SettingsFile)
        {
            using (var sw2 = new StreamWriter(SettingsFile, false))
            {
                sw2.WriteLine("{0}\t{1}\t{2}\t{3}", fn, gamesInSeason, ptFile, mode);
            }
        }

        /// <summary>
        /// Exports the stats from the current database to the NBA 2K12 save file.
        /// </summary>
        /// <param name="fn">The save file path.</param>
        /// <param name="tst">The team stats dictionary.</param>
        /// <param name="TeamOrder">The team order.</param>
        /// <param name="pt">The playoff tree.</param>
        public static void updateSavegame(string fn, Dictionary<int, TeamStats> tst, SortedDictionary<string, int> TeamOrder, PlayoffTree pt)
        {
            using (FileStream openRead = File.OpenRead(fn))
            {
                var br = new BinaryReader(openRead);
                using (var ms = new MemoryStream(br.ReadBytes(Convert.ToInt32(br.BaseStream.Length)), true))
                {
                    if ((pt != null) && (pt.teams[0] != "Invalid"))
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            ms.Seek(tst[TeamOrder[pt.teams[i]]].pl_offset, SeekOrigin.Begin);
                            var pl_winloss = new byte[2];
                            tst[TeamOrder[pt.teams[i]]].pl_winloss.CopyTo(pl_winloss, 0);
                            ms.Write(pl_winloss, 0, 2);
                            for (int j = 0; j < 18; j++)
                            {
                                ms.Write(BitConverter.GetBytes(tst[TeamOrder[pt.teams[i]]].pl_stats[j]), 0, 2);
                            }
                        }
                    }

                    for (int i = 0; i < 30; i++)
                    {
                        ms.Seek(tst[i].offset, SeekOrigin.Begin);
                        var winloss = new byte[2];
                        tst[i].winloss.CopyTo(winloss, 0);
                        ms.Write(winloss, 0, 2);
                        for (int j = 0; j < 18; j++)
                        {
                            ms.Write(BitConverter.GetBytes(tst[i].stats[j]), 0, 2);
                        }
                    }

                    using (FileStream fileStream = File.OpenWrite(App.AppTempPath + Tools.getSafeFilename(fn)))
                    {
                        var bw = new BinaryWriter(fileStream);

                        ms.Position = 4;
                        var t = new byte[1048576];
                        int count;
                        do
                        {
                            count = ms.Read(t, 0, 1048576);
                            bw.Write(t, 0, count);
                        } while (count > 0);
                    }
                }
            }

            byte[] crc = Tools.ReverseByteOrder(Tools.StringToByteArray(Tools.getCRC(App.AppTempPath + Tools.getSafeFilename(fn))), 4);

            File.Delete(fn + ".bak");

            File.Move(fn, fn + ".bak");
            using (FileStream fileStream = File.OpenRead(App.AppTempPath + Tools.getSafeFilename(fn)))
            {
                var br2 = new BinaryReader(fileStream);
                using (FileStream openWrite = File.OpenWrite(fn))
                {
                    var bw2 = new BinaryWriter(openWrite);
                    bw2.Write(crc);
                    byte[] readBytes;
                    do
                    {
                        readBytes = br2.ReadBytes(1048576);
                        bw2.Write(readBytes);
                    } while (readBytes.Length > 0);
                }
            }
            File.Delete(App.AppTempPath + Tools.getSafeFilename(fn));
        }

        /// <summary>
        /// Sets the team order (team name & ID pairs).
        /// </summary>
        /// <param name="modeToSet">The compatibility mode to use.</param>
        /// <returns></returns>
        public static SortedDictionary<string, int> setTeamOrder(string modeToSet)
        {
            SortedDictionary<string, int> TeamOrder;

            switch (modeToSet)
            {
                default:
                    TeamOrder = new SortedDictionary<string, int> {
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
                    TeamOrder = new SortedDictionary<string, int> {
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
                    TeamOrder = new SortedDictionary<string, int> {
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
                    TeamOrder = new SortedDictionary<string, int> {
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
                    TeamOrder = new SortedDictionary<string, int> {
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
                    TeamOrder = new SortedDictionary<string, int> {
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
                    TeamOrder = new SortedDictionary<string, int> {
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
                    MessageBox.Show("Conflict for " + modeToSet + " TeamOrder on ID " + kvp.Value);
                    Environment.Exit(-1);
                }
            }

            return TeamOrder;
        }

        /// <summary>
        /// Asks the user how many games are in a season for the current save file being loaded.
        /// </summary>
        /// <param name="gamesInSeason">The games in season.</param>
        /// <returns></returns>
        private static int askGamesInSeason()
        {
            int gamesInSeason = 82;
            MessageBoxResult r =
                MessageBox.Show("How many games does each season have in this save?\n\n82 Games: Yes\n58 Games: No\n29 Games: Cancel", "",
                                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
                gamesInSeason = 82;
            else if (r == MessageBoxResult.No)
                gamesInSeason = 58;
            else if (r == MessageBoxResult.Cancel)
                gamesInSeason = 29;
            return gamesInSeason;
        }

        public static void checkForRedundantSettings()
        {
            string[] stgFiles = Directory.GetFiles(MainWindow.AppDocsPath, "*.cfg");
            if (Directory.Exists(MainWindow.SavesPath))
            {
                foreach (string file in stgFiles)
                {
                    string realfile = file.Substring(0, file.Length - 4);
                    if (File.Exists(MainWindow.SavesPath + Tools.getSafeFilename(realfile)) == false)
                        File.Delete(file);
                }
            }

            string[] bshFiles = Directory.GetFiles(MainWindow.AppDocsPath, "*.bsh");
            if (Directory.Exists(MainWindow.SavesPath))
            {
                foreach (string file in bshFiles)
                {
                    string realfile = file.Substring(0, file.Length - 4);
                    if (File.Exists(MainWindow.SavesPath + Tools.getSafeFilename(realfile)) == false)
                        File.Delete(file);
                }
            }
        }
    }

    /// <summary>
    /// Implements the Playoff Tree structure, containing the 16 teams participating in the playoffs.
    /// </summary>
    [Serializable]
    public class PlayoffTree : ISerializable
    {
        public readonly string[] teams = new string[16];
        public bool done;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayoffTree" /> class.
        /// </summary>
        public PlayoffTree()
        {
            teams[0] = "Invalid";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayoffTree" /> class. Used for serialization.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="ctxt">The CTXT.</param>
        protected PlayoffTree(SerializationInfo info, StreamingContext ctxt)
        {
            teams = (string[]) info.GetValue("teams", typeof (string[]));
            done = (bool) info.GetValue("done", typeof (bool));
        }

        #region ISerializable Members

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            GetObjectData(info, context);
        }

        #endregion

        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("teams", teams);
            info.AddValue("done", done);
        }
    }
}