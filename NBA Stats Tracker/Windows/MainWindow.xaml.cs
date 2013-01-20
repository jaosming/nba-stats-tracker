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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using LeftosCommonLibrary;
using LeftosCommonLibrary.BeTimvwFramework;
using Microsoft.Win32;
using NBA_Stats_Tracker.Data.BoxScores;
using NBA_Stats_Tracker.Data.Misc;
using NBA_Stats_Tracker.Data.Players;
using NBA_Stats_Tracker.Data.SQLiteIO;
using NBA_Stats_Tracker.Data.Teams;
using NBA_Stats_Tracker.Helper.Miscellaneous;
using NBA_Stats_Tracker.Helper.WindowsForms;
using NBA_Stats_Tracker.Interop.BR;
using NBA_Stats_Tracker.Interop.REDitor;
using SQLite_Database;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

#endregion

namespace NBA_Stats_Tracker.Windows
{
    /// <summary>
    ///     The Main window, offering quick access to the program's features
    /// </summary>
    public partial class MainWindow
    {
        public static readonly string AppDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                                    @"\NBA Stats Tracker\";

        public static readonly string AppTempPath = AppDocsPath + @"Temp\";
        public static string SavesPath = "";
        public static readonly string AppPath = Environment.CurrentDirectory + "\\";

        public static string input = "";

        public static MainWindow mwInstance;

        public static Dictionary<int, TeamStats> tst = new Dictionary<int, TeamStats>();
        public static Dictionary<int, TeamStats> tstopp = new Dictionary<int, TeamStats>();
        public static Dictionary<int, PlayerStats> pst = new Dictionary<int, PlayerStats>();
        public static List<BoxScoreEntry> bshist = new List<BoxScoreEntry>();
        public static Dictionary<int, Dictionary<string, TeamStats>> splitTeamStats = new Dictionary<int, Dictionary<string, TeamStats>>();

        public static Dictionary<int, Dictionary<string, PlayerStats>> splitPlayerStats =
            new Dictionary<int, Dictionary<string, PlayerStats>>();

        public static Dictionary<int, Dictionary<int, UInt16[]>> seasonHighs = new Dictionary<int, Dictionary<int, ushort[]>>();

        public static TeamRankings SeasonTeamRankings;
        public static TeamRankings PlayoffTeamRankings;
        public static PlayerRankings SeasonPlayerRankings;
        public static PlayerRankings PlayoffPlayerRankings;
        public static Timeframe tf = new Timeframe(0);

        private static readonly Dictionary<int, TeamStats> realtst = new Dictionary<int, TeamStats>();
        public static TeamBoxScore bs;
        public static string currentDB = "";
        public static string addInfo;
        private static int _curSeason;
        public static List<Division> Divisions = new List<Division>();
        public static List<Conference> Conferences = new List<Conference>();

        public static int gameLength = 48;
        public static int seasonLength = 82;

        public static readonly ObservableCollection<KeyValuePair<int, string>> SeasonList =
            new ObservableCollection<KeyValuePair<int, string>>();

        public static List<SortableBindingList<PlayerBoxScore>> pbsLists;
        public static BoxScoreEntry tempbse;

        public static SortedDictionary<string, int> TeamOrder;

        public static List<Dictionary<string, string>> selectedTeams;
        public static bool selectedTeamsChanged;

        /// <summary>
        ///     Teams participating in the Western Conference of the NBA. Used to filter teams in the Playoff Tree window.
        /// </summary>
        public static readonly List<string> West = new List<string>
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

        public static SQLiteDatabase db;
        public static bool loadingSeason;
        private static bool showUpdateMessage;
        public static Dictionary<string, string> imageDict = new Dictionary<string, string>();

        public static RoutedCommand cmndImport = new RoutedCommand();
        public static RoutedCommand cmndOpen = new RoutedCommand();
        public static RoutedCommand cmndSave = new RoutedCommand();
        public static RoutedCommand cmndExport = new RoutedCommand();

        private static List<string> notables = new List<string>();

        public static Dictionary<string, string> DisplayNames;
        public static string teamsT;
        public static string pl_teamsT;
        public static string oppT;
        public static string pl_oppT;
        public static string playersT;
        public static string pl_playersT;
        private DispatcherTimer dispatcherTimer;
        private DispatcherTimer marqueeTimer;
        private int notableIndex;
        private double progress;
        private Semaphore sem;
        private BackgroundWorker worker1 = new BackgroundWorker();
        public static PlayerRankings SeasonLeadersRankings;
        public static PlayerRankings PlayoffsLeadersRankings;
        public static bool IsImperial = true;
        public static string RatingsGPPctSetting, RatingsMPGSetting, MyLeadersGPPctSetting, MyLeadersMPGSetting;
        public static double RatingsGPPctRequired, MyLeadersGPPctRequired;
        public static float RatingsMPGRequired, MyLeadersMPGRequired;
        public static string LeadersPrefSetting;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class.
        ///     Creates the program's documents directories if needed, initializes structures, and loads the settings from registry.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            mwInstance = this;
            bs = new TeamBoxScore();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            //btnInject.Visibility = Visibility.Hidden;
#if DEBUG
            btnTest.Visibility = Visibility.Visible;
#else
            btnTest.Visibility = Visibility.Hidden;
            #endif
            
            if (Directory.Exists(AppDocsPath) == false)
                Directory.CreateDirectory(AppDocsPath);
            if (Directory.Exists(AppTempPath) == false)
                Directory.CreateDirectory(AppTempPath);

            tst[0] = new TeamStats("$$NewDB");
            tstopp[0] = new TeamStats("$$NewDB");

            for (int i = 0; i < 30; i++)
            {
                realtst[i] = new TeamStats();
            }

            //teamOrder = StatsTracker.setTeamOrder("Mode 0");
            TeamOrder = new SortedDictionary<string, int>();

            RegistryKey rk = null;

            try
            {
                rk = Registry.CurrentUser;
            }
            catch (Exception ex)
            {
                App.errorReport(ex, "Registry.CurrentUser");
            }

            Debug.Assert(rk != null, "rk != null");
            rk = rk.OpenSubKey(@"SOFTWARE\2K Sports\NBA 2K12");
            if (rk == null)
            {
                SavesPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\2K Sports\NBA 2K12\Saves\";
            }
            else
            {
                try
                {
                    SavesPath = rk.GetValue("Saves").ToString();
                }
                catch (Exception)
                {
                    SavesPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\2K Sports\NBA 2K12\Saves\";
                }
            }
            
            // TODO: Re-enable downloading NBA stats when possible
            mnuFileGetRealStats.IsEnabled = false;
            btnDownloadBoxScore.IsEnabled = false;
            btnGrabNBAStats.IsEnabled = false;
            //

            if (App.realNBAonly)
            {
                // TODO: Re-enable downloading NBA stats when possible
                /*
                mnuFileGetRealStats_Click(null, null);
                MessageBox.Show("Nothing but net! Thanks for using NBA Stats Tracker!");
                Environment.Exit(-1);
                */
                //
                MessageBox.Show("This feature is temporarily disabled. Sorry for the inconvenience.", "NBA Stats Tracker",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(-1);
            }
            else
            {
                int importSetting = Misc.GetRegistrySetting("NBA2K12ImportMethod", 0);
                if (importSetting == 0)
                {
                    mnuOptionsImportREditor.IsChecked = true;
                }
                else
                {
                    Misc.SetRegistrySetting("NBA2K12ImportMethod", 0);
                    mnuOptionsImportREditor.IsChecked = true;
                }

                int ExportTeamsOnly = Misc.GetRegistrySetting("ExportTeamsOnly", 1);
                mnuOptionsExportTeamsOnly.IsChecked = ExportTeamsOnly == 1;

                int CompatibilityCheck = Misc.GetRegistrySetting("CompatibilityCheck", 1);
                mnuOptionsCompatibilityCheck.IsChecked = CompatibilityCheck == 1;

                IsImperial = Misc.GetRegistrySetting("IsImperial", 1) == 1;
                mnuOptionsIsImperial.IsChecked = IsImperial;

                // Displays a message to urge the user to donate at the 50th start of the program.
                int TimesStarted = Misc.GetRegistrySetting("TimesStarted", -1);
                if (TimesStarted == -1)
                    Misc.SetRegistrySetting("TimesStarted", 1);
                else if (TimesStarted <= 50)
                {
                    if (TimesStarted == 50)
                    {
                        MessageBoxResult r =
                            MessageBox.Show(
                                "Hey there! This is a friendly reminder from the creator of NBA Stats Tracker.\n\n" +
                                "You seem to like using NBA Stats Tracker a lot, and I'm sure you enjoy the fact that it's free. " +
                                "However, if you believe that I deserve your support and you want to help me to continue my studies, " +
                                "as well as continue developing and supporting NBA Stats Tracker, you can always donate!\n\n" +
                                "Even a small amount can help a lot!\n\n" + "Would you like to find out how you can donate?\n\n" +
                                "Clicking Cancel will make sure this message never shows up again.",
                                "NBA Stats Tracker - A friendly reminder", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
                        if (r == MessageBoxResult.Yes)
                        {
                            mnuHelpDonate_Click(null, null);
                        }
                        else if (r == MessageBoxResult.No)
                        {
                            TimesStarted = -1;
                        }
                    }
                    Misc.SetRegistrySetting("TimesStarted", TimesStarted + 1);
                }
            }

            #region Keyboard Shortcuts

            cmndImport.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(cmndImport, btnImport2K12_Click));

            cmndExport.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(cmndExport, btnExport2K12_Click));

