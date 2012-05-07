﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LeftosCommonLibrary;
using Microsoft.Win32;

namespace NBA_Stats_Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int tMINS = 0, tPF = 1, tPA = 2, tFGM = 4, tFGA = 5, tTPM = 6, tTPA = 7, tFTM = 8, tFTA = 9, tOREB = 10, tDREB = 11, tSTL = 12, tTO = 13, tBLK = 14, tAST = 15, tFOUL = 16;

        public const int tPPG = 0, tPAPG = 1, tFGp = 2, tFGeff = 3, tTPp = 4, tTPeff = 5, tFTp = 6, tFTeff = 7, tRPG = 8, tORPG = 9, tDRPG = 10, tSPG = 11, tBPG = 12, tTPG = 13, tAPG = 14, tFPG = 15, tWp = 16, tWeff = 17, tPD = 18;

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

        public static string AppDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                           @"\NBA Stats Tracker\";

        public static string AppTempPath = AppDocsPath + @"Temp\";
        public static string SavesPath = "";
        public static string AppPath = Environment.CurrentDirectory + "\\";
        public static bool isCustom;

        public static MainWindow mwInstance;

        public static TeamStats[] tst = new TeamStats[1];
        public static TeamStats[] tstopp = new TeamStats[1];
        public static TeamStats[] realtst = new TeamStats[30];
        public static Dictionary<int, PlayerStats> pst = new Dictionary<int, PlayerStats>();
        public static BoxScore bs;
        public static IList<BoxScoreEntry> bshist = new List<BoxScoreEntry>();
        public static PlayoffTree pt;
        public static string ext;
        public static string myTeam;
        public static string currentDB = "";
        public static string addInfo;
        public static int curSeason = 1;
        public static List<BindingList<PlayerBoxScore>> pbsLists;

        public static SortedDictionary<string, int> TeamOrder;

        public static List<string> West = new List<string>
                                              {
                                                  "Thunder",
                                                  "Spurs",
                                                  "Trail Blazers",
                                                  "Clippers",
                                                  "Nuggets",
                                                  "Jazz",
                                                  "Lakers",
                                                  "Mavericks",
                                                  "Suns",
                                                  "Grizzlies",
                                                  "Kings",
                                                  "Timberwolves",
                                                  "Rockets",
                                                  "Hornets",
                                                  "Warriors"
                                              };

        private static SQLiteDatabase db;

        private DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();

            mwInstance = this;

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            btnSave.Visibility = Visibility.Hidden;
            btnCRC.Visibility = Visibility.Hidden;
            btnSaveCustomTeam.Visibility = Visibility.Hidden;
            //btnInject.Visibility = Visibility.Hidden;
            //btnTest.Visibility = Visibility.Hidden;

            isCustom = true;

            if (
                Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                 @"\NBA 2K12 Correct Team Stats"))
                if (Directory.Exists(AppDocsPath) == false)
                    Directory.Move(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                        @"\NBA 2K12 Correct Team Stats",
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker");

            if (Directory.Exists(AppDocsPath) == false) Directory.CreateDirectory(AppDocsPath);
            if (Directory.Exists(AppTempPath) == false) Directory.CreateDirectory(AppTempPath);

            tst[0] = new TeamStats("$$NewDB");
            tstopp[0] = new TeamStats("$$NewDB");

            for (int i = 0; i < 30; i++)
            {
                realtst[i] = new TeamStats();
            }

            //TeamOrder = StatsTracker.setTeamOrder("Mode 0");
            TeamOrder = new SortedDictionary<string, int>();

            foreach (var kvp in TeamOrder)
            {
                cmbTeam1.Items.Add(kvp.Key);
            }

            RegistryKey rk = null;

            try
            {
                rk = Registry.CurrentUser;
            }
            catch (Exception ex)
            {
                App.errorReport(ex, "Registry.CurrentUser");
            }

            rk = rk.OpenSubKey(@"SOFTWARE\2K Sports\NBA 2K12");
            if (rk == null)
            {
                SavesPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                            @"\2K Sports\NBA 2K12\Saves\";
            }
            else
            {
                SavesPath = rk.GetValue("Saves").ToString();
            }

            checkForRedundantSettings();

            if (App.realNBAonly)
            {
                mnuFileGetRealStats_Click(null, new RoutedEventArgs());
                MessageBox.Show("Nothing but net! Thanks for using NBA Stats Tracker!");
                Environment.Exit(-1);
            }
            else
            {
                checkForUpdates();
            }
        }

        public static string AppDocsPath1
        {
            get { return AppDocsPath; }
        }

        public static void checkForRedundantSettings()
        {
            string[] stgFiles = Directory.GetFiles(AppDocsPath, "*.cfg");
            if (Directory.Exists(SavesPath))
            {
                foreach (string file in stgFiles)
                {
                    string realfile = file.Substring(0, file.Length - 4);
                    if (File.Exists(SavesPath + Tools.getSafeFilename(realfile)) == false)
                        File.Delete(file);
                }
            }

            string[] bshFiles = Directory.GetFiles(AppDocsPath, "*.bsh");
            if (Directory.Exists(SavesPath))
            {
                foreach (string file in bshFiles)
                {
                    string realfile = file.Substring(0, file.Length - 4);
                    if (File.Exists(SavesPath + Tools.getSafeFilename(realfile)) == false)
                        File.Delete(file);
                }
            }
        }

        private void btnImport2K12_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Please select the Career file you're playing...";
            ofd.Filter = "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
                         +
                         "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                         "Create A Legend files (*.SMG)|*.SMG";
            if (Directory.Exists(SavesPath)) ofd.InitialDirectory = SavesPath;
            ofd.ShowDialog();
            if (ofd.FileName == "") return;

            cmbTeam1.SelectedIndex = -1;

            isCustom = true;
            //prepareWindow(isCustom);
            TeamOrder = NSTHelper.setTeamOrder("Mode 0");

            TeamStats[] temp = new TeamStats[1];

            //TODO: Implement Opponents stats from 2K12 Save
            //TeamStats[] tempopp = new TeamStats[1];
            TeamStats[] tempopp = tstopp;

            NSTHelper.GetStatsFrom2K12Save(ofd.FileName, ref temp, ref tempopp, ref TeamOrder, ref pt);
            if (temp.Length > 1)
            {
                tst = temp;
                tstopp = tempopp;
                populateTeamsComboBox(TeamOrder, pt);
            }

            if (tst.Length != tstopp.Length)
            {
                tstopp = new TeamStats[tst.Length];
                for (int i = 0; i<tst.Length; i++) tstopp[i] = new TeamStats(tst[i].name);
            }

            cmbTeam1.SelectedIndex = 0;

            // Following is commented out since Box Scores are now kept in the Database file
            /*
            if (File.Exists(AppDocsPath + Tools.getSafeFilename(ofd.FileName) + ".bsh"))
            {
                BinaryReader stream = new BinaryReader(File.OpenRead(AppDocsPath + Tools.getSafeFilename(ofd.FileName) + ".bsh"));
                string cur = stream.ReadString();
                string expect = "NST_BSH_FILE_START";
                if (cur != expect)
                    MessageBox.Show("Error while reading box score history: Expected " + expect);

                cur = stream.ReadString();
                expect = "BOXSCOREHISTORY_START";
                if (cur != expect)
                    MessageBox.Show("Error while reading box score history: Expected " + expect);

                int bshistlen = stream.ReadInt32();
                bshist = new List<BoxScoreEntry>(bshistlen);
                for (int i = 0; i < bshistlen; i++)
                {
                    BoxScore bs = new BoxScore();
                    cur = stream.ReadString();
                    expect = "BOXSCORE_START";
                    if (cur != expect)
                    {
                        MessageBox.Show("Error while reading stats: Expected " + expect);
                    }

                    bs.Team1 = stream.ReadString();
                    bs.PTS1 = stream.ReadUInt16();
                    bs.REB1 = stream.ReadUInt16();
                    bs.AST1 = stream.ReadUInt16();
                    bs.STL1 = stream.ReadUInt16();
                    bs.BLK1 = stream.ReadUInt16();
                    bs.TO1 = stream.ReadUInt16();
                    bs.FGM1 = stream.ReadUInt16();
                    bs.FGA1 = stream.ReadUInt16();
                    bs.TPM1 = stream.ReadUInt16();
                    bs.TPA1 = stream.ReadUInt16();
                    bs.FTM1 = stream.ReadUInt16();
                    bs.FTA1 = stream.ReadUInt16();
                    bs.OFF1 = stream.ReadUInt16();
                    bs.PF1 = stream.ReadUInt16();
                    bs.Team2 = stream.ReadString();
                    bs.PTS2 = stream.ReadUInt16();
                    bs.REB2 = stream.ReadUInt16();
                    bs.AST2 = stream.ReadUInt16();
                    bs.STL2 = stream.ReadUInt16();
                    bs.BLK2 = stream.ReadUInt16();
                    bs.TO2 = stream.ReadUInt16();
                    bs.FGM2 = stream.ReadUInt16();
                    bs.FGA2 = stream.ReadUInt16();
                    bs.TPM2 = stream.ReadUInt16();
                    bs.TPA2 = stream.ReadUInt16();
                    bs.FTM2 = stream.ReadUInt16();
                    bs.FTA2 = stream.ReadUInt16();
                    bs.OFF2 = stream.ReadUInt16();
                    bs.PF2 = stream.ReadUInt16();
                    DateTime date = new DateTime(stream.ReadInt32(), stream.ReadInt32(), stream.ReadInt32(), stream.ReadInt32(),
                        stream.ReadInt32(), stream.ReadInt32());

                    cur = stream.ReadString();
                    expect = "BOXSCORE_END";
                    if (cur != expect)
                    {
                        MessageBox.Show("Error while reading stats: Expected " + expect);
                    }
                    BoxScoreEntry bse = new BoxScoreEntry(bs, date);
                    bshist.Add(bse);
                }

                cur = stream.ReadString();
                expect = "BOXSCOREHISTORY_END";
                if (cur != expect)
                {
                    MessageBox.Show("Error while reading stats: Expected " + expect);
                }

                cur = stream.ReadString();
                expect = "NST_BSH_FILE_END";
                if (cur != expect)
                {
                    MessageBox.Show("Error while reading stats: Expected " + expect);
                }
            }
            */

            updateStatus("NBA 2K12 stats imported successfully! Verify that you want this by saving the current season.");
            //cmbTeam1.SelectedItem = "Pistons";
        }

        private void btnCRC_Click(object sender, RoutedEventArgs e)
        {
            String hash = Tools.getCRC(txtFile.Text);

            MessageBox.Show(hash);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            NSTHelper.updateSavegame(txtFile.Text, tst, TeamOrder, pt);
        }

        private void cmbTeam1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string team = cmbTeam1.SelectedItem.ToString();
                int id = TeamOrder[team];
                txtW1.Text = tst[id].winloss[0].ToString();
                txtL1.Text = tst[id].winloss[1].ToString();
                txtPF1.Text = tst[id].stats[tPF].ToString();
                txtPA1.Text = tst[id].stats[tPA].ToString();
                txtFGM1.Text = tst[id].stats[tFGM].ToString();
                txtFGA1.Text = tst[id].stats[tFGA].ToString();
                txt3PM1.Text = tst[id].stats[tTPM].ToString();
                txt3PA1.Text = tst[id].stats[tTPA].ToString();
                txtFTM1.Text = tst[id].stats[tFTM].ToString();
                txtFTA1.Text = tst[id].stats[tFTA].ToString();
                txtOREB1.Text = tst[id].stats[tOREB].ToString();
                txtDREB1.Text = tst[id].stats[tDREB].ToString();
                txtSTL1.Text = tst[id].stats[tSTL].ToString();
                txtTO1.Text = tst[id].stats[tTO].ToString();
                txtBLK1.Text = tst[id].stats[tBLK].ToString();
                txtAST1.Text = tst[id].stats[tAST].ToString();
                txtFOUL1.Text = tst[id].stats[tFOUL].ToString();
            }
            catch
            {
            }
        }

        private void mnuFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "NST Database (*.tst)|*.tst";
            sfd.InitialDirectory = AppDocsPath;
            sfd.ShowDialog();

            if (sfd.FileName == "") return;

            string file = sfd.FileName;

            File.Delete(file);
            saveAllSeasons(file);
        }

        private static void saveAllSeasons(string file)
        {
            string oldDB = currentDB;
            int oldSeason = curSeason;

            int maxSeason = getMaxSeason(oldDB);

            saveSeasonToDatabase(file, tst, tstopp, pst, curSeason, maxSeason);

            for (int i = 1; i <= maxSeason; i++)
            {
                if (i != oldSeason)
                {
                    LoadDatabase(oldDB, ref tst, ref tstopp, ref pst, ref TeamOrder, ref pt, ref bshist, _curSeason: i, doNotLoadBoxScores: true);
                    saveSeasonToDatabase(file, tst, tstopp, pst, curSeason, maxSeason, doNotSaveBoxScores: true);
                }
            }
            LoadDatabase(file, ref tst, ref tstopp, ref pst, ref TeamOrder, ref pt, ref bshist, true, oldSeason, doNotLoadBoxScores: true);

            mwInstance.updateStatus("All seasons saved successfully.");
        }

        public static void saveSeasonToDatabase(string file, TeamStats[] tstToSave, TeamStats[] tstoppToSave,
                                                Dictionary<int, PlayerStats> pstToSave,
                                                int season, int maxSeason, bool doNotSaveBoxScores = false)
        {
            // Delete the file and create it from scratch. If partial updating is implemented later, maybe
            // we won't delete the file before all this.
            //File.Delete(file); 

            // Isn't really needed since we delete the file, but is left for partial updating efforts later.
            bool FileExists = File.Exists(file);

            // SQLite
            //try
            //{
                db = new SQLiteDatabase(file);
                if (!FileExists) prepareNewDB(db, season, maxSeason);
                DataTable res;

                string teamsT = "Teams";
                string pl_teamsT = "PlayoffTeams";
                string oppT = "Opponents";
                string pl_oppT = "PlayoffOpponents";

                if (season != maxSeason)
                {
                    teamsT += "S" + season;
                    pl_teamsT += "S" + season;
                    oppT += "S" + season;
                    pl_oppT += "S" + season;
                }

                db.ClearTable(teamsT);
                db.ClearTable(pl_teamsT);
                db.ClearTable(oppT);
                db.ClearTable(pl_oppT);

                String q = "select Name from " + teamsT + ";";
                res = db.GetDataTable(q);

                foreach (TeamStats ts in tstToSave)
                {
                    bool found = false;

                    var dict = new Dictionary<string, string>();
                    dict.Add("Name", ts.name);
                    dict.Add("ID", TeamOrder[ts.name].ToString());
                    dict.Add("WIN", ts.winloss[0].ToString());
                    dict.Add("LOSS", ts.winloss[1].ToString());
                    dict.Add("MINS", ts.stats[tMINS].ToString());
                    dict.Add("PF", ts.stats[tPF].ToString());
                    dict.Add("PA", ts.stats[tPA].ToString());
                    dict.Add("FGM", ts.stats[tFGM].ToString());
                    dict.Add("FGA", ts.stats[tFGA].ToString());
                    dict.Add("TPM", ts.stats[tTPM].ToString());
                    dict.Add("TPA", ts.stats[tTPA].ToString());
                    dict.Add("FTM", ts.stats[tFTM].ToString());
                    dict.Add("FTA", ts.stats[tFTA].ToString());
                    dict.Add("OREB", ts.stats[tOREB].ToString());
                    dict.Add("DREB", ts.stats[tDREB].ToString());
                    dict.Add("STL", ts.stats[tSTL].ToString());
                    dict.Add("TOS", ts.stats[tTO].ToString());
                    dict.Add("BLK", ts.stats[tBLK].ToString());
                    dict.Add("AST", ts.stats[tAST].ToString());
                    dict.Add("FOUL", ts.stats[tFOUL].ToString());
                    dict.Add("OFFSET", ts.offset.ToString());

                    var pl_dict = new Dictionary<string, string>();
                    pl_dict.Add("Name", ts.name);
                    pl_dict.Add("ID", TeamOrder[ts.name].ToString());
                    pl_dict.Add("WIN", ts.pl_winloss[0].ToString());
                    pl_dict.Add("LOSS", ts.pl_winloss[1].ToString());
                    pl_dict.Add("MINS", ts.pl_stats[tMINS].ToString());
                    pl_dict.Add("PF", ts.pl_stats[tPF].ToString());
                    pl_dict.Add("PA", ts.pl_stats[tPA].ToString());
                    pl_dict.Add("FGM", ts.pl_stats[tFGM].ToString());
                    pl_dict.Add("FGA", ts.pl_stats[tFGA].ToString());
                    pl_dict.Add("TPM", ts.pl_stats[tTPM].ToString());
                    pl_dict.Add("TPA", ts.pl_stats[tTPA].ToString());
                    pl_dict.Add("FTM", ts.pl_stats[tFTM].ToString());
                    pl_dict.Add("FTA", ts.pl_stats[tFTA].ToString());
                    pl_dict.Add("OREB", ts.pl_stats[tOREB].ToString());
                    pl_dict.Add("DREB", ts.pl_stats[tDREB].ToString());
                    pl_dict.Add("STL", ts.pl_stats[tSTL].ToString());
                    pl_dict.Add("TOS", ts.pl_stats[tTO].ToString());
                    pl_dict.Add("BLK", ts.pl_stats[tBLK].ToString());
                    pl_dict.Add("AST", ts.pl_stats[tAST].ToString());
                    pl_dict.Add("FOUL", ts.pl_stats[tFOUL].ToString());
                    pl_dict.Add("OFFSET", ts.pl_offset.ToString());

                    foreach (DataRow r in res.Rows)
                    {
                        if (r[0].ToString().Equals(ts.name))
                        {
                            db.Update(teamsT, dict, "Name LIKE \'" + ts.name + "\'");
                            db.Update(pl_teamsT, pl_dict, "Name LIKE \'" + ts.name + "\'");
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        db.Insert(teamsT, dict);
                        db.Insert(pl_teamsT, pl_dict);
                    }
                }

                foreach (TeamStats ts in tstoppToSave)
                {
                    bool found = false;

                    var dict = new Dictionary<string, string>();
                    dict.Add("Name", ts.name);
                    dict.Add("ID", TeamOrder[ts.name].ToString());
                    dict.Add("WIN", ts.winloss[0].ToString());
                    dict.Add("LOSS", ts.winloss[1].ToString());
                    dict.Add("MINS", ts.stats[tMINS].ToString());
                    dict.Add("PF", ts.stats[tPF].ToString());
                    dict.Add("PA", ts.stats[tPA].ToString());
                    dict.Add("FGM", ts.stats[tFGM].ToString());
                    dict.Add("FGA", ts.stats[tFGA].ToString());
                    dict.Add("TPM", ts.stats[tTPM].ToString());
                    dict.Add("TPA", ts.stats[tTPA].ToString());
                    dict.Add("FTM", ts.stats[tFTM].ToString());
                    dict.Add("FTA", ts.stats[tFTA].ToString());
                    dict.Add("OREB", ts.stats[tOREB].ToString());
                    dict.Add("DREB", ts.stats[tDREB].ToString());
                    dict.Add("STL", ts.stats[tSTL].ToString());
                    dict.Add("TOS", ts.stats[tTO].ToString());
                    dict.Add("BLK", ts.stats[tBLK].ToString());
                    dict.Add("AST", ts.stats[tAST].ToString());
                    dict.Add("FOUL", ts.stats[tFOUL].ToString());
                    dict.Add("OFFSET", ts.offset.ToString());

                    var pl_dict = new Dictionary<string, string>();
                    pl_dict.Add("Name", ts.name);
                    pl_dict.Add("ID", TeamOrder[ts.name].ToString());
                    pl_dict.Add("WIN", ts.pl_winloss[0].ToString());
                    pl_dict.Add("LOSS", ts.pl_winloss[1].ToString());
                    pl_dict.Add("MINS", ts.pl_stats[tMINS].ToString());
                    pl_dict.Add("PF", ts.pl_stats[tPF].ToString());
                    pl_dict.Add("PA", ts.pl_stats[tPA].ToString());
                    pl_dict.Add("FGM", ts.pl_stats[tFGM].ToString());
                    pl_dict.Add("FGA", ts.pl_stats[tFGA].ToString());
                    pl_dict.Add("TPM", ts.pl_stats[tTPM].ToString());
                    pl_dict.Add("TPA", ts.pl_stats[tTPA].ToString());
                    pl_dict.Add("FTM", ts.pl_stats[tFTM].ToString());
                    pl_dict.Add("FTA", ts.pl_stats[tFTA].ToString());
                    pl_dict.Add("OREB", ts.pl_stats[tOREB].ToString());
                    pl_dict.Add("DREB", ts.pl_stats[tDREB].ToString());
                    pl_dict.Add("STL", ts.pl_stats[tSTL].ToString());
                    pl_dict.Add("TOS", ts.pl_stats[tTO].ToString());
                    pl_dict.Add("BLK", ts.pl_stats[tBLK].ToString());
                    pl_dict.Add("AST", ts.pl_stats[tAST].ToString());
                    pl_dict.Add("FOUL", ts.pl_stats[tFOUL].ToString());
                    pl_dict.Add("OFFSET", ts.pl_offset.ToString());

                    foreach (DataRow r in res.Rows)
                    {
                        if (r[0].ToString().Equals(ts.name))
                        {
                            db.Update(oppT, dict, "Name LIKE \'" + ts.name + "\'");
                            db.Update(pl_oppT, pl_dict, "Name LIKE \'" + ts.name + "\'");
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        db.Insert(oppT, dict);
                        db.Insert(pl_oppT, pl_dict);
                    }
                }

                #region Save Player Stats

                savePlayersToDatabase(file, pstToSave, season, maxSeason);

                #endregion

                #region Save Box Scores

                if (!doNotSaveBoxScores)
                {
                    q = "select GameID from GameResults;";
                    res = db.GetDataTable(q);
                    var idList = new List<int>();
                    foreach (DataRow r in res.Rows)
                    {
                        idList.Add(Convert.ToInt32(r[0].ToString()));
                    }

                    foreach (BoxScoreEntry bse in bshist)
                    {
                        string md5 = Tools.GetMD5(DateTime.Now.ToString(CultureInfo.InvariantCulture));
                        if ((!FileExists) || (bse.bs.id == -1) || (!idList.Contains(bse.bs.id)) || (bse.mustUpdate))
                        {
                            var dict2 = new Dictionary<string, string>();

                            dict2.Add("T1Name", bse.bs.Team1);
                            dict2.Add("T2Name", bse.bs.Team2);
                            dict2.Add("Date", String.Format("{0:yyyy-MM-dd HH:mm:ss}", bse.bs.gamedate));
                            dict2.Add("SeasonNum", bse.bs.SeasonNum.ToString());
                            dict2.Add("IsPlayoff", bse.bs.isPlayoff.ToString());
                            dict2.Add("T1PTS", bse.bs.PTS1.ToString());
                            dict2.Add("T1REB", bse.bs.REB1.ToString());
                            dict2.Add("T1AST", bse.bs.AST1.ToString());
                            dict2.Add("T1STL", bse.bs.STL1.ToString());
                            dict2.Add("T1BLK", bse.bs.BLK1.ToString());
                            dict2.Add("T1TOS", bse.bs.TO1.ToString());
                            dict2.Add("T1FGM", bse.bs.FGM1.ToString());
                            dict2.Add("T1FGA", bse.bs.FGA1.ToString());
                            dict2.Add("T13PM", bse.bs.TPM1.ToString());
                            dict2.Add("T13PA", bse.bs.TPA1.ToString());
                            dict2.Add("T1FTM", bse.bs.FTM1.ToString());
                            dict2.Add("T1FTA", bse.bs.FTA1.ToString());
                            dict2.Add("T1OREB", bse.bs.OFF1.ToString());
                            dict2.Add("T1FOUL", bse.bs.PF1.ToString());
                            dict2.Add("T1MINS", bse.bs.MINS1.ToString());
                            dict2.Add("T2PTS", bse.bs.PTS2.ToString());
                            dict2.Add("T2REB", bse.bs.REB2.ToString());
                            dict2.Add("T2AST", bse.bs.AST2.ToString());
                            dict2.Add("T2STL", bse.bs.STL2.ToString());
                            dict2.Add("T2BLK", bse.bs.BLK2.ToString());
                            dict2.Add("T2TOS", bse.bs.TO2.ToString());
                            dict2.Add("T2FGM", bse.bs.FGM2.ToString());
                            dict2.Add("T2FGA", bse.bs.FGA2.ToString());
                            dict2.Add("T23PM", bse.bs.TPM2.ToString());
                            dict2.Add("T23PA", bse.bs.TPA2.ToString());
                            dict2.Add("T2FTM", bse.bs.FTM2.ToString());
                            dict2.Add("T2FTA", bse.bs.FTA2.ToString());
                            dict2.Add("T2OREB", bse.bs.OFF2.ToString());
                            dict2.Add("T2FOUL", bse.bs.PF2.ToString());
                            dict2.Add("T2MINS", bse.bs.MINS2.ToString());
                            dict2.Add("HASH", md5);

                            if (idList.Contains(bse.bs.id))
                            {
                                db.Update("GameResults", dict2, "GameID = " + bse.bs.id);
                            }
                            else
                            {
                                db.Insert("GameResults", dict2);

                                int lastid =
                                    Convert.ToInt32(
                                        db.GetDataTable("select GameID from GameResults where HASH LIKE '" + md5 + "'").
                                            Rows
                                            [0][
                                                "GameID"].ToString());
                                bse.bs.id = lastid;
                            }
                        }
                        db.Delete("PlayerResults", "GameID = " + bse.bs.id.ToString());
                        foreach (PlayerBoxScore pbs in bse.pbsList)
                        {
                            var dict2 = new Dictionary<string, string>();
                            dict2.Add("GameID", bse.bs.id.ToString());
                            dict2.Add("PlayerID", pbs.PlayerID.ToString());
                            dict2.Add("Team", pbs.Team);
                            dict2.Add("isStarter", pbs.isStarter.ToString());
                            dict2.Add("playedInjured", pbs.playedInjured.ToString());
                            dict2.Add("isOut", pbs.isOut.ToString());
                            dict2.Add("MINS", pbs.MINS.ToString());
                            dict2.Add("PTS", pbs.PTS.ToString());
                            dict2.Add("REB", pbs.REB.ToString());
                            dict2.Add("AST", pbs.AST.ToString());
                            dict2.Add("STL", pbs.STL.ToString());
                            dict2.Add("BLK", pbs.BLK.ToString());
                            dict2.Add("TOS", pbs.TOS.ToString());
                            dict2.Add("FGM", pbs.FGM.ToString());
                            dict2.Add("FGA", pbs.FGA.ToString());
                            dict2.Add("TPM", pbs.TPM.ToString());
                            dict2.Add("TPA", pbs.TPA.ToString());
                            dict2.Add("FTM", pbs.FTM.ToString());
                            dict2.Add("FTA", pbs.FTA.ToString());
                            dict2.Add("OREB", pbs.OREB.ToString());
                            dict2.Add("FOUL", pbs.FOUL.ToString());

                            db.Insert("PlayerResults", dict2);
                        }
                    }
                }

                #endregion
                
                mwInstance.txtFile.Text = file;
                currentDB = file;
                isCustom = true;
                mwInstance.updateStatus("File saved successfully. Season " + season.ToString() + " updated.");
            //}
            //catch (Exception ex)
            //{
                //App.errorReport(ex, "Trying to save team stats - SQLite");
            //}
        }

        public static void savePlayersToDatabase(string file, Dictionary<int, PlayerStats> playerStats, int season,
                                                 int maxSeason)
        {
            var _db = new SQLiteDatabase(file);

            string playersT = "Players";

            if (season != maxSeason)
            {
                playersT += "S" + season.ToString();
            }

            string q = "select ID from " + playersT + ";";
            DataTable res = db.GetDataTable(q);

            var idList = new List<int>();
            foreach (DataRow dr in res.Rows)
            {
                idList.Add(Convert.ToInt32(dr["ID"].ToString()));
            }

            foreach (var kvp in playerStats)
            {
                PlayerStats ps = kvp.Value;
                var dict = new Dictionary<string, string>();
                dict.Add("ID", ps.ID.ToString());
                dict.Add("LastName", ps.LastName);
                dict.Add("FirstName", ps.FirstName);
                dict.Add("Position1", ps.Position1);
                dict.Add("Position2", ps.Position2);
                dict.Add("isActive", ps.isActive.ToString());
                dict.Add("isInjured", ps.isInjured.ToString());
                dict.Add("TeamFin", ps.TeamF);
                dict.Add("TeamSta", ps.TeamS);
                dict.Add("GP", ps.stats[pGP].ToString());
                dict.Add("GS", ps.stats[pGS].ToString());
                dict.Add("MINS", ps.stats[pMINS].ToString());
                dict.Add("PTS", ps.stats[pPTS].ToString());
                dict.Add("FGM", ps.stats[pFGM].ToString());
                dict.Add("FGA", ps.stats[pFGA].ToString());
                dict.Add("TPM", ps.stats[pTPM].ToString());
                dict.Add("TPA", ps.stats[pTPA].ToString());
                dict.Add("FTM", ps.stats[pFTM].ToString());
                dict.Add("FTA", ps.stats[pFTA].ToString());
                dict.Add("OREB", ps.stats[pOREB].ToString());
                dict.Add("DREB", ps.stats[pDREB].ToString());
                dict.Add("STL", ps.stats[pSTL].ToString());
                dict.Add("TOS", ps.stats[pTO].ToString());
                dict.Add("BLK", ps.stats[pBLK].ToString());
                dict.Add("AST", ps.stats[pAST].ToString());
                dict.Add("FOUL", ps.stats[pFOUL].ToString());
                dict.Add("isAllStar", ps.isAllStar.ToString());
                dict.Add("isNBAChampion", ps.isNBAChampion.ToString());

                if (idList.Contains(ps.ID))
                {
                    dict.Remove("ID");
                    _db.Update(playersT, dict, "ID = " + ps.ID.ToString());
                }
                else
                {
                    _db.Insert(playersT, dict);
                }
            }
        }

        private static void prepareNewDB(SQLiteDatabase sqldb, int curSeason, int maxSeason, bool onlyNewSeason = false)
        {
            try
            {
                String qr;

                if (!onlyNewSeason)
                {
                    qr = "DROP TABLE IF EXISTS \"GameResults\"";
                    sqldb.ExecuteNonQuery(qr);
                    qr =
                        "CREATE TABLE \"GameResults\" (\"GameID\" INTEGER PRIMARY KEY  NOT NULL ,\"T1Name\" TEXT NOT NULL ,\"T2Name\" TEXT NOT NULL ,\"Date\" DATE NOT NULL ,\"SeasonNum\" INTEGER NOT NULL ,\"IsPlayoff\" TEXT NOT NULL  DEFAULT ('FALSE') ,\"T1PTS\" INTEGER NOT NULL ,\"T1REB\" INTEGER NOT NULL ,\"T1AST\" INTEGER NOT NULL ,\"T1STL\" INTEGER NOT NULL ,\"T1BLK\" INTEGER NOT NULL ,\"T1TOS\" INTEGER NOT NULL ,\"T1FGM\" INTEGER NOT NULL ,\"T1FGA\" INTEGER NOT NULL ,\"T13PM\" INTEGER NOT NULL ,\"T13PA\" INTEGER NOT NULL ,\"T1FTM\" INTEGER NOT NULL ,\"T1FTA\" INTEGER NOT NULL ,\"T1OREB\" INTEGER NOT NULL ,\"T1FOUL\" INTEGER NOT NULL,\"T1MINS\" INTEGER NOT NULL ,\"T2PTS\" INTEGER NOT NULL ,\"T2REB\" INTEGER NOT NULL ,\"T2AST\" INTEGER NOT NULL ,\"T2STL\" INTEGER NOT NULL ,\"T2BLK\" INTEGER NOT NULL ,\"T2TOS\" INTEGER NOT NULL ,\"T2FGM\" INTEGER NOT NULL ,\"T2FGA\" INTEGER NOT NULL ,\"T23PM\" INTEGER NOT NULL ,\"T23PA\" INTEGER NOT NULL ,\"T2FTM\" INTEGER NOT NULL ,\"T2FTA\" INTEGER NOT NULL ,\"T2OREB\" INTEGER NOT NULL ,\"T2FOUL\" INTEGER NOT NULL,\"T2MINS\" INTEGER NOT NULL, \"HASH\" TEXT )";
                    sqldb.ExecuteNonQuery(qr);
                    qr = "DROP TABLE IF EXISTS \"PlayerResults\"";
                    sqldb.ExecuteNonQuery(qr);
                    qr =
                        "CREATE TABLE \"PlayerResults\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"GameID\" INTEGER NOT NULL ,\"PlayerID\" INTEGER NOT NULL ,\"Team\" TEXT NOT NULL ,\"isStarter\" TEXT, \"playedInjured\" TEXT, \"isOut\" TEXT, \"MINS\" INTEGER NOT NULL  DEFAULT (0), \"PTS\" INTEGER NOT NULL ,\"REB\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL  DEFAULT (0) )";
                    sqldb.ExecuteNonQuery(qr);
                    qr = "DROP TABLE IF EXISTS \"Misc\"";
                    sqldb.ExecuteNonQuery(qr);
                    qr = "CREATE TABLE \"Misc\" (\"CurSeason\" INTEGER);";
                    sqldb.ExecuteNonQuery(qr);
                }
                string teamsT = "Teams";
                string pl_teamsT = "PlayoffTeams";
                string oppT = "Opponents";
                string pl_oppT = "PlayoffOpponents";
                string playersT = "Players";
                if (curSeason != maxSeason)
                {
                    string s = "S" + curSeason.ToString();
                    teamsT += s;
                    pl_teamsT += s;
                    oppT += s;
                    pl_oppT += s;
                    playersT += s;
                }
                qr = "DROP TABLE IF EXISTS \"" + pl_teamsT + "\"";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"" + pl_teamsT +
                     "\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"Name\" TEXT NOT NULL ,\"WIN\" INTEGER NOT NULL ,\"LOSS\" INTEGER NOT NULL ,\"MINS\" INTEGER, \"PF\" INTEGER NOT NULL ,\"PA\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"OFFSET\" INTEGER)";
                sqldb.ExecuteNonQuery(qr);

                qr = "DROP TABLE IF EXISTS \"" + teamsT + "\"";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"" + teamsT +
                     "\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"Name\" TEXT NOT NULL ,\"WIN\" INTEGER NOT NULL ,\"LOSS\" INTEGER NOT NULL ,\"MINS\" INTEGER, \"PF\" INTEGER NOT NULL ,\"PA\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"OFFSET\" INTEGER)";
                sqldb.ExecuteNonQuery(qr);

                qr = "DROP TABLE IF EXISTS \"" + pl_oppT + "\"";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"" + pl_oppT +
                     "\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"Name\" TEXT NOT NULL ,\"WIN\" INTEGER NOT NULL ,\"LOSS\" INTEGER NOT NULL ,\"MINS\" INTEGER, \"PF\" INTEGER NOT NULL ,\"PA\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"OFFSET\" INTEGER)";
                sqldb.ExecuteNonQuery(qr);

                qr = "DROP TABLE IF EXISTS \"" + oppT + "\"";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"" + oppT +
                     "\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"Name\" TEXT NOT NULL ,\"WIN\" INTEGER NOT NULL ,\"LOSS\" INTEGER NOT NULL ,\"MINS\" INTEGER, \"PF\" INTEGER NOT NULL ,\"PA\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"OFFSET\" INTEGER)";
                sqldb.ExecuteNonQuery(qr);

                qr = "DROP TABLE IF EXISTS \"" + playersT + "\"";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"" + playersT +
                     "\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"LastName\" TEXT NOT NULL ,\"FirstName\" TEXT NOT NULL ,\"Position1\" TEXT,\"Position2\" TEXT,\"isActive\" TEXT,\"isInjured\" TEXT,\"TeamFin\" TEXT,\"TeamSta\" TEXT,\"GP\" INTEGER,\"GS\" INTEGER,\"MINS\" INTEGER NOT NULL  DEFAULT (0) ,\"PTS\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"isAllStar\" TEXT,\"isNBAChampion\" TEXT)";
                sqldb.ExecuteNonQuery(qr);
            }
            catch
            {
            }
        }

        private static void writeBoxScoreHistory(BinaryWriter stream)
        {
            stream.Write("BOXSCOREHISTORY_START");
            stream.Write(bshist.Count);
            for (int i = 0; i < bshist.Count; i++)
            {
                stream.Write("BOXSCORE_START");
                stream.Write(bshist[i].bs.Team1);
                stream.Write(bshist[i].bs.PTS1);
                stream.Write(bshist[i].bs.REB1);
                stream.Write(bshist[i].bs.AST1);
                stream.Write(bshist[i].bs.STL1);
                stream.Write(bshist[i].bs.BLK1);
                stream.Write(bshist[i].bs.TO1);
                stream.Write(bshist[i].bs.FGM1);
                stream.Write(bshist[i].bs.FGA1);
                stream.Write(bshist[i].bs.TPM1);
                stream.Write(bshist[i].bs.TPA1);
                stream.Write(bshist[i].bs.FTM1);
                stream.Write(bshist[i].bs.FTA1);
                stream.Write(bshist[i].bs.OFF1);
                stream.Write(bshist[i].bs.PF1);
                stream.Write(bshist[i].bs.Team2);
                stream.Write(bshist[i].bs.PTS2);
                stream.Write(bshist[i].bs.REB2);
                stream.Write(bshist[i].bs.AST2);
                stream.Write(bshist[i].bs.STL2);
                stream.Write(bshist[i].bs.BLK2);
                stream.Write(bshist[i].bs.TO2);
                stream.Write(bshist[i].bs.FGM2);
                stream.Write(bshist[i].bs.FGA2);
                stream.Write(bshist[i].bs.TPM2);
                stream.Write(bshist[i].bs.TPA2);
                stream.Write(bshist[i].bs.FTM2);
                stream.Write(bshist[i].bs.FTA2);
                stream.Write(bshist[i].bs.OFF2);
                stream.Write(bshist[i].bs.PF2);
                stream.Write(bshist[i].date.Year);
                stream.Write(bshist[i].date.Month);
                stream.Write(bshist[i].date.Day);
                stream.Write(bshist[i].date.Hour);
                stream.Write(bshist[i].date.Minute);
                stream.Write(bshist[i].date.Second);
                stream.Write("BOXSCORE_END");
            }
            stream.Write("BOXSCOREHISTORY_END");
        }

        private void mnuFileOpenCustom_Click(object sender, RoutedEventArgs e)
        {
            tst = new TeamStats[30];
            TeamOrder = new SortedDictionary<string, int>();
            bshist = new List<BoxScoreEntry>();

            var ofd = new OpenFileDialog();
            ofd.Filter = "NST Database (*.tst)|*.tst";
            ofd.InitialDirectory = AppDocsPath;
            ofd.Title = "Please select the TST file that you want to edit...";
            ofd.ShowDialog();

            if (ofd.FileName == "") return;

            LoadDatabase(ofd.FileName, ref tst, ref tstopp, ref pst, ref TeamOrder, ref pt, ref bshist);
            //tst = getCustomStats("", ref TeamOrder, ref pt, ref bshist);

            cmbTeam1.SelectedIndex = -1;
            cmbTeam1.SelectedIndex = 0;
            txtFile.Text = ofd.FileName;

            updateStatus(tst.GetLength(0).ToString() + " teams loaded successfully");
            currentDB = txtFile.Text;
            //txtFile.Text = "SQLite";

            //MessageBox.Show(bshist.Count.ToString());
        }

        public static int getMaxSeason(string file)
        {
            SQLiteDatabase _db = new SQLiteDatabase(file);

            DataTable res;

            String q;
            q = "select Name from sqlite_master";
            res = _db.GetDataTable(q);

            int maxseason = 0;

            foreach (DataRow r in res.Rows)
            {
                string name = r["Name"].ToString();
                if (name.Length > 5 && name.Substring(0, 5) == "Teams")
                {
                    int season = Convert.ToInt32(name.Substring(6, 1));
                    if (season > maxseason)
                    {
                        maxseason = season;
                    }
                }
            }

            maxseason++;

            return maxseason;
        }

        public static void GetTeamStatsFromDatabase(string file, string team, int season, ref TeamStats ts, ref TeamStats tsopp)
        {
            SQLiteDatabase _db = new SQLiteDatabase(file);

            DataTable res;

            String q;
            int maxSeason = getMaxSeason(file);

            if (season == 0) season = maxSeason;

            if (maxSeason == season)
            {
                q = "select * from Teams where Name LIKE '" + team + "'";
            }
            else
            {
                q = "select * from TeamsS" + season.ToString() + " where Name LIKE '" + team + "'";
            }

            res = _db.GetDataTable(q);

            ts = new TeamStats();

            foreach (DataRow r in res.Rows)
            {
                ts = new TeamStats();
                ts.name = r["Name"].ToString();
                ts.offset = Convert.ToInt32(r["OFFSET"].ToString());
                ts.winloss[0] = Convert.ToByte(r["WIN"].ToString());
                ts.winloss[1] = Convert.ToByte(r["LOSS"].ToString());
                ts.stats[tMINS] = Convert.ToUInt16(r["MINS"].ToString());
                ts.stats[tPF] = Convert.ToUInt16(r["PF"].ToString());
                ts.stats[tPA] = Convert.ToUInt16(r["PA"].ToString());
                ts.stats[tFGM] = Convert.ToUInt16(r["FGM"].ToString());
                ts.stats[tFGA] = Convert.ToUInt16(r["FGA"].ToString());
                ts.stats[tTPM] = Convert.ToUInt16(r["TPM"].ToString());
                ts.stats[tTPA] = Convert.ToUInt16(r["TPA"].ToString());
                ts.stats[tFTM] = Convert.ToUInt16(r["FTM"].ToString());
                ts.stats[tFTA] = Convert.ToUInt16(r["FTA"].ToString());
                ts.stats[tOREB] = Convert.ToUInt16(r["OREB"].ToString());
                ts.stats[tDREB] = Convert.ToUInt16(r["DREB"].ToString());
                ts.stats[tSTL] = Convert.ToUInt16(r["STL"].ToString());
                ts.stats[tTO] = Convert.ToUInt16(r["TOS"].ToString());
                ts.stats[tBLK] = Convert.ToUInt16(r["BLK"].ToString());
                ts.stats[tAST] = Convert.ToUInt16(r["AST"].ToString());
                ts.stats[tFOUL] = Convert.ToUInt16(r["FOUL"].ToString());
            }

            if (maxSeason == season)
            {
                q = "select * from PlayoffTeams;";
            }
            else
            {
                q = "select * from PlayoffTeamsS" + season.ToString() + ";";
            }
            res = _db.GetDataTable(q);

            foreach (DataRow r in res.Rows)
            {
                ts.pl_offset = Convert.ToInt32(r["OFFSET"].ToString());
                ts.pl_winloss[0] = Convert.ToByte(r["WIN"].ToString());
                ts.pl_winloss[1] = Convert.ToByte(r["LOSS"].ToString());
                ts.pl_stats[tMINS] = Convert.ToUInt16(r["MINS"].ToString());
                ts.pl_stats[tPF] = Convert.ToUInt16(r["PF"].ToString());
                ts.pl_stats[tPA] = Convert.ToUInt16(r["PA"].ToString());
                ts.pl_stats[tFGM] = Convert.ToUInt16(r["FGM"].ToString());
                ts.pl_stats[tFGA] = Convert.ToUInt16(r["FGA"].ToString());
                ts.pl_stats[tTPM] = Convert.ToUInt16(r["TPM"].ToString());
                ts.pl_stats[tTPA] = Convert.ToUInt16(r["TPA"].ToString());
                ts.pl_stats[tFTM] = Convert.ToUInt16(r["FTM"].ToString());
                ts.pl_stats[tFTA] = Convert.ToUInt16(r["FTA"].ToString());
                ts.pl_stats[tOREB] = Convert.ToUInt16(r["OREB"].ToString());
                ts.pl_stats[tDREB] = Convert.ToUInt16(r["DREB"].ToString());
                ts.pl_stats[tSTL] = Convert.ToUInt16(r["STL"].ToString());
                ts.pl_stats[tTO] = Convert.ToUInt16(r["TOS"].ToString());
                ts.pl_stats[tBLK] = Convert.ToUInt16(r["BLK"].ToString());
                ts.pl_stats[tAST] = Convert.ToUInt16(r["AST"].ToString());
                ts.pl_stats[tFOUL] = Convert.ToUInt16(r["FOUL"].ToString());

                ts.calcAvg();
            }

            if (maxSeason == season)
            {
                q = "select * from Opponents where Name LIKE '" + team + "'";
            }
            else
            {
                q = "select * from OpponentsS" + season.ToString() + " where Name LIKE '" + team + "'";
            }

            res = _db.GetDataTable(q);

            tsopp = new TeamStats();

            foreach (DataRow r in res.Rows)
            {
                tsopp = new TeamStats();
                tsopp.name = r["Name"].ToString();
                tsopp.offset = Convert.ToInt32(r["OFFSET"].ToString());
                tsopp.winloss[0] = Convert.ToByte(r["WIN"].ToString());
                tsopp.winloss[1] = Convert.ToByte(r["LOSS"].ToString());
                tsopp.stats[tMINS] = Convert.ToUInt16(r["MINS"].ToString());
                tsopp.stats[tPF] = Convert.ToUInt16(r["PF"].ToString());
                tsopp.stats[tPA] = Convert.ToUInt16(r["PA"].ToString());
                tsopp.stats[tFGM] = Convert.ToUInt16(r["FGM"].ToString());
                tsopp.stats[tFGA] = Convert.ToUInt16(r["FGA"].ToString());
                tsopp.stats[tTPM] = Convert.ToUInt16(r["TPM"].ToString());
                tsopp.stats[tTPA] = Convert.ToUInt16(r["TPA"].ToString());
                tsopp.stats[tFTM] = Convert.ToUInt16(r["FTM"].ToString());
                tsopp.stats[tFTA] = Convert.ToUInt16(r["FTA"].ToString());
                tsopp.stats[tOREB] = Convert.ToUInt16(r["OREB"].ToString());
                tsopp.stats[tDREB] = Convert.ToUInt16(r["DREB"].ToString());
                tsopp.stats[tSTL] = Convert.ToUInt16(r["STL"].ToString());
                tsopp.stats[tTO] = Convert.ToUInt16(r["TOS"].ToString());
                tsopp.stats[tBLK] = Convert.ToUInt16(r["BLK"].ToString());
                tsopp.stats[tAST] = Convert.ToUInt16(r["AST"].ToString());
                tsopp.stats[tFOUL] = Convert.ToUInt16(r["FOUL"].ToString());
            }

            if (maxSeason == season)
            {
                q = "select * from PlayoffOpponents;";
            }
            else
            {
                q = "select * from PlayoffOpponentsS" + season.ToString() + ";";
            }
            res = _db.GetDataTable(q);

            foreach (DataRow r in res.Rows)
            {
                tsopp.pl_offset = Convert.ToInt32(r["OFFSET"].ToString());
                tsopp.pl_winloss[0] = Convert.ToByte(r["WIN"].ToString());
                tsopp.pl_winloss[1] = Convert.ToByte(r["LOSS"].ToString());
                tsopp.pl_stats[tMINS] = Convert.ToUInt16(r["MINS"].ToString());
                tsopp.pl_stats[tPF] = Convert.ToUInt16(r["PF"].ToString());
                tsopp.pl_stats[tPA] = Convert.ToUInt16(r["PA"].ToString());
                tsopp.pl_stats[tFGM] = Convert.ToUInt16(r["FGM"].ToString());
                tsopp.pl_stats[tFGA] = Convert.ToUInt16(r["FGA"].ToString());
                tsopp.pl_stats[tTPM] = Convert.ToUInt16(r["TPM"].ToString());
                tsopp.pl_stats[tTPA] = Convert.ToUInt16(r["TPA"].ToString());
                tsopp.pl_stats[tFTM] = Convert.ToUInt16(r["FTM"].ToString());
                tsopp.pl_stats[tFTA] = Convert.ToUInt16(r["FTA"].ToString());
                tsopp.pl_stats[tOREB] = Convert.ToUInt16(r["OREB"].ToString());
                tsopp.pl_stats[tDREB] = Convert.ToUInt16(r["DREB"].ToString());
                tsopp.pl_stats[tSTL] = Convert.ToUInt16(r["STL"].ToString());
                tsopp.pl_stats[tTO] = Convert.ToUInt16(r["TOS"].ToString());
                tsopp.pl_stats[tBLK] = Convert.ToUInt16(r["BLK"].ToString());
                tsopp.pl_stats[tAST] = Convert.ToUInt16(r["AST"].ToString());
                tsopp.pl_stats[tFOUL] = Convert.ToUInt16(r["FOUL"].ToString());

                tsopp.calcAvg();
            }
        }

        public static void GetAllTeamStatsFromDatabase(string file, int season, ref TeamStats[] _tst, ref TeamStats[] _tstopp, ref SortedDictionary<string,int> TeamOrder)
        {
            SQLiteDatabase _db = new SQLiteDatabase(file);

            DataTable res;

            String q;
            int maxSeason = getMaxSeason(file);

            if (season == 0) season = maxSeason;

            if (maxSeason == season)
            {
                q = "select Name from Teams;";
            }
            else
            {
                q = "select Name from TeamsS" + season.ToString() + ";";
            }

            res = _db.GetDataTable(q);

            _tst = new TeamStats[res.Rows.Count];
            _tstopp = new TeamStats[res.Rows.Count];
            TeamOrder = new SortedDictionary<string, int>();
            int i = 0;

            foreach (DataRow r in res.Rows)
            {
                string name = r["Name"].ToString();
                _tst[i] = new TeamStats(name);
                _tstopp[i] = new TeamStats(name);
                GetTeamStatsFromDatabase(file, name, curSeason, ref _tst[i], ref _tstopp[i]);
                TeamOrder.Add(name, i);
                i++;
            }
        }

        public static void LoadDatabase(string file, ref TeamStats[] _tst, ref TeamStats[] _tstopp, ref Dictionary<int, PlayerStats> pst,
                                               ref SortedDictionary<string, int> _TeamOrder, ref PlayoffTree _pt,
                                               ref IList<BoxScoreEntry> _bshist, bool updateCombo = true,
                                               int _curSeason = 0, bool doNotLoadBoxScores = false)
        {
            SQLiteDatabase _db = new SQLiteDatabase(file);

            DataTable res;

            String q;
            int maxSeason = getMaxSeason(file);

            if (_curSeason == 0) _curSeason = maxSeason;

            if (maxSeason == _curSeason)
            {
                q = "select Name from Teams;";
            }
            else
            {
                q = "select Name from TeamsS" + _curSeason.ToString() + ";";
            }

            res = _db.GetDataTable(q);

            _tst = new TeamStats[res.Rows.Count];
            _tstopp = new TeamStats[res.Rows.Count];
            _TeamOrder = new SortedDictionary<string, int>();

            GetAllTeamStatsFromDatabase(file, _curSeason, ref _tst, ref _tstopp, ref _TeamOrder);

            pst = GetPlayersFromDatabase(file, _tst, _tstopp, _curSeason, maxSeason);

            if (!doNotLoadBoxScores) _bshist = GetBoxScoresFromDatabase(file);

            /*
            try
            {
                q = "select CurSeason from Misc limit 1;";
                res = _db.GetDataTable(q);
                curSeason = Convert.ToInt32(res.Rows[0]["CurSeason"].ToString());
            }
            catch
            {
                curSeason = 1;
            }
            */
            curSeason = _curSeason;
            mwInstance.txbCurSeason.Text = "Current Season: " + _curSeason.ToString() + "/" + maxSeason.ToString();

            /*
            if (updateCombo)
            {
                mwInstance.cmbTeam1.Items.Clear();
                foreach (KeyValuePair<string, int> kvp in _TeamOrder)
                {
                    mwInstance.cmbTeam1.Items.Add(kvp.Key);
                }
            }
            */
        }

        public static IList<BoxScoreEntry> GetBoxScoresFromDatabase(string file)
        {
            SQLiteDatabase _db = new SQLiteDatabase(file);

            IList<BoxScoreEntry> _bshist;
            string q;
            q = "select * from GameResults ORDER BY Date DESC;";
            DataTable res2 = _db.GetDataTable(q);

            _bshist = new List<BoxScoreEntry>(res2.Rows.Count);
            foreach (DataRow r in res2.Rows)
            {
                var bs = new BoxScore();
                bs.id = Convert.ToInt32(r["GameID"].ToString());
                bs.Team1 = r["T1Name"].ToString();
                bs.Team2 = r["T2Name"].ToString();
                bs.gamedate = Convert.ToDateTime(r["Date"].ToString());
                bs.SeasonNum = Convert.ToInt32(r["SeasonNum"].ToString());
                bs.isPlayoff = Convert.ToBoolean(r["IsPlayoff"].ToString());
                bs.PTS1 = Convert.ToUInt16(r["T1PTS"].ToString());
                bs.REB1 = Convert.ToUInt16(r["T1REB"].ToString());
                bs.AST1 = Convert.ToUInt16(r["T1AST"].ToString());
                bs.STL1 = Convert.ToUInt16(r["T1STL"].ToString());
                bs.BLK1 = Convert.ToUInt16(r["T1BLK"].ToString());
                bs.TO1 = Convert.ToUInt16(r["T1TOS"].ToString());
                bs.FGM1 = Convert.ToUInt16(r["T1FGM"].ToString());
                bs.FGA1 = Convert.ToUInt16(r["T1FGA"].ToString());
                bs.TPM1 = Convert.ToUInt16(r["T13PM"].ToString());
                bs.TPA1 = Convert.ToUInt16(r["T13PA"].ToString());
                bs.FTM1 = Convert.ToUInt16(r["T1FTM"].ToString());
                bs.FTA1 = Convert.ToUInt16(r["T1FTA"].ToString());
                bs.OFF1 = Convert.ToUInt16(r["T1OREB"].ToString());
                bs.PF1 = Convert.ToUInt16(r["T1FOUL"].ToString());
                bs.MINS1 = Convert.ToUInt16(r["T1MINS"].ToString());

                bs.PTS2 = Convert.ToUInt16(r["T2PTS"].ToString());
                bs.REB2 = Convert.ToUInt16(r["T2REB"].ToString());
                bs.AST2 = Convert.ToUInt16(r["T2AST"].ToString());
                bs.STL2 = Convert.ToUInt16(r["T2STL"].ToString());
                bs.BLK2 = Convert.ToUInt16(r["T2BLK"].ToString());
                bs.TO2 = Convert.ToUInt16(r["T2TOS"].ToString());
                bs.FGM2 = Convert.ToUInt16(r["T2FGM"].ToString());
                bs.FGA2 = Convert.ToUInt16(r["T2FGA"].ToString());
                bs.TPM2 = Convert.ToUInt16(r["T23PM"].ToString());
                bs.TPA2 = Convert.ToUInt16(r["T23PA"].ToString());
                bs.FTM2 = Convert.ToUInt16(r["T2FTM"].ToString());
                bs.FTA2 = Convert.ToUInt16(r["T2FTA"].ToString());
                bs.OFF2 = Convert.ToUInt16(r["T2OREB"].ToString());
                bs.PF2 = Convert.ToUInt16(r["T2FOUL"].ToString());
                bs.MINS2 = Convert.ToUInt16(r["T2MINS"].ToString());

                var bse = new BoxScoreEntry(bs);
                bse.date = bs.gamedate;

                string q2 = "select * from PlayerResults WHERE GameID = " + bs.id.ToString();
                DataTable res3 = _db.GetDataTable(q2);
                bse.pbsList = new List<PlayerBoxScore>(res3.Rows.Count);

                foreach (DataRow r3 in res3.Rows)
                {
                    bse.pbsList.Add(new PlayerBoxScore(r3));
                }

                _bshist.Add(bse);
            }
            return _bshist;
        }

        public static Dictionary<int, PlayerStats> GetPlayersFromDatabase(string file, TeamStats[] _tst, TeamStats[] _tstopp, int curSeason, int maxSeason)
        {
            var _pst = new Dictionary<int, PlayerStats>();
            string q;
            DataTable res;

            if (curSeason == maxSeason)
            {
                q = "select * from Players;";
            }
            else
            {
                q = "select * from PlayersS" + curSeason.ToString() + ";";
            }

            var _db = new SQLiteDatabase(file);
            res = _db.GetDataTable(q);

            foreach (DataRow r in res.Rows)
            {
                var ps = new PlayerStats(r);

                _pst.Add(ps.ID, ps);
            }

            NSTHelper.CalculateAllMetrics(ref _pst, _tst, _tstopp);

            return _pst;
        }

        private void btnLoadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (isTSTEmpty())
            {
                updateStatus("No file is loaded or the file currently loaded is empty");
                return;
            }

            bs = new BoxScore();
            var bsW = new boxScoreW();
            bsW.ShowDialog();

            if (bs.done == false) return;

            int id1 = -1;
            int id2 = -1;

            id1 = TeamOrder[bs.Team1];
            id2 = TeamOrder[bs.Team2];

            LoadDatabase(currentDB, ref tst, ref tstopp, ref pst, ref TeamOrder, ref pt, ref bshist, _curSeason: bs.SeasonNum);

            var list = new List<PlayerBoxScore>();
            foreach (var pbsList in pbsLists)
            {
                foreach (PlayerBoxScore pbs in pbsList)
                {
                    list.Add(pbs);
                }
            }

            if (!bs.doNotUpdate)
            {
                AddTeamStatsFromBoxScore(bs, ref tst[id1], ref tst[id2], ref tstopp[id1], ref tstopp[id2]);

                foreach (PlayerBoxScore pbs in list)
                {
                    if (pbs.PlayerID == -1) continue;
                    pst[pbs.PlayerID].AddBoxScore(pbs);
                }
            }

            cmbTeam1.SelectedIndex = -1;
            cmbTeam1.SelectedItem = bs.Team1;

            if (bs.bshistid == -1)
            {
                var bse = new BoxScoreEntry(bs, bs.gamedate, list);
                bshist.Add(bse);
            }
            else
            {
                bshist[bs.bshistid].bs = bs;
            }

            saveSeasonToDatabase(currentDB, tst, tstopp, pst, curSeason, getMaxSeason(currentDB));

            updateStatus(
                "One or more Box Scores have been added/updated. Database saved.");
            
        }

        public static void AddTeamStatsFromBoxScore(BoxScore bs, ref TeamStats ts1, ref TeamStats ts2)
        {
            TeamStats tsopp1 = new TeamStats();
            TeamStats tsopp2 = new TeamStats();
            AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2, ref tsopp1, ref tsopp2);
        }

        public static void AddTeamStatsFromBoxScore(BoxScore bs, ref TeamStats ts1, ref TeamStats ts2, ref TeamStats tsopp1, ref TeamStats tsopp2)
        {
            if (!bs.isPlayoff)
            {
                // Add win & loss
                if (bs.PTS1 > bs.PTS2)
                {
                    ts1.winloss[0]++;
                    ts2.winloss[1]++;
                }
                else
                {
                    ts1.winloss[1]++;
                    ts2.winloss[0]++;
                }
                // Add minutes played
                ts1.stats[tMINS] += bs.MINS1;
                ts2.stats[tMINS] += bs.MINS2;

                // Add Points For
                ts1.stats[tPF] += bs.PTS1;
                ts2.stats[tPF] += bs.PTS2;

                // Add Points Against
                ts1.stats[tPA] += bs.PTS2;
                ts2.stats[tPA] += bs.PTS1;

                //
                ts1.stats[tFGM] += bs.FGM1;
                ts2.stats[tFGM] += bs.FGM2;

                ts1.stats[tFGA] += bs.FGA1;
                ts2.stats[tFGA] += bs.FGA2;

                //
                ts1.stats[tTPM] += bs.TPM1;
                ts2.stats[tTPM] += bs.TPM2;

                //
                ts1.stats[tTPA] += bs.TPA1;
                ts2.stats[tTPA] += bs.TPA2;

                //
                ts1.stats[tFTM] += bs.FTM1;
                ts2.stats[tFTM] += bs.FTM2;

                //
                ts1.stats[tFTA] += bs.FTA1;
                ts2.stats[tFTA] += bs.FTA2;

                //
                ts1.stats[tOREB] += bs.OFF1;
                ts2.stats[tOREB] += bs.OFF2;

                //
                ts1.stats[tDREB] += Convert.ToUInt16(bs.REB1 - bs.OFF1);
                ts2.stats[tDREB] += Convert.ToUInt16(bs.REB2 - bs.OFF2);

                //
                ts1.stats[tSTL] += bs.STL1;
                ts2.stats[tSTL] += bs.STL2;

                //
                ts1.stats[tTO] += bs.TO1;
                ts2.stats[tTO] += bs.TO2;

                //
                ts1.stats[tBLK] += bs.BLK1;
                ts2.stats[tBLK] += bs.BLK2;

                //
                ts1.stats[tAST] += bs.AST1;
                ts2.stats[tAST] += bs.AST2;

                //
                ts1.stats[tFOUL] += bs.PF1;
                ts2.stats[tFOUL] += bs.PF2;


                // Opponents Team Stats
                // Add win & loss
                if (bs.PTS1 > bs.PTS2)
                {
                    tsopp2.winloss[0]++;
                    tsopp1.winloss[1]++;
                }
                else
                {
                    tsopp2.winloss[1]++;
                    tsopp1.winloss[0]++;
                }
                // Add minutes played
                tsopp2.stats[tMINS] += bs.MINS1;
                tsopp1.stats[tMINS] += bs.MINS2;

                // Add Points For
                tsopp2.stats[tPF] += bs.PTS1;
                tsopp1.stats[tPF] += bs.PTS2;

                // Add Points Against
                tsopp2.stats[tPA] += bs.PTS2;
                tsopp1.stats[tPA] += bs.PTS1;

                //
                tsopp2.stats[tFGM] += bs.FGM1;
                tsopp1.stats[tFGM] += bs.FGM2;

                tsopp2.stats[tFGA] += bs.FGA1;
                tsopp1.stats[tFGA] += bs.FGA2;

                //
                tsopp2.stats[tTPM] += bs.TPM1;
                tsopp1.stats[tTPM] += bs.TPM2;

                //
                tsopp2.stats[tTPA] += bs.TPA1;
                tsopp1.stats[tTPA] += bs.TPA2;

                //
                tsopp2.stats[tFTM] += bs.FTM1;
                tsopp1.stats[tFTM] += bs.FTM2;

                //
                tsopp2.stats[tFTA] += bs.FTA1;
                tsopp1.stats[tFTA] += bs.FTA2;

                //
                tsopp2.stats[tOREB] += bs.OFF1;
                tsopp1.stats[tOREB] += bs.OFF2;

                //
                tsopp2.stats[tDREB] += Convert.ToUInt16(bs.REB1 - bs.OFF1);
                tsopp1.stats[tDREB] += Convert.ToUInt16(bs.REB2 - bs.OFF2);

                //
                tsopp2.stats[tSTL] += bs.STL1;
                tsopp1.stats[tSTL] += bs.STL2;

                //
                tsopp2.stats[tTO] += bs.TO1;
                tsopp1.stats[tTO] += bs.TO2;

                //
                tsopp2.stats[tBLK] += bs.BLK1;
                tsopp1.stats[tBLK] += bs.BLK2;

                //
                tsopp2.stats[tAST] += bs.AST1;
                tsopp1.stats[tAST] += bs.AST2;

                //
                tsopp2.stats[tFOUL] += bs.PF1;
                tsopp1.stats[tFOUL] += bs.PF2;
            }
            else
            {
                // Add win & loss
                if (bs.PTS1 > bs.PTS2)
                {
                    ts1.pl_winloss[0]++;
                    ts2.pl_winloss[1]++;
                }
                else
                {
                    ts1.pl_winloss[1]++;
                    ts2.pl_winloss[0]++;
                }
                // Add minutes played
                ts1.pl_stats[tMINS] += bs.MINS1;
                ts2.pl_stats[tMINS] += bs.MINS2;

                // Add Points For
                ts1.pl_stats[tPF] += bs.PTS1;
                ts2.pl_stats[tPF] += bs.PTS2;

                // Add Points Against
                ts1.pl_stats[tPA] += bs.PTS2;
                ts2.pl_stats[tPA] += bs.PTS1;

                //
                ts1.pl_stats[tFGM] += bs.FGM1;
                ts2.pl_stats[tFGM] += bs.FGM2;

                ts1.pl_stats[tFGA] += bs.FGA1;
                ts2.pl_stats[tFGA] += bs.FGA2;

                //
                ts1.pl_stats[tTPM] += bs.TPM1;
                ts2.pl_stats[tTPM] += bs.TPM2;

                //
                ts1.pl_stats[tTPA] += bs.TPA1;
                ts2.pl_stats[tTPA] += bs.TPA2;

                //
                ts1.pl_stats[tFTM] += bs.FTM1;
                ts2.pl_stats[tFTM] += bs.FTM2;

                //
                ts1.pl_stats[tFTA] += bs.FTA1;
                ts2.pl_stats[tFTA] += bs.FTA2;

                //
                ts1.pl_stats[tOREB] += bs.OFF1;
                ts2.pl_stats[tOREB] += bs.OFF2;

                //
                ts1.pl_stats[tDREB] += Convert.ToUInt16(bs.REB1 - bs.OFF1);
                ts2.pl_stats[tDREB] += Convert.ToUInt16(bs.REB2 - bs.OFF2);

                //
                ts1.pl_stats[tSTL] += bs.STL1;
                ts2.pl_stats[tSTL] += bs.STL2;

                //
                ts1.pl_stats[tTO] += bs.TO1;
                ts2.pl_stats[tTO] += bs.TO2;

                //
                ts1.pl_stats[tBLK] += bs.BLK1;
                ts2.pl_stats[tBLK] += bs.BLK2;

                //
                ts1.pl_stats[tAST] += bs.AST1;
                ts2.pl_stats[tAST] += bs.AST2;

                //
                ts1.pl_stats[tFOUL] += bs.PF1;
                ts2.pl_stats[tFOUL] += bs.PF2;


                // Opponents Team Stats
                // Add win & loss
                if (bs.PTS1 > bs.PTS2)
                {
                    tsopp2.pl_winloss[0]++;
                    tsopp1.pl_winloss[1]++;
                }
                else
                {
                    tsopp2.pl_winloss[1]++;
                    tsopp1.pl_winloss[0]++;
                }
                // Add minutes played
                tsopp2.pl_stats[tMINS] += bs.MINS1;
                tsopp1.pl_stats[tMINS] += bs.MINS2;

                // Add Points For
                tsopp2.pl_stats[tPF] += bs.PTS1;
                tsopp1.pl_stats[tPF] += bs.PTS2;

                // Add Points Against
                tsopp2.pl_stats[tPA] += bs.PTS2;
                tsopp1.pl_stats[tPA] += bs.PTS1;

                //
                tsopp2.pl_stats[tFGM] += bs.FGM1;
                tsopp1.pl_stats[tFGM] += bs.FGM2;

                tsopp2.pl_stats[tFGA] += bs.FGA1;
                tsopp1.pl_stats[tFGA] += bs.FGA2;

                //
                tsopp2.pl_stats[tTPM] += bs.TPM1;
                tsopp1.pl_stats[tTPM] += bs.TPM2;

                //
                tsopp2.pl_stats[tTPA] += bs.TPA1;
                tsopp1.pl_stats[tTPA] += bs.TPA2;

                //
                tsopp2.pl_stats[tFTM] += bs.FTM1;
                tsopp1.pl_stats[tFTM] += bs.FTM2;

                //
                tsopp2.pl_stats[tFTA] += bs.FTA1;
                tsopp1.pl_stats[tFTA] += bs.FTA2;

                //
                tsopp2.pl_stats[tOREB] += bs.OFF1;
                tsopp1.pl_stats[tOREB] += bs.OFF2;

                //
                tsopp2.pl_stats[tDREB] += Convert.ToUInt16(bs.REB1 - bs.OFF1);
                tsopp1.pl_stats[tDREB] += Convert.ToUInt16(bs.REB2 - bs.OFF2);

                //
                tsopp2.pl_stats[tSTL] += bs.STL1;
                tsopp1.pl_stats[tSTL] += bs.STL2;

                //
                tsopp2.pl_stats[tTO] += bs.TO1;
                tsopp1.pl_stats[tTO] += bs.TO2;

                //
                tsopp2.pl_stats[tBLK] += bs.BLK1;
                tsopp1.pl_stats[tBLK] += bs.BLK2;

                //
                tsopp2.pl_stats[tAST] += bs.AST1;
                tsopp1.pl_stats[tAST] += bs.AST2;

                //
                tsopp2.pl_stats[tFOUL] += bs.PF1;
                tsopp1.pl_stats[tFOUL] += bs.PF2;
            }

            ts1.calcAvg();
            ts2.calcAvg();
            tsopp1.calcAvg();
            tsopp2.calcAvg();
        }

        private void populateTeamsComboBox(SortedDictionary<string, int> TeamOrder, PlayoffTree pt)
        {
            bool done = false;

            cmbTeam1.Items.Clear();
            if (pt != null)
            {
                if (pt.teams[0] != "Invalid")
                {
                    var newteams = new List<string>();
                    for (int i = 0; i < 16; i++)
                        newteams.Add(pt.teams[i]);
                    newteams.Sort();
                    for (int i = 0; i < 16; i++)
                        cmbTeam1.Items.Add(newteams[i]);
                    done = true;
                }
            }

            if (!done)
            {
                foreach (var kvp in TeamOrder)
                    cmbTeam1.Items.Add(kvp.Key);
            }
        }

        private static void checkForUpdates()
        {
            try
            {
                var webClient = new WebClient();
                webClient.DownloadFileCompleted += Completed;
                //webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                webClient.DownloadFileAsync(new Uri("http://students.ceid.upatras.gr/~aslanoglou/nstversion.txt"),
                                            AppDocsPath + @"nstversion.txt");
            }
            catch
            {
            }
        }

        /*
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
        */

        private static void Completed(object sender, AsyncCompletedEventArgs e)
        {
            string[] updateInfo;
            string[] versionParts;
            try
            {
                updateInfo = File.ReadAllLines(AppDocsPath + @"nstversion.txt");
                versionParts = updateInfo[0].Split('.');
            }
            catch
            {
                return;
            }
            string[] curVersionParts = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
            var iVP = new int[versionParts.Length];
            var iCVP = new int[versionParts.Length];
            for (int i = 0; i < versionParts.Length; i++)
            {
                iVP[i] = Convert.ToInt32(versionParts[i]);
                iCVP[i] = Convert.ToInt32(curVersionParts[i]);
                if (iCVP[i] > iVP[i]) break;
                if (iVP[i] > iCVP[i])
                {
                    string changelog = "\n\nVersion " + String.Join(".", versionParts);
                    try
                    {
                        for (int j = 2; j < updateInfo.Length; j++)
                        {
                            changelog += "\n" + updateInfo[j];
                        }
                    }
                    catch
                    {
                    }
                    MessageBoxResult mbr = MessageBox.Show(
                        "A new version is available! Would you like to download it?" + changelog, "NBA Stats Tracker",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (mbr == MessageBoxResult.Yes)
                    {
                        Process.Start(updateInfo[1]);
                        break;
                    }
                }
            }
        }

        private void btnEraseSettings_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Please select the Career file you want to reset the settings for...";
            ofd.Filter = "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
                         +
                         "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                         "Create A Legend files (*.SMG)|*.SMG";
            if (Directory.Exists(SavesPath)) ofd.InitialDirectory = SavesPath;
            ofd.ShowDialog();
            if (ofd.FileName == "") return;

            string safefn = Tools.getSafeFilename(ofd.FileName);
            string SettingsFile = AppDocsPath + safefn + ".cfg";
            if (File.Exists(SettingsFile)) File.Delete(SettingsFile);
            MessageBox.Show(
                "Settings for this file have been erased. You'll be asked to set them again next time you import it.");
        }

        private void _AnyTextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox) sender;
            tb.SelectAll();
        }

        private void btnShowAvg_Click(object sender, RoutedEventArgs e)
        {
            string msg = NSTHelper.averagesAndRankings(cmbTeam1.SelectedItem.ToString(), tst, TeamOrder);
            if (msg != "")
            {
                var cw = new copyableW(msg, cmbTeam1.SelectedItem.ToString(), TextAlignment.Center);
                cw.ShowDialog();
            }
        }

        private void btnScout_Click(object sender, RoutedEventArgs e)
        {
            int id = -1;
            try
            {
                id = TeamOrder[cmbTeam1.SelectedItem.ToString()];
            }
            catch
            {
                return;
            }

            int[][] rating = NSTHelper.calculateRankings(tst);
            if (rating.Length != 1)
            {
                string msg = NSTHelper.scoutReport(rating, id, cmbTeam1.SelectedItem.ToString());
                var cw = new copyableW(msg, "Scouting Report", TextAlignment.Left);
                cw.ShowDialog();
            }
        }

        private void btnTeamCSV_Click(object sender, RoutedEventArgs e)
        {
            int id = -1;
            try
            {
                id = TeamOrder[cmbTeam1.SelectedItem.ToString()];
            }
            catch
            {
                return;
            }

            string header1 = "GP,W,L,PF,PA,FGM,FGA,3PM,3PA,FTM,FTA,OREB,DREB,STL,TO,BLK,AST,FOUL";
            /*
            string data = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}", 
                tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1], tst[id].stats[tFGM], tst[id].stats[tFGA], tst[id].stats[tTPM], tst[id].stats[tTPA],
                tst[id].stats[tFTM], tst[id].stats[tFTA], tst[
             */
            string data1 = String.Format("{0},{1},{2}", tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1]);
            for (int j = 1; j <= 16; j++)
            {
                if (j != 3)
                {
                    data1 += "," + tst[id].stats[j].ToString();
                }
            }

            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            string header2 = "W%,Weff,PPG,PAPG,FG%,FGeff,3P%,3Peff,FT%,FTeff,RPG,ORPG,DRPG,SPG,BPG,TPG,APG,FPG";
            string data2 = String.Format("{0:F3}", tst[id].averages[tWp]) + "," +
                           String.Format("{0:F1}", tst[id].averages[tWeff]);
            for (int j = 0; j <= 15; j++)
            {
                switch (j)
                {
                    case 2:
                    case 4:
                    case 6:
                        data2 += String.Format(",{0:F3}", tst[id].averages[j]);
                        break;
                    default:
                        data2 += String.Format(",{0:F1}", tst[id].averages[j]);
                        break;
                }
            }

            int[][] rankings = NSTHelper.calculateRankings(tst);

            string data3 = String.Format("{0:F3}", rankings[id][tWp]) + "," + String.Format("{0:F1}", rankings[id][tWeff]);
            for (int j = 0; j <= 15; j++)
            {
                switch (j)
                {
                    case 1:
                    case 13:
                    case 15:
                        data3 += "," + (31 - rankings[id][j]).ToString();
                        break;
                    default:
                        data3 += "," + rankings[id][j].ToString();
                        break;
                }
            }

            var sfd = new SaveFileDialog();
            sfd.Filter = "Comma-Separated Values file (*.csv)|*.csv";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.Title = "Select a file to save the CSV to...";
            sfd.ShowDialog();
            if (sfd.FileName == "") return;

            var sw = new StreamWriter(sfd.FileName);
            /*
            sw.WriteLine(header1);
            sw.WriteLine(data1);
            sw.WriteLine();
            sw.WriteLine(header2);
            sw.WriteLine(data2);
            sw.WriteLine(data3);
            */
            sw.WriteLine(header1 + "," + header2);
            sw.WriteLine(data1 + "," + data2);
            sw.WriteLine();
            sw.WriteLine(header2);
            sw.WriteLine(data3);
            sw.Close();
        }

        private void btnLeagueCSV_Click(object sender, RoutedEventArgs e)
        {
            string header1 = ",Team,GP,W,L,PF,PA,FGM,FGA,3PM,3PA,FTM,FTA,OREB,DREB,STL,TO,BLK,AST,FOUL,";
            //string header2 = "Team,W%,Weff,PPG,PAPG,FG%,FGeff,3P%,3Peff,FT%,FTeff,RPG,ORPG,DRPG,SPG,BPG,TPG,APG,FPG";
            string header2 = "W%,Weff,PPG,PAPG,FG%,FGeff,3P%,3Peff,FT%,FTeff,RPG,ORPG,DRPG,SPG,BPG,TPG,APG,FPG";
            /*
            string data = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}", 
                tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1], tst[id].stats[tFGM], tst[id].stats[tFGA], tst[id].stats[tTPM], tst[id].stats[tTPA],
                tst[id].stats[tFTM], tst[id].stats[tFTA], tst[
             */
            string data1 = "";
            for (int id = 0; id < 30; id++)
            {
                if (tst[id].name == "") continue;

                data1 += (id + 1).ToString() + ",";
                foreach (var kvp in TeamOrder)
                {
                    if (kvp.Value == id)
                    {
                        data1 += kvp.Key + ",";
                        break;
                    }
                }
                data1 += String.Format("{0},{1},{2}", tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1]);
                for (int j = 1; j <= 16; j++)
                {
                    if (j != 3)
                    {
                        data1 += "," + tst[id].stats[j].ToString();
                    }
                }
                data1 += ",";
                data1 += String.Format("{0:F3}", tst[id].averages[tWp]) + "," +
                         String.Format("{0:F1}", tst[id].averages[tWeff]);
                for (int j = 0; j <= 15; j++)
                {
                    switch (j)
                    {
                        case 2:
                        case 4:
                        case 6:
                            data1 += String.Format(",{0:F3}", tst[id].averages[j]);
                            break;
                        default:
                            data1 += String.Format(",{0:F1}", tst[id].averages[j]);
                            break;
                    }
                }
                data1 += "\n";
            }

            var sfd = new SaveFileDialog();
            sfd.Filter = "Comma-Separated Values file (*.csv)|*.csv";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.Title = "Select a file to save the CSV to...";
            sfd.ShowDialog();
            if (sfd.FileName == "") return;

            var sw = new StreamWriter(sfd.FileName);
            sw.WriteLine(header1 + header2);
            sw.WriteLine(data1);
            sw.Close();
        }

        private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            btnImport2K12_Click(sender, e);
        }

        private void btnSaveCustomTeam_Click(object sender, RoutedEventArgs e)
        {
            int id = TeamOrder[cmbTeam1.SelectedItem.ToString()];
            tst[id].winloss[0] = Convert.ToByte(txtW1.Text);
            tst[id].winloss[1] = Convert.ToByte(txtL1.Text);
            tst[id].stats[tPF] = Convert.ToUInt16(txtPF1.Text);
            tst[id].stats[tPA] = Convert.ToUInt16(txtPA1.Text);
            tst[id].stats[tFGM] = Convert.ToUInt16(txtFGM1.Text);
            tst[id].stats[tFGA] = Convert.ToUInt16(txtFGA1.Text);
            tst[id].stats[tTPM] = Convert.ToUInt16(txt3PM1.Text);
            tst[id].stats[tTPA] = Convert.ToUInt16(txt3PA1.Text);
            tst[id].stats[tFTM] = Convert.ToUInt16(txtFTM1.Text);
            tst[id].stats[tFTA] = Convert.ToUInt16(txtFTA1.Text);
            tst[id].stats[tOREB] = Convert.ToUInt16(txtOREB1.Text);
            tst[id].stats[tDREB] = Convert.ToUInt16(txtDREB1.Text);
            tst[id].stats[tSTL] = Convert.ToUInt16(txtSTL1.Text);
            tst[id].stats[tTO] = Convert.ToUInt16(txtTO1.Text);
            tst[id].stats[tBLK] = Convert.ToUInt16(txtBLK1.Text);
            tst[id].stats[tAST] = Convert.ToUInt16(txtAST1.Text);
            tst[id].stats[tFOUL] = Convert.ToUInt16(txtFOUL1.Text);

            tst[id].calcAvg();
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(-1);
        }

        private void btnInject_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Please select the Career file you want to update...";
            ofd.Filter = "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
                         +
                         "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                         "Create A Legend files (*.SMG)|*.SMG";
            if (Directory.Exists(SavesPath)) ofd.InitialDirectory = SavesPath;
            ofd.ShowDialog();

            if (ofd.FileName == "") return;
            string fn = ofd.FileName;

            NSTHelper.prepareOffsets(fn, tst, ref TeamOrder, ref pt);

            TeamStats[] temp = new TeamStats[1];
            TeamStats[] tempopp = new TeamStats[1];

            NSTHelper.GetStatsFrom2K12Save(fn, ref temp, ref tempopp, ref TeamOrder, ref pt);
            if (temp.Length == 1)
            {
                MessageBox.Show("Couldn't get stats from " + Tools.getSafeFilename(fn) + ". Update failed.");
                return;
            }
            else
            {
                bool incompatible = false;

                if (temp.Length != tst.Length) incompatible = true;
                else
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (temp[i].name != tst[i].name)
                        {
                            incompatible = true;
                            break;
                        }

                        if ((!temp[i].winloss.SequenceEqual(tst[i].winloss)) ||
                            (!temp[i].pl_winloss.SequenceEqual(tst[i].pl_winloss)))
                        {
                            incompatible = true;
                            break;
                        }
                    }
                }

                if (incompatible)
                {
                    MessageBoxResult r =
                        MessageBox.Show(
                            "The file currently loaded seems incompatible with the NBA 2K save you're trying to save into." +
                            "\nThis could be happening for a number of reasons:\n\n" +
                            "1. The file currently loaded isn't one that had stats imported to it from your 2K save.\n" +
                            "2. The Win/Loss record for one or more teams would be different after this procedure.\n\n" +
                            "If you're updating using a box score, then either you're not using the NST database you imported your stats\n" +
                            "into before the game, or you entered the box score incorrectly. Remember that you need to import your stats\n" +
                            "into a database right before the game starts, let the game end and save the Association, and then update the\n" +
                            "database using the box score. If you follow these steps correctly, you shouldn't get this message when you try\n" +
                            "to export the stats from the database to your 2K save.\n\n" +
                            "Are you sure you want to continue? SAVE CORRUPTION MAY OCCUR, AND I WON'T BE HELD LIABLE FOR IT. ALWAYS KEEP BACKUPS.",
                            "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (r == MessageBoxResult.No) return;
                }
            }


            NSTHelper.updateSavegame(fn, tst, TeamOrder, pt);
            updateStatus("Injected custom Team Stats into " + Tools.getSafeFilename(fn) + " successfully!");
            cmbTeam1.SelectedIndex = -1;
            cmbTeam1.SelectedIndex = 0;
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aw = new askTeamW(true, cmbTeam1.SelectedIndex);
                aw.ShowDialog();
            }
            catch
            {
            }
        }

        private void mnuHelpReadme_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(AppPath + @"\readme.txt");
        }

        private void mnuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            var aw = new aboutW();
            aw.ShowDialog();
        }

        private void mnuFileGetRealStats_Click(object sender, RoutedEventArgs e)
        {
            string file = "";

            if (!String.IsNullOrWhiteSpace(txtFile.Text))
            {
                MessageBoxResult r =
                    MessageBox.Show(
                        "This will overwrite the stats in the currently opened file. Are you sure?\n\nClick Yes to overwrite.\nClick No to create a new file automatically. Any unsaved changes to the current file will be lost.\nClick Cancel to return to the main window.",
                        "NBA Stats Tracker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) file = currentDB;
                else if (r == MessageBoxResult.No) txtFile.Text = "";
                else return;
            }

            if (String.IsNullOrWhiteSpace(txtFile.Text))
            {
                file = AppDocsPath + "Real NBA Stats " + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" +
                       DateTime.Now.Day + ".tst";
                if (File.Exists(file))
                {
                    if (App.realNBAonly)
                    {
                        MessageBoxResult r =
                            MessageBox.Show(
                                "Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?",
                                "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question,
                                MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                            return;
                    }
                    else
                    {
                        MessageBoxResult r =
                            MessageBox.Show(
                                "Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?",
                                "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question,
                                MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                        {
                            LoadDatabase(file, ref tst, ref tstopp, ref pst, ref TeamOrder, ref pt, ref bshist);

                            cmbTeam1.SelectedIndex = -1;
                            cmbTeam1.SelectedIndex = 0;
                            txtFile.Text = file;
                            return;
                        }
                    }
                }
            }

            var grsw = new getRealStatsW();
            grsw.ShowDialog();
            TeamOrder = NSTHelper.setTeamOrder("Mode 0");

            //TeamStats[] realtstopp;
            //RealStats.ImportRealStats(out realtst, out realtstopp, out pst, out TeamOrder);

            if (realtst[0].name != "Canceled")
            {
                int len = realtst.GetLength(0);

                tst = new TeamStats[len];
                tstopp = new TeamStats[len];
                for (int i = 0; i < len; i++)
                {
                    foreach (var kvp in TeamOrder)
                    {
                        if (kvp.Value == i)
                        {
                            tst[i] = new TeamStats(kvp.Key);
                            tstopp[i] = new TeamStats(kvp.Key);
                            break;
                        }
                    }
                }

                tst = realtst;
                //tstopp = realtstopp;
                saveSeasonToDatabase(file, tst, tstopp, pst, curSeason, curSeason);
                cmbTeam1.SelectedIndex = -1;
                cmbTeam1.SelectedIndex = 0;
                txtFile.Text = file;
                LoadDatabase(file, ref tst, ref tstopp, ref pst, ref TeamOrder, ref pt, ref bshist);
            }
        }

        private void btnCompareToReal_Click(object sender, RoutedEventArgs e)
        {
            var realteam = new TeamStats();

            if (File.Exists(AppDocsPath + cmbTeam1.SelectedItem + ".rst"))
            {
                var fi = new FileInfo(AppDocsPath + cmbTeam1.SelectedItem + ".rst");
                TimeSpan sinceLastModified = DateTime.Now - fi.LastWriteTime;
                if (sinceLastModified.Days >= 1)
                    realteam = NSTHelper.getRealStats(cmbTeam1.SelectedItem.ToString());
                else
                    try
                    {
                        realteam = NSTHelper.getRealStats(cmbTeam1.SelectedItem.ToString(), true);
                    }
                    catch
                    {
                        try
                        {
                            realteam = NSTHelper.getRealStats(cmbTeam1.SelectedItem.ToString());
                        }
                        catch
                        {
                            MessageBox.Show(
                                "An incomplete real stats file is present and locked in the disk. Please restart NBA Stats Tracker and try again.");
                        }
                    }
            }
            else
            {
                realteam = NSTHelper.getRealStats(cmbTeam1.SelectedItem.ToString());
            }
            TeamStats curteam = tst[TeamOrder[cmbTeam1.SelectedItem.ToString()]];

            var vw = new versusW(curteam, "Current", realteam, "Real");
            vw.ShowDialog();
        }

        private void btnCompareOtherFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Select the TST file that has the team stats you want to compare to...";
            ofd.Filter = "Team Stats files (*.tst)|*.tst";
            ofd.InitialDirectory = AppDocsPath;
            ofd.ShowDialog();

            string file = ofd.FileName;
            if (file != "")
            {
                string team = cmbTeam1.SelectedItem.ToString();
                string safefn = Tools.getSafeFilename(file);
                var _newTeamOrder = new SortedDictionary<string, int>();

                FileStream stream = File.Open(ofd.FileName, FileMode.Open);
                var bf = new BinaryFormatter();
                bf.AssemblyFormat = FormatterAssemblyStyle.Simple;

                var _newtst = new TeamStats[30];
                for (int i = 0; i < 30; i++)
                {
                    _newtst[i] = new TeamStats();
                    _newtst[i] = (TeamStats) bf.Deserialize(stream);
                    if (_newtst[i].name == "") continue;
                    try
                    {
                        _newTeamOrder.Add(_newtst[i].name, i);
                        _newtst[i].calcAvg();
                    }
                    catch
                    {
                    }
                }

                TeamStats newteam = _newtst[_newTeamOrder[team]];
                TeamStats curteam = tst[TeamOrder[team]];

                var vw = new versusW(curteam, "Current", newteam, "Other");
                vw.ShowDialog();
            }
        }

        private void txtFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtFile.ScrollToHorizontalOffset(txtFile.GetRectFromCharacterIndex(txtFile.Text.Length).Right);
            currentDB = txtFile.Text;
            db = new SQLiteDatabase(currentDB);
        }

        private void btnTrends_Click(object sender, RoutedEventArgs e)
        {
            var ofd1 = new OpenFileDialog();
            if (txtFile.Text == "")
            {
                ofd1.Title = "Select the TST file that has the current team stats...";
                ofd1.Filter = "Team Stats files (*.tst)|*.tst";
                ofd1.InitialDirectory = AppDocsPath;
                ofd1.ShowDialog();

                if (ofd1.FileName == "") return;

                LoadDatabase(ofd1.FileName, ref tst, ref tstopp, ref pst, ref TeamOrder, ref pt, ref bshist, true);
                cmbTeam1.SelectedIndex = 0;
            }

            var ofd = new OpenFileDialog();
            ofd.Title = "Select the TST file that has the team stats you want to compare to...";
            ofd.Filter = "Team Stats files (*.tst)|*.tst";
            ofd.InitialDirectory = AppDocsPath;
            ofd.ShowDialog();

            if (ofd.FileName == "") return;

            string team = cmbTeam1.SelectedItem.ToString();
            int id = TeamOrder[team];

            TeamStats[] curTST = tst;

            var oldTeamOrder = new SortedDictionary<string, int>();
            var oldPT = new PlayoffTree();
            IList<BoxScoreEntry> oldbshist = new List<BoxScoreEntry>();
            TeamStats[] oldTST = new TeamStats[1];
            TeamStats[] oldTSTopp = new TeamStats[1];
            LoadDatabase(ofd.FileName, ref oldTST, ref oldTSTopp, ref pst, ref oldTeamOrder, ref oldPT, ref oldbshist, false);

            var curR = new Rankings(tst);
            var oldR = new Rankings(oldTST);
            int[][] diffrnk = calculateDifferenceRanking(curR, oldR);
            float[][] diffavg = calculateDifferenceAverage(curTST, oldTST);

            int maxi = 0;
            int mini = 0;
            for (int i = 1; i < 30; i++)
            {
                if (diffavg[i][0] > diffavg[maxi][0])
                    maxi = i;
                if (diffavg[i][0] < diffavg[mini][0])
                    mini = i;
            }

            string str = "";

            string team1 = tst[maxi].name;
            if (diffrnk[maxi][0] > 0)
            {
                str =
                    String.Format(
                        "Most improved in {7}, the {0}. They were #{1} ({4:F1}), climbing {3} places they are now at #{2} ({5:F1}), a {6:F1} {7} difference!",
                        tst[maxi].name, oldR.rankings[maxi][0], curR.rankings[maxi][0], diffrnk[maxi][0],
                        oldTST[maxi].averages[0], tst[maxi].averages[0], diffavg[maxi][0], "PPG");
            }
            else
            {
                str =
                    String.Format(
                        "Most improved in {7}, the {0}. They were #{1} ({4:F1}) and they are now at #{2} ({5:F1}), a {6:F1} {7} difference!",
                        tst[maxi].name, oldR.rankings[maxi][0], curR.rankings[maxi][0], diffrnk[maxi][0],
                        oldTST[maxi].averages[0], tst[maxi].averages[0], diffavg[maxi][0], "PPG");
            }
            str += " ";
            str +=
                String.Format(
                    "Taking this improvement apart, their FG% went from {0:F3} to {1:F3}, 3P% was {2:F3} and now is {3:F3}, and FT% is now at {4:F3}, having been at {5:F3}.",
                    oldTST[maxi].averages[tFGp], tst[maxi].averages[tFGp], oldTST[maxi].averages[tTPp],
                    tst[maxi].averages[tTPp], tst[maxi].averages[tFTp], oldTST[maxi].averages[tFTp]);

            if (curR.rankings[maxi][tFGeff] <= 5)
            {
                str += " ";
                if (oldR.rankings[maxi][tFGeff] > 20)
                    str +=
                        "Huge leap in Field Goal efficiency. Back then they were on of the worst teams on the offensive end, now in the Top 5.";
                else if (oldR.rankings[maxi][tFGeff] > 10)
                    str +=
                        "An average offensive team turned great. From the middle of the pack, they are now in Top 5 in Field Goal efficiency.";
                else if (oldR.rankings[maxi][tFGeff] > 5)
                    str +=
                        "They were already hot, and they're just getting better. Moving on up from Top 10 in FGeff, to Top 5.";
                else
                    str +=
                        "They just know how to stay hot at the offensive end. Still in the Top 5 of the most efficient teams from the floor.";
            }
            if (curR.rankings[maxi][tFTeff] <= 5)
                str +=
                    " They're not afraid of contact, and they know how to make the most from the line. Top 5 in Free Throw efficiency.";
            if (diffavg[maxi][tAPG] > 0)
                str +=
                    String.Format(
                        " They are getting better at finding the open man with a timely pass. {0:F1} improvement in assists per game.",
                        diffavg[maxi][tAPG]);
            if (diffavg[maxi][tRPG] > 0) str += String.Format(" Their additional rebounds have helped as well.");
            if (diffavg[maxi][tTPG] < 0)
                str += String.Format(" Also taking better care of the ball, making {0:F1} less turnovers per game.",
                                     -diffavg[maxi][tTPG]);

            ///////////////////////////
            str += "$";
            ///////////////////////////

            string team2 = tst[mini].name;
            if (diffrnk[mini][0] < 0)
            {
                str +=
                    String.Format(
                        "On the other end, the {0} have lost some of their scoring power. They were #{1} ({4:F1}), dropping {3} places they are now at #{2} ({5:F1}).",
                        tst[mini].name, oldR.rankings[mini][0], curR.rankings[mini][0], -diffrnk[mini][0],
                        oldTST[mini].averages[0], tst[mini].averages[0]);
            }
            else
            {
                str +=
                    String.Format(
                        "On the other end, the {0} have lost some of their scoring power. They were #{1} ({4:F1}) and are now in #{2} ({5:F1}). Guess even that {6:F1} PPG drop wasn't enough to knock them down!",
                        tst[mini].name, oldR.rankings[mini][0], curR.rankings[mini][0], -diffrnk[mini][0],
                        oldTST[mini].averages[0], tst[mini].averages[0], -diffavg[mini][0]);
            }
            str += " ";
            str +=
                String.Format(
                    "So why has this happened? Their FG% went from {0:F3} to {1:F3}, 3P% was {2:F3} and now is {3:F3}, and FT% is now at {4:F3}, having been at {5:F3}.",
                    oldTST[mini].averages[tFGp], tst[mini].averages[tFGp], oldTST[mini].averages[tTPp],
                    tst[mini].averages[tTPp], tst[mini].averages[tFTp], oldTST[mini].averages[tFTp]);
            if (diffavg[mini][tTPG] > 0)
                str +=
                    String.Format(
                        " You can't score as many points when you commit turnovers; they've seen them increase by {0:F1} per game.",
                        diffavg[mini][tTPG]);

            var tw = new trendsW(str, team1, team2);
            tw.ShowDialog();
        }

        private int[][] calculateDifferenceRanking(Rankings curR, Rankings newR)
        {
            var diff = new int[30][];
            for (int i = 0; i < 30; i++)
            {
                diff[i] = new int[18];
                for (int j = 0; j < 18; j++)
                {
                    diff[i][j] = newR.rankings[i][j] - curR.rankings[i][j];
                }
            }
            return diff;
        }

        private float[][] calculateDifferenceAverage(TeamStats[] curTST, TeamStats[] oldTST)
        {
            var diff = new float[30][];
            for (int i = 0; i < 30; i++)
            {
                diff[i] = new float[18];
                for (int j = 0; j < 18; j++)
                {
                    diff[i][j] = curTST[i].averages[j] - oldTST[i].averages[j];
                }
            }
            return diff;
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            DataSet ds = RealStats.GetPlayoffTeamStats(@"http://www.basketball-reference.com/playoffs/NBA_2012.html");

            testW tw = new testW(ds);
            tw.ShowDialog();

        }

        private void mnuHistoryBoxScores_Click(object sender, RoutedEventArgs e)
        {
            if (isTSTEmpty()) return;

            bs = new BoxScore();
            var bsw = new boxScoreW(boxScoreW.Mode.View);
            bsw.ShowDialog();

            UpdateBoxScore();
        }

        public static void UpdateBoxScore()
        {
            if (bs.bshistid != -1)
            {
                if (bs.done)
                {
                    var list = new List<PlayerBoxScore>();
                    foreach (var pbsList in pbsLists)
                    {
                        foreach (PlayerBoxScore pbs in pbsList)
                        {
                            list.Add(pbs);
                        }
                    }

                    bshist[bs.bshistid].bs = bs;
                    bshist[bs.bshistid].pbsList = list;
                    bshist[bs.bshistid].mustUpdate = true;
                }
            }
        }

        private void btnTeamOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB)) return;
            if (isTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using the Analysis features.");
                return;
            }

            var tow = new teamOverviewW();
            tow.ShowDialog();
        }

        private void btnOpenCustom_Click(object sender, RoutedEventArgs e)
        {
            mnuFileOpenCustom_Click(null, null);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
            //dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            status.FontWeight = FontWeights.Normal;
            status.Content = "Ready";
            dispatcherTimer.Stop();
        }

        private void updateStatus(string newStatus)
        {
            dispatcherTimer.Stop();
            status.FontWeight = FontWeights.Bold;
            status.Content = newStatus;
            dispatcherTimer.Start();
        }

        private void btnSaveTeamStats_Click(object sender, RoutedEventArgs e)
        {
            if (!isCustom)
            {
                mnuFileSaveAs_Click(null, null);
            }
            else
            {
                saveSeasonToDatabase(currentDB, tst, tstopp, pst, curSeason, getMaxSeason(currentDB));
                txtFile.Text = currentDB;
            }
        }

        private void btnLeagueOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB)) return;
            /*
            if (!isCustom)
            {
                MessageBox.Show("Save the data into a Team Stats file before using the tool's features.");
                return;
            }
            */
            if (isTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using any Analysis features.");
                return;
            }

            dispatcherTimer.Stop();
            var low = new leagueOverviewW(tst, tstopp, pst);
            low.ShowDialog();
            dispatcherTimer.Start();
        }

        private void mnuFileNew_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "NST Database (*.tst)|*.tst";
            sfd.InitialDirectory = AppDocsPath;
            sfd.ShowDialog();

            if (sfd.FileName == "") return;

            File.Delete(sfd.FileName);

            db = new SQLiteDatabase(sfd.FileName);

            prepareNewDB(db, 1, 1);

            curSeason = 1;
            txbCurSeason.Text = "Current Season: 1/1";

            tst = new TeamStats[1];
            tst[0] = new TeamStats("$$NewDB");
            TeamOrder = new SortedDictionary<string, int>();

            txtFile.Text = sfd.FileName;

            //
            // tst = new TeamStats[2];
        }

        public static bool isTSTEmpty()
        {
            if (String.IsNullOrWhiteSpace(currentDB)) return true;

            db = new SQLiteDatabase(currentDB);

            string teamsT = "Teams";
            if (curSeason != getMaxSeason(currentDB)) teamsT += "S" + curSeason;
            string q = "select Name from " + teamsT;
            DataTable res = db.GetDataTable(q);

            try
            {
                if (res.Rows[0]["Name"].ToString() == "$$NewDB") return true;
                else return false;
            }
            catch
            {
                return true;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB)) return;

            addInfo = "";
            var aw = new addW(ref pst);
            aw.ShowDialog();

            if (!String.IsNullOrEmpty(addInfo))
            {
                if (addInfo != "$$NST Players Added")
                {
                    string[] parts = Regex.Split(addInfo, "\r\n");
                    var newTeams = new List<string>();
                    foreach (string s in parts)
                    {
                        if (!String.IsNullOrWhiteSpace(s))
                            newTeams.Add(s);
                    }

                    int oldlen = tst.GetLength(0);
                    if (isTSTEmpty()) oldlen--;

                    Array.Resize(ref tst, oldlen + newTeams.Count);
                    Array.Resize(ref tstopp, oldlen + newTeams.Count);

                    for (int i = 0; i < newTeams.Count; i++)
                    {
                        tst[oldlen + i] = new TeamStats(newTeams[i]);
                        tstopp[oldlen + i] = new TeamStats(newTeams[i]);
                        TeamOrder.Add(newTeams[i], oldlen + i);
                    }
                    updateStatus("Teams were added, you should save the database now");
                }
                else
                {
                    updateStatus("Players were added, you should save the database now");
                }
            }
        }

        private void btnGrabNBAStats_Click(object sender, RoutedEventArgs e)
        {
            mnuFileGetRealStats_Click(null, null);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mnuMiscStartNewSeason_Click(object sender, RoutedEventArgs e)
        {
            if (!isTSTEmpty())
            {
                MessageBoxResult r =
                    MessageBox.Show(
                        "Are you sure you want to do this? This is an irreversible action.\nStats and box Scores will be retained, and you'll be able to use all the tool's features on them.",
                        "NBA Stats Tracker", MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    curSeason = getMaxSeason(currentDB);

                    string q = "alter table Teams rename to TeamsS" + curSeason;
                    int code = db.ExecuteNonQuery(q);

                    q = "alter table PlayoffTeams rename to PlayoffTeamsS" + curSeason;
                    code = db.ExecuteNonQuery(q);

                    q = "alter table Opponents rename to OpponentsS" + curSeason;
                    code = db.ExecuteNonQuery(q);

                    q = "alter table PlayoffOpponents rename to PlayoffOpponentsS" + curSeason;
                    code = db.ExecuteNonQuery(q);

                    q = "alter table Players rename to PlayersS" + curSeason;
                    code = db.ExecuteNonQuery(q);

                    curSeason++;

                    prepareNewDB(db, curSeason, curSeason, true);

                    txbCurSeason.Text = "Current Season: " + curSeason.ToString() + "/" + curSeason.ToString();
                    foreach (TeamStats ts in tst)
                    {
                        for (int i = 0; i < ts.stats.Length; i++)
                        {
                            ts.stats[i] = 0;
                            ts.pl_stats[i] = 0;
                        }
                        ts.winloss[0] = 0;
                        ts.winloss[1] = 0;
                        ts.pl_winloss[0] = 0;
                        ts.pl_winloss[1] = 0;
                        ts.calcAvg();
                    }

                    foreach (var ps in pst)
                    {
                        for (int i = 0; i < ps.Value.stats.Length; i++)
                        {
                            ps.Value.stats[i] = 0;
                        }
                        ps.Value.isAllStar = false;
                        ps.Value.isNBAChampion = false;
                        ps.Value.CalcAvg();
                    }

                    saveSeasonToDatabase(currentDB, tst, tstopp, pst, curSeason, curSeason);
                    updateStatus("New season started. Database saved.");
                }
            }
        }

        private void btnSaveAllSeasons_Click(object sender, RoutedEventArgs e)
        {
            saveAllSeasons(currentDB);
        }

        public static int GetMaxPlayerID(string dbFile)
        {
            var db = new SQLiteDatabase(dbFile);

            string q = "select ID from Players ORDER BY ID DESC LIMIT 1;";
            DataTable res = db.GetDataTable(q);

            try
            {
                return Convert.ToInt32(res.Rows[0]["ID"].ToString());
            }
            catch
            {
                return -1;
            }
        }

        private void btnPlayerOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB)) return;
            if (isTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using the Analysis features.");
                return;
            }

            var pow = new playerOverviewW();
            pow.ShowDialog();
        }

        private void mnuMiscImportBoxScores_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "NST Database (*.tst)|*.tst";
            ofd.InitialDirectory = AppDocsPath;
            ofd.Title = "Please select the TST file that you want to import from...";
            ofd.ShowDialog();

            if (ofd.FileName == "") return;

            string file = ofd.FileName;
            
            var newBShist = GetBoxScoresFromDatabase(file);

            foreach (BoxScoreEntry newbse in newBShist)
            {
                bool doNotAdd = false;
                foreach (BoxScoreEntry bse in bshist)
                {
                    if (bse.bs.id == newbse.bs.id)
                    {
                        if (bse.bs.gamedate == newbse.bs.gamedate 
                            && bse.bs.Team1 == newbse.bs.Team1 && bse.bs.Team2 == newbse.bs.Team2)
                        {
                            MessageBoxResult r;
                            if (bse.bs.PTS1 == newbse.bs.PTS1 && bse.bs.PTS2 == newbse.bs.PTS2)
                            {
                                r =
                                    MessageBox.Show(
                                        "A box score with the same date, teams and score as one in the current database was found in the file being imported." +
                                        "\n" + bse.bs.gamedate.ToShortDateString() + ": " + bse.bs.Team1 + " " + bse.bs.PTS1 + " @ " + bse.bs.Team2 + " " + bse.bs.PTS2 +
                                        "\n\nClick Yes to only keep the box score that is already in this databse." +
                                        "\nClick No to only keep the box score that is being imported, replacing the one in this database." +
                                        "\nClick Cancel to keep both box scores.", "NBA Stats Tracker",
                                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            }
                            else
                            {
                                r =
                                    MessageBox.Show(
                                        "A box score with the same date, teams and score as one in the current database was found in the file being imported." +
                                        "\nCurrent: " + bse.bs.gamedate.ToShortDateString() + ": " + bse.bs.Team1 + " " + bse.bs.PTS1 + " @ " + bse.bs.Team2 + " " + bse.bs.PTS2 +
                                        "\nTo be imported: " + newbse.bs.gamedate.ToShortDateString() + ": " + newbse.bs.Team1 + " " + newbse.bs.PTS1 + " @ " + newbse.bs.Team2 + " " + newbse.bs.PTS2 +
                                        "\n\nClick Yes to only keep the box score that is already in this databse." +
                                        "\nClick No to only keep the box score that is being imported, replacing the one in this database." +
                                        "\nClick Cancel to keep both box scores.", "NBA Stats Tracker",
                                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            }
                            if (r == MessageBoxResult.Yes)
                            {
                                doNotAdd = true;
                                break;
                            }
                            else if (r == MessageBoxResult.No)
                            {
                                bshist.Remove(bse);
                                break;
                            }
                        }
                        newbse.bs.id = GetFreeBseId();
                        break;
                    }
                }
                if (!doNotAdd) bshist.Add(newbse);
            }
        }

        private int GetFreeBseId()
        {
            List<int> bseIDs = new List<int>();
            foreach (BoxScoreEntry bse in bshist)
            {
                bseIDs.Add(bse.bs.id);
            }

            bseIDs.Sort();

            int i = 0;
            while (true)
            {
                if (!bseIDs.Contains(i)) return i;
                i++;
            }
        }
    }
}