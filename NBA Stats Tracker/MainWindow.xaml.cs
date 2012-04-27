﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using LeftosCommonLibrary;
using Microsoft.Win32;
using System.Data.SQLite;
using System.Data;
using System.Windows.Threading;
using System.Text.RegularExpressions;

namespace NBA_2K12_Correct_Team_Stats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string AppDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\";
        public static string AppTempPath = AppDocsPath + @"Temp\";
        public static string SavesPath = "";
        public static string AppPath = Environment.CurrentDirectory + "\\";
        public static bool isCustom;

        public static MainWindow mwInstance;

        /// <summary>
        /// TeamStats array.
        /// 0: Nuggets, 1: Trail Blazers, 2: Pacers, 3: Pistons, 4: Heat,
        /// 5: Knicks, 6: Grizzlies, 7: Clippers, 8: Warriors, 9: Bucks,
        /// 10: Spurs, 11: Cavaliers, 12: Celtcs, 13: Kings, 14: Suns,
        /// 15:Hornets, 16: Hawks, 17: Timberwolves, 18: nets, 19: Wizards,
        /// 20: 76ers, 21: Raptors, 22: Bobcats, 23: Magic, 24: Thunder,
        /// 25: Lakers, 26: Rockets, 27: Jazz, 28: Bulls, 29: Mavericks
        /// </summary>
        public static TeamStats[] tst = new TeamStats[1];
        public static TeamStats[] realtst = new TeamStats[30];
        public static BoxScore bs;
        public static IList<BoxScoreEntry> bshist = new List<BoxScoreEntry>();
        public static PlayoffTree pt;
        public static string ext;
        public static string myTeam;
        public static string currentDB = "";
        public static string addInfo;
        public static int curSeason = 1;

        public static SortedDictionary<string, int> TeamOrder;

        public static List<string> West = new List<string>
        {
            "Thunder", "Spurs", "Trail Blazers",
            "Clippers", "Nuggets", "Jazz",
            "Lakers", "Mavericks", "Suns",
            "Grizzlies", "Kings", "Timberwolves",
            "Rockets", "Hornets", "Warriors"
        };

        public const int M = 0, PF = 1, PA = 2, FGM = 4, FGA = 5, TPM = 6, TPA = 7,
            FTM = 8, FTA = 9, OREB = 10, DREB = 11, STL = 12, TO = 13, BLK = 14, AST = 15,
            FOUL = 16;
        public const int PPG = 0, PAPG = 1, FGp = 2, FGeff = 3, TPp = 4, TPeff = 5,
            FTp = 6, FTeff = 7, RPG = 8, ORPG = 9, DRPG = 10, SPG = 11, BPG = 12,
            TPG = 13, APG = 14, FPG = 15, Wp = 16, Weff = 17;

        private static SQLiteDatabase db;

        private DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();

            mwInstance = this;

            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            btnSave.Visibility = Visibility.Hidden;
            btnCRC.Visibility = Visibility.Hidden;
            btnSaveCustomTeam.Visibility = Visibility.Hidden;
            //btnInject.Visibility = Visibility.Hidden;
            btnTest.Visibility = Visibility.Hidden;

            isCustom = true;

            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA 2K12 Correct Team Stats"))
                if (Directory.Exists(AppDocsPath) == false)
                    Directory.Move(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA 2K12 Correct Team Stats",
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker");

            if (Directory.Exists(AppDocsPath) == false) Directory.CreateDirectory(AppDocsPath);
            if (Directory.Exists(AppTempPath) == false) Directory.CreateDirectory(AppTempPath);

            tst[0] = new TeamStats("$$NewDB");

            for (int i = 0; i < 30; i++)
            {
                realtst[i] = new TeamStats();
            }

            //TeamOrder = StatsTracker.setTeamOrder("Mode 0");
            TeamOrder = new SortedDictionary<string, int>();

            foreach (KeyValuePair<string, int> kvp in TeamOrder)
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

            if ((rk = rk.OpenSubKey(@"SOFTWARE\2K Sports\NBA 2K12")) == null)
            {
                SavesPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\2K Sports\NBA 2K12\Saves\";
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
                foreach (string file in stgFiles)
                {
                    string realfile = file.Substring(0, file.Length - 4);
                    if (File.Exists(SavesPath + Tools.getSafeFilename(realfile)) == false)
                        File.Delete(file);
                }
            }
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Please select the Career file you're playing...";
            ofd.Filter = "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
            + "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                "Create A Legend files (*.SMG)|*.SMG";
            if (Directory.Exists(SavesPath)) ofd.InitialDirectory = SavesPath;
            ofd.ShowDialog();
            if (ofd.FileName == "") return;

            cmbTeam1.SelectedIndex = -1;

            isCustom = true;
            //prepareWindow(isCustom);
            TeamOrder = StatsTracker.setTeamOrder("Mode 0");

            TeamStats[] temp = StatsTracker.GetStats(ofd.FileName, ref TeamOrder, ref pt);
            if (temp.Length > 1)
            {
                tst = temp;
                populateTeamsComboBox(TeamOrder, pt);
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

            updateStatus("NBA 2K12 Save loaded successfully!");
            //cmbTeam1.SelectedItem = "Pistons";
        }

        private void btnCRC_Click(object sender, RoutedEventArgs e)
        {
            String hash = Tools.getCRC(txtFile.Text);

            MessageBox.Show(hash);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            StatsTracker.updateSavegame(txtFile.Text, tst, TeamOrder, pt);
        }

        private void cmbTeam1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string team = cmbTeam1.SelectedItem.ToString();
                int id = TeamOrder[team];
                txtW1.Text = tst[id].winloss[0].ToString();
                txtL1.Text = tst[id].winloss[1].ToString();
                txtPF1.Text = tst[id].stats[PF].ToString();
                txtPA1.Text = tst[id].stats[PA].ToString();
                txtFGM1.Text = tst[id].stats[FGM].ToString();
                txtFGA1.Text = tst[id].stats[FGA].ToString();
                txt3PM1.Text = tst[id].stats[TPM].ToString();
                txt3PA1.Text = tst[id].stats[TPA].ToString();
                txtFTM1.Text = tst[id].stats[FTM].ToString();
                txtFTA1.Text = tst[id].stats[FTA].ToString();
                txtOREB1.Text = tst[id].stats[OREB].ToString();
                txtDREB1.Text = tst[id].stats[DREB].ToString();
                txtSTL1.Text = tst[id].stats[STL].ToString();
                txtTO1.Text = tst[id].stats[TO].ToString();
                txtBLK1.Text = tst[id].stats[BLK].ToString();
                txtAST1.Text = tst[id].stats[AST].ToString();
                txtFOUL1.Text = tst[id].stats[FOUL].ToString();
            }
            catch
            { }
        }

        private void btnSaveTS_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Team Stats Table (*.tst)|*.tst";
            sfd.InitialDirectory = AppDocsPath;
            sfd.ShowDialog();

            if (sfd.FileName == "") return;

            string file = sfd.FileName;

            saveTeamStatsFile(file);
        }

        private static void OBSOLETE_saveTeamStatsFile(string file)
        {
            try
            {
                BinaryWriter stream = new BinaryWriter(File.Open(file, FileMode.Create));

                stream.Write("NST_STATS_FILE_START");

                // Team Stats
                stream.Write("TEAMSTATS_START");
                stream.Write(tst.GetLength(0));
                for (int i = 0; i < tst.GetLength(0); i++)
                {
                    stream.Write("TEAM_START");
                    stream.Write("NAME");
                    if (tst[i].name == null)
                    {
                        stream.Write("__NOTEAM");
                        continue;
                    }
                    else
                    {
                        stream.Write(tst[i].name);
                    }
                    stream.Write("OFFSET");
                    stream.Write(tst[i].offset);
                    stream.Write("WINLOSS");
                    stream.Write(tst[i].winloss);
                    stream.Write("STATS_START");
                    stream.Write(tst[i].stats.Length);
                    for (int j = 0; j < tst[i].stats.Length; j++)
                    {
                        stream.Write(tst[i].stats[j]);
                    }
                    stream.Write("STATS_END");
                    stream.Write("TEAM_END");
                }
                stream.Write("TEAMSTATS_END");

                // Playoff Tree
                if (pt != null)
                {
                    stream.Write("PLAYOFFTREE_START");
                    stream.Write(pt.teams.GetLength(0));
                    for (int i = 0; i < pt.teams.GetLength(0); i++)
                    {
                        stream.Write(pt.teams[i]);
                    }
                    stream.Write("PLAYOFFTREE_END");
                }
                else
                {
                    pt = new PlayoffTree();
                    stream.Write("PLAYOFFTREE_START");
                    stream.Write(pt.teams.GetLength(0));
                    for (int i = 0; i < pt.teams.GetLength(0); i++)
                    {
                        stream.Write("Invalid");
                    }
                    stream.Write("PLAYOFFTREE_END");
                    pt = null;
                }

                // Box Score History
                writeBoxScoreHistory(stream);

                // End
                stream.Write("NST_STATS_FILE_END");

                stream.Close();
            }
            catch (Exception ex)
            {
                App.errorReport(ex, "Trying to save team stats");
            }
        }

        private void saveTeamStatsFile(string file)
        {
            // Delete the file and create it from scratch. If partial updating is implemented later, maybe
            // we won't delete the file before all this.
            File.Delete(file); 

            // Isn't really needed since we delete the file, but is left for partial updating efforts later.
            bool FileExists = File.Exists(file);
            // SQLite
            try
            {
                db = new SQLiteDatabase(file);
                prepareNewDB(db);
                DataTable res;
                String q = "select Name from Teams;";
                res = db.GetDataTable(q);

                foreach (TeamStats ts in tst)
                {
                    bool found = false;

                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("Name", ts.name);
                    dict.Add("WIN", ts.winloss[0].ToString());
                    dict.Add("LOSS", ts.winloss[1].ToString());
                    dict.Add("PF", ts.stats[PF].ToString());
                    dict.Add("PA", ts.stats[PA].ToString());
                    dict.Add("FGM", ts.stats[FGM].ToString());
                    dict.Add("FGA", ts.stats[FGA].ToString());
                    dict.Add("TPM", ts.stats[TPM].ToString());
                    dict.Add("TPA", ts.stats[TPA].ToString());
                    dict.Add("FTM", ts.stats[FTM].ToString());
                    dict.Add("FTA", ts.stats[FTA].ToString());
                    dict.Add("OREB", ts.stats[OREB].ToString());
                    dict.Add("DREB", ts.stats[DREB].ToString());
                    dict.Add("STL", ts.stats[STL].ToString());
                    dict.Add("TOS", ts.stats[TO].ToString());
                    dict.Add("BLK", ts.stats[BLK].ToString());
                    dict.Add("AST", ts.stats[AST].ToString());
                    dict.Add("FOUL", ts.stats[FOUL].ToString());
                    dict.Add("OFFSET", ts.offset.ToString());

                    Dictionary<string, string> pl_dict = new Dictionary<string, string>();
                    pl_dict.Add("Name", ts.name);
                    pl_dict.Add("WIN", ts.pl_winloss[0].ToString());
                    pl_dict.Add("LOSS", ts.pl_winloss[1].ToString());
                    pl_dict.Add("PF", ts.pl_stats[PF].ToString());
                    pl_dict.Add("PA", ts.pl_stats[PA].ToString());
                    pl_dict.Add("FGM", ts.pl_stats[FGM].ToString());
                    pl_dict.Add("FGA", ts.pl_stats[FGA].ToString());
                    pl_dict.Add("TPM", ts.pl_stats[TPM].ToString());
                    pl_dict.Add("TPA", ts.pl_stats[TPA].ToString());
                    pl_dict.Add("FTM", ts.pl_stats[FTM].ToString());
                    pl_dict.Add("FTA", ts.pl_stats[FTA].ToString());
                    pl_dict.Add("OREB", ts.pl_stats[OREB].ToString());
                    pl_dict.Add("DREB", ts.pl_stats[DREB].ToString());
                    pl_dict.Add("STL", ts.pl_stats[STL].ToString());
                    pl_dict.Add("TOS", ts.pl_stats[TO].ToString());
                    pl_dict.Add("BLK", ts.pl_stats[BLK].ToString());
                    pl_dict.Add("AST", ts.pl_stats[AST].ToString());
                    pl_dict.Add("FOUL", ts.pl_stats[FOUL].ToString());
                    pl_dict.Add("OFFSET", ts.pl_offset.ToString());

                    foreach (DataRow r in res.Rows)
                    {
                        if (r[0].ToString().Equals(ts.name))
                        {
                            bool success = db.Update("Teams", dict, "Name LIKE \'" + ts.name + "\'");
                            bool success2 = db.Update("PlayoffTeams", pl_dict, "Name LIKE \'" + ts.name + "\'");
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        bool success = db.Insert("Teams", dict);
                        bool success2 = db.Insert("PlayoffTeams", pl_dict);
                    }
                }

                q = "select GameID from GameResults;";
                res = db.GetDataTable(q);
                List<int> idlist = new List<int>();
                foreach (DataRow r in res.Rows)
                {
                    idlist.Add(Convert.ToInt32(r[0].ToString()));
                }

                foreach (BoxScoreEntry bse in bshist)
                {   
                    if ((!FileExists) || (bse.bs.id == -1) || (!idlist.Contains(bse.bs.id)) || (bse.mustUpdate))
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();

                        dict.Add("T1Name", bse.bs.Team1);
                        dict.Add("T2Name", bse.bs.Team2);
                        dict.Add("Date", String.Format("{0:yyyy-MM-dd HH:mm:ss}", bse.bs.gamedate));
                        dict.Add("SeasonNum", bse.bs.SeasonNum.ToString());
                        dict.Add("IsPlayoff", bse.bs.isPlayoff.ToString());
                        dict.Add("T1PTS", bse.bs.PTS1.ToString());
                        dict.Add("T1REB", bse.bs.REB1.ToString());
                        dict.Add("T1AST", bse.bs.AST1.ToString());
                        dict.Add("T1STL", bse.bs.STL1.ToString());
                        dict.Add("T1BLK", bse.bs.BLK1.ToString());
                        dict.Add("T1TOS", bse.bs.TO1.ToString());
                        dict.Add("T1FGM", bse.bs.FGM1.ToString());
                        dict.Add("T1FGA", bse.bs.FGA1.ToString());
                        dict.Add("T13PM", bse.bs.TPM1.ToString());
                        dict.Add("T13PA", bse.bs.TPA1.ToString());
                        dict.Add("T1FTM", bse.bs.FTM1.ToString());
                        dict.Add("T1FTA", bse.bs.FTA1.ToString());
                        dict.Add("T1OREB", bse.bs.OFF1.ToString());
                        dict.Add("T1FOUL", bse.bs.PF1.ToString());
                        dict.Add("T2PTS", bse.bs.PTS2.ToString());
                        dict.Add("T2REB", bse.bs.REB2.ToString());
                        dict.Add("T2AST", bse.bs.AST2.ToString());
                        dict.Add("T2STL", bse.bs.STL2.ToString());
                        dict.Add("T2BLK", bse.bs.BLK2.ToString());
                        dict.Add("T2TOS", bse.bs.TO2.ToString());
                        dict.Add("T2FGM", bse.bs.FGM2.ToString());
                        dict.Add("T2FGA", bse.bs.FGA2.ToString());
                        dict.Add("T23PM", bse.bs.TPM2.ToString());
                        dict.Add("T23PA", bse.bs.TPA2.ToString());
                        dict.Add("T2FTM", bse.bs.FTM2.ToString());
                        dict.Add("T2FTA", bse.bs.FTA2.ToString());
                        dict.Add("T2OREB", bse.bs.OFF2.ToString());
                        dict.Add("T2FOUL", bse.bs.PF2.ToString());

                        if (idlist.Contains(bse.bs.id))
                        {
                            db.Update("GameResults", dict, "GameID = " + bse.bs.id);
                        }
                        else
                        {
                            db.Insert("GameResults", dict);

                            string sql = @"select last_insert_rowid()";
                            int lastId = Convert.ToInt32(db.ExecuteScalar(sql)); // Need to type-cast since `ExecuteScalar` returns an object.
                            bse.bs.id = lastId;
                        }
                    }
                }
                txtFile.Text = file;
                currentDB = file;
                isCustom = true;
                updateStatus("File saved successfully.");
            }
            catch (Exception ex)
            {
                App.errorReport(ex, "Trying to save team stats - SQLite");
            }
        }

        private static void prepareNewDB(SQLiteDatabase sqldb)
        {
            try
            {
                String qr = "CREATE TABLE \"GameResults\" (\"GameID\" INTEGER PRIMARY KEY  NOT NULL ,\"T1Name\" TEXT NOT NULL ,\"T2Name\" TEXT NOT NULL ,\"Date\" DATE NOT NULL ,\"SeasonNum\" INTEGER NOT NULL ,\"IsPlayoff\" BOOLEAN NOT NULL  DEFAULT ('FALSE') ,\"T1PTS\" INTEGER NOT NULL ,\"T1REB\" INTEGER NOT NULL ,\"T1AST\" INTEGER NOT NULL ,\"T1STL\" INTEGER NOT NULL ,\"T1BLK\" INTEGER NOT NULL ,\"T1TOS\" INTEGER NOT NULL ,\"T1FGM\" INTEGER NOT NULL ,\"T1FGA\" INTEGER NOT NULL ,\"T13PM\" INTEGER NOT NULL ,\"T13PA\" INTEGER NOT NULL ,\"T1FTM\" INTEGER NOT NULL ,\"T1FTA\" INTEGER NOT NULL ,\"T1OREB\" INTEGER NOT NULL ,\"T1FOUL\" INTEGER NOT NULL ,\"T2PTS\" INTEGER NOT NULL ,\"T2REB\" INTEGER NOT NULL ,\"T2AST\" INTEGER NOT NULL ,\"T2STL\" INTEGER NOT NULL ,\"T2BLK\" INTEGER NOT NULL ,\"T2TOS\" INTEGER NOT NULL ,\"T2FGM\" INTEGER NOT NULL ,\"T2FGA\" INTEGER NOT NULL ,\"T23PM\" INTEGER NOT NULL ,\"T23PA\" INTEGER NOT NULL ,\"T2FTM\" INTEGER NOT NULL ,\"T2FTA\" INTEGER NOT NULL ,\"T2OREB\" INTEGER NOT NULL ,\"T2FOUL\" INTEGER NOT NULL )";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"PlayerResults\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"GameID\" INTEGER NOT NULL ,\"PlayerID\" INTEGER NOT NULL ,\"TeamID\" INTEGER NOT NULL ,\"PTS\" INTEGER NOT NULL ,\"REB\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL  DEFAULT (0) ,\"MINS\" INTEGER NOT NULL  DEFAULT (0) )";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"Players\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"LastName\" TEXT NOT NULL ,\"FirstName\" TEXT NOT NULL ,\"WIN\" INTEGER NOT NULL ,\"LOS\" INTEGER NOT NULL ,\"PF\" INTEGER NOT NULL ,\"PA\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"CurTeam\" INTEGER,\"MINS\" INTEGER NOT NULL  DEFAULT (0), \"Position\" TEXT, \"Injured\" BOOL )";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"PlayoffTeams\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"Name\" TEXT NOT NULL ,\"WIN\" INTEGER NOT NULL ,\"LOSS\" INTEGER NOT NULL ,\"PF\" INTEGER NOT NULL ,\"PA\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"OFFSET\" INTEGER)";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"Teams\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL ,\"Name\" TEXT NOT NULL ,\"WIN\" INTEGER NOT NULL ,\"LOSS\" INTEGER NOT NULL ,\"PF\" INTEGER NOT NULL ,\"PA\" INTEGER NOT NULL ,\"FGM\" INTEGER NOT NULL ,\"FGA\" INTEGER NOT NULL ,\"TPM\" INTEGER NOT NULL ,\"TPA\" INTEGER NOT NULL ,\"FTM\" INTEGER NOT NULL ,\"FTA\" INTEGER NOT NULL ,\"OREB\" INTEGER NOT NULL ,\"DREB\" INTEGER NOT NULL ,\"STL\" INTEGER NOT NULL ,\"TOS\" INTEGER NOT NULL ,\"BLK\" INTEGER NOT NULL ,\"AST\" INTEGER NOT NULL ,\"FOUL\" INTEGER NOT NULL ,\"OFFSET\" INTEGER)";
                sqldb.ExecuteNonQuery(qr);
                qr = "CREATE TABLE \"Misc\" (\"CurSeason\" INTEGER);";
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
                        
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Team Stats Table (*.tst)|*.tst";
            ofd.InitialDirectory = AppDocsPath;
            ofd.Title = "Please select the TST file that you want to edit...";
            ofd.ShowDialog();

            if (ofd.FileName == "") return;
            
            tst = getCustomStats(ofd.FileName, ref TeamOrder, ref pt, ref bshist);
            //tst = getCustomStats("", ref TeamOrder, ref pt, ref bshist);

            cmbTeam1.SelectedIndex = -1;
            cmbTeam1.SelectedIndex = 0;
            txtFile.Text = ofd.FileName;

            updateStatus(tst.GetLength(0).ToString() + " teams loaded successfully");
            currentDB = txtFile.Text;
            //txtFile.Text = "SQLite";

            //MessageBox.Show(bshist.Count.ToString());
        }

        private TeamStats[] OBSOLETE_getCustomStats(string file, ref SortedDictionary<string, int> _TeamOrder, ref PlayoffTree _pt, ref IList<BoxScoreEntry> _bshist, bool updateCombo = true)
        {
            BinaryReader stream = new BinaryReader(File.Open(file, FileMode.Open));
            string cur = stream.ReadString();
            string expect = "NST_STATS_FILE_START";
            if (cur != expect)
            {
                MessageBox.Show("Error while reading stats: Expected " + expect);
                return new TeamStats[30];
            }

            cur = stream.ReadString();
            expect = "TEAMSTATS_START";
            if (cur != expect)
            {
                MessageBox.Show("Error while reading stats: Expected " + expect);
                return new TeamStats[30];
            }

            int len = stream.ReadInt32();

            TeamStats[] _tst = new TeamStats[len];
            _TeamOrder = new SortedDictionary<string, int>();

            for (int i = 0; i < len; i++)
            {
                cur = stream.ReadString();
                expect = "TEAM_START";
                if (cur != expect)
                {
                    MessageBox.Show("Error while reading stats: Expected " + expect);
                    return new TeamStats[30];
                }

                _tst[i] = new TeamStats();

                bool done = false;
                bool invalid = false;
                while (!done)
                {
                    cur = stream.ReadString();
                    switch (cur)
                    {
                        case "NAME":
                            _tst[i].name = stream.ReadString();
                            if (_tst[i].name == "__NOTEAM")
                            {
                                done = true;
                                invalid = true;
                                break;
                            }
                            _TeamOrder.Add(_tst[i].name, i);
                            break;
                        case "OFFSET":
                            _tst[i].offset = stream.ReadInt32();
                            break;
                        case "WINLOSS":
                            _tst[i].winloss = stream.ReadBytes(2);
                            break;
                        case "STATS_START":
                            int statslen = stream.ReadInt32();
                            for (int j = 0; j < statslen; j++)
                            {
                                _tst[i].stats[j] = stream.ReadUInt16();
                            }

                            cur = stream.ReadString();
                            expect = "STATS_END";
                            if (cur != expect)
                            {
                                MessageBox.Show("Error while reading stats: Expected " + expect);
                                return new TeamStats[30];
                            }
                            break;
                        case "TEAM_END":
                            done = true;
                            break;
                    }
                }
                if (!invalid) _tst[i].calcAvg();
            }

            cur = stream.ReadString();
            expect = "TEAMSTATS_END";
            if (cur != expect)
            {
                MessageBox.Show("Error while reading stats: Expected " + expect);
                return new TeamStats[30];
            }

            bool done2 = false;
            while (!done2)
            {
                cur = stream.ReadString();
                switch (cur)
                {
                    case "NST_STATS_FILE_END":
                        done2 = true;
                        break;

                    case "PLAYOFFTREE_START":
                        int ptlen = stream.ReadInt32();
                        _pt = new PlayoffTree();
                        for (int i = 0; i < ptlen; i++)
                        {
                            _pt.teams[i] = stream.ReadString();
                        }

                        cur = stream.ReadString();
                        expect = "PLAYOFFTREE_END";
                        if (cur != expect)
                        {
                            MessageBox.Show("Error while reading stats: Expected " + expect);
                            return new TeamStats[30];
                        }

                        if (_pt.teams[0] == "Invalid")
                            _pt = null;

                        break;

                    case "BOXSCOREHISTORY_START":
                        int bshistlen = stream.ReadInt32();
                        _bshist = new List<BoxScoreEntry>(bshistlen);
                        for (int i = 0; i < bshistlen; i++)
                        {
                            BoxScore bs = new BoxScore();
                            cur = stream.ReadString();
                            expect = "BOXSCORE_START";
                            if (cur != expect)
                            {
                                MessageBox.Show("Error while reading stats: Expected " + expect);
                                return new TeamStats[30];
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
                                return new TeamStats[30];
                            }
                            BoxScoreEntry bse = new BoxScoreEntry(bs, date);
                            _bshist.Add(bse);
                        }

                        cur = stream.ReadString();
                        expect = "BOXSCOREHISTORY_END";
                        if (cur != expect)
                        {
                            MessageBox.Show("Error while reading stats: Expected " + expect);
                            return new TeamStats[30];
                        }
                        break;

                    default:
                        MessageBox.Show("Warning while reading stats: Unknown section " + cur);
                        break;
                }
            }

            if (updateCombo)
            {
                cmbTeam1.Items.Clear();
                foreach (KeyValuePair<string, int> kvp in _TeamOrder)
                {
                    cmbTeam1.Items.Add(kvp.Key);
                }
            }

            stream.Close();

            return (_tst);
        }

        private TeamStats[] getCustomStats(string file, ref SortedDictionary<string, int> _TeamOrder, ref PlayoffTree _pt, ref IList<BoxScoreEntry> _bshist, bool updateCombo = true)
        {
            db = new SQLiteDatabase(file);

            DataTable res;
            String q = "select * from Teams;";
            res = db.GetDataTable(q);

            TeamStats[] _tst = new TeamStats[res.Rows.Count];
            _TeamOrder = new SortedDictionary<string, int>();
            int i = 0;

            foreach (DataRow r in res.Rows)
            {
                _tst[i] = new TeamStats();
                _tst[i].name = r["Name"].ToString();
                _tst[i].offset = Convert.ToInt32(r["OFFSET"].ToString());
                _tst[i].winloss[0] = Convert.ToByte(r["WIN"].ToString());
                _tst[i].winloss[1] = Convert.ToByte(r["LOSS"].ToString());
                _tst[i].stats[PF] = Convert.ToUInt16(r["PF"].ToString());
                _tst[i].stats[PA] = Convert.ToUInt16(r["PA"].ToString());
                _tst[i].stats[FGM] = Convert.ToUInt16(r["FGM"].ToString());
                _tst[i].stats[FGA] = Convert.ToUInt16(r["FGA"].ToString());
                _tst[i].stats[TPM] = Convert.ToUInt16(r["TPM"].ToString());
                _tst[i].stats[TPA] = Convert.ToUInt16(r["TPA"].ToString());
                _tst[i].stats[FTM] = Convert.ToUInt16(r["FTM"].ToString());
                _tst[i].stats[FTA] = Convert.ToUInt16(r["FTA"].ToString());
                _tst[i].stats[OREB] = Convert.ToUInt16(r["OREB"].ToString());
                _tst[i].stats[DREB] = Convert.ToUInt16(r["DREB"].ToString());
                _tst[i].stats[STL] = Convert.ToUInt16(r["STL"].ToString());
                _tst[i].stats[TO] = Convert.ToUInt16(r["TOS"].ToString());
                _tst[i].stats[BLK] = Convert.ToUInt16(r["BLK"].ToString());
                _tst[i].stats[AST] = Convert.ToUInt16(r["AST"].ToString());
                _tst[i].stats[FOUL] = Convert.ToUInt16(r["FOUL"].ToString());

                _TeamOrder.Add(_tst[i].name, i);
                i++;
            }

            q = "select * from PlayoffTeams;";
            res = db.GetDataTable(q);
            
            i = 0;

            foreach (DataRow r in res.Rows)
            {
                _tst[i].pl_offset = Convert.ToInt32(r["OFFSET"].ToString());
                _tst[i].pl_winloss[0] = Convert.ToByte(r["WIN"].ToString());
                _tst[i].pl_winloss[1] = Convert.ToByte(r["LOSS"].ToString());
                _tst[i].pl_stats[PF] = Convert.ToUInt16(r["PF"].ToString());
                _tst[i].pl_stats[PA] = Convert.ToUInt16(r["PA"].ToString());
                _tst[i].pl_stats[FGM] = Convert.ToUInt16(r["FGM"].ToString());
                _tst[i].pl_stats[FGA] = Convert.ToUInt16(r["FGA"].ToString());
                _tst[i].pl_stats[TPM] = Convert.ToUInt16(r["TPM"].ToString());
                _tst[i].pl_stats[TPA] = Convert.ToUInt16(r["TPA"].ToString());
                _tst[i].pl_stats[FTM] = Convert.ToUInt16(r["FTM"].ToString());
                _tst[i].pl_stats[FTA] = Convert.ToUInt16(r["FTA"].ToString());
                _tst[i].pl_stats[OREB] = Convert.ToUInt16(r["OREB"].ToString());
                _tst[i].pl_stats[DREB] = Convert.ToUInt16(r["DREB"].ToString());
                _tst[i].pl_stats[STL] = Convert.ToUInt16(r["STL"].ToString());
                _tst[i].pl_stats[TO] = Convert.ToUInt16(r["TOS"].ToString());
                _tst[i].pl_stats[BLK] = Convert.ToUInt16(r["BLK"].ToString());
                _tst[i].pl_stats[AST] = Convert.ToUInt16(r["AST"].ToString());
                _tst[i].pl_stats[FOUL] = Convert.ToUInt16(r["FOUL"].ToString());

                _tst[i].calcAvg();
                i++;
            }

            q = "select * from GameResults ORDER BY Date DESC;";
            DataTable res2 = db.GetDataTable(q);

            _bshist = new List<BoxScoreEntry>(res.Rows.Count);
            i = 0;
            foreach (DataRow r in res2.Rows)
            {
                BoxScore bs = new BoxScore();
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

                BoxScoreEntry bse = new BoxScoreEntry(bs);
                bse.date = bs.gamedate;
                _bshist.Add(bse);
            }

            try
            {
                q = "select CurSeason from Misc limit 1;";
                res = db.GetDataTable(q);
                curSeason = Convert.ToInt32(res.Rows[0]["CurSeason"].ToString());
            }
            catch
            {
                curSeason = 1;
            }

            if (updateCombo)
            {
                cmbTeam1.Items.Clear();
                foreach (KeyValuePair<string, int> kvp in _TeamOrder)
                {
                    cmbTeam1.Items.Add(kvp.Key);
                }
            }

            return _tst;
        }

        private void btnLoadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (isTSTEmpty())
            {
                updateStatus("No file is loaded or the file currently loaded is empty");
                return;
            }

            if (!isCustom)
            {
                TeamStats[] temptst = new TeamStats[30];
                bshist = new List<BoxScoreEntry>();
                bool havePT = false;

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Team Stats Table (*.tst)|*.tst";
                ofd.InitialDirectory = AppDocsPath;
                ofd.Title = "Please select the TST file that you saved before the game...";
                ofd.ShowDialog();

                if (ofd.FileName == "") return;

                /*
                FileStream stream = File.Open(ofd.FileName, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

                for (int i = 0; i < 30; i++)
                {
                    temptst[i] = new TeamStats();
                    temptst[i] = (TeamStats)bf.Deserialize(stream);
                }
                pt = (PlayoffTree)bf.Deserialize(stream);
                stream.Close();
                */
                temptst = getCustomStats(ofd.FileName, ref TeamOrder, ref pt, ref bshist);

                bs = new BoxScore();
                boxScoreW bsW = new boxScoreW();
                bsW.ShowDialog();

                if (bs.done == false) return;

                int id1 = -1;
                int id2 = -1;

                if ((pt == null) || (pt.teams[0] == "Invalid"))
                {
                    id1 = TeamOrder[bs.Team1];
                    id2 = TeamOrder[bs.Team2];
                    havePT = false;
                }
                else
                {
                    for (int i = 0; i < 16; i++)
                    {
                        if (pt.teams[i] == bs.Team1)
                            id1 = TeamOrder[pt.teams[i]];
                        else if (pt.teams[i] == bs.Team2)
                            id2 = TeamOrder[pt.teams[i]];
                    }
                    havePT = true;
                }

                // Add win & loss
                if (bs.PTS1 > bs.PTS2)
                {
                    temptst[id1].winloss[0]++;
                    temptst[id2].winloss[1]++;
                }
                else
                {
                    temptst[id1].winloss[1]++;
                    temptst[id2].winloss[0]++;
                }
                // Add minutes played
                temptst[id1].stats[M] += 48;
                temptst[id2].stats[M] += 48;

                // Add Points For
                temptst[id1].stats[PF] += bs.PTS1;
                temptst[id2].stats[PF] += bs.PTS2;

                // Add Points Against
                temptst[id1].stats[PA] += bs.PTS2;
                temptst[id2].stats[PA] += bs.PTS1;

                //
                temptst[id1].stats[FGM] += bs.FGM1;
                temptst[id2].stats[FGM] += bs.FGM2;

                temptst[id1].stats[FGA] += bs.FGA1;
                temptst[id2].stats[FGA] += bs.FGA2;

                //
                temptst[id1].stats[TPM] += bs.TPM1;
                temptst[id2].stats[TPM] += bs.TPM2;

                //
                temptst[id1].stats[TPA] += bs.TPA1;
                temptst[id2].stats[TPA] += bs.TPA2;

                //
                temptst[id1].stats[FTM] += bs.FTM1;
                temptst[id2].stats[FTM] += bs.FTM2;

                //
                temptst[id1].stats[FTA] += bs.FTA1;
                temptst[id2].stats[FTA] += bs.FTA2;

                //
                temptst[id1].stats[OREB] += bs.OFF1;
                temptst[id2].stats[OREB] += bs.OFF2;

                //
                temptst[id1].stats[DREB] += Convert.ToUInt16(bs.REB1 - bs.OFF1);
                temptst[id2].stats[DREB] += Convert.ToUInt16(bs.REB2 - bs.OFF2);

                //
                temptst[id1].stats[STL] += bs.STL1;
                temptst[id2].stats[STL] += bs.STL2;

                //
                temptst[id1].stats[TO] += bs.TO1;
                temptst[id2].stats[TO] += bs.TO2;

                //
                temptst[id1].stats[BLK] += bs.BLK1;
                temptst[id2].stats[BLK] += bs.BLK2;

                //
                temptst[id1].stats[AST] += bs.AST1;
                temptst[id2].stats[AST] += bs.AST2;

                //
                temptst[id1].stats[FOUL] += bs.PF1;
                temptst[id2].stats[FOUL] += bs.PF2;

                ofd = new OpenFileDialog();
                ofd.Title = "Please select the Career file you want to update...";
                ofd.Filter = "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
                + "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                    "Create A Legend files (*.SMG)|*.SMG";
                if (Directory.Exists(SavesPath)) ofd.InitialDirectory = SavesPath;
                ofd.ShowDialog();

                if (ofd.FileName == "") return;
                string fn = ofd.FileName;
                TeamStats[] temp = StatsTracker.GetStats(fn, ref TeamOrder, ref pt, havePT);
                if (temp.Length == 1)
                {
                    MessageBox.Show("Couldn't get stats from " + Tools.getSafeFilename(fn) + ". Update failed.");
                    return;
                }

                // Check if Win/Loss remain the same
                if ((temp[id1].winloss.SequenceEqual(temptst[id1].winloss) == false) || (temp[id2].winloss.SequenceEqual(temptst[id2].winloss) == false))
                {
                    MessageBoxResult r = MessageBox.Show("Your updates to the saved team stats don't seem to be compatible with the save you've selected.\n" +
                        "Making these updates would mean that the Wins/Losses stats would be different than what NBA 2K12 has saved inside the file.\n\n" +
                        "Probable causes:\n\t1. You didn't save your Association and then the team stats in the tool right before the game started.\n" +
                        "\t2. You didn't save your Association right after the game ended.\n\n" +
                        "Make sure you're using the saved Team Stats from right before the game, and an Association save from right after the game ended.\n\n" +
                        "You can continue, but this may cause stat corruption.\nAre you sure you want to continue?",
                        "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
                    if (r == MessageBoxResult.No) return;
                }

                tst = temp;
                tst[id1].winloss = temptst[id1].winloss;
                tst[id2].winloss = temptst[id2].winloss;
                tst[id1].stats = temptst[id1].stats;
                tst[id2].stats = temptst[id2].stats;
                tst[id1].calcAvg();
                tst[id2].calcAvg();

                StatsTracker.updateSavegame(fn, tst, TeamOrder, pt);

                cmbTeam1.SelectedIndex = -1;
                //cmbTeam1.SelectedIndex = 0;
                cmbTeam1.SelectedItem = bs.Team1;
                txtFile.Text = ofd.FileName;

                BoxScoreEntry bse = new BoxScoreEntry(bs);
                bshist.Add(bse);

                MessageBox.Show("Team Stats updated in " + Tools.getSafeFilename(fn) + " succesfully!");
                BinaryWriter bw = new BinaryWriter(File.Open(AppDocsPath + Tools.getSafeFilename(ofd.FileName) + ".bsh", FileMode.Append));
                bw.Write("NST_BSH_FILE_START");
                writeBoxScoreHistory(bw);
                bw.Write("NST_BSH_FILE_END");
                bw.Close();
            }
            else
            {
                bs = new BoxScore();
                boxScoreW bsW = new boxScoreW();
                bsW.ShowDialog();

                if (bs.done == false) return;

                int id1 = -1;
                int id2 = -1;

                id1 = TeamOrder[bs.Team1];
                id2 = TeamOrder[bs.Team2];

                if (!bs.doNotUpdate)
                {
                    if (!bs.isPlayoff)
                    {
                        // Add win & loss
                        if (bs.PTS1 > bs.PTS2)
                        {
                            tst[id1].winloss[0]++;
                            tst[id2].winloss[1]++;
                        }
                        else
                        {
                            tst[id1].winloss[1]++;
                            tst[id2].winloss[0]++;
                        }
                        // Add minutes played
                        tst[id1].stats[M] += 48;
                        tst[id2].stats[M] += 48;

                        // Add Points For
                        tst[id1].stats[PF] += bs.PTS1;
                        tst[id2].stats[PF] += bs.PTS2;

                        // Add Points Against
                        tst[id1].stats[PA] += bs.PTS2;
                        tst[id2].stats[PA] += bs.PTS1;

                        //
                        tst[id1].stats[FGM] += bs.FGM1;
                        tst[id2].stats[FGM] += bs.FGM2;

                        tst[id1].stats[FGA] += bs.FGA1;
                        tst[id2].stats[FGA] += bs.FGA2;

                        //
                        tst[id1].stats[TPM] += bs.TPM1;
                        tst[id2].stats[TPM] += bs.TPM2;

                        //
                        tst[id1].stats[TPA] += bs.TPA1;
                        tst[id2].stats[TPA] += bs.TPA2;

                        //
                        tst[id1].stats[FTM] += bs.FTM1;
                        tst[id2].stats[FTM] += bs.FTM2;

                        //
                        tst[id1].stats[FTA] += bs.FTA1;
                        tst[id2].stats[FTA] += bs.FTA2;

                        //
                        tst[id1].stats[OREB] += bs.OFF1;
                        tst[id2].stats[OREB] += bs.OFF2;

                        //
                        tst[id1].stats[DREB] += Convert.ToUInt16(bs.REB1 - bs.OFF1);
                        tst[id2].stats[DREB] += Convert.ToUInt16(bs.REB2 - bs.OFF2);

                        //
                        tst[id1].stats[STL] += bs.STL1;
                        tst[id2].stats[STL] += bs.STL2;

                        //
                        tst[id1].stats[TO] += bs.TO1;
                        tst[id2].stats[TO] += bs.TO2;

                        //
                        tst[id1].stats[BLK] += bs.BLK1;
                        tst[id2].stats[BLK] += bs.BLK2;

                        //
                        tst[id1].stats[AST] += bs.AST1;
                        tst[id2].stats[AST] += bs.AST2;

                        //
                        tst[id1].stats[FOUL] += bs.PF1;
                        tst[id2].stats[FOUL] += bs.PF2;
                    }
                    else
                    {
                        // Add win & loss
                        if (bs.PTS1 > bs.PTS2)
                        {
                            tst[id1].pl_winloss[0]++;
                            tst[id2].pl_winloss[1]++;
                        }
                        else
                        {
                            tst[id1].pl_winloss[1]++;
                            tst[id2].pl_winloss[0]++;
                        }
                        // Add minutes played
                        tst[id1].pl_stats[M] += 48;
                        tst[id2].pl_stats[M] += 48;

                        // Add Points For
                        tst[id1].pl_stats[PF] += bs.PTS1;
                        tst[id2].pl_stats[PF] += bs.PTS2;

                        // Add Points Against
                        tst[id1].pl_stats[PA] += bs.PTS2;
                        tst[id2].pl_stats[PA] += bs.PTS1;

                        //
                        tst[id1].pl_stats[FGM] += bs.FGM1;
                        tst[id2].pl_stats[FGM] += bs.FGM2;

                        tst[id1].pl_stats[FGA] += bs.FGA1;
                        tst[id2].pl_stats[FGA] += bs.FGA2;

                        //
                        tst[id1].pl_stats[TPM] += bs.TPM1;
                        tst[id2].pl_stats[TPM] += bs.TPM2;

                        //
                        tst[id1].pl_stats[TPA] += bs.TPA1;
                        tst[id2].pl_stats[TPA] += bs.TPA2;

                        //
                        tst[id1].pl_stats[FTM] += bs.FTM1;
                        tst[id2].pl_stats[FTM] += bs.FTM2;

                        //
                        tst[id1].pl_stats[FTA] += bs.FTA1;
                        tst[id2].pl_stats[FTA] += bs.FTA2;

                        //
                        tst[id1].pl_stats[OREB] += bs.OFF1;
                        tst[id2].pl_stats[OREB] += bs.OFF2;

                        //
                        tst[id1].pl_stats[DREB] += Convert.ToUInt16(bs.REB1 - bs.OFF1);
                        tst[id2].pl_stats[DREB] += Convert.ToUInt16(bs.REB2 - bs.OFF2);

                        //
                        tst[id1].pl_stats[STL] += bs.STL1;
                        tst[id2].pl_stats[STL] += bs.STL2;

                        //
                        tst[id1].pl_stats[TO] += bs.TO1;
                        tst[id2].pl_stats[TO] += bs.TO2;

                        //
                        tst[id1].pl_stats[BLK] += bs.BLK1;
                        tst[id2].pl_stats[BLK] += bs.BLK2;

                        //
                        tst[id1].pl_stats[AST] += bs.AST1;
                        tst[id2].pl_stats[AST] += bs.AST2;

                        //
                        tst[id1].pl_stats[FOUL] += bs.PF1;
                        tst[id2].pl_stats[FOUL] += bs.PF2;
                    }

                    tst[id1].calcAvg();
                    tst[id2].calcAvg();
                }

                cmbTeam1.SelectedIndex = -1;
                cmbTeam1.SelectedItem = bs.Team1;

                if (bs.bshistid == -1)
                {
                    BoxScoreEntry bse = new BoxScoreEntry(bs);
                    bshist.Add(bse);
                }
                else
                {
                    bshist[bs.bshistid].bs = bs;
                }

                updateStatus("One or more Box Scores have been added/updated. Save the Team Stats file before continuing.");
            }
        }

        private void populateTeamsComboBox(SortedDictionary<string, int> TeamOrder, PlayoffTree pt)
        {
            bool done = false;

            cmbTeam1.Items.Clear();
            if (pt != null)
            {
                if (pt.teams[0] != "Invalid")
                {
                    List<string> newteams = new List<string>();
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
                foreach (KeyValuePair<string, int> kvp in TeamOrder)
                    cmbTeam1.Items.Add(kvp.Key);
            }
        }

        private static void checkForUpdates()
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                //webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                webClient.DownloadFileAsync(new Uri("http://students.ceid.upatras.gr/~aslanoglou/ctsversion.txt"), AppDocsPath + @"ctsversion.txt");
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
                updateInfo = File.ReadAllLines(AppDocsPath + @"ctsversion.txt");
                versionParts = updateInfo[0].Split('.');
            }
            catch
            {
                return;
            }
            string[] curVersionParts = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
            int[] iVP = new int[versionParts.Length];
            int[] iCVP = new int[versionParts.Length];
            for (int i = 0; i < versionParts.Length; i++)
            {
                iVP[i] = Convert.ToInt32(versionParts[i]);
                iCVP[i] = Convert.ToInt32(curVersionParts[i]);
                if (iCVP[i] > iVP[i]) break;
                if (iVP[i] > iCVP[i])
                {
                    MessageBoxResult mbr = MessageBox.Show("A new version is available! Would you like to download it?", "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Information);
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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Please select the Career file you want to reset the settings for...";
            ofd.Filter = "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
            + "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                "Create A Legend files (*.SMG)|*.SMG";
            if (Directory.Exists(SavesPath)) ofd.InitialDirectory = SavesPath;
            ofd.ShowDialog();
            if (ofd.FileName == "") return;

            string safefn = Tools.getSafeFilename(ofd.FileName);
            string SettingsFile = AppDocsPath + safefn + ".cfg";
            if (File.Exists(SettingsFile)) File.Delete(SettingsFile);
            MessageBox.Show("Settings for this file have been erased. You'll be asked to set them again next time you import it.");
        }

        private void _AnyTextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.SelectAll();
        }

        private void btnShowAvg_Click(object sender, RoutedEventArgs e)
        {
            string msg = StatsTracker.averagesAndRankings(cmbTeam1.SelectedItem.ToString(), tst, TeamOrder);
            if (msg != "")
            {
                copyableW cw = new copyableW(msg, cmbTeam1.SelectedItem.ToString(), TextAlignment.Center);
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

            int[][] rating = StatsTracker.calculateRankings(tst);
            if (rating.Length != 1)
            {
                string msg = StatsTracker.scoutReport(rating, id, cmbTeam1.SelectedItem.ToString());
                copyableW cw = new copyableW(msg, "Scouting Report", TextAlignment.Left);
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
                tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1], tst[id].stats[FGM], tst[id].stats[FGA], tst[id].stats[TPM], tst[id].stats[TPA],
                tst[id].stats[FTM], tst[id].stats[FTA], tst[
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
            string data2 = String.Format("{0:F3}", tst[id].averages[Wp]) + "," + String.Format("{0:F1}", tst[id].averages[Weff]);
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

            int[][] rankings = StatsTracker.calculateRankings(tst);

            string data3 = String.Format("{0:F3}", rankings[id][Wp]) + "," + String.Format("{0:F1}", rankings[id][Weff]);
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

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Comma-Separated Values file (*.csv)|*.csv";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.Title = "Select a file to save the CSV to...";
            sfd.ShowDialog();
            if (sfd.FileName == "") return;

            StreamWriter sw = new StreamWriter(sfd.FileName);
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
                tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1], tst[id].stats[FGM], tst[id].stats[FGA], tst[id].stats[TPM], tst[id].stats[TPA],
                tst[id].stats[FTM], tst[id].stats[FTA], tst[
             */
            string data1 = "";
            for (int id = 0; id < 30; id++)
            {
                if (tst[id].name == "") continue;

                data1 += (id + 1).ToString() + ",";
                foreach (KeyValuePair<string, int> kvp in TeamOrder)
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
                data1 += String.Format("{0:F3}", tst[id].averages[Wp]) + "," + String.Format("{0:F1}", tst[id].averages[Weff]);
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

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Comma-Separated Values file (*.csv)|*.csv";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.Title = "Select a file to save the CSV to...";
            sfd.ShowDialog();
            if (sfd.FileName == "") return;

            StreamWriter sw = new StreamWriter(sfd.FileName);
            sw.WriteLine(header1 + header2);
            sw.WriteLine(data1);
            sw.Close();
        }

        private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            btnSelect_Click(sender, e);
        }

        private void btnSaveCustomTeam_Click(object sender, RoutedEventArgs e)
        {
            int id = TeamOrder[cmbTeam1.SelectedItem.ToString()];
            tst[id].winloss[0] = Convert.ToByte(txtW1.Text);
            tst[id].winloss[1] = Convert.ToByte(txtL1.Text);
            tst[id].stats[PF] = Convert.ToUInt16(txtPF1.Text);
            tst[id].stats[PA] = Convert.ToUInt16(txtPA1.Text);
            tst[id].stats[FGM] = Convert.ToUInt16(txtFGM1.Text);
            tst[id].stats[FGA] = Convert.ToUInt16(txtFGA1.Text);
            tst[id].stats[TPM] = Convert.ToUInt16(txt3PM1.Text);
            tst[id].stats[TPA] = Convert.ToUInt16(txt3PA1.Text);
            tst[id].stats[FTM] = Convert.ToUInt16(txtFTM1.Text);
            tst[id].stats[FTA] = Convert.ToUInt16(txtFTA1.Text);
            tst[id].stats[OREB] = Convert.ToUInt16(txtOREB1.Text);
            tst[id].stats[DREB] = Convert.ToUInt16(txtDREB1.Text);
            tst[id].stats[STL] = Convert.ToUInt16(txtSTL1.Text);
            tst[id].stats[TO] = Convert.ToUInt16(txtTO1.Text);
            tst[id].stats[BLK] = Convert.ToUInt16(txtBLK1.Text);
            tst[id].stats[AST] = Convert.ToUInt16(txtAST1.Text);
            tst[id].stats[FOUL] = Convert.ToUInt16(txtFOUL1.Text);

            tst[id].calcAvg();
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(-1);
        }

        private void btnInject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Please select the Career file you want to update...";
            ofd.Filter = "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
            + "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                "Create A Legend files (*.SMG)|*.SMG";
            if (Directory.Exists(SavesPath)) ofd.InitialDirectory = SavesPath;
            ofd.ShowDialog();

            if (ofd.FileName == "") return;
            string fn = ofd.FileName;

            StatsTracker.prepareOffsets(fn, tst, ref TeamOrder, ref pt);

            TeamStats[] temp = StatsTracker.GetStats(fn, ref TeamOrder, ref pt);
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

                        if ((!temp[i].winloss.SequenceEqual(tst[i].winloss)) || (!temp[i].pl_winloss.SequenceEqual(tst[i].pl_winloss)))
                        {
                            incompatible = true;
                            break;
                        }
                    }
                }

                if (incompatible)
                {
                    MessageBoxResult r = MessageBox.Show("The file currently loaded seems incompatible with the NBA 2K save you're trying to save into." +
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

            

            StatsTracker.updateSavegame(fn, tst, TeamOrder, pt);
            updateStatus("Injected custom Team Stats into " + Tools.getSafeFilename(fn) + " successfully!");
            cmbTeam1.SelectedIndex = -1;
            cmbTeam1.SelectedIndex = 0;
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                askTeamW aw = new askTeamW(true, cmbTeam1.SelectedIndex);
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
            aboutW aw = new aboutW();
            aw.ShowDialog();
        }

        private void mnuFileGetRealStats_Click(object sender, RoutedEventArgs e)
        {
            string file = "";

            if (!String.IsNullOrWhiteSpace(txtFile.Text))
            {
                MessageBoxResult r = MessageBox.Show("This will overwrite the stats in the currently opened file. Are you sure?\n\nClick Yes to overwrite.\nClick No to create a new file automatically. Any unsaved changes to the current file will be lost.\nClick Cancel to return to the main window.", "NBA Stats Tracker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) file = currentDB;
                else if (r == MessageBoxResult.No) txtFile.Text = "";
                else return;

            }

            if (String.IsNullOrWhiteSpace(txtFile.Text))
            {
                file = AppDocsPath + "Real NBA Stats " + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + ".tst";
                if (File.Exists(file))
                {
                    if (App.realNBAonly)
                    {
                        MessageBoxResult r = MessageBox.Show("Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?", "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                            return;
                    }
                    else
                    {
                        MessageBoxResult r = MessageBox.Show("Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?", "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                        {
                            tst = getCustomStats(file, ref TeamOrder, ref pt, ref bshist);

                            cmbTeam1.SelectedIndex = -1;
                            cmbTeam1.SelectedIndex = 0;
                            txtFile.Text = file;
                            return;
                        }
                    }
                }
            }

            getRealStatsW grsw = new getRealStatsW();
            grsw.ShowDialog();
            if (realtst[0].name != "Canceled")
            {
                int len = realtst.GetLength(0);

                tst = new TeamStats[len];
                for (int i = 0; i < len; i++)
                {
                    tst[i] = new TeamStats();
                }

                tst = realtst;
                saveTeamStatsFile(file);
                cmbTeam1.SelectedIndex = -1;
                cmbTeam1.SelectedIndex = 0;
                txtFile.Text = file;
                tst = getCustomStats(file, ref TeamOrder, ref pt, ref bshist);
            }
        }

        private void btnCompareToReal_Click(object sender, RoutedEventArgs e)
        {
            TeamStats realteam = new TeamStats();

            if (File.Exists(AppDocsPath + cmbTeam1.SelectedItem.ToString() + ".rst"))
            {
                FileInfo fi = new FileInfo(AppDocsPath + cmbTeam1.SelectedItem.ToString() + ".rst");
                TimeSpan sinceLastModified = DateTime.Now - fi.LastWriteTime;
                if (sinceLastModified.Days >= 1)
                    realteam = StatsTracker.getRealStats(cmbTeam1.SelectedItem.ToString());
                else
                    try
                    {
                        realteam = StatsTracker.getRealStats(cmbTeam1.SelectedItem.ToString(), true);
                    }
                    catch
                    {
                        try
                        {
                            realteam = StatsTracker.getRealStats(cmbTeam1.SelectedItem.ToString());
                        }
                        catch
                        {
                            MessageBox.Show("An incomplete real stats file is present and locked in the disk. Please restart NBA Stats Tracker and try again.");
                        }
                    }
            }
            else
            {
                realteam = StatsTracker.getRealStats(cmbTeam1.SelectedItem.ToString());
            }
            TeamStats curteam = tst[TeamOrder[cmbTeam1.SelectedItem.ToString()]];

            versusW vw = new versusW(curteam, "Current", realteam, "Real");
            vw.ShowDialog();
        }

        private void btnCompareOtherFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select the TST file that has the team stats you want to compare to...";
            ofd.Filter = "Team Stats files (*.tst)|*.tst";
            ofd.InitialDirectory = AppDocsPath;
            ofd.ShowDialog();

            string file = ofd.FileName;
            if (file != "")
            {
                string team = cmbTeam1.SelectedItem.ToString();
                string safefn = Tools.getSafeFilename(file);
                SortedDictionary<string, int> _newTeamOrder = new SortedDictionary<string, int>();

                FileStream stream = File.Open(ofd.FileName, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

                TeamStats[] _newtst = new TeamStats[30];
                for (int i = 0; i < 30; i++)
                {
                    _newtst[i] = new TeamStats();
                    _newtst[i] = (TeamStats)bf.Deserialize(stream);
                    if (_newtst[i].name == "") continue;
                    try
                    {
                        _newTeamOrder.Add(_newtst[i].name, i);
                        _newtst[i].calcAvg();
                    }
                    catch
                    { }
                }

                TeamStats newteam = _newtst[_newTeamOrder[team]];
                TeamStats curteam = tst[TeamOrder[team]];

                versusW vw = new versusW(curteam, "Current", newteam, "Other");
                vw.ShowDialog();
            }
        }

        private void txtFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtFile.ScrollToHorizontalOffset(txtFile.GetRectFromCharacterIndex(txtFile.Text.Length).Right);
            currentDB = txtFile.Text;
        }

        private void btnTrends_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd1 = new OpenFileDialog();
            if (txtFile.Text == "")
            {
                ofd1.Title = "Select the TST file that has the current team stats...";
                ofd1.Filter = "Team Stats files (*.tst)|*.tst";
                ofd1.InitialDirectory = AppDocsPath;
                ofd1.ShowDialog();

                if (ofd1.FileName == "") return;

                tst = getCustomStats(ofd1.FileName, ref TeamOrder, ref pt, ref bshist, true);
                cmbTeam1.SelectedIndex = 0;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select the TST file that has the team stats you want to compare to...";
            ofd.Filter = "Team Stats files (*.tst)|*.tst";
            ofd.InitialDirectory = AppDocsPath;
            ofd.ShowDialog();

            if (ofd.FileName == "") return;

            string team = cmbTeam1.SelectedItem.ToString();
            int id = TeamOrder[team];

            TeamStats[] curTST = tst;

            SortedDictionary<string, int> oldTeamOrder = new SortedDictionary<string, int>();
            PlayoffTree oldPT = new PlayoffTree();
            IList<BoxScoreEntry> oldbshist = new List<BoxScoreEntry>();
            TeamStats[] oldTST = getCustomStats(ofd.FileName, ref oldTeamOrder, ref oldPT, ref oldbshist, false);

            Rankings curR = new Rankings(tst);
            Rankings oldR = new Rankings(oldTST);
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
                str = String.Format("Most improved in {7}, the {0}. They were #{1} ({4:F1}), climbing {3} places they are now at #{2} ({5:F1}), a {6:F1} {7} difference!",
                    tst[maxi].name, oldR.rankings[maxi][0], curR.rankings[maxi][0], diffrnk[maxi][0], oldTST[maxi].averages[0], tst[maxi].averages[0], diffavg[maxi][0], "PPG");
            }
            else
            {
                str = String.Format("Most improved in {7}, the {0}. They were #{1} ({4:F1}) and they are now at #{2} ({5:F1}), a {6:F1} {7} difference!",
                    tst[maxi].name, oldR.rankings[maxi][0], curR.rankings[maxi][0], diffrnk[maxi][0], oldTST[maxi].averages[0], tst[maxi].averages[0], diffavg[maxi][0], "PPG");
            }
            str += " ";
            str += String.Format("Taking this improvement apart, their FG% went from {0:F3} to {1:F3}, 3P% was {2:F3} and now is {3:F3}, and FT% is now at {4:F3}, having been at {5:F3}.",
                oldTST[maxi].averages[FGp], tst[maxi].averages[FGp], oldTST[maxi].averages[TPp], tst[maxi].averages[TPp], tst[maxi].averages[FTp], oldTST[maxi].averages[FTp]);

            if (curR.rankings[maxi][FGeff] <= 5)
            {
                str += " ";
                if (oldR.rankings[maxi][FGeff] > 20) str += "Huge leap in Field Goal efficiency. Back then they were on of the worst teams on the offensive end, now in the Top 5.";
                else if (oldR.rankings[maxi][FGeff] > 10) str += "An average offensive team turned great. From the middle of the pack, they are now in Top 5 in Field Goal efficiency.";
                else if (oldR.rankings[maxi][FGeff] > 5) str += "They were already hot, and they're just getting better. Moving on up from Top 10 in FGeff, to Top 5.";
                else str += "They just know how to stay hot at the offensive end. Still in the Top 5 of the most efficient teams from the floor.";
            }
            if (curR.rankings[maxi][FTeff] <= 5) str += " They're not afraid of contact, and they know how to make the most from the line. Top 5 in Free Throw efficiency.";
            if (diffavg[maxi][APG] > 0) str += String.Format(" They are getting better at finding the open man with a timely pass. {0:F1} improvement in assists per game.", diffavg[maxi][APG]);
            if (diffavg[maxi][RPG] > 0) str += String.Format(" Their additional rebounds have helped as well.");
            if (diffavg[maxi][TPG] < 0) str += String.Format(" Also taking better care of the ball, making {0:F1} less turnovers per game.", -diffavg[maxi][TPG]);

            ///////////////////////////
            str += "$";
            ///////////////////////////

            string team2 = tst[mini].name;
            if (diffrnk[mini][0] < 0)
            {
                str += String.Format("On the other end, the {0} have lost some of their scoring power. They were #{1} ({4:F1}), dropping {3} places they are now at #{2} ({5:F1}).",
                    tst[mini].name, oldR.rankings[mini][0], curR.rankings[mini][0], -diffrnk[mini][0], oldTST[mini].averages[0], tst[mini].averages[0]);
            }
            else
            {
                str += String.Format("On the other end, the {0} have lost some of their scoring power. They were #{1} ({4:F1}) and are now in #{2} ({5:F1}). Guess even that {6:F1} PPG drop wasn't enough to knock them down!",
                    tst[mini].name, oldR.rankings[mini][0], curR.rankings[mini][0], -diffrnk[mini][0], oldTST[mini].averages[0], tst[mini].averages[0], -diffavg[mini][0]);
            }
            str += " ";
            str += String.Format("So why has this happened? Their FG% went from {0:F3} to {1:F3}, 3P% was {2:F3} and now is {3:F3}, and FT% is now at {4:F3}, having been at {5:F3}.",
                 oldTST[mini].averages[FGp], tst[mini].averages[FGp], oldTST[mini].averages[TPp], tst[mini].averages[TPp], tst[mini].averages[FTp], oldTST[mini].averages[FTp]);
            if (diffavg[mini][TPG] > 0) str += String.Format(" You can't score as many points when you commit turnovers; they've seen them increase by {0:F1} per game.", diffavg[mini][TPG]);

            trendsW tw = new trendsW(str, team1, team2);
            tw.ShowDialog();
        }

        private int[][] calculateDifferenceRanking(Rankings curR, Rankings newR)
        {
            int[][] diff = new int[30][];
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
            float[][] diff = new float[30][];
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
        public static string AppDocsPath1
        {
            get
            {
                return AppDocsPath;
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            teamOverviewW tow = new teamOverviewW(tst);
            tow.ShowDialog();
        }

        private void mnuHistoryBoxScores_Click(object sender, RoutedEventArgs e)
        {
            bs = new BoxScore();
            boxScoreW bsw = new boxScoreW(boxScoreW.Mode.View);
            bsw.ShowDialog();

            if (bs.bshistid != -1)
            {
                if (bs.done)
                {
                    bshist[bs.bshistid].bs = bs;
                    bshist[bs.bshistid].mustUpdate = true;

                    updateStatus("One or more Box Scores have been updated. Save the Team Stats file before continuing.");
                }
            }
        }

        private void btnTeamOverview_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("You need to create a team or import stats before using the Analysis features.");
                return;
            }

            dispatcherTimer.Stop();
            teamOverviewW tow = new teamOverviewW(tst);
            tow.ShowDialog();
            dispatcherTimer.Start();
        }

        private void btnOpenCustom_Click(object sender, RoutedEventArgs e)
        {
            mnuFileOpenCustom_Click(null, null);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 3);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            status.FontWeight = FontWeights.Normal;
            status.Content = "Ready";
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
                btnSaveTS_Click(null, null);
            }
            else
            {
                saveTeamStatsFile(currentDB);
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
            leagueOverviewW low = new leagueOverviewW(tst);
            low.ShowDialog();
            dispatcherTimer.Start();
        }

        private void mnuFileNew_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Team Stats Table (*.tst)|*.tst";
            sfd.InitialDirectory = AppDocsPath;
            sfd.ShowDialog();

            if (sfd.FileName == "") return;

            db = new SQLiteDatabase(sfd.FileName);

            prepareNewDB(db);

            tst = new TeamStats[1];
            tst[0] = new TeamStats("$$NewDB");

            txtFile.Text = sfd.FileName;

            //
            // tst = new TeamStats[2];
            
        }

        private bool isTSTEmpty()
        {
            try
            {
                if (tst[0].name == "$$NewDB") return true;
                else return false;
            }
            catch
            {
                tst = new TeamStats[1];
                tst[0] = new TeamStats("$$NewDB");
                return true;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB)) return;

            addInfo = "";
            addW aw = new addW();
            aw.ShowDialog();

            if (!String.IsNullOrEmpty(addInfo))
            {
                string[] parts = Regex.Split(addInfo, "\r\n");
                List<string> newTeams = new List<string>();
                foreach (string s in parts)
                {
                    if (!String.IsNullOrWhiteSpace(s))
                        newTeams.Add(s);
                }

                int oldlen = tst.GetLength(0);
                if (isTSTEmpty()) oldlen--;

                Array.Resize(ref tst, oldlen + newTeams.Count);

                for (int i = 0; i < newTeams.Count; i++)
                {
                    tst[oldlen + i] = new TeamStats(newTeams[i]);
                    TeamOrder.Add(newTeams[i], oldlen + i);
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
                MessageBoxResult r = MessageBox.Show("Are you sure you want to do this? All current regular season & playoff\n"
                                                   + "stats for all teams will be erased permanently. This is an irreversible\n"
                                                   + "action. Box Scores will be retained.", "NBA Stats Tracker", MessageBoxButton.YesNo,
                                                   MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    MainWindow.curSeason++;
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
                }
                updateStatus("New season started. You should save the database.");
            }
        }
    }
}