            cmndOpen.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(cmndOpen, mnuFileOpen_Click));

            cmndSave.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(cmndSave, btnSaveCurrentSeason_Click));

            #endregion

            //prepareImageCache();
        }

        public static int curSeason
        {
            get { return _curSeason; }
            set
            {
                try
                {
                    _curSeason = value;
                    mwInstance.cmbSeasonNum.SelectedValue = curSeason;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        ///     TODO: To be used to build a dictionary of all available images for teams and players to use throughout the program
        /// </summary>
        private static void prepareImageCache()
        {
            string curTeamsPath = AppPath + @"Images\Teams\Current";
            string[] curTeamsImages = Directory.GetFiles(curTeamsPath);
            foreach (string file in curTeamsImages)
            {
                imageDict.Add(Path.GetFileNameWithoutExtension(file), Path.GetFullPath(file));
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnImport2K12 control.
        ///     Asks the user for the folder containing the NBA 2K12 save (in the case of the old method), or the REDitor-exported CSV files.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnImport2K12_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB))
                return;

            if (curSeason != SQLiteIO.getMaxSeason(currentDB))
            {
                if (
                    MessageBox.Show(
                        "Note that the currently selected season isn't the latest one in the database. " +
                        "Are you sure you want to import into this season?",
                        "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            if (tf.isBetween)
            {
                tf = new Timeframe(curSeason);
                UpdateAllData();
            }

            var fbd = new FolderBrowserDialog
                        {
                            Description = "Select folder with REditor-exported CSVs",
                            ShowNewFolderButton = false,
                            SelectedPath = Misc.GetRegistrySetting("LastImportDir", "")
                        };
            DialogResult dr = fbd.ShowDialog(this.GetIWin32Window());

            if (dr != System.Windows.Forms.DialogResult.OK)
                return;

            if (fbd.SelectedPath == "")
                return;

            Misc.SetRegistrySetting("LastImportDir", fbd.SelectedPath);

            int result = REDitor.ImportAll(ref tst, ref tstopp, ref TeamOrder, ref pst, fbd.SelectedPath);

            if (result != 0)
            {
                MessageBox.Show("Import failed! Please reload your database immediatelly to avoid saving corrupt data.",
                                "NBA Stats Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            updateStatus("NBA 2K12 stats imported successfully! Verify that you want this by saving the current season.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuFileSaveAs control.
        ///     Allows the user to save the database to a different file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(currentDB))
                return;

            var sfd = new SaveFileDialog {Filter = "NST Database (*.tst)|*.tst", InitialDirectory = AppDocsPath};
            sfd.ShowDialog();

            if (sfd.FileName == "")
                return;

            string file = sfd.FileName;

            if (!SQLiteIO.SaveDatabaseAs(file))
                return;
            updateStatus("All seasons saved successfully.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuFileOpen control.
        ///     Opens a database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            loadingSeason = true;
            tst = new Dictionary<int, TeamStats>();
            TeamOrder = new SortedDictionary<string, int>();
            bshist = new List<BoxScoreEntry>();

            var ofd = new OpenFileDialog
                      {
                          Filter = "NST Database (*.tst)|*.tst",
                          InitialDirectory = AppDocsPath,
                          Title = "Please select the TST file that you want to edit..."
                      };
            ofd.ShowDialog();

            if (ofd.FileName == "")
                return;

            PopulateSeasonCombo(ofd.FileName);

            txtFile.Text = ofd.FileName;
            currentDB = txtFile.Text;

            LoadRatingsCriteria();
            LoadMyLeadersCriteria();
            bool preferNBALeaders = LeadersPrefSetting == "NBA";
            mwInstance.mnuMiscPreferNBALeaders.IsChecked = preferNBALeaders;
            mwInstance.mnuMiscPreferMyLeaders.IsChecked = !preferNBALeaders;

            SQLiteIO.LoadSeason();

            txtFile.Text = ofd.FileName;

            gameLength = SQLiteIO.GetSetting("Game Length", 48);
            seasonLength = SQLiteIO.GetSetting("Season Length", 82);

            UpdateNotables();

            if (notables.Count > 0)
            {
                marqueeTimer.Start();
            }

            updateStatus(String.Format("{0} teams & {1} players loaded successfully!", tst.Count, pst.Count));
            loadingSeason = false;

            mnuTools.IsEnabled = true;
            grdAnalysis.IsEnabled = true;
            grdUpdate.IsEnabled = true;
        }

        public static void LoadMyLeadersCriteria()
        {
            MyLeadersGPPctSetting = SQLiteIO.GetSetting("MyLeadersGPPct", "-1");
            MyLeadersGPPctRequired = Convert.ToDouble(MyLeadersGPPctSetting);
            MyLeadersMPGSetting = SQLiteIO.GetSetting("MyLeadersMPG", "-1");
            MyLeadersMPGRequired = Convert.ToSingle(MyLeadersMPGSetting);
            LeadersPrefSetting = SQLiteIO.GetSetting("Leaders", "NBA");
        }

        public static void LoadRatingsCriteria()
        {
            RatingsGPPctSetting = SQLiteIO.GetSetting("RatingsGPPct", "-1");
            RatingsGPPctRequired = Convert.ToDouble(RatingsGPPctSetting);
            RatingsMPGSetting = SQLiteIO.GetSetting("RatingsMPG", "-1");
            RatingsMPGRequired = Convert.ToSingle(RatingsMPGSetting);
        }

        private void marqueeTimer_Tick(object sender, EventArgs e)
        {
            if (notables.Count == 0)
            {
                txbMarquee.Text = "";
                marqueeTimer.Stop();
                return;
            }

            if (notableIndex < notables.Count - 1)
                notableIndex++;
            else
                notableIndex = 0;

            txbMarquee.Text = notables[notableIndex];
        }

        /// <summary>
        ///     Changes the current season.
        /// </summary>
        /// <param name="curSeason">The ID of the season to change to.</param>
        public static void ChangeSeason(int curSeason)
        {
            MainWindow.curSeason = curSeason;
            mwInstance.cmbSeasonNum.SelectedValue = MainWindow.curSeason.ToString();
            int maxSeason = SQLiteIO.getMaxSeason(currentDB);
            teamsT = "Teams" + SQLiteIO.AddSuffix(curSeason, maxSeason);
            pl_teamsT = "PlayoffTeams" + SQLiteIO.AddSuffix(curSeason, maxSeason);
            oppT = "Opponents" + SQLiteIO.AddSuffix(curSeason, maxSeason);
            pl_oppT = "PlayoffOpponents" + SQLiteIO.AddSuffix(curSeason, maxSeason);
            playersT = "Players" + SQLiteIO.AddSuffix(curSeason, maxSeason);
            pl_playersT = "PlayoffPlayers" + SQLiteIO.AddSuffix(curSeason, maxSeason);
        }

        /// <summary>
        ///     Handles the Click event of the btnLoadUpdate control.
        ///     Opens the Box Score window to allow the user to update the team stats by entering a box score.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnLoadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.isTSTEmpty())
            {
                updateStatus("No file is loaded or the file currently loaded is empty");
                return;
            }

            bs = new TeamBoxScore();
            var bsW = new BoxScoreWindow();
            bsW.ShowDialog();

            ParseBoxScoreResult();
        }

        /// <summary>
        ///     Parses the local box score instance; adds the stats to the according teams and players and adds the box score to the box score history.
        /// </summary>
        private void ParseBoxScoreResult()
        {
            if (bs.done == false)
                return;

            int id1 = TeamOrder[bs.Team1];
            int id2 = TeamOrder[bs.Team2];

            SQLiteIO.LoadSeason(bs.SeasonNum);

            List<PlayerBoxScore> list = pbsLists.SelectMany(pbsList => pbsList).ToList();

            if (!bs.doNotUpdate)
            {
                TeamStats.AddTeamStatsFromBoxScore(bs, ref tst, ref tstopp, id1, id2);

                foreach (PlayerBoxScore pbs in list)
                {
                    if (pbs.PlayerID == -1)
                        continue;
                    pst[pbs.PlayerID].AddBoxScore(pbs, bs.isPlayoff);
                }
            }

            if (bs.bshistid == -1)
            {
                var bse = new BoxScoreEntry(bs, bs.gamedate, list);
                bshist.Add(bse);
            }
            else
            {
                bshist[bs.bshistid].bs = bs;
            }
            
            SQLiteIO.saveSeasonToDatabase(currentDB, tst, tstopp, pst, curSeason, SQLiteIO.getMaxSeason(currentDB));
            UpdateAllData();

            updateStatus("One or more Box Scores have been added/updated. Database saved.");
        }

        /// <summary>
        ///     Checks for software updates asynchronously.
        /// </summary>
        /// <param name="showMessage">
        ///     if set to <c>true</c>, a message will be shown even if no update is found.
        /// </param>
        public static void CheckForUpdates(bool showMessage = false)
        {
            showUpdateMessage = showMessage;
            try
            {
                var webClient = new WebClient();
                string updateUri = "http://students.ceid.upatras.gr/~aslanoglou/nstversion.txt";
                if (!showMessage)
                {
                    webClient.DownloadFileCompleted += CheckForUpdatesCompleted;
                    webClient.DownloadFileAsync(new Uri(updateUri), AppDocsPath + @"nstversion.txt");
                }
                else
                {
                    webClient.DownloadFile(new Uri(updateUri), AppDocsPath + @"nstversion.txt");
                    CheckForUpdatesCompleted(null, null);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        ///     Checks the downloaded version file to see if there's a newer version, and displays a message if needed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        ///     The <see cref="AsyncCompletedEventArgs" /> instance containing the event data.
        /// </param>
        private static void CheckForUpdatesCompleted(object sender, AsyncCompletedEventArgs e)
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
                if (iCVP[i] > iVP[i])
                    break;
                if (iVP[i] > iCVP[i])
                {
                    string changelog = "\n\nVersion " + String.Join(".", versionParts);
                    try
                    {
                        for (int j = 2; j < updateInfo.Length; j++)
                        {
                            changelog += "\n" + updateInfo[j].Replace('\t', ' ');
                        }
                    }
                    catch
                    {
                    }
                    MessageBoxResult mbr = MessageBox.Show("A new version is available! Would you like to download it?" + changelog,
                                                           "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (mbr == MessageBoxResult.Yes)
                    {
                        Process.Start(updateInfo[1]);
                        break;
                    }
                    return;
                }
            }
            if (showUpdateMessage)
                MessageBox.Show("No updates found!");
        }

        /// <summary>
        ///     Handles the Click event of the btnEraseSettings control.
        ///     Allows the user to erase the saved settings file for a particular NBA 2K save.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnEraseSettings_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                      {
                          Title = "Please select the Career file you want to reset the settings for...",
                          Filter =
                              "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|" +
                              "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|" +
                              "Create A Legend files (*.SMG)|*.SMG"
                      };
            if (Directory.Exists(SavesPath))
                ofd.InitialDirectory = SavesPath;
            ofd.ShowDialog();
            if (ofd.FileName == "")
                return;

            string safefn = Tools.getSafeFilename(ofd.FileName);
            string SettingsFile = AppDocsPath + safefn + ".cfg";
            if (File.Exists(SettingsFile))
                File.Delete(SettingsFile);
            MessageBox.Show("Settings for this file have been erased. You'll be asked to set them again next time you import it.");
        }

        /// <summary>
        ///     Exports the current league-wide team stats to a tab-separated values formatted file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnLeagueTSV_Click(object sender, RoutedEventArgs e)
        {
            const string header1 = "\tTeam\tGP\tW\tL\tPF\tPA\tFGM\tFGA\t3PM\t3PA\tFTM\tFTA\tOREB\tDREB\tSTL\tTO\tBLK\tAST\tFOUL\t";
            //string header2 = "Team\tW%\tWeff\tPPG\tPAPG\tFG%\tFGeff\t3P%\t3Peff\tFT%\tFTeff\tRPG\tORPG\tDRPG\tSPG\tBPG\tTPG\tAPG\tFPG";
            const string header2 = "W%\tWeff\tPPG\tPAPG\tFG%\tFGeff\t3P%\t3Peff\tFT%\tFTeff\tRPG\tORPG\tDRPG\tSPG\tBPG\tTPG\tAPG\tFPG";
            /*
            string data = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}", 
                tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1], tst[id].stats[t.FGM], tst[id].stats[t.FGA], tst[id].stats[t.TPM], tst[id].stats[t.TPA],
                tst[id].stats[t.FTM], tst[id].stats[t.FTA], tst[
             */

            var sfd = new SaveFileDialog
                      {
                          Filter = "Tab-Separated Values file (*.tsv)|*.tsv",
                          InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                          Title = "Export To TSV"
                      };
            sfd.ShowDialog();
            if (sfd.FileName == "")
                return;

            string data1 = "";
            for (int id = 0; id < tst.Count; id++)
            {
                if (tst[id].name == "")
                    continue;

                data1 += (id + 1).ToString() + "\t";
                foreach (var kvp in TeamOrder)
                {
                    if (kvp.Value == id)
                    {
                        data1 += kvp.Key + "\t";
                        break;
                    }
                }
                data1 += String.Format("{0}\t{1}\t{2}", tst[id].getGames(), tst[id].winloss[0], tst[id].winloss[1]);
                for (int j = 1; j <= 16; j++)
                {
                    if (j != 3)
                    {
                        data1 += "\t" + tst[id].stats[j].ToString();
                    }
                }
                data1 += "\t";
                data1 += String.Format("{0:F3}", tst[id].averages[t.Wp]) + "\t" + String.Format("{0:F1}", tst[id].averages[t.Weff]);
                for (int j = 0; j <= 15; j++)
                {
                    switch (j)
                    {
                        case 2:
                        case 4:
                        case 6:
                            data1 += String.Format("\t{0:F3}", tst[id].averages[j]);
                            break;
                        default:
                            data1 += String.Format("\t{0:F1}", tst[id].averages[j]);
                            break;
                    }
                }
                data1 += "\n";
            }

            var sw = new StreamWriter(sfd.FileName);
            sw.WriteLine(header1 + header2);
            sw.WriteLine(data1);
            sw.Close();
        }

        /// <summary>
        ///     Handles the Click event of the mnuExit control.
        ///     Plans world domination and reticulates splines.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(-1);
        }

        /// <summary>
        ///     Handles the Click event of the btnExport2K12 control.
        ///     Exports the current team and player stats to an NBA 2K save.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnExport2K12_Click(object sender, RoutedEventArgs e)
        {
            if (tst.Count != 30)
            {
                MessageBox.Show("You can't export a database that has less/more than 30 teams to an NBA 2K12 save.");
                return;
            }

            if (curSeason != SQLiteIO.getMaxSeason(currentDB))
            {
                if (
                    MessageBox.Show(
                        "Note that the currently selected season isn't the latest one in the database. " +
                        "Are you sure you want to export from this season?",
                        "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            if (tf.isBetween)
            {
                tf = new Timeframe(curSeason);
                UpdateAllData();
            }
            
            var fbd = new FolderBrowserDialog
                        {
                            Description = "Select folder with REditor-exported CSVs",
                            ShowNewFolderButton = false,
                            SelectedPath = Misc.GetRegistrySetting("LastExportDir", "")
                        };
            DialogResult dr = fbd.ShowDialog(this.GetIWin32Window());

            if (dr != System.Windows.Forms.DialogResult.OK)
                return;

            if (fbd.SelectedPath == "")
                return;

            Misc.SetRegistrySetting("LastExportDir", fbd.SelectedPath);

            if (mnuOptionsCompatibilityCheck.IsChecked)
            {
                var temptst = new Dictionary<int, TeamStats>();
                var temptstopp = new Dictionary<int, TeamStats>();
                var temppst = new Dictionary<int, PlayerStats>();
                int result = REDitor.ImportAll(ref temptst, ref temptstopp, ref TeamOrder, ref temppst, fbd.SelectedPath, true);

                if (result != 0)
                {
                    MessageBox.Show("Export failed.");
                    return;
                }

                bool incompatible = false;

                if (temptst.Count != tst.Count)
                    incompatible = true;
                else
                {
                    for (int i = 0; i < temptst.Count; i++)
                    {
                        if (temptst[i].name != tst[i].name)
                        {
                            incompatible = true;
                            break;
                        }

                        if ((!temptst[i].winloss.SequenceEqual(tst[i].winloss)) ||
                            (!temptst[i].pl_winloss.SequenceEqual(tst[i].pl_winloss)))
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

                    if (r == MessageBoxResult.No)
                        return;
                }

                int eresult = REDitor.ExportAll(tst, tstopp, pst, fbd.SelectedPath, mnuOptionsExportTeamsOnly.IsChecked);

                if (eresult != 0)
                {
                    MessageBox.Show("Export failed.");
                    return;
                }
                updateStatus("Injected at " + fbd.SelectedPath + " successfully!");
            }
        }

        /// <summary>
        ///     Handles the Click event of the mnuHelpReadme control.
        ///     Opens the Readme file with the default txt file handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuHelpReadme_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(AppPath + @"\readme.txt");
        }

        /// <summary>
        ///     Handles the Click event of the mnuHelpAbout control.
        ///     Shows the About window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            var aw = new AboutWindow();
            aw.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the mnuFileGetRealStats control.
        ///     Downloads and imports the current NBA stats from Basketball-Reference.com.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuFileGetRealStats_Click(object sender, RoutedEventArgs e)
        {
            string file = "";

            if (!String.IsNullOrWhiteSpace(txtFile.Text))
            {
                MessageBoxResult r =
                    MessageBox.Show(
                        "This will overwrite the stats for the current season. Are you sure?\n\nClick Yes to overwrite.\nClick No to create a new file automatically. Any unsaved changes to the current file will be lost.\nClick Cancel to return to the main window.",
                        "NBA Stats Tracker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                    file = currentDB;
                else if (r == MessageBoxResult.No)
                    txtFile.Text = "";
                else
                    return;
            }

            if (String.IsNullOrWhiteSpace(txtFile.Text))
            {
                file = AppDocsPath + "Real NBA Stats " + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + ".tst";
                if (File.Exists(file))
                {
                    if (App.realNBAonly)
                    {
                        MessageBoxResult r =
                            MessageBox.Show(
                                "Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?",
                                "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                            return;
                    }
                    else
                    {
                        MessageBoxResult r =
                            MessageBox.Show(
                                "Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?",
                                "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                        {
                            SQLiteIO.LoadSeason(file);
                            txtFile.Text = file;
                            return;
                        }
                    }
                }
            }

            //var grsw = new getRealStatsW();
            //grsw.ShowDialog();

            var realtstopp = new Dictionary<int, TeamStats>();
            var realpst = new Dictionary<int, PlayerStats>();
            sem = new Semaphore(1, 1);

            mainGrid.Visibility = Visibility.Hidden;
            txbWait.Visibility = Visibility.Visible;

            progress = 0;

            //MessageBox.Show("Please wait after pressing OK, this could take a few minutes.");

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
                                     {"Nets", "BRK"},
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

            var TeamDivisions = new Dictionary<string, int>
                                {
                                    {"76ers", 0},
                                    {"Bobcats", 2},
                                    {"Bucks", 1},
                                    {"Bulls", 1},
                                    {"Cavaliers", 1},
                                    {"Celtics", 0},
                                    {"Clippers", 5},
                                    {"Grizzlies", 3},
                                    {"Hawks", 2},
                                    {"Heat", 2},
                                    {"Hornets", 3},
                                    {"Jazz", 4},
                                    {"Kings", 5},
                                    {"Knicks", 0},
                                    {"Lakers", 5},
                                    {"Magic", 2},
                                    {"Mavericks", 3},
                                    {"Nets", 0},
                                    {"Nuggets", 4},
                                    {"Pacers", 1},
                                    {"Pistons", 1},
                                    {"Raptors", 0},
                                    {"Rockets", 3},
                                    {"Spurs", 3},
                                    {"Suns", 5},
                                    {"Thunder", 4},
                                    {"Timberwolves", 4},
                                    {"Trail Blazers", 4},
                                    {"Warriors", 5},
                                    {"Wizards", 2}
                                };

            worker1 = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

            worker1.DoWork += delegate
                              {
                                  REDitor.CreateDivisions();

                                  foreach (var kvp in TeamNamesShort)
                                  {
                                      Dictionary<int, PlayerStats> temppst;
                                      TeamStats realts;
                                      TeamStats realtsopp;
                                      BR.ImportRealStats(kvp, out realts, out realtsopp, out temppst);
                                      int id = TeamOrder[kvp.Key];
                                      realtst[id] = realts;
                                      realtst[id].ID = id;
                                      realtst[id].division = TeamDivisions[kvp.Key];
                                      realtst[id].conference = Divisions.Single(d => d.ID == realtst[id].division).ConferenceID;
                                      realtstopp[id] = realtsopp;
                                      realtstopp[id].ID = id;
                                      realtstopp[id].division = realtst[id].division;
                                      realtstopp[id].conference = realtst[id].conference;
                                      foreach (var kvp2 in temppst)
                                      {
                                          kvp2.Value.ID = realpst.Count;
                                          realpst.Add(realpst.Count, kvp2.Value);
                                      }
                                      worker1.ReportProgress(1);
                                  }
                                  // TODO: Re-enable once Playoffs start
                                  //BR.AddPlayoffTeamStats(ref realtst, ref realtstopp);
                              };

            worker1.ProgressChanged += delegate
                                       {
                                           sem.WaitOne();
                                           GetRealStats_UpdateProgressBar();
                                           sem.Release();
                                       };

            worker1.RunWorkerCompleted += delegate
                                          {
                                              if (realtst[0].name != "Canceled")
                                              {
                                                  int len = realtst.Count;

                                                  tst = new Dictionary<int, TeamStats>();
                                                  tstopp = new Dictionary<int, TeamStats>();
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
                                                  tstopp = realtstopp;
                                                  pst = realpst;
                                                  if (curSeason == 0)
                                                      curSeason = 1;
                                                  SQLiteIO.saveSeasonToDatabase(file, tst, tstopp, pst, curSeason,
                                                                                SQLiteIO.getMaxSeason(file));
                                                  txtFile.Text = file;
                                                  PopulateSeasonCombo(file);
                                                  SQLiteIO.LoadSeason(file, curSeason);

                                                  txbWait.Visibility = Visibility.Hidden;
                                                  mainGrid.Visibility = Visibility.Visible;

                                                  updateStatus("The download of real NBA stats is done.");
                                              }
                                          };

            worker1.RunWorkerAsync();
        }

        /// <summary>
        ///     Updates the progress bar during the download of the real NBA stats.
        /// </summary>
        private void GetRealStats_UpdateProgressBar()
        {
            progress += (double) 100/30;
            var percentage = (int) progress;
            if (percentage < 97)
            {
                status.Content = "Downloading NBA stats from Basketball-Reference.com (" + percentage + "% complete)...";
            }
            else
            {
                status.Content = "Season stats downloaded, now downloading playoff stats and saving...";
            }
        }

        // TODO: Implement Compare to Real again sometime
        /// <summary>
        ///     OBSOLETE:
        ///     Handles the Click event of the btnCompareToReal control.
        ///     Used to compare a team's stats to the ones of its real counterpart.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnCompareToReal_Click(object sender, RoutedEventArgs e)
        {
            /*
            var realteam = new TeamStats();

            if (File.Exists(AppDocsPath + cmbTeam1.SelectedItem + ".rst"))
            {
                var fi = new FileInfo(AppDocsPath + cmbTeam1.SelectedItem + ".rst");
                TimeSpan sinceLastModified = DateTime.Now - fi.LastWriteTime;
                if (sinceLastModified.Days >= 1)
                    realteam = Helper.getRealStats(cmbTeam1.SelectedItem.ToString());
                else
                    try
                    {
                        realteam = Helper.getRealStats(cmbTeam1.SelectedItem.ToString(), true);
                    }
                    catch
                    {
                        try
                        {
                            realteam = Helper.getRealStats(cmbTeam1.SelectedItem.ToString());
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
                realteam = Helper.getRealStats(cmbTeam1.SelectedItem.ToString());
            }
            TeamStats curteam = tst[teamOrder[cmbTeam1.SelectedItem.ToString()]];

            var vw = new VersusWindow(curteam, "Current", realteam, "Real");
            vw.ShowDialog();
            */
        }

        // TODO: Implement Compare to Other file again sometime
        /// <summary>
        ///     OBSOLETE:
        ///     Handles the Click event of the btnCompareOtherFile control.
        ///     Used to compare a team's stats to the ones in another NST database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnCompareOtherFile_Click(object sender, RoutedEventArgs e)
        {
            /*
            var ofd = new OpenFileDialog
                          {
                              Title = "Select the TST file that has the team stats you want to compare to...",
                              Filter = "Team Stats files (*.tst)|*.tst",
                              InitialDirectory = AppDocsPath
                          };
            ofd.ShowDialog();

            string file = ofd.FileName;
            if (file != "")
            {
                string team = cmbTeam1.SelectedItem.ToString();
                var _newTeamOrder = new SortedDictionary<string, int>();

                FileStream stream = File.Open(ofd.FileName, FileMode.Open);
                var bf = new BinaryFormatter {AssemblyFormat = FormatterAssemblyStyle.Simple};

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

                var vw = new VersusWindow(curteam, "Current", newteam, "Other");
                vw.ShowDialog();
            }
            */
        }

        /// <summary>
        ///     Handles the TextChanged event of the txtFile control.
        ///     Updates the currentDB field of MainWindow with the new file loaded. Usually called after Open or Save As.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="TextChangedEventArgs" /> instance containing the event data.
        /// </param>
        private void txtFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtFile.Text))
                return;

            txtFile.ScrollToHorizontalOffset(txtFile.GetRectFromCharacterIndex(txtFile.Text.Length).Right);
            currentDB = txtFile.Text;
            //PopulateSeasonCombo();
            db = new SQLiteDatabase(currentDB);
        }

        /// <summary>
        ///     Populates the season combo using a specified NST database file.
        /// </summary>
        /// <param name="file">The file from which to determine the available seasons.</param>
        public void PopulateSeasonCombo(string file)
        {
            db = new SQLiteDatabase(file);

            GenerateSeasons();
        }

        /// <summary>
        ///     Populates the season combo using the current database.
        /// </summary>
        public void PopulateSeasonCombo()
        {
            PopulateSeasonCombo(currentDB);
        }

        /// <summary>
        ///     Generates the entries used to populate the season combo.
        /// </summary>
        public void GenerateSeasons()
        {
            const string qr = "SELECT * FROM SeasonNames ORDER BY ID DESC";
            DataTable dataTable = db.GetDataTable(qr);
            SeasonList.Clear();
            foreach (DataRow row in dataTable.Rows)
            {
                int id = Tools.getInt(row, "ID");
                string name = Tools.getString(row, "Name");
                SeasonList.Add(new KeyValuePair<int, string>(id, name));
            }

            cmbSeasonNum.ItemsSource = SeasonList;

            cmbSeasonNum.SelectedValue = curSeason;
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the cmbSeasonNum control.
        ///     Changes the curSeason property accordingly.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="SelectionChangedEventArgs" /> instance containing the event data.
        /// </param>
        private void cmbSeasonNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSeasonNum.SelectedIndex == -1)
                return;

            curSeason = ((KeyValuePair<int, string>) (((cmbSeasonNum)).SelectedItem)).Key;

            if (curSeason == tf.SeasonNum && !tf.isBetween)
                return;

            tf = new Timeframe(curSeason);
            if (!loadingSeason)
                UpdateAllData();
        }

        /// <summary>
        ///     Calculates the difference in a team's stats rankingsPerGame between two TeamRankings instances.
        /// </summary>
        /// <param name="oldR">The old team rankingsPerGame.</param>
        /// <param name="newR">The new team rankingsPerGame.</param>
        /// <returns></returns>
        private int[][] calculateDifferenceRanking(TeamRankings oldR, TeamRankings newR)
        {
            var diff = new int[30][];
            for (int i = 0; i < 30; i++)
            {
                diff[i] = new int[18];
                for (int j = 0; j < 18; j++)
                {
                    diff[i][j] = newR.rankingsPerGame[i][j] - oldR.rankingsPerGame[i][j];
                }
            }
            return diff;
        }

        /// <summary>
        ///     Calculates the difference average.
        /// </summary>
        /// <param name="curTST">The cur TST.</param>
        /// <param name="oldTST">The old TST.</param>
        /// <returns></returns>
        private float[][] calculateDifferenceAverage(Dictionary<int, TeamStats> curTST, Dictionary<int, TeamStats> oldTST)
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

        /// <summary>
        ///     Handles the Click event of the btnTest control.
        ///     Displays the Test window or runs a specific test method. Used for various debugging purposes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            //TestWindow tw = new TestWindow(ds);
            //tw.ShowDialog();

            /*
            var lbsw = new LiveBoxScoreWindow();
            lbsw.ShowDialog();
            */

            RecalculateOpponentStats();
        }

        /// <summary>
        ///     Recalculates the opponent stats for all teams by accumulating the stats from the box scores.
        /// </summary>
        private static void RecalculateOpponentStats()
        {
            var temptst = new Dictionary<int, TeamStats>();
            foreach (int to in TeamOrder.Values)
            {
                temptst.Add(to, tst[to].DeepClone());
            }

            foreach (int key in tstopp.Keys)
                tstopp[key].ResetStats(Span.SeasonAndPlayoffsToSeason);

            foreach (BoxScoreEntry bse in bshist)
            {
                if (bse.bs.SeasonNum == _curSeason)
                    TeamStats.AddTeamStatsFromBoxScore(bse.bs, ref temptst, ref tstopp);
            }
        }

        /// <summary>
        ///     OBSOLETE:
        ///     Handles the Click event of the mnuHistoryBoxScores control.
        ///     Used to open the Box Score window in View mode so that the user can view and edit any box score.
        ///     Superseded by the Box Scores tab in the League Overview window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuHistoryBoxScores_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.isTSTEmpty())
                return;

            bs = new TeamBoxScore();
            var bsw = new BoxScoreWindow(BoxScoreWindow.Mode.View);
            bsw.ShowDialog();

            UpdateBoxScore();
        }

        /// <summary>
        ///     Updates a specific box score using the local box score instance.
        /// </summary>
        public static void UpdateBoxScore()
        {
            if (bs.bshistid != -1)
            {
                if (bs.done)
                {
                    List<PlayerBoxScore> list = pbsLists.SelectMany(pbsList => pbsList).ToList();

                    bshist[bs.bshistid].bs = bs;
                    bshist[bs.bshistid].pbsList = list;
                    bshist[bs.bshistid].date = bs.gamedate;
                    bshist[bs.bshistid].mustUpdate = true;
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnTeamOverview control.
        ///     Displays the Team Overview window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnTeamOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB))
                return;
            if (SQLiteIO.isTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using the Analysis features.");
                return;
            }

            dispatcherTimer_Tick(null, null);
            var tow = new TeamOverviewWindow();
            tow.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the btnOpen control.
        ///     Opens a database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            mnuFileOpen_Click(null, null);
        }

        /// <summary>
        ///     Handles the Loaded event of the Window control.
        ///     Creates the DispatcherTimer instance used to revert the status bar message after a number of seconds.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            marqueeTimer = new DispatcherTimer();
            marqueeTimer.Tick += marqueeTimer_Tick;
            marqueeTimer.Interval = new TimeSpan(0, 0, 6);

            txbMarquee.Width = canMarquee.Width;

            /*
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.From = -txbMarquee.ActualWidth;
            doubleAnimation.To = grdMain.ActualWidth;
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            doubleAnimation.Duration = new Duration(new TimeSpan(0, 0, 10));
            txbMarquee.BeginAnimation(Canvas.BottomProperty, doubleAnimation);
            */

            mnuTools.IsEnabled = false;
            grdAnalysis.IsEnabled = false;
            grdUpdate.IsEnabled = false;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 4);
            //dispatcherTimer.Start();

            int checkForUpdatesSetting = Misc.GetRegistrySetting("CheckForUpdates", 1);
            if (checkForUpdatesSetting == 1)
            {
                mnuOptionsCheckForUpdates.IsChecked = true;
                var w = new BackgroundWorker();
                w.DoWork += delegate { CheckForUpdates(); };
                w.RunWorkerAsync();
            }
            else
            {
                mnuOptionsCheckForUpdates.IsChecked = false;
            }
        }

        /// <summary>
        ///     Handles the Tick event of the dispatcherTimer control.
        ///     Reverts the status bar message to "Ready".
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="EventArgs" /> instance containing the event data.
        /// </param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            status.FontWeight = FontWeights.Normal;
            status.Content = "Ready";
            dispatcherTimer.Stop();
        }

        /// <summary>
        ///     Updates the status bar message and starts the timer which will revert it after a number of seconds.
        /// </summary>
        /// <param name="newStatus">The new status.</param>
        public void updateStatus(string newStatus)
        {
            dispatcherTimer.Stop();
            status.FontWeight = FontWeights.Bold;
            status.Content = newStatus;
            dispatcherTimer.Start();
        }

        /// <summary>
        ///     Handles the Click event of the btnSaveCurrentSeason control.
        ///     Saves the current season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnSaveCurrentSeason_Click(object sender, RoutedEventArgs e)
        {
            if (tf.isBetween)
            {
                tf = new Timeframe(curSeason);
                UpdateAllData();
            }
            SQLiteIO.saveSeasonToDatabase(currentDB, tst, tstopp, pst, curSeason, SQLiteIO.getMaxSeason(currentDB));
            txtFile.Text = currentDB;
            UpdateAllData();
            mwInstance.updateStatus("File saved successfully. Season " + curSeason.ToString() + " updated.");
        }

        /// <summary>
        ///     Handles the Click event of the btnLeagueOverview control.
        ///     Displays the League Overview window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnLeagueOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB))
                return;
            /*
            if (!isCustom)
            {
                MessageBox.Show("Save the data into a Team Stats file before using the tool's features.");
                return;
            }
            */
            if (SQLiteIO.isTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using any Analysis features.");
                return;
            }

            dispatcherTimer_Tick(null, null);
            var low = new LeagueOverviewWindow();
            low.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the mnuFileNew control.
        ///     Allows the user to create a new database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuFileNew_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog {Filter = "NST Database (*.tst)|*.tst", InitialDirectory = AppDocsPath};
            sfd.ShowDialog();

            if (sfd.FileName == "")
                return;

            File.Delete(sfd.FileName);

            db = new SQLiteDatabase(sfd.FileName);

            SQLiteIO.prepareNewDB(db, 1, 1);

            tst = new Dictionary<int, TeamStats>();
            tst[0] = new TeamStats("$$NewDB");
            tstopp = new Dictionary<int, TeamStats>();
            tstopp[0] = new TeamStats("$$NewDB");
            TeamOrder = new SortedDictionary<string, int>();
            bshist = new List<BoxScoreEntry>();

            txtFile.Text = sfd.FileName;
            currentDB = txtFile.Text;
            PopulateSeasonCombo();
            ChangeSeason(1);

            SQLiteIO.SetSetting("Game Length", 48);
            SQLiteIO.SetSetting("Season Length", 82);

            mnuTools.IsEnabled = true;
            grdAnalysis.IsEnabled = true;
            grdUpdate.IsEnabled = true;

            UpdateAllData();
        }

        /// <summary>
        ///     Handles the Click event of the btnAdd control.
        ///     Allows the user to add teams or players the database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB))
                return;

            addInfo = "";
            var aw = new AddWindow(ref pst);
            aw.ShowDialog();

            if (!String.IsNullOrEmpty(addInfo))
            {
                if (addInfo != "$$NST Players Added")
                {
                    string[] parts = Tools.SplitLinesToArray(addInfo);
                    List<string> newTeams = parts.Where(s => !String.IsNullOrWhiteSpace(s)).ToList();

                    int oldlen = tst.Count;
                    if (SQLiteIO.isTSTEmpty())
                        oldlen = 0;

                    for (int i = 0; i < newTeams.Count; i++)
                    {
                        if (tst.Where(pair => pair.Value.name == newTeams[i]).Count() == 1)
                        {
                            MessageBox.Show("There's a team with the name " + newTeams[i] +
                                            " already in the database so it won't be added again.");
                            continue;
                        }
                        int newid = oldlen + i;
                        tst[newid] = new TeamStats(newTeams[i]) {ID = newid};
                        tstopp[newid] = new TeamStats(newTeams[i]) {ID = newid};
                        TeamOrder.Add(newTeams[i], newid);
                    }
                    SQLiteIO.saveSeasonToDatabase();
                    updateStatus("Teams were added, database saved.");
                }
                else
                {
                    SQLiteIO.savePlayersToDatabase(currentDB, pst, curSeason, SQLiteIO.getMaxSeason(currentDB));
                    updateStatus("Players were added, database saved.");
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnGrabNBAStats control.
        ///     Allows the user to download the current NBA stats from Basketball-Reference.com.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnGrabNBAStats_Click(object sender, RoutedEventArgs e)
        {
            mnuFileGetRealStats_Click(null, null);
        }

        /// <summary>
        ///     Handles the Closed event of the Window control.
        ///     Makes sure the application shuts down properly after this window closes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="EventArgs" /> instance containing the event data.
        /// </param>
        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscStartNewSeason control.
        ///     Allows the user to add a new season to the database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscStartNewSeason_Click(object sender, RoutedEventArgs e)
        {
            if (!SQLiteIO.isTSTEmpty())
            {
                MessageBoxResult r =
                    MessageBox.Show(
                        "Are you sure you want to do this? This is an irreversible action.\nStats and box Scores will be retained, and you'll be able to use all the tool's features on them.",
                        "NBA Stats Tracker", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    SQLiteIO.saveSeasonToDatabase();

                    curSeason = SQLiteIO.getMaxSeason(currentDB);
                    var ibw = new InputBoxWindow("Enter a name for the new season", (curSeason + 1).ToString());
                    ibw.ShowDialog();

                    string seasonName = String.IsNullOrWhiteSpace(input) ? (curSeason + 1).ToString() : input;

                    string q = "alter table Teams rename to TeamsS" + curSeason;
                    db.ExecuteNonQuery(q);

                    q = "alter table PlayoffTeams rename to PlayoffTeamsS" + curSeason;
                    db.ExecuteNonQuery(q);

                    q = "alter table Opponents rename to OpponentsS" + curSeason;
                    db.ExecuteNonQuery(q);

                    q = "alter table PlayoffOpponents rename to PlayoffOpponentsS" + curSeason;
                    db.ExecuteNonQuery(q);

                    q = "alter table Players rename to PlayersS" + curSeason;
                    db.ExecuteNonQuery(q);

                    q = "alter table PlayoffPlayers rename to PlayoffPlayersS" + curSeason;
                    db.ExecuteNonQuery(q);

                    curSeason++;

                    SQLiteIO.prepareNewDB(db, curSeason, curSeason, true);
                    db.Insert("SeasonNames", new Dictionary<string, string> {{"ID", curSeason.ToString()}, {"Name", seasonName}});

                    foreach (int key in tst.Keys.ToList())
                    {
                        TeamStats ts = tst[key];
                        for (int i = 0; i < ts.stats.Length; i++)
                        {
                            ts.stats[i] = 0;
                            ts.pl_stats[i] = 0;
                        }
                        ts.winloss[0] = 0;
                        ts.winloss[1] = 0;
                        ts.pl_winloss[0] = 0;
                        ts.pl_winloss[1] = 0;
                        ts.CalcAvg();
                        tst[key] = ts;
                    }

                    foreach (int key in tstopp.Keys.ToList())
                    {
                        TeamStats ts = tstopp[key];
                        for (int i = 0; i < ts.stats.Length; i++)
                        {
                            ts.stats[i] = 0;
                            ts.pl_stats[i] = 0;
                        }
                        ts.winloss[0] = 0;
                        ts.winloss[1] = 0;
                        ts.pl_winloss[0] = 0;
                        ts.pl_winloss[1] = 0;
                        ts.CalcAvg();
                        tstopp[key] = ts;
                    }

                    foreach (var ps in pst)
                    {
                        for (int i = 0; i < ps.Value.stats.Length; i++)
                        {
                            ps.Value.stats[i] = 0;
                            ps.Value.pl_stats[i] = 0;
                        }
                        ps.Value.isAllStar = false;
                        ps.Value.isNBAChampion = false;
                        if (ps.Value.Contract.Option == PlayerContractOption.Team2Yr)
                        {
                            ps.Value.Contract.Option = PlayerContractOption.Team;
                        }
                        else if (ps.Value.Contract.Option == PlayerContractOption.Team ||
                                 ps.Value.Contract.Option == PlayerContractOption.Player)
                        {
                            ps.Value.Contract.Option = PlayerContractOption.None;
                        }
                        try
                        {
                            ps.Value.Contract.ContractSalaryPerYear.RemoveAt(0);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                        ps.Value.CalcAvg();
                    }

                    SQLiteIO.saveSeasonToDatabase(currentDB, tst, tstopp, pst, curSeason, curSeason);
                    PopulateSeasonCombo();
                    ChangeSeason(curSeason);
                    tf = new Timeframe(curSeason);
                    UpdateAllData();
                    updateStatus("New season started. Database saved.");
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnSaveAllSeasons control.
        ///     Saves all the seasons to the database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnSaveAllSeasons_Click(object sender, RoutedEventArgs e)
        {
            SQLiteIO.saveAllSeasons(currentDB);
        }

        /// <summary>
        ///     Handles the Click event of the btnPlayerOverview control.
        ///     Opens the Player Overview window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnPlayerOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentDB))
                return;
            if (SQLiteIO.isTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using the Analysis features.");
                return;
            }

            dispatcherTimer_Tick(null, null);
            var pow = new PlayerOverviewWindow();
            pow.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscImportBoxScores control.
        ///     Allows the user to import box scores from another database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscImportBoxScores_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                      {
                          Filter = "NST Database (*.tst)|*.tst",
                          InitialDirectory = AppDocsPath,
                          Title = "Please select the TST file that you want to import from..."
                      };
            ofd.ShowDialog();

            if (ofd.FileName == "")
                return;

            string file = ofd.FileName;

            int maxSeason = SQLiteIO.getMaxSeason(file);
            for (int i = 1; i <= maxSeason; i++)
            {
                List<BoxScoreEntry> newBShist = SQLiteIO.GetSeasonBoxScoresFromDatabase(file, i, maxSeason);

                foreach (BoxScoreEntry newbse in newBShist)
                {
                    bool doNotAdd = false;
                    foreach (BoxScoreEntry bse in bshist)
                    {
                        if (bse.bs.id == newbse.bs.id)
                        {
                            if (bse.bs.gamedate == newbse.bs.gamedate && bse.bs.Team1 == newbse.bs.Team1 && bse.bs.Team2 == newbse.bs.Team2)
                            {
                                MessageBoxResult r;
                                if (bse.bs.PTS1 == newbse.bs.PTS1 && bse.bs.PTS2 == newbse.bs.PTS2)
                                {
                                    r =
                                        MessageBox.Show(
                                            "A box score with the same date, teams and score as one in the current database was found in the file being imported." +
                                            "\n" + bse.bs.gamedate.ToShortDateString() + ": " + bse.bs.Team1 + " " + bse.bs.PTS1 + " @ " +
                                            bse.bs.Team2 + " " + bse.bs.PTS2 +
                                            "\n\nClick Yes to only keep the box score that is already in this databse." +
                                            "\nClick No to only keep the box score that is being imported, replacing the one in this database." +
                                            "\nClick Cancel to keep both box scores.", "NBA Stats Tracker", MessageBoxButton.YesNoCancel,
                                            MessageBoxImage.Question);
                                }
                                else
                                {
                                    r =
                                        MessageBox.Show(
                                            "A box score with the same date, teams and score as one in the current database was found in the file being imported." +
                                            "\nCurrent: " + bse.bs.gamedate.ToShortDateString() + ": " + bse.bs.Team1 + " " + bse.bs.PTS1 +
                                            " @ " + bse.bs.Team2 + " " + bse.bs.PTS2 + "\nTo be imported: " +
                                            newbse.bs.gamedate.ToShortDateString() + ": " + newbse.bs.Team1 + " " + newbse.bs.PTS1 + " @ " +
                                            newbse.bs.Team2 + " " + newbse.bs.PTS2 +
                                            "\n\nClick Yes to only keep the box score that is already in this databse." +
                                            "\nClick No to only keep the box score that is being imported, replacing the one in this database." +
                                            "\nClick Cancel to keep both box scores.", "NBA Stats Tracker", MessageBoxButton.YesNoCancel,
                                            MessageBoxImage.Question);
                                }
                                if (r == MessageBoxResult.Yes)
                                {
                                    doNotAdd = true;
                                    break;
                                }

                                if (r == MessageBoxResult.No)
                                {
                                    bshist.Remove(bse);
                                    break;
                                }
                            }
                            newbse.bs.id = GetFreeBseId();
                            break;
                        }
                    }
                    if (!doNotAdd)
                    {
                        newbse.mustUpdate = true;
                        bshist.Add(newbse);
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the first free BoxScoreEntry ID in the box score history list.
        /// </summary>
        /// <returns></returns>
        private int GetFreeBseId()
        {
            List<int> bseIDs = bshist.Select(bse => bse.bs.id).ToList();

            bseIDs.Sort();

            int i = 0;
            while (true)
            {
                if (!bseIDs.Contains(i))
                    return i;
                i++;
            }
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEnableTeams control.
        ///     Used to enable/disable (i.e. show/hide) teams for the current season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEnableTeams_Click(object sender, RoutedEventArgs e)
        {
            addInfo = "";
            var etw = new DualListWindow(currentDB, curSeason, SQLiteIO.getMaxSeason(currentDB), DualListWindow.Mode.HiddenTeams);
            etw.ShowDialog();

            if (addInfo == "$$TEAMSENABLED")
            {
                SQLiteIO.GetAllTeamStatsFromDatabase(currentDB, curSeason, out tst, out tstopp, out TeamOrder);
                updateStatus("Teams were enabled/disabled. Database saved.");
            }
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscDeleteBoxScores control.
        ///     Allows the user to delete box score entries.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscDeleteBoxScores_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.isTSTEmpty())
                return;

            var bslw = new BoxScoreListWindow();
            bslw.ShowDialog();
            SQLiteIO.LoadSeason();
        }

        /// <summary>
        ///     Handles the Click event of the btnPlayerSearch control.
        ///     Allows the user to search for players fulfilling any combination of user-specified criteria.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnPlayerSearch_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.isTSTEmpty())
                return;

            var psw = new PlayerSearchWindow();
            psw.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscResetTeamStats control.
        ///     Allows the user to reset all team stats for the current season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscResetTeamStats_Click(object sender, RoutedEventArgs e)
        {
            if (!SQLiteIO.isTSTEmpty())
            {
                MessageBoxResult r =
                    MessageBox.Show(
                        "Are you sure you want to do this? This is an irreversible action.\nThis only applies to the current season.",
                        "Reset All Team Stats", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    foreach (int key in tst.Keys)
                        tst[key].ResetStats(Span.SeasonAndPlayoffsToSeason);

                    foreach (int key in tstopp.Keys)
                        tstopp[key].ResetStats(Span.SeasonAndPlayoffsToSeason);
                }
            }

            SQLiteIO.saveSeasonToDatabase();

            updateStatus("All Team Stats for current season have been reset. Database saved.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscResetPlayerStats control.
        ///     Allows the user to reset all player stats for the current season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscResetPlayerStats_Click(object sender, RoutedEventArgs e)
        {
            if (!SQLiteIO.isTSTEmpty())
            {
                MessageBoxResult r =
                    MessageBox.Show(
                        "Are you sure you want to do this? This is an irreversible action.\nThis only applies to the current season.",
                        "Reset All Team Stats", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    foreach (PlayerStats ps in pst.Values)
                        ps.ResetStats();
                }
            }

            SQLiteIO.saveSeasonToDatabase();

            updateStatus("All Player Stats for current season have been reset. Database saved.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuOptionsCheckForUpdates control.
        ///     Enables/disables the automatic check for updates each time the program starts.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuOptionsCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            Misc.SetRegistrySetting("CheckForUpdates", mnuOptionsCheckForUpdates.IsChecked ? 1 : 0);
        }

        /// <summary>
        ///     Handles the Click event of the mnuOptionsImportREditor control.
        ///     Changes the NBA 2K import/export method to using REDitor-exported CSV files.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuOptionsImportREditor_Click(object sender, RoutedEventArgs e)
        {
            if (!mnuOptionsImportREditor.IsChecked)
                mnuOptionsImportREditor.IsChecked = true;

            Misc.SetRegistrySetting("NBA2K12ImportMethod", 0);
        }

        /// <summary>
        ///     Handles the Click event of the mnuOptionsExportTeamsOnly control.
        ///     Sets whether only the team stats will be exported when exporting to an NBA 2K save.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuOptionsExportTeamsOnly_Click(object sender, RoutedEventArgs e)
        {
            Misc.SetRegistrySetting("ExportTeamsOnly", mnuOptionsExportTeamsOnly.IsChecked ? 1 : 0);
        }

        /// <summary>
        ///     Handles the Click event of the mnuOptionsCompatibilityCheck control.
        ///     Sets whether the database-save compatibility check will be run before exporting to an NBA 2K save.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuOptionsCompatibilityCheck_Click(object sender, RoutedEventArgs e)
        {
            Misc.SetRegistrySetting("CompatibilityCheck", mnuOptionsCompatibilityCheck.IsChecked ? 1 : 0);
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscRenameCurrentSeason control.
        ///     Renames the current season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscRenameCurrentSeason_Click(object sender, RoutedEventArgs e)
        {
            string curName = GetSeasonName(curSeason);
            var ibw = new InputBoxWindow("Enter the new name for the current season", curName);
            ibw.ShowDialog();

            if (!String.IsNullOrWhiteSpace(input))
            {
                SetSeasonName(curSeason, input);
                cmbSeasonNum.SelectedValue = curSeason;
            }
        }

        /// <summary>
        ///     Sets the name of the specified season.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <param name="name">The name.</param>
        private void SetSeasonName(int season, string name)
        {
            for (int i = 0; i < SeasonList.Count; i++)
            {
                if (SeasonList[i].Key == season)
                {
                    SeasonList[i] = new KeyValuePair<int, string>(season, name);
                    break;
                }
            }

            SQLiteIO.SaveSeasonName(season);
        }

        /// <summary>
        ///     Gets the name of the specified season.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <returns></returns>
        public static string GetSeasonName(int season)
        {
            return SeasonList.Single(delegate(KeyValuePair<int, string> kvp)
                                     {
                                         if (kvp.Key == season)
                                             return true;
                                         return false;
                                     }).Value;
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscLiveBoxScore control.
        ///     Allows the user to keep track of the box score of a live game.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscLiveBoxScore_Click(object sender, RoutedEventArgs e)
        {
            var lbsw = new LiveBoxScoreWindow();
            if (lbsw.ShowDialog() == true)
            {
                var bsw = new BoxScoreWindow(tempbse);
                bsw.ShowDialog();

                ParseBoxScoreResult();
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnDownloadBoxScore control.
        ///     Allows the user to download and import a box score from Basketball-Reference.com.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnDownloadBoxScore_Click(object sender, RoutedEventArgs e)
        {
            var ibw = new InputBoxWindow("Enter the full URL of the box score you want to download");
            if (ibw.ShowDialog() == true)
            {
                try
                {
                    int result = BR.ImportBoxScore(input);
                    if (result == -1)
                    {
                        MessageBox.Show(
                            "The Box Score was saved, but some required players were missing.\nMake sure you're using an up-to-date downloaded database." +
                            "\n\nThe database wasn't saved in case you want to reload it and try again.");
                    }
                    else
                    {
                        SQLiteIO.saveSeasonToDatabase();
                        updateStatus("Box score downloaded and season saved!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Couldn't download & import box score.\n\nError: " + ex.Message);
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEnablePlayers control.
        ///     Allows the user to enable/disable (i.e. show/hide) specific players in the current season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEnablePlayers_Click(object sender, RoutedEventArgs e)
        {
            addInfo = "";
            var etw = new DualListWindow(currentDB, curSeason, SQLiteIO.getMaxSeason(currentDB), DualListWindow.Mode.HiddenPlayers);
            etw.ShowDialog();

            if (addInfo == "$$PLAYERSENABLED")
            {
                pst = SQLiteIO.GetPlayersFromDatabase(currentDB, tst, tstopp, TeamOrder, curSeason, SQLiteIO.getMaxSeason(currentDB));
                updateStatus("Players were enabled/disabled. Database saved.");
            }
        }

        /// <summary>
        ///     Copies the team & player stats dictionaries to the corresponding local MainWindow instances.
        /// </summary>
        /// <param name="teamStats">The team stats.</param>
        /// <param name="oppStats">The opp stats.</param>
        /// <param name="playerStats">The player stats.</param>
        public static void CopySeasonToMainWindow(Dictionary<int, TeamStats> teamStats, Dictionary<int, TeamStats> oppStats,
                                                  Dictionary<int, PlayerStats> playerStats)
        {
            tst = teamStats;
            tstopp = oppStats;
            pst = playerStats;
        }

        /// <summary>
        ///     Handles the Click event of the mnuHelpDonate control.
        ///     Shows the user a website prompting for a donation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuHelpDonate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://users.tellas.gr/~aslan16/donate.html");
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEditDivisions control.
        ///     Allows the user to edit the divisions and conferences.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEditDivisions_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(currentDB))
                return;

            var lw = new ListWindow();
            lw.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscRecalculateOppStats control.
        ///     Allows the user to recalculate the opponent stats by accumulating the stats in the available box scores.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscRecalculateOppStats_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.isTSTEmpty())
                return;

            RecalculateOpponentStats();

            updateStatus("Opponent stats for the current season have been recalculated. You should save the current season now.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEditGameLength control.
        ///     Allows the user to change the default game length in minutes used in statistical calculations and in the Box Score window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEditGameLength_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.isTSTEmpty())
                return;

            var ibw = new InputBoxWindow("Insert the length of each game in minutes, without overtime (e.g. 48):", gameLength.ToString());
            if (ibw.ShowDialog() == true)
            {
                try
                {
                    gameLength = Convert.ToInt32(input);
                }
                catch (Exception)
                {
                    return;
                }
            }

            SQLiteIO.SetSetting("Game Length", gameLength);

            updateStatus("Game Length saved. Database updated.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEditSeasonLength control.
        ///     Allows the user to edit the season length used in statistical calculations.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEditSeasonLength_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.isTSTEmpty())
                return;

            var ibw = new InputBoxWindow("Insert the length of the season in games (etc. 82):", seasonLength.ToString());
            if (ibw.ShowDialog() == true)
            {
                try
                {
                    seasonLength = Convert.ToInt32(input);
                }
                catch (Exception)
                {
                    return;
                }
            }

            SQLiteIO.SetSetting("Season Length", seasonLength);

            updateStatus("Season Length saved. Database updated.");
        }

        private void btnAdvStatCalc_Click(object sender, RoutedEventArgs e)
        {
            var ascw = new AdvancedStatCalculatorWindow();
            ascw.ShowDialog();
        }

        public static void UpdateAllData()
        {
            SQLiteIO.PopulateAll(tf, out tst, out tstopp, out TeamOrder, out pst, out splitTeamStats, out splitPlayerStats, out bshist,
                                 out SeasonTeamRankings, out SeasonPlayerRankings, out PlayoffTeamRankings, out PlayoffPlayerRankings,
                                 out DisplayNames);

            UpdateNotables();
        }

        public static void UpdateNotables()
        {
            Dictionary<int, PlayerStats> pstLeaders;
            SeasonLeadersRankings = PlayerRankings.CalculateLeadersRankings(out pstLeaders);
            notables = new List<string>();

            Dictionary<int, PlayerStats> temp;
            PlayoffsLeadersRankings = PlayerRankings.CalculateLeadersRankings(out temp, true);

            if (pstLeaders.Count == 0)
                return;

            var psrList = new List<PlayerStatsRow>();
            pstLeaders.Values.ToList().ForEach(delegate(PlayerStats ps)
                                               {
                                                   var psr = new PlayerStatsRow(ps, calcRatings: false);
                                                   psrList.Add(psr);
                                               });

            PlayerStatsRow curL = psrList.OrderByDescending(pair => pair.PPG).First();

            string m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 3, p.PPG);
            string s = String.Format("PPG Leader: {0} {1} ({2}) ({3:F1} PPG, {4})", curL.FirstName, curL.LastName,
                                     Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.PPG, m);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.FGp).First();
            var ppg = double.IsNaN(curL.PPG) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.FGp);
            s = String.Format("FG% Leader: {0} {1} ({2}) ({3:F3} FG%, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.FGp, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.RPG).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.RPG);
            s = String.Format("RPG Leader: {0} {1} ({2}) ({3:F1} RPG, {5:F1} PPG,{4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.RPG, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.BPG).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.BPG);
            s = String.Format("BPG Leader: {0} {1} ({2}) ({3:F1} BPG, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.BPG, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.APG).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.APG);
            s = String.Format("APG Leader: {0} {1} ({2}) ({3:F1} APG, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.APG, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.SPG).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.SPG);
            s = String.Format("SPG Leader: {0} {1} ({2}) ({3:F1} SPG, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.SPG, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.ORPG).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.ORPG);
            s = String.Format("ORPG Leader: {0} {1} ({2}) ({3:F1} ORPG, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.ORPG, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.DRPG).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.DRPG);
            s = String.Format("DRPG Leader: {0} {1} ({2}) ({3:F1} DRPG, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.DRPG, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.TPp).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.TPp);
            s = String.Format("3P% Leader: {0} {1} ({2}) ({3:F3} 3P%, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.TPp, m, ppg);
            notables.Add(s);

            curL = psrList.OrderByDescending(pair => pair.FTp).First();
            ppg = double.IsNaN(ppg) ? pst[curL.ID].averages[p.PPG] : curL.PPG;

            m = GetBestStatsForMarquee(curL, SeasonLeadersRankings, 2, p.FTp);
            s = String.Format("FT% Leader: {0} {1} ({2}) ({3:F3} FT%, {5:F1} PPG, {4})", curL.FirstName, curL.LastName,
                              Misc.GetDisplayNameFromTeam(tst, curL.TeamF), curL.FTp, m, ppg);
            notables.Add(s);

            notables.Shuffle();

            mwInstance.txbMarquee.Text = "League Notables";
            mwInstance.marqueeTimer.Start();
        }

        private static string GetBestStatsForMarquee(PlayerStatsRow curLr, PlayerRankings rankingsActive, int count, int statToIgnore)
        {
            string s = "";
            var dict = new Dictionary<int, int>();
            for (int k = 0; k < rankingsActive.rankingsPerGame[curLr.ID].Length; k++)
            {
                dict.Add(k, rankingsActive.rankingsPerGame[curLr.ID][k]);
            }
            dict[p.FPG] = pst.Count + 1 - dict[p.FPG];
            dict[p.TPG] = pst.Count + 1 - dict[p.TPG];
            //dict[t.PAPG] = pst.Count + 1 - dict[t.PAPG];
            List<int> strengths = (from entry in dict orderby entry.Value ascending select entry.Key).ToList();
            int m = 0;
            int j = count;
            while (true)
            {
                if (m == j)
                    break;
                if (strengths[m] == statToIgnore || strengths[m] == p.PPG || strengths[m] == p.DRPG)
                {
                    j++;
                    m++;
                    continue;
                }
                switch (strengths[m])
                {
                    case p.APG:
                        s += String.Format("{0:F1} APG, ", curLr.APG);
                        break;
                    case p.BPG:
                        s += String.Format("{0:F1} BPG, ", curLr.BPG);
                        break;
                    case p.DRPG:
                        s += String.Format("{0:F1} DRPG, ", curLr.DRPG);
                        break;
                    case p.FGp:
                        s += String.Format("{0:F3} FG%, ", curLr.FGp);
                        break;
                    case p.FPG:
                        s += String.Format("{0:F1} FPG, ", curLr.FPG);
                        break;
                    case p.FTp:
                        s += String.Format("{0:F3} FT%, ", curLr.FTp);
                        break;
                    case p.ORPG:
                        s += String.Format("{0:F1} ORPG, ", curLr.ORPG);
                        break;
                    case p.PPG:
                        s += String.Format("{0:F1} PPG, ", curLr.PPG);
                        break;
                    case p.RPG:
                        s += String.Format("{0:F1} RPG, ", curLr.RPG);
                        break;
                    case p.SPG:
                        s += String.Format("{0:F1} SPG, ", curLr.SPG);
                        break;
                    case p.TPG:
                        s += String.Format("{0:F1} TPG, ", curLr.TPG);
                        break;
                    case p.TPp:
                        s += String.Format("{0:F3} 3P%, ", curLr.TPp);
                        break;
                    default:
                        j++;
                        break;
                }
                m++;
            }
            s = s.TrimEnd(new[] {' ', ','});
            return s;
        }

        private void mnuMiscRecalculateCareerHighs_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += delegate(object o, DoWorkEventArgs args)
                         {
                             var highsCount = pst.FirstOrDefault().Value.careerHighs.Length;
                             var plCount = pst.Keys.Count();
                             for (int i = 0; i < plCount; i++)
                             {
                                 bw.ReportProgress(Convert.ToInt32((double)100*i/plCount));
                                 var pID = pst.Keys.ToList()[i];
                                 pst[pID].CalculateSeasonHighs(MainWindow.bshist);
                                 bool fail = false;
                                 for (int k = 0; k < highsCount; k++)
                                 {
                                     try
                                     {
                                         pst[pID].careerHighs[k] = seasonHighs[pID].Select(sh => sh.Value[k]).Max();
                                     }
                                     catch (InvalidOperationException)
                                     {
                                         for (int j = 0; j < highsCount; j++)
                                         {
                                             pst[pID].careerHighs[j] = 0;
                                         }
                                         fail = true;
                                         break;
                                     }
                                     if (fail)
                                         continue;
                                 }
                             }
                         };

            bw.RunWorkerCompleted += delegate(object o, RunWorkerCompletedEventArgs args)
                                     {
                                         mainGrid.Visibility = Visibility.Visible;
                                         updateStatus(
                                             "Calculated career highs from season highs. Confirm you want this by saving the database.");
                                     };

            bw.ProgressChanged += delegate(object o, ProgressChangedEventArgs args)
                                  { status.Content = "Updating (" + args.ProgressPercentage + "% complete)..."; };

            mainGrid.Visibility = Visibility.Hidden;
            bw.RunWorkerAsync();
        }

        public static void RelinkEverything(DBData dbdata)
        {
            tst = dbdata.tst;
            tstopp = dbdata.tstopp;
            TeamOrder = dbdata.TeamOrder;
            pst = dbdata.pst;
            splitTeamStats = dbdata.splitTeamStats;
            splitPlayerStats = dbdata.splitPlayerStats;
            bshist = dbdata.bshist;
            SeasonTeamRankings = dbdata.teamRankings;
            SeasonPlayerRankings = dbdata.playerRankings;
            PlayoffTeamRankings = dbdata.playoffTeamRankings;
            PlayoffPlayerRankings = dbdata.playoffPlayerRankings;
            DisplayNames = dbdata.DisplayNames;
        }

        private void mnuMiscImportPrevYear2K12_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(
                    "Are you sure you want to import the stats of the previous season from your NBA 2K12 save? This " +
                    "will overwrite any data in the current season of the database.", "NBA Stats Tracker", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var fbd = new FolderBrowserDialog
                      {
                          Description = "Select folder with REditor-exported CSVs",
                          ShowNewFolderButton = false,
                          SelectedPath = Misc.GetRegistrySetting("LastImportDir", "")
                      };
            DialogResult dr = fbd.ShowDialog(this.GetIWin32Window());

            if (dr != System.Windows.Forms.DialogResult.OK)
                return;

            if (fbd.SelectedPath == "")
                return;

            Misc.SetRegistrySetting("LastImportDir", fbd.SelectedPath);

            int result = REDitor.ImportPrevious(ref tst, ref tstopp, ref TeamOrder, ref pst, fbd.SelectedPath);

            if (result != 0)
            {
                MessageBox.Show("Import failed! Please reload your database immediately to avoid saving corrupt data.",
                                "NBA Stats Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            updateStatus("NBA 2K12 stats imported successfully! Verify that you want this by saving the current season.");
        }

        private void mnuOptionsIsImperial_Click(object sender, RoutedEventArgs e)
        {
            IsImperial = mnuOptionsIsImperial.IsChecked;
            Misc.SetRegistrySetting("IsImperial", IsImperial ? 1 : 0);
        }

        private void mnuMiscPreferNBALeaders_Checked(object sender, RoutedEventArgs e)
        {
            mnuMiscPreferMyLeaders.IsChecked = false;
            SQLiteIO.SetSetting("Leaders", "NBA");
            LoadMyLeadersCriteria();
            UpdateNotables();
        }

        private void mnuMiscPreferMyLeaders_Checked(object sender, RoutedEventArgs e)
        {
            mnuMiscPreferNBALeaders.IsChecked = false;
            SQLiteIO.SetSetting("Leaders", "My");
            LoadMyLeadersCriteria();
            UpdateNotables();
        }
    }
}