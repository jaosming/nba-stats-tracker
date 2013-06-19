#region Copyright Notice

//    Copyright 2011-2013 Eleftherios Aslanoglou
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

#endregion

namespace NBA_Stats_Tracker.Windows.MainInterface
{
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
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Threading;

    using LeftosCommonLibrary;
    using LeftosCommonLibrary.BeTimvwFramework;
    using LeftosCommonLibrary.CommonDialogs;

    using Microsoft.Win32;

    using NBA_Stats_Tracker.Data.BoxScores;
    using NBA_Stats_Tracker.Data.BoxScores.PlayByPlay;
    using NBA_Stats_Tracker.Data.Other;
    using NBA_Stats_Tracker.Data.Players;
    using NBA_Stats_Tracker.Data.Players.Contracts;
    using NBA_Stats_Tracker.Data.SQLiteIO;
    using NBA_Stats_Tracker.Data.Teams;
    using NBA_Stats_Tracker.Helper.Miscellaneous;
    using NBA_Stats_Tracker.Helper.WindowsForms;
    using NBA_Stats_Tracker.Interop.BR;
    using NBA_Stats_Tracker.Interop.REDitor;
    using NBA_Stats_Tracker.Windows.MainInterface.ASC;
    using NBA_Stats_Tracker.Windows.MainInterface.BoxScores;
    using NBA_Stats_Tracker.Windows.MainInterface.League;
    using NBA_Stats_Tracker.Windows.MainInterface.Players;
    using NBA_Stats_Tracker.Windows.MainInterface.Teams;
    using NBA_Stats_Tracker.Windows.MainInterface.ToolWindows;
    using NBA_Stats_Tracker.Windows.MiscDialogs;

    using SQLite_Database;

    using Application = System.Windows.Application;
    using MessageBox = System.Windows.MessageBox;
    using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
    using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

    #endregion

    /// <summary>The Main window, offering quick access to the program's features</summary>
    public partial class MainWindow
    {
        #region Delegates

        public delegate void UpdateStatusDelegate(string message, bool noResetTimer = false);

        #endregion

        public static readonly string PSFiltersPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                                                      + @"\NBA Stats Tracker\" + @"Search Filters\";

        public static readonly string ASCFiltersPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                                                       + @"\NBA Stats Tracker\" + @"Advanced Stats Filters\";

        public static string SavesPath = "";
        public static readonly string AppPath = Environment.CurrentDirectory + "\\";
        public static readonly Random Random = new Random();

        public static int BaseYear = DateTime.Now.Year;

        public static MainWindow MWInstance;

        public static Dictionary<int, TeamStats> TST = new Dictionary<int, TeamStats>();
        public static Dictionary<int, TeamStats> TSTOpp = new Dictionary<int, TeamStats>();
        public static Dictionary<int, string> DisplayNames = new Dictionary<int, string>();
        public static Dictionary<int, PlayerStats> PST = new Dictionary<int, PlayerStats>();
        public static List<BoxScoreEntry> BSHist = new List<BoxScoreEntry>();

        public static Dictionary<int, Dictionary<string, TeamStats>> SplitTeamStats =
            new Dictionary<int, Dictionary<string, TeamStats>>();

        public static Dictionary<int, Dictionary<string, PlayerStats>> SplitPlayerStats =
            new Dictionary<int, Dictionary<string, PlayerStats>>();

        public static Dictionary<int, Dictionary<int, UInt16[]>> SeasonHighs = new Dictionary<int, Dictionary<int, ushort[]>>();

        public static TeamRankings SeasonTeamRankings;
        public static TeamRankings PlayoffTeamRankings;
        public static PlayerRankings SeasonPlayerRankings;
        public static PlayerRankings PlayoffPlayerRankings;
        public static Timeframe Tf = new Timeframe(0);

        internal static Dictionary<int, TeamStats> RealTST = new Dictionary<int, TeamStats>();
        public static TeamBoxScore TempBSE_BS;

        public static string AddInfo;
        private static int _curSeason;
        public static List<Division> Divisions = new List<Division>();
        public static List<Conference> Conferences = new List<Conference>();

        public static int GameLength = 48;
        public static int SeasonLength = 82;
        public static int NumberOfPeriods = 4;
        public static int ShotClockDuration = 24;

        public static readonly ObservableCollection<KeyValuePair<int, string>> SeasonList =
            new ObservableCollection<KeyValuePair<int, string>>();

        public static List<SortableBindingList<PlayerBoxScore>> TempBSE_PBSLists;
        public static BoxScoreEntry TempLiveBSE;

        public static List<Dictionary<string, string>> SelectedTeams;
        public static bool SelectedTeamsChanged;

        /// <summary>Teams participating in the Western Conference of the NBA. Used to filter teams in the Playoff Tree window.</summary>
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

        public static SQLiteDatabase DB;
        public static bool LoadingSeason;
        private static bool _showUpdateMessage;

        private static readonly RoutedCommand CmndImport = new RoutedCommand();
        private static readonly RoutedCommand CmndOpen = new RoutedCommand();
        private static readonly RoutedCommand CmndSave = new RoutedCommand();
        private static readonly RoutedCommand CmndExport = new RoutedCommand();
        private static readonly RoutedCommand CmndFind = new RoutedCommand();

        private static List<string> _notables = new List<string>();

        public static PlayerRankings SeasonLeadersRankings;
        public static PlayerRankings PlayoffsLeadersRankings;
        public static bool IsImperial = true;
        public static string RatingsGPPctSetting, RatingsMPGSetting, MyLeadersGPPctSetting, MyLeadersMPGSetting;
        public static double RatingsGPPctRequired, MyLeadersGPPctRequired;
        public static float RatingsMPGRequired, MyLeadersMPGRequired;
        public static string LeadersPrefSetting;
        private static string _currentDB;
        private static readonly object _lock = new object();
        public static List<PlayByPlayEntry> TempBSE_PBPEList = new List<PlayByPlayEntry>();
        public static List<SearchItem> SearchCache;
        public readonly TaskScheduler UIScheduler;
        private DispatcherTimer _dispatcherTimer;
        private DispatcherTimer _marqueeTimer;
        private int _notableIndex;
        private double _progress;
        private Semaphore _sem;
        private bool _watchTimerRunning;
        private DispatcherTimer dt;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class. Creates the program's documents directories if needed,
        ///     initializes structures, and loads the settings from registry.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            MWInstance = this;
            TempBSE_BS = new TeamBoxScore();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en-US");
            setDefaultCulture(CultureInfo.CreateSpecificCulture("en-US"));

            //btnInject.Visibility = Visibility.Hidden;
#if DEBUG
            btnTest.Visibility = Visibility.Visible;
#else
            btnTest.Visibility = Visibility.Hidden;
            #endif

            if (Directory.Exists(PSFiltersPath) == false)
            {
                Directory.CreateDirectory(PSFiltersPath);
            }
            if (Directory.Exists(ASCFiltersPath) == false)
            {
                Directory.CreateDirectory(ASCFiltersPath);
            }
            var appTempPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\" + @"Temp\";
            if (Directory.Exists(appTempPath) == false)
            {
                Directory.CreateDirectory(appTempPath);
            }

            TST[0] = new TeamStats(-1, "$$NewDB");
            TSTOpp[0] = new TeamStats(-1, "$$NewDB");

            for (var i = 0; i < 30; i++)
            {
                RealTST[i] = new TeamStats();
            }

            RegistryKey rk = null;

            try
            {
                rk = Registry.CurrentUser;
            }
            catch (Exception ex)
            {
                App.ForceCriticalError(ex, "Registry.CurrentUser");
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

            CSV.ReplaceREDitorSortingChars = true;

            if (App.RealNBAOnly)
            {
                mnuFileGetRealStats_Click(null, null);
                MessageBox.Show("Nothing but net! Thanks for using NBA Stats Tracker!");
                Environment.Exit(-1);
            }
            else
            {
                var exportTeamsOnly = Tools.GetRegistrySetting("ExportTeamsOnly", 1);
                mnuOptionsExportTeamsOnly.IsChecked = exportTeamsOnly == 1;

                var compatibilityCheck = Tools.GetRegistrySetting("CompatibilityCheck", 1);
                mnuOptionsCompatibilityCheck.IsChecked = compatibilityCheck == 1;

                IsImperial = Tools.GetRegistrySetting("IsImperial", 1) == 1;
                mnuOptionsIsImperial.IsChecked = IsImperial;

                // Displays a message to urge the user to donate at the 50th start of the program.
                var timesStarted = Tools.GetRegistrySetting("TimesStarted", -1);
                if (timesStarted == -1)
                {
                    Tools.SetRegistrySetting("TimesStarted", 1);
                }
                else if (timesStarted <= 50)
                {
                    if (timesStarted == 50)
                    {
                        var r =
                            MessageBox.Show(
                                "Hey there! This is a friendly reminder from the creator of NBA Stats Tracker.\n\n"
                                + "You seem to like using NBA Stats Tracker a lot, and I'm sure you enjoy the fact that it's free. "
                                + "However, if you believe that I deserve your support and you want to help me to continue my studies, "
                                + "as well as continue developing and supporting NBA Stats Tracker, you can always donate!\n\n"
                                + "Even a small amount can help a lot!\n\n" + "Would you like to find out how you can donate?\n\n"
                                + "Clicking Cancel will make sure this message never shows up again.",
                                "NBA Stats Tracker - A friendly reminder",
                                MessageBoxButton.YesNoCancel,
                                MessageBoxImage.Information);
                        if (r == MessageBoxResult.Yes)
                        {
                            mnuHelpDonate_Click(null, null);
                        }
                        else if (r == MessageBoxResult.No)
                        {
                            timesStarted = -1;
                        }
                    }
                    Tools.SetRegistrySetting("TimesStarted", timesStarted + 1);
                }
            }
            UIScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            var metricsNames = PAbbr.MetricsNames;
            foreach (var name in metricsNames)
            {
                PAbbr.MetricsDict.Add(name, double.NaN);
            }

            SearchCache = new List<SearchItem>();

            #region Keyboard Shortcuts

            CmndImport.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(CmndImport, btnImport2K12_Click));

            CmndExport.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(CmndExport, btnExport2K12_Click));

            CmndOpen.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(CmndOpen, btnOpen_Click));

            CmndSave.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(CmndSave, btnSaveCurrentSeason_Click));

            CmndFind.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(CmndFind, QuickFind));

            #endregion

            //prepareImageCache();
        }

        public static string CurrentDB
        {
            get { return _currentDB; }
            set
            {
                _currentDB = value;
                DB = new SQLiteDatabase(value);
            }
        }

        public static int CurSeason
        {
            get { return _curSeason; }
            set
            {
                try
                {
                    _curSeason = value;
                    MWInstance.cmbSeasonNum.SelectedValue = CurSeason;
                }
                catch
                {
                }
            }
        }

        public static string TeamsT
        {
            get { return "Teams" + SQLiteIO.AddSuffix(Tf.SeasonNum, SQLiteIO.GetMaxSeason(CurrentDB)); }
        }

        public static string PlTeamsT
        {
            get { return "PlayoffTeams" + SQLiteIO.AddSuffix(Tf.SeasonNum, SQLiteIO.GetMaxSeason(CurrentDB)); }
        }

        public static string OppT
        {
            get { return "Opponents" + SQLiteIO.AddSuffix(Tf.SeasonNum, SQLiteIO.GetMaxSeason(CurrentDB)); }
        }

        public static string PlOppT
        {
            get { return "PlayoffOpponents" + SQLiteIO.AddSuffix(Tf.SeasonNum, SQLiteIO.GetMaxSeason(CurrentDB)); }
        }

        private void QuickFind(object sender, ExecutedRoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            var qfw = new QuickFindWindow();
            if (qfw.ShowDialog() != true)
            {
                return;
            }

            var item = QuickFindWindow.SelectedItem;
            if (item.Type == SearchItem.SelectionType.Team)
            {
                var w = new TeamOverviewWindow(item.ID);
                w.ShowDialog();
            }
            else
            {
                var w = new PlayerOverviewWindow(PST[item.ID].TeamF, item.ID);
                w.ShowDialog();
            }
        }

        private static void setDefaultCulture(CultureInfo culture)
        {
            var type = typeof(CultureInfo);

            try
            {
                type.InvokeMember(
                    "s_userDefaultCulture",
                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    culture,
                    new object[] { culture });

                type.InvokeMember(
                    "s_userDefaultUICulture",
                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    culture,
                    new object[] { culture });
            }
            catch
            {
            }

            try
            {
                type.InvokeMember(
                    "m_userDefaultCulture",
                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    culture,
                    new object[] { culture });

                type.InvokeMember(
                    "m_userDefaultUICulture",
                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    culture,
                    new object[] { culture });
            }
            catch
            {
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnImport2K12 control. Asks the user for the folder containing the NBA 2K12 save (in the case
        ///     of the old method), or the REDitor-exported CSV files.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void btnImport2K12_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(CurrentDB))
            {
                return;
            }

            if (CurSeason != SQLiteIO.GetMaxSeason(CurrentDB))
            {
                if (
                    MessageBox.Show(
                        "Note that the currently selected season isn't the latest one in the database. "
                        + "Are you sure you want to import into this season?",
                        "NBA Stats Tracker",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            if (Tf.IsBetween)
            {
                Tf = new Timeframe(CurSeason);
                await UpdateAllData();
            }

            var fbd = new FolderBrowserDialog
                {
                    Description = "Select folder with REDitor-exported CSVs",
                    ShowNewFolderButton = false,
                    SelectedPath = Tools.GetRegistrySetting("LastImportDir", "")
                };
            var dr = FolderBrowserLauncher.ShowFolderBrowser(fbd, this.GetIWin32Window());
            if (dr != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                return;
            }

            Tools.SetRegistrySetting("LastImportDir", fbd.SelectedPath);

            IsEnabled = false;
            status.Content = "Please wait...";
            status.FontWeight = FontWeights.Bold;

            var result = await Task.Run(() => REDitor.ImportCurrentYear(ref TST, ref TSTOpp, ref PST, fbd.SelectedPath));

            IsEnabled = true;
            status.FontWeight = FontWeights.Normal;
            status.Content = "Ready";

            if (result != 0)
            {
                MessageBox.Show(
                    "Import failed! Please reload your database immediatelly to avoid saving corrupt data.",
                    "NBA Stats Tracker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            UpdateStatus("NBA 2K12/2K13 stats imported successfully! Please save the current season!");
        }

        /// <summary>Handles the Click event of the mnuFileSaveAs control. Allows the user to save the database to a different file.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void mnuFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(CurrentDB))
            {
                return;
            }

            var sfd = new SaveFileDialog
                {
                    Filter = "NST Database (*.tst)|*.tst",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\"
                };
            sfd.ShowDialog();

            if (String.IsNullOrWhiteSpace(sfd.FileName))
            {
                return;
            }

            var file = sfd.FileName;

            IsEnabled = false;

            startProgressWatchTimer();
            ProgressHelper.Progress = new ProgressInfo(0, 10, "Saving new database...");
            var result = await Task.Run(() => SQLiteIO.SaveDatabaseAs(file));
            GameLength = SQLiteIO.GetSetting("Game Length", 48);
            SeasonLength = SQLiteIO.GetSetting("Season Length", 82);
            NumberOfPeriods = SQLiteIO.GetSetting("NumberOfPeriods", 4);
            ShotClockDuration = SQLiteIO.GetSetting("Shot Clock Duration", 24);

            Interlocked.Exchange(ref ProgressHelper.Progress, new ProgressInfo(ProgressHelper.Progress, "Updating notables..."));
            //MessageBox.Show(SQLiteIO.Progress.CurrentStage.ToString());
            updateNotables();
            Interlocked.Exchange(ref ProgressHelper.Progress, new ProgressInfo(ProgressHelper.Progress, "Updating search cache..."));
            UpdateSearchCache();

            if (_notables.Count > 0)
            {
                _marqueeTimer.Start();
            }

            mnuTools.IsEnabled = true;
            grdAnalysis.IsEnabled = true;
            grdUpdate.IsEnabled = true;

            StopProgressWatchTimer();
            IsEnabled = true;

            UpdateStatus(String.Format("{0} teams & {1} players loaded successfully!", TST.Count, PST.Count));
            LoadingSeason = false;
            mainGrid.Visibility = Visibility.Visible;
            StopProgressWatchTimer();
            IsEnabled = true;
            if (result)
            {
                txtFile.Text = file;
                UpdateStatus("All seasons saved successfully.");
            }
        }

        public void StopProgressWatchTimer()
        {
            Interlocked.Exchange(ref ProgressHelper.Progress, new ProgressInfo(ProgressHelper.Progress, "Finished"));
            ProgressHelper.Progress.Timing.Stop();
            try
            {
                ProgressWindow.PwInstance.CanClose = true;
                ProgressWindow.PwInstance.Close();
            }
            catch
            {
                Console.WriteLine("ProgressWindow couldn't be closed; maybe it wasn't open.");
            }
            dt.Stop();

            _watchTimerRunning = false;
        }

        /// <summary>Handles the Click event of the mnuFileOpen control. Opens a database.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async Task FileOpen()
        {
            LoadingSeason = true;
            TST = new Dictionary<int, TeamStats>();
            BSHist = new List<BoxScoreEntry>();

            var ofd = new OpenFileDialog
                {
                    Filter = "NST Database (*.tst)|*.tst",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\",
                    Title = "Please select the TST file that you want to edit..."
                };
            ofd.ShowDialog();

            if (String.IsNullOrWhiteSpace(ofd.FileName))
            {
                return;
            }

            populateSeasonCombo(ofd.FileName);

            txtFile.Text = ofd.FileName;
            CurrentDB = txtFile.Text;

            ChangeSeason(SeasonList.First().Key);

            LoadRatingsCriteria();
            LoadMyLeadersCriteria();
            var preferNBALeaders = LeadersPrefSetting == "NBA";
            MWInstance.mnuMiscPreferNBALeaders.IsChecked = preferNBALeaders;
            MWInstance.mnuMiscPreferMyLeaders.IsChecked = !preferNBALeaders;

            txtFile.Text = ofd.FileName;

            mainGrid.Visibility = Visibility.Hidden;

            startProgressWatchTimer();
            ProgressHelper.Progress = new ProgressInfo(0, "Loading database...");
            parseDBData(await Task.Run(() => SQLiteIO.LoadSeason()));

            GameLength = SQLiteIO.GetSetting("Game Length", 48);
            SeasonLength = SQLiteIO.GetSetting("Season Length", 82);
            NumberOfPeriods = SQLiteIO.GetSetting("NumberOfPeriods", 4);
            ShotClockDuration = SQLiteIO.GetSetting("Shot Clock Duration", 24);

            Interlocked.Exchange(ref ProgressHelper.Progress, new ProgressInfo(ProgressHelper.Progress, "Updating notables..."));
            //MessageBox.Show(SQLiteIO.Progress.CurrentStage.ToString());
            updateNotables();
            Interlocked.Exchange(ref ProgressHelper.Progress, new ProgressInfo(ProgressHelper.Progress, "Updating search cache..."));
            UpdateSearchCache();

            if (_notables.Count > 0)
            {
                _marqueeTimer.Start();
            }

            mnuTools.IsEnabled = true;
            grdAnalysis.IsEnabled = true;
            grdUpdate.IsEnabled = true;

            if (!false)
            {
                StopProgressWatchTimer();
                IsEnabled = true;
            }

            UpdateStatus(String.Format("{0} teams & {1} players loaded successfully!", TST.Count, PST.Count));
            LoadingSeason = false;
            mainGrid.Visibility = Visibility.Visible;

            //SQLiteIO.LoadSeason();
        }

        private void startProgressWatchTimer()
        {
            if (_watchTimerRunning)
            {
                return;
            }

            _watchTimerRunning = true;
            ProgressHelper.Progress = new ProgressInfo(0, 8, "");
            if (!IsActive)
            {
                var pw = new ProgressWindow("", false);
                pw.Show();
            }
            dt = new DispatcherTimer();
            //dt.Interval = new TimeSpan(100);
            dt.Tick += updateBasedOnProgress;
            dt.Start();
        }

        private void updateBasedOnProgress(object o, EventArgs args)
        {
            /*
            var newStatus = string.Format("Stage {0}/{1}: {2}", SQLiteIO.Progress.CurrentStage, SQLiteIO.Progress.MaxStage,
                                          SQLiteIO.Progress.Message);
            */
            var newStatus = string.Format("Stage {0}: {1}", ProgressHelper.Progress.CurrentStage, ProgressHelper.Progress.Message);
            if (ProgressHelper.Progress.Percentage > 0)
            {
                newStatus += " (" + ProgressHelper.Progress.Percentage + "%)";
            }
            UpdateStatus(newStatus, true);
            try
            {
                ProgressWindow.PwInstance.SetMessage(newStatus);
            }
            catch
            {
            }
        }

        private static void parseDBData(DBData dbData)
        {
            TST = dbData.TST;
            TSTOpp = dbData.TSTOpp;
            PST = dbData.PST;
            SeasonTeamRankings = dbData.SeasonTeamRankings;
            PlayoffTeamRankings = dbData.PlayoffTeamRankings;
            SplitTeamStats = dbData.SplitTeamStats;
            SplitPlayerStats = dbData.SplitPlayerStats;
            SeasonPlayerRankings = dbData.SeasonPlayerRankings;
            PlayoffPlayerRankings = dbData.PlayoffPlayerRankings;
            DisplayNames = dbData.DisplayNames;
            BSHist = dbData.BSHist;
        }

        private void UpdateSearchCache()
        {
            SearchCache.Clear();
            foreach (var team in TST)
            {
                SearchCache.Add(new SearchItem(SearchItem.SelectionType.Team, team.Key, team.Value.DisplayName));
            }
            foreach (var player in PST)
            {
                SearchCache.Add(new SearchItem(SearchItem.SelectionType.Player, player.Key, player.Value.FullInfo(TST)));
            }
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
            if (_notables.Count == 0)
            {
                txbMarquee.Text = "";
                _marqueeTimer.Stop();
                return;
            }

            if (_notableIndex < _notables.Count - 1)
            {
                _notableIndex++;
            }
            else
            {
                _notableIndex = 0;
            }

            txbMarquee.Text = _notables[_notableIndex];
        }

        /// <summary>Changes the current season.</summary>
        /// <param name="curSeason">The ID of the season to change to.</param>
        public static void ChangeSeason(int curSeason)
        {
            CurSeason = curSeason;
            MWInstance.cmbSeasonNum.SelectedValue = CurSeason.ToString();
        }

        /// <summary>
        ///     Handles the Click event of the btnLoadUpdate control. Opens the Box Score window to allow the user to update the team stats
        ///     by entering a box score.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void btnLoadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                UpdateStatus("No file is loaded or the file currently loaded is empty");
                return;
            }

            TempBSE_BS = new TeamBoxScore();
            var bsW = new BoxScoreWindow();
            bsW.ShowDialog();

            await parseBoxScoreResult();
        }

        /// <summary>
        ///     Parses the local box score instance; adds the stats to the according teams and players and adds the box score to the box
        ///     score history.
        /// </summary>
        private async Task parseBoxScoreResult()
        {
            if (TempBSE_BS.Done == false)
            {
                return;
            }

            IsEnabled = false;

            var id1 = TempBSE_BS.Team1ID;
            var id2 = TempBSE_BS.Team2ID;

            var list = TempBSE_PBSLists.SelectMany(pbsList => pbsList).ToList();

            if (!TempBSE_BS.DoNotUpdate)
            {
                TeamStats.AddTeamStatsFromBoxScore(TempBSE_BS, ref TST, ref TSTOpp, id1, id2);

                foreach (var pbs in list)
                {
                    if (pbs.PlayerID == -1)
                    {
                        continue;
                    }
                    PST[pbs.PlayerID].AddBoxScore(pbs, TempBSE_BS.IsPlayoff);
                }
            }

            if (TempBSE_BS.ID == -1)
            {
                TempBSE_BS.ID = getFreeBseID();
            }

            if (BSHist.Any(bse => bse.BS.ID == TempBSE_BS.ID) == false)
            {
                var bse = new BoxScoreEntry(TempBSE_BS, list, TempBSE_PBPEList);
                BSHist.Add(bse);
            }
            else
            {
                BSHist.Single(bse => bse.BS.ID == TempBSE_BS.ID).BS = TempBSE_BS;
            }

            startProgressWatchTimer();
            ProgressHelper.Progress = new ProgressInfo(0, "Inserting box score to database...");
            await
                Task.Run(() => SQLiteIO.SaveSeasonToDatabase(CurrentDB, TST, TSTOpp, PST, CurSeason, SQLiteIO.GetMaxSeason(CurrentDB)));
            await UpdateAllData();
            UpdateStatus("One or more Box Scores have been added/updated. Database saved.");
            StopProgressWatchTimer();
            IsEnabled = true;
        }

        /// <summary>Checks for software updates asynchronously.</summary>
        /// <param name="showMessage">
        ///     if set to <c>true</c>, a message will be shown even if no update is found.
        /// </param>
        public static void CheckForUpdates(bool showMessage = false)
        {
            _showUpdateMessage = showMessage;
            try
            {
                var webClient = new WebClient();
                const string updateUri = "http://students.ceid.upatras.gr/~aslanoglou/nstversion.txt";
                var appDocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\";
                if (!showMessage)
                {
                    webClient.DownloadFileCompleted += checkForUpdatesCompleted;
                    webClient.DownloadFileAsync(new Uri(updateUri), appDocsPath + @"nstversion.txt");
                }
                else
                {
                    webClient.DownloadFile(new Uri(updateUri), appDocsPath + @"nstversion.txt");
                    checkForUpdatesCompleted(null, null);
                }
            }
            catch
            {
            }
        }

        /// <summary>Checks the downloaded version file to see if there's a newer version, and displays a message if needed.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        ///     The <see cref="AsyncCompletedEventArgs" /> instance containing the event data.
        /// </param>
        private static void checkForUpdatesCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string[] updateInfo;
            string[] versionParts;
            try
            {
                updateInfo =
                    File.ReadAllLines(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\" + @"nstversion.txt");
                versionParts = updateInfo[0].Split('.');
            }
            catch
            {
                return;
            }
            var curVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var curVersionParts = curVersion.Split('.');
            var iVP = new int[versionParts.Length];
            var iCVP = new int[versionParts.Length];
            for (var i = 0; i < versionParts.Length; i++)
            {
                iVP[i] = Convert.ToInt32(versionParts[i]);
                iCVP[i] = Convert.ToInt32(curVersionParts[i]);
                if (iCVP[i] > iVP[i])
                {
                    break;
                }
                if (iVP[i] > iCVP[i])
                {
                    var message = "";
                    try
                    {
                        for (var j = 6; j < updateInfo.Length; j++)
                        {
                            message += updateInfo[j].Replace('\t', ' ') + "\n";
                        }
                    }
                    catch
                    {
                    }
                    var uio = new UpdateInfoContainer { CurVersion = curVersion, UpdateInfo = updateInfo, Message = message };
                    MWInstance.Dispatcher.BeginInvoke(new Action<object>(showUpdateWindow), uio);
                    return;
                }
            }
            if (_showUpdateMessage)
            {
                MessageBox.Show("No updates found!");
            }
        }

        private static void showUpdateWindow(object o)
        {
            var uio = (UpdateInfoContainer) o;
            var curVersion = uio.CurVersion;
            var updateInfo = uio.UpdateInfo;
            var message = uio.Message;
            var uw = new UpdateWindow(curVersion, updateInfo[0], message, updateInfo[2], updateInfo[1], updateInfo[3], updateInfo[4]);
            uw.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the btnEraseSettings control. Allows the user to erase the saved settings file for a particular
        ///     NBA 2K save.
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
                        "All NBA 2K12 Career Files (*.FXG; *.CMG; *.RFG; *.PMG; *.SMG)|*.FXG;*.CMG;*.RFG;*.PMG;*.SMG|"
                        + "Association files (*.FXG)|*.FXG|My Player files (*.CMG)|*.CMG|Season files (*.RFG)|*.RFG|Playoff files (*.PMG)|*.PMG|"
                        + "Create A Legend files (*.SMG)|*.SMG"
                };
            if (Directory.Exists(SavesPath))
            {
                ofd.InitialDirectory = SavesPath;
            }
            ofd.ShowDialog();
            if (String.IsNullOrWhiteSpace(ofd.FileName))
            {
                return;
            }

            var safefn = Tools.GetSafeFilename(ofd.FileName);
            var settingsFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\" + safefn
                               + ".cfg";
            if (File.Exists(settingsFile))
            {
                File.Delete(settingsFile);
            }
            MessageBox.Show("Settings for this file have been erased. You'll be asked to set them again next time you import it.");
        }

        /// <summary>Handles the Click event of the mnuExit control. Plans world domination and reticulates splines.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(-1);
        }

        /// <summary>Handles the Click event of the btnExport2K12 control. Exports the current team and player stats to an NBA 2K save.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void btnExport2K12_Click(object sender, RoutedEventArgs e)
        {
            if (TST.Count != 30)
            {
                MessageBox.Show("You can't export a database that has less/more than 30 teams to an NBA 2K12 save.");
                return;
            }

            if (CurSeason != SQLiteIO.GetMaxSeason(CurrentDB))
            {
                if (
                    MessageBox.Show(
                        "Note that the currently selected season isn't the latest one in the database. "
                        + "Are you sure you want to export from this season?",
                        "NBA Stats Tracker",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            if (Tf.IsBetween)
            {
                Tf = new Timeframe(CurSeason);
                await UpdateAllData();
            }

            var fbd = new FolderBrowserDialog
                {
                    Description = "Select folder with REDitor-exported CSVs",
                    ShowNewFolderButton = false,
                    SelectedPath = Tools.GetRegistrySetting("LastExportDir", "")
                };
            var dr = FolderBrowserLauncher.ShowFolderBrowser(fbd, this.GetIWin32Window());

            if (dr != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                return;
            }

            IsEnabled = false;

            Tools.SetRegistrySetting("LastExportDir", fbd.SelectedPath);

            if (mnuOptionsCompatibilityCheck.IsChecked)
            {
                var temptst = new Dictionary<int, TeamStats>();
                var temptstOpp = new Dictionary<int, TeamStats>();
                var temppst = new Dictionary<int, PlayerStats>();
                var result =
                    await Task.Run(() => REDitor.ImportCurrentYear(ref temptst, ref temptstOpp, ref temppst, fbd.SelectedPath, true));

                if (result != 0)
                {
                    MessageBox.Show("Export failed.");
                    IsEnabled = true;
                    return;
                }

                var incompatible = false;

                if (temptst.Count != TST.Count)
                {
                    incompatible = true;
                }
                else
                {
                    for (var i = 0; i < temptst.Count; i++)
                    {
                        if (temptst[i].Name != TST[i].Name)
                        {
                            incompatible = true;
                            break;
                        }

                        if ((!temptst[i].Record.SequenceEqual(TST[i].Record)) || (!temptst[i].PlRecord.SequenceEqual(TST[i].PlRecord)))
                        {
                            incompatible = true;
                            break;
                        }
                    }
                }

                if (incompatible)
                {
                    var r =
                        MessageBox.Show(
                            "The file currently loaded seems incompatible with the NBA 2K save you're trying to save into."
                            + "\nThis could be happening for a number of reasons:\n\n"
                            + "1. The file currently loaded isn't one that had stats imported to it from your 2K save.\n"
                            + "2. The Win/Loss record for one or more teams would be different after this procedure.\n\n"
                            + "If you're updating using a box score, then either you're not using the NST database you imported your stats\n"
                            + "into before the game, or you entered the box score incorrectly. Remember that you need to import your stats\n"
                            + "into a database right before the game starts, let the game end and save the Association, and then update the\n"
                            + "database using the box score. If you follow these steps correctly, you shouldn't get this message when you try\n"
                            + "to export the stats from the database to your 2K save.\n\n"
                            + "Are you sure you want to continue? SAVE CORRUPTION MAY OCCUR, AND I WON'T BE HELD LIABLE FOR IT. ALWAYS KEEP BACKUPS.",
                            "NBA Stats Tracker",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                    if (r == MessageBoxResult.No)
                    {
                        IsEnabled = true;
                        return;
                    }
                }
            }

            var eresult = REDitor.ExportCurrentYear(TST, PST, fbd.SelectedPath, mnuOptionsExportTeamsOnly.IsChecked);

            IsEnabled = true;

            if (eresult != 0)
            {
                MessageBox.Show("Export failed.");
                return;
            }
            UpdateStatus("Exported to " + fbd.SelectedPath + " successfully!");
        }

        /// <summary>Handles the Click event of the mnuHelpReadme control. Opens the Readme file with the default txt file handler.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuHelpReadme_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(AppPath + @"\readme.txt");
        }

        /// <summary>Handles the Click event of the mnuHelpAbout control. Shows the About window.</summary>
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
        ///     Handles the Click event of the mnuFileGetRealStats control. Downloads and imports the current NBA stats from
        ///     Basketball-Reference.com.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void mnuFileGetRealStats_Click(object sender, RoutedEventArgs e)
        {
            var file = "";

            if (!String.IsNullOrWhiteSpace(txtFile.Text))
            {
                var r =
                    MessageBox.Show(
                        "This will overwrite the stats for the current season. Are you sure?\n\nClick Yes to overwrite.\nClick No to create a new file automatically. Any unsaved changes to the current file will be lost.\nClick Cancel to return to the main window.",
                        "NBA Stats Tracker",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    file = CurrentDB;
                }
                else if (r == MessageBoxResult.No)
                {
                    txtFile.Text = "";
                }
                else
                {
                    return;
                }
            }

            if (String.IsNullOrWhiteSpace(txtFile.Text))
            {
                file = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\" + "Real NBA Stats "
                       + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + ".tst";
                if (File.Exists(file))
                {
                    if (App.RealNBAOnly)
                    {
                        var r =
                            MessageBox.Show(
                                "Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?",
                                "NBA Stats Tracker",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question,
                                MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                    else
                    {
                        var r =
                            MessageBox.Show(
                                "Today's Real NBA Stats have already been downloaded and saved. Are you sure you want to re-download them?",
                                "NBA Stats Tracker",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question,
                                MessageBoxResult.No);
                        if (r == MessageBoxResult.No)
                        {
                            await SQLiteIO.LoadSeason(file);
                            txtFile.Text = file;
                            return;
                        }
                    }
                }
            }

            //var grsw = new getRealStatsW();
            //grsw.ShowDialog();

            RealTST = new Dictionary<int, TeamStats>();
            var realTSTOpp = new Dictionary<int, TeamStats>();
            var realPST = new Dictionary<int, PlayerStats>();
            _sem = new Semaphore(1, 1);

            mainGrid.Visibility = Visibility.Hidden;
            txbWait.Visibility = Visibility.Visible;

            _progress = 0;

            //MessageBox.Show("Please wait after pressing OK, this could take a few minutes.");

            var teamNamesShort = new Dictionary<string, string>
                {
                    { "76ers", "PHI" },
                    { "Bobcats", "CHA" },
                    { "Bucks", "MIL" },
                    { "Bulls", "CHI" },
                    { "Cavaliers", "CLE" },
                    { "Celtics", "BOS" },
                    { "Clippers", "LAC" },
                    { "Grizzlies", "MEM" },
                    { "Hawks", "ATL" },
                    { "Heat", "MIA" },
                    { "Hornets", "NOH" },
                    { "Jazz", "UTA" },
                    { "Kings", "SAC" },
                    { "Knicks", "NYK" },
                    { "Lakers", "LAL" },
                    { "Magic", "ORL" },
                    { "Mavericks", "DAL" },
                    { "Nets", "BRK" },
                    { "Nuggets", "DEN" },
                    { "Pacers", "IND" },
                    { "Pistons", "DET" },
                    { "Raptors", "TOR" },
                    { "Rockets", "HOU" },
                    { "Spurs", "SAS" },
                    { "Suns", "PHO" },
                    { "Thunder", "OKC" },
                    { "Timberwolves", "MIN" },
                    { "Trail Blazers", "POR" },
                    { "Warriors", "GSW" },
                    { "Wizards", "WAS" }
                };

            var teamDivisions = new Dictionary<string, int>
                {
                    { "76ers", 0 },
                    { "Bobcats", 2 },
                    { "Bucks", 1 },
                    { "Bulls", 1 },
                    { "Cavaliers", 1 },
                    { "Celtics", 0 },
                    { "Clippers", 5 },
                    { "Grizzlies", 3 },
                    { "Hawks", 2 },
                    { "Heat", 2 },
                    { "Hornets", 3 },
                    { "Jazz", 4 },
                    { "Kings", 5 },
                    { "Knicks", 0 },
                    { "Lakers", 5 },
                    { "Magic", 2 },
                    { "Mavericks", 3 },
                    { "Nets", 0 },
                    { "Nuggets", 4 },
                    { "Pacers", 1 },
                    { "Pistons", 1 },
                    { "Raptors", 0 },
                    { "Rockets", 3 },
                    { "Spurs", 3 },
                    { "Suns", 5 },
                    { "Thunder", 4 },
                    { "Timberwolves", 4 },
                    { "Trail Blazers", 4 },
                    { "Warriors", 5 },
                    { "Wizards", 2 }
                };

            var k = 0;
            var teamOrder = new Dictionary<string, int>();
            REDitor.CreateDivisions();
            teamNamesShort.ToList().ForEach(
                tn =>
                    {
                        teamOrder.Add(tn.Key, k);
                        RealTST.Add(k, new TeamStats { ID = k, Name = tn.Key });
                        k++;
                    });

            foreach (var kvp in teamNamesShort)
            {
                await Task.Run(() => doGetRealTeam(kvp, teamOrder, teamDivisions, realTSTOpp, realPST));
                Tools.AppInvoke(getRealStats_UpdateProgressBar);
            }
            // TODO: Re-enable once Playoffs start
            BR.AddPlayoffTeamStats(ref RealTST, ref realTSTOpp);

            if (RealTST[0].Name != "Canceled")
            {
                TST = RealTST;
                TSTOpp = realTSTOpp;
                PST = realPST;
                if (CurSeason == 0)
                {
                    CurSeason = 1;
                }
                int maxSeason;
                if (File.Exists(file))
                {
                    maxSeason = SQLiteIO.GetMaxSeason(file);
                }
                else
                {
                    maxSeason = 0;
                }
                SQLiteIO.SaveSeasonToDatabase(file, TST, TSTOpp, PST, CurSeason, maxSeason);
                txtFile.Text = file;
                populateSeasonCombo(file);
                await SQLiteIO.LoadSeason(file, CurSeason);

                txbWait.Visibility = Visibility.Hidden;
                mainGrid.Visibility = Visibility.Visible;

                mnuTools.IsEnabled = true;
                grdAnalysis.IsEnabled = true;
                grdUpdate.IsEnabled = true;

                UpdateStatus("The download of real NBA stats is done.");
            }
            else
            {
                MessageBox.Show(
                    "The download of real NBA stats was cancelled. Please reload the current database, if any, before continuing work.");
            }
        }

        private static void doGetRealTeam(
            KeyValuePair<string, string> kvp,
            Dictionary<string, int> teamOrder,
            Dictionary<string, int> teamDivisions,
            Dictionary<int, TeamStats> realTSTOpp,
            Dictionary<int, PlayerStats> realPST)
        {
            Dictionary<int, PlayerStats> temppst;
            TeamStats realts;
            TeamStats realtsopp;
            BR.ImportRealStats(kvp, out realts, out realtsopp, out temppst);
            var id = teamOrder[kvp.Key];
            RealTST[id] = realts;
            RealTST[id].Division = teamDivisions[realts.Name];
            RealTST[id].Conference = Divisions.Single(d => d.ID == RealTST[id].Division).ConferenceID;
            realTSTOpp[id] = realtsopp;
            realTSTOpp[id].ID = id;
            realTSTOpp[id].Division = RealTST[id].Division;
            realTSTOpp[id].Conference = RealTST[id].Conference;
            foreach (var kvp2 in temppst)
            {
                kvp2.Value.ID = realPST.Count;
                realPST.Add(realPST.Count, kvp2.Value);
            }
        }

        /// <summary>Updates the progress bar during the download of the real NBA stats.</summary>
        private void getRealStats_UpdateProgressBar()
        {
            _progress += (double) 100 / 30;
            var percentage = (int) _progress;
            if (percentage < 97)
            {
                status.Content = "Downloading NBA stats from Basketball-Reference.com (" + percentage + "% complete)...";
            }
            else
            {
                status.Content = "Season stats downloaded, now downloading playoff stats and saving...";
            }
        }

        /// <summary>
        ///     Handles the TextChanged event of the txtFile control. Updates the currentDB field of MainWindow with the new file loaded.
        ///     Usually called after Open or Save As.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="TextChangedEventArgs" /> instance containing the event data.
        /// </param>
        private void txtFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtFile.Text))
            {
                return;
            }

            txtFile.ScrollToHorizontalOffset(txtFile.GetRectFromCharacterIndex(txtFile.Text.Length).Right);
        }

        /// <summary>Populates the season combo using a specified NST database file.</summary>
        /// <param name="file">The file from which to determine the available seasons.</param>
        private void populateSeasonCombo(string file)
        {
            DB = new SQLiteDatabase(file);

            generateSeasons();
        }

        /// <summary>Populates the season combo using the current database.</summary>
        private void populateSeasonCombo()
        {
            populateSeasonCombo(CurrentDB);
        }

        /// <summary>Generates the entries used to populate the season combo.</summary>
        private void generateSeasons()
        {
            const string qr = "SELECT * FROM SeasonNames ORDER BY ID DESC";
            var dataTable = DB.GetDataTable(qr);
            SeasonList.Clear();
            foreach (DataRow row in dataTable.Rows)
            {
                var id = ParseCell.GetInt32(row, "ID");
                var name = ParseCell.GetString(row, "Name");
                SeasonList.Add(new KeyValuePair<int, string>(id, name));
            }

            cmbSeasonNum.ItemsSource = SeasonList;

            cmbSeasonNum.SelectedValue = CurSeason;
        }

        /// <summary>Handles the SelectionChanged event of the cmbSeasonNum control. Changes the curSeason property accordingly.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="SelectionChangedEventArgs" /> instance containing the event data.
        /// </param>
        private async void cmbSeasonNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSeasonNum.SelectedIndex == -1)
            {
                return;
            }

            CurSeason = ((KeyValuePair<int, string>) (((cmbSeasonNum)).SelectedItem)).Key;

            if (CurSeason == Tf.SeasonNum && !Tf.IsBetween)
            {
                return;
            }

            Tf = new Timeframe(CurSeason);
            if (!LoadingSeason)
            {
                IsEnabled = false;
                await UpdateAllData();
                IsEnabled = true;
            }
        }

        /// <summary>Calculates the difference in a team's stats rankingsPerGame between two TeamRankings instances.</summary>
        /// <param name="oldR">The old team rankingsPerGame.</param>
        /// <param name="newR">The new team rankingsPerGame.</param>
        /// <returns></returns>
        private int[][] calculateDifferenceRanking(TeamRankings oldR, TeamRankings newR)
        {
            var diff = new int[30][];
            for (var i = 0; i < 30; i++)
            {
                diff[i] = new int[18];
                for (var j = 0; j < 18; j++)
                {
                    diff[i][j] = newR.RankingsPerGame[i][j] - oldR.RankingsPerGame[i][j];
                }
            }
            return diff;
        }

        /// <summary>Calculates the difference average.</summary>
        /// <param name="curTST">The cur TST.</param>
        /// <param name="oldTST">The old TST.</param>
        /// <returns></returns>
        private float[][] calculateDifferenceAverage(Dictionary<int, TeamStats> curTST, Dictionary<int, TeamStats> oldTST)
        {
            var diff = new float[30][];
            for (var i = 0; i < 30; i++)
            {
                diff[i] = new float[18];
                for (var j = 0; j < 18; j++)
                {
                    diff[i][j] = curTST[i].PerGame[j] - oldTST[i].PerGame[j];
                }
            }
            return diff;
        }

        /// <summary>
        ///     Handles the Click event of the btnTest control. Displays the Test window or runs a specific test method. Used for various
        ///     debugging purposes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            var w = new TestWindow();
            w.ShowDialog();
#endif
        }

        /// <summary>Recalculates the opponent stats for all teams by accumulating the stats from the box scores.</summary>
        private static void recalculateOpponentStats()
        {
            var temptst = TST.ToDictionary(to => to.Key, to => to.Value.CustomClone());

            foreach (var key in TSTOpp.Keys)
            {
                temptst[key].ResetStats(Span.SeasonAndPlayoffs);
                TSTOpp[key].ResetStats(Span.SeasonAndPlayoffs);
            }

            foreach (var bse in BSHist)
            {
                if (bse.BS.SeasonNum == _curSeason)
                {
                    TeamStats.AddTeamStatsFromBoxScore(bse.BS, ref temptst, ref TSTOpp);
                }
            }
        }

        /// <summary>Updates a specific box score using the local box score instance.</summary>
        public static async Task UpdateBoxScore()
        {
            if (TempBSE_BS.ID == -1 || !TempBSE_BS.Done)
            {
                return;
            }

            var list = TempBSE_PBSLists.SelectMany(pbsList => pbsList).ToList();

            var bse = BSHist.Single(entry => entry.BS.ID == TempBSE_BS.ID);
            bse.BS = TempBSE_BS;
            bse.PBSList = list;
            bse.PBPEList = TempBSE_PBPEList;
            bse.MustUpdate = true;

            MessageBox.Show("The database will now save to update the box score. This could take a few moments.");
            SQLiteIO.SaveSeasonToDatabase();
            await UpdateAllData();
        }

        /// <summary>Handles the Click event of the btnTeamOverview control. Displays the Team Overview window.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnTeamOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(CurrentDB))
            {
                return;
            }
            if (SQLiteIO.IsTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using the Analysis features.");
                return;
            }

            dispatcherTimer_Tick(null, null);
            var tow = new TeamOverviewWindow();
            tow.ShowDialog();
        }

        /// <summary>Handles the Click event of the btnOpen control. Opens a database.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            await FileOpen();
        }

        /// <summary>
        ///     Handles the Loaded event of the Window control. Creates the DispatcherTimer instance used to revert the status bar message
        ///     after a number of seconds.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            _marqueeTimer = new DispatcherTimer();
            _marqueeTimer.Tick += marqueeTimer_Tick;
            _marqueeTimer.Interval = new TimeSpan(0, 0, 6);

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

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 4);
            //dispatcherTimer.Start();

            var checkForUpdatesSetting = Tools.GetRegistrySetting("CheckForUpdates", 1);
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

            try
            {
                Directory.GetFiles(App.AppTempPath).ToList().ForEach(File.Delete);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't delete temp files: " + ex.Message);
            }
        }

        /// <summary>Handles the Tick event of the dispatcherTimer control. Reverts the status bar message to "Ready".</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="EventArgs" /> instance containing the event data.
        /// </param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            status.FontWeight = FontWeights.Normal;
            status.Content = "Ready";
            _dispatcherTimer.Stop();
        }

        /// <summary>Updates the status bar message and starts the timer which will revert it after a number of seconds.</summary>
        /// <param name="newStatus">The new status.</param>
        /// <param name="noResetTimer">
        ///     If <c>true</c>, the status won't be reset after a few seconds.
        /// </param>
        public void UpdateStatus(string newStatus, bool noResetTimer = false)
        {
            _dispatcherTimer.Stop();
            status.FontWeight = FontWeights.Bold;
            status.Content = newStatus;
            if (!noResetTimer)
            {
                _dispatcherTimer.Start();
            }
        }

        /// <summary>Handles the Click event of the btnSaveCurrentSeason control. Saves the current season.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        public async void btnSaveCurrentSeason_Click(object sender, RoutedEventArgs e)
        {
            if (Tf.IsBetween)
            {
                Misc.ShowInformativeMessage(
                    "The current timeframe is set to between specific dates. To save the season, switch to "
                    + "a season timeframe, do any changes you want, and save.");
                return;
            }

            IsEnabled = false;
            startProgressWatchTimer();
            ProgressHelper.Progress = new ProgressInfo(0, "Saving database...");
            await
                Task.Run(() => SQLiteIO.SaveSeasonToDatabase(CurrentDB, TST, TSTOpp, PST, CurSeason, SQLiteIO.GetMaxSeason(CurrentDB)));
            await UpdateAllData(true);
            StopProgressWatchTimer();
            txtFile.Text = CurrentDB;
            MWInstance.UpdateStatus("File saved successfully. Season " + CurSeason.ToString() + " updated.");
            IsEnabled = true;
        }

        /// <summary>Handles the Click event of the btnLeagueOverview control. Displays the League Overview window.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnLeagueOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(CurrentDB))
            {
                return;
            }
            /*
            if (!isCustom)
            {
                MessageBox.Show("Save the data into a Team Stats file before using the tool's features.");
                return;
            }
            */
            if (SQLiteIO.IsTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using any Analysis features.");
                return;
            }

            dispatcherTimer_Tick(null, null);
            var low = new LeagueOverviewWindow();
            low.ShowDialog();
        }

        /// <summary>Handles the Click event of the mnuFileNew control. Allows the user to create a new database.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void mnuFileNew_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
                {
                    Filter = "NST Database (*.tst)|*.tst",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\"
                };
            sfd.ShowDialog();

            if (sfd.FileName == "")
            {
                return;
            }

            try
            {
                File.Delete(sfd.FileName);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Couldn't create the database.\n" + ex.Message);
                return;
            }

            DB = new SQLiteDatabase(sfd.FileName);

            SQLiteIO.PrepareNewDB(DB, 1, 1);

            TST = new Dictionary<int, TeamStats>();
            TSTOpp = new Dictionary<int, TeamStats>();
            PST = new Dictionary<int, PlayerStats>();
            BSHist = new List<BoxScoreEntry>();

            txtFile.Text = sfd.FileName;
            CurrentDB = txtFile.Text;
            populateSeasonCombo();
            ChangeSeason(1);

            SQLiteIO.SetSetting("Game Length", 48);
            SQLiteIO.SetSetting("Season Length", 82);

            mnuTools.IsEnabled = true;
            grdAnalysis.IsEnabled = true;
            grdUpdate.IsEnabled = true;

            MWInstance.IsEnabled = false;
            await UpdateAllData();
            StopProgressWatchTimer();
            MWInstance.IsEnabled = true;
        }

        /// <summary>Handles the Click event of the btnAdd control. Allows the user to add teams or players the database.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(CurrentDB))
            {
                return;
            }

            AddInfo = "";
            var aw = new AddWindow(ref PST);
            aw.ShowDialog();

            if (!String.IsNullOrEmpty(AddInfo))
            {
                if (AddInfo != "$$NST Players Added")
                {
                    var parts = Tools.SplitLinesToArray(AddInfo);
                    var newTeams = parts.Where(s => !String.IsNullOrWhiteSpace(s)).ToList();

                    var oldlen = TST.Count;
                    if (SQLiteIO.IsTSTEmpty())
                    {
                        oldlen = 0;
                    }

                    for (var i = 0; i < newTeams.Count; i++)
                    {
                        if (TST.Count(pair => pair.Value.Name == newTeams[i]) == 1)
                        {
                            MessageBox.Show(
                                "There's a team with the name " + newTeams[i] + " already in the database so it won't be added again.");
                            continue;
                        }
                        var newid = oldlen + i;
                        TST[newid] = new TeamStats(newid, newTeams[i]);
                        TSTOpp[newid] = new TeamStats(newid, newTeams[i]);
                    }
                    SQLiteIO.SaveSeasonToDatabase();
                    UpdateStatus("Teams were added, database saved.");
                }
                else
                {
                    SQLiteIO.SavePlayersToDatabase(CurrentDB, PST, CurSeason, SQLiteIO.GetMaxSeason(CurrentDB));
                    UpdateStatus("Players were added, database saved.");
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnGrabNBAStats control. Allows the user to download the current NBA stats from
        ///     Basketball-Reference.com.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnGrabNBAStats_Click(object sender, RoutedEventArgs e)
        {
            mnuFileGetRealStats_Click(null, null);
        }

        /// <summary>Handles the Closed event of the Window control. Makes sure the application shuts down properly after this window closes.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="EventArgs" /> instance containing the event data.
        /// </param>
        private void window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>Handles the Click event of the mnuMiscStartNewSeason control. Allows the user to add a new season to the database.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void mnuMiscStartNewSeason_Click(object sender, RoutedEventArgs e)
        {
            if (!SQLiteIO.IsTSTEmpty())
            {
                var r =
                    MessageBox.Show(
                        "Are you sure you want to do this? This is an irreversible action.\nStats and box Scores will be retained, and you'll be able to use all the tool's features on them.",
                        "NBA Stats Tracker",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    SQLiteIO.SaveSeasonToDatabase();

                    CurSeason = SQLiteIO.GetMaxSeason(CurrentDB);
                    var ibw = new InputBoxWindow("Enter a name for the new season", (CurSeason + 1).ToString());
                    ibw.ShowDialog();

                    var seasonName = String.IsNullOrWhiteSpace(InputBoxWindow.UserInput)
                                         ? (CurSeason + 1).ToString()
                                         : InputBoxWindow.UserInput;

                    var q = "alter table Teams rename to TeamsS" + CurSeason;
                    DB.ExecuteNonQuery(q);

                    q = "alter table PlayoffTeams rename to PlayoffTeamsS" + CurSeason;
                    DB.ExecuteNonQuery(q);

                    q = "alter table Opponents rename to OpponentsS" + CurSeason;
                    DB.ExecuteNonQuery(q);

                    q = "alter table PlayoffOpponents rename to PlayoffOpponentsS" + CurSeason;
                    DB.ExecuteNonQuery(q);

                    q = "alter table Players rename to PlayersS" + CurSeason;
                    DB.ExecuteNonQuery(q);

                    q = "alter table PlayoffPlayers rename to PlayoffPlayersS" + CurSeason;
                    DB.ExecuteNonQuery(q);

                    CurSeason++;

                    SQLiteIO.PrepareNewDB(DB, CurSeason, CurSeason, true);
                    DB.Insert("SeasonNames", new Dictionary<string, string> { { "ID", CurSeason.ToString() }, { "Name", seasonName } });
                    SeasonList.Add(new KeyValuePair<int, string>(CurSeason, seasonName));

                    foreach (var key in TST.Keys.ToList())
                    {
                        var ts = TST[key];
                        for (var i = 0; i < ts.Totals.Length; i++)
                        {
                            ts.Totals[i] = 0;
                            ts.PlTotals[i] = 0;
                        }
                        ts.Record[0] = 0;
                        ts.Record[1] = 0;
                        ts.PlRecord[0] = 0;
                        ts.PlRecord[1] = 0;
                        ts.CalcAvg();
                        TST[key] = ts;
                    }

                    foreach (var key in TSTOpp.Keys.ToList())
                    {
                        var ts = TSTOpp[key];
                        for (var i = 0; i < ts.Totals.Length; i++)
                        {
                            ts.Totals[i] = 0;
                            ts.PlTotals[i] = 0;
                        }
                        ts.Record[0] = 0;
                        ts.Record[1] = 0;
                        ts.PlRecord[0] = 0;
                        ts.PlRecord[1] = 0;
                        ts.CalcAvg();
                        TSTOpp[key] = ts;
                    }

                    foreach (var ps in PST)
                    {
                        for (var i = 0; i < ps.Value.Totals.Length; i++)
                        {
                            ps.Value.Totals[i] = 0;
                            ps.Value.PlTotals[i] = 0;
                        }
                        ps.Value.IsAllStar = false;
                        ps.Value.IsNBAChampion = false;
                        if (ps.Value.Contract.Option == PlayerContractOption.Team2Yr)
                        {
                            ps.Value.Contract.Option = PlayerContractOption.Team;
                        }
                        else if (ps.Value.Contract.Option == PlayerContractOption.Team
                                 || ps.Value.Contract.Option == PlayerContractOption.Player)
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

                    populateSeasonCombo();
                    SQLiteIO.SaveSeasonToDatabase(CurrentDB, TST, TSTOpp, PST, CurSeason, CurSeason);
                    ChangeSeason(CurSeason);
                    Tf = new Timeframe(CurSeason);
                    IsEnabled = false;
                    await UpdateAllData();
                    UpdateStatus("New season started. Database saved.");
                    IsEnabled = true;
                }
            }
        }

        /// <summary>Handles the Click event of the btnSaveAllSeasons control. Saves all the seasons to the database.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void btnSaveAllSeasons_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            startProgressWatchTimer();
            ProgressHelper.Progress = new ProgressInfo(0, "Saving all seasons...");
            await Task.Run(() => SQLiteIO.SaveAllSeasons(CurrentDB));
            UpdateStatus("All seasons saved successfully!");
            StopProgressWatchTimer();
            IsEnabled = true;
        }

        /// <summary>Handles the Click event of the btnPlayerOverview control. Opens the Player Overview window.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnPlayerOverview_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(CurrentDB))
            {
                return;
            }
            if (SQLiteIO.IsTSTEmpty())
            {
                MessageBox.Show("You need to create a team or import stats before using the Analysis features.");
                return;
            }

            dispatcherTimer_Tick(null, null);
            var pow = new PlayerOverviewWindow();
            pow.ShowDialog();
        }

        /// <summary>Handles the Click event of the mnuMiscImportBoxScores control. Allows the user to import box scores from another database.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscImportBoxScores_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    Filter = "NST Database (*.tst)|*.tst",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NBA Stats Tracker\",
                    Title = "Please select the TST file that you want to import from..."
                };
            ofd.ShowDialog();

            if (String.IsNullOrWhiteSpace(ofd.FileName))
            {
                return;
            }

            var file = ofd.FileName;

            var maxSeason = SQLiteIO.GetMaxSeason(file);
            for (var i = 1; i <= maxSeason; i++)
            {
                var newBShist = SQLiteIO.GetSeasonBoxScoresFromDatabase(file, i, maxSeason, TST, PST);

                foreach (var newbse in newBShist)
                {
                    var doNotAdd = false;
                    foreach (var bse in BSHist)
                    {
                        if (bse.BS.ID == newbse.BS.ID)
                        {
                            if (bse.BS.GameDate == newbse.BS.GameDate && bse.BS.Team1ID == newbse.BS.Team1ID
                                && bse.BS.Team2ID == newbse.BS.Team2ID)
                            {
                                MessageBoxResult r;
                                if (bse.BS.PTS1 == newbse.BS.PTS1 && bse.BS.PTS2 == newbse.BS.PTS2)
                                {
                                    r =
                                        MessageBox.Show(
                                            "A box score with the same date, teams and score as one in the current database was found in the file being imported."
                                            + "\n" + bse.BS.GameDate.ToShortDateString() + ": " + bse.BS.Team1ID + " " + bse.BS.PTS1
                                            + " @ " + bse.BS.Team2ID + " " + bse.BS.PTS2
                                            + "\n\nClick Yes to only keep the box score that is already in this databse."
                                            + "\nClick No to only keep the box score that is being imported, replacing the one in this database."
                                            + "\nClick Cancel to keep both box scores.",
                                            "NBA Stats Tracker",
                                            MessageBoxButton.YesNoCancel,
                                            MessageBoxImage.Question);
                                }
                                else
                                {
                                    r =
                                        MessageBox.Show(
                                            "A box score with the same date, teams and score as one in the current database was found in the file being imported."
                                            + "\nCurrent: " + bse.BS.GameDate.ToShortDateString() + ": " + bse.BS.Team1ID + " "
                                            + bse.BS.PTS1 + " @ " + bse.BS.Team2ID + " " + bse.BS.PTS2 + "\nTo be imported: "
                                            + newbse.BS.GameDate.ToShortDateString() + ": " + newbse.BS.Team1ID + " " + newbse.BS.PTS1
                                            + " @ " + newbse.BS.Team2ID + " " + newbse.BS.PTS2
                                            + "\n\nClick Yes to only keep the box score that is already in this databse."
                                            + "\nClick No to only keep the box score that is being imported, replacing the one in this database."
                                            + "\nClick Cancel to keep both box scores.",
                                            "NBA Stats Tracker",
                                            MessageBoxButton.YesNoCancel,
                                            MessageBoxImage.Question);
                                }
                                if (r == MessageBoxResult.Yes)
                                {
                                    doNotAdd = true;
                                    break;
                                }

                                if (r == MessageBoxResult.No)
                                {
                                    BSHist.Remove(bse);
                                    break;
                                }
                            }
                            newbse.BS.ID = getFreeBseID();
                            break;
                        }
                    }
                    if (!doNotAdd)
                    {
                        newbse.MustUpdate = true;
                        BSHist.Add(newbse);
                    }
                }
            }
        }

        /// <summary>Gets the first free BoxScoreEntry ID in the box score history list.</summary>
        /// <returns></returns>
        private static int getFreeBseID()
        {
            var bseIDs = BSHist.Select(bse => bse.BS.ID).ToList();

            bseIDs.Sort();

            var i = 0;
            while (true)
            {
                if (!bseIDs.Contains(i))
                {
                    return i;
                }
                i++;
            }
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEnableTeams control. Used to enable/disable (i.e. show/hide) teams for the current
        ///     season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEnableTeams_Click(object sender, RoutedEventArgs e)
        {
            AddInfo = "";
            var etw = new DualListWindow(CurrentDB, CurSeason, SQLiteIO.GetMaxSeason(CurrentDB), DualListWindow.Mode.HiddenTeams);
            etw.ShowDialog();

            if (AddInfo == "$$TEAMSENABLED")
            {
                SQLiteIO.GetAllTeamStatsFromDatabase(CurrentDB, CurSeason, out TST, out TSTOpp);
                UpdateStatus("Teams were enabled/disabled. Database saved.");
            }
        }

        /// <summary>Handles the Click event of the mnuMiscDeleteBoxScores control. Allows the user to delete box score entries.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void mnuMiscDeleteBoxScores_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            var bslw = new BoxScoreListWindow();
            bslw.ShowDialog();
            await SQLiteIO.LoadSeason();
        }

        /// <summary>
        ///     Handles the Click event of the btnPlayerSearch control. Allows the user to search for players fulfilling any combination of
        ///     user-specified criteria.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnPlayerSearch_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            var psw = new PlayerSearchWindow();
            psw.ShowDialog();
        }

        /// <summary>Handles the Click event of the mnuMiscResetTeamStats control. Allows the user to reset all team stats for the current season.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscResetTeamStats_Click(object sender, RoutedEventArgs e)
        {
            if (!SQLiteIO.IsTSTEmpty())
            {
                var r =
                    MessageBox.Show(
                        "Are you sure you want to do this? This is an irreversible action.\nThis only applies to the current season.",
                        "Reset All Team Stats",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    foreach (var key in TST.Keys)
                    {
                        TST[key].ResetStats(Span.SeasonAndPlayoffs);
                    }

                    foreach (var key in TSTOpp.Keys)
                    {
                        TSTOpp[key].ResetStats(Span.SeasonAndPlayoffs);
                    }
                }
            }

            SQLiteIO.SaveSeasonToDatabase();

            UpdateStatus("All Team Stats for current season have been reset. Database saved.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscResetPlayerStats control. Allows the user to reset all player stats for the current
        ///     season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscResetPlayerStats_Click(object sender, RoutedEventArgs e)
        {
            if (!SQLiteIO.IsTSTEmpty())
            {
                var r =
                    MessageBox.Show(
                        "Are you sure you want to do this? This is an irreversible action.\nThis only applies to the current season.",
                        "Reset All Team Stats",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    foreach (var ps in PST.Values)
                    {
                        ps.ResetStats();
                    }
                }
            }

            SQLiteIO.SaveSeasonToDatabase();

            UpdateStatus("All Player Stats for current season have been reset. Database saved.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuOptionsCheckForUpdates control. Enables/disables the automatic check for updates each time
        ///     the program starts.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuOptionsCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            SetCheckForUpdatesRegistrySetting();
        }

        internal void SetCheckForUpdatesRegistrySetting()
        {
            Tools.SetRegistrySetting("CheckForUpdates", mnuOptionsCheckForUpdates.IsChecked ? 1 : 0);
        }

        /// <summary>
        ///     Handles the Click event of the mnuOptionsExportTeamsOnly control. Sets whether only the team stats will be exported when
        ///     exporting to an NBA 2K save.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuOptionsExportTeamsOnly_Click(object sender, RoutedEventArgs e)
        {
            Tools.SetRegistrySetting("ExportTeamsOnly", mnuOptionsExportTeamsOnly.IsChecked ? 1 : 0);
        }

        /// <summary>
        ///     Handles the Click event of the mnuOptionsCompatibilityCheck control. Sets whether the database-save compatibility check will
        ///     be run before exporting to an NBA 2K save.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuOptionsCompatibilityCheck_Click(object sender, RoutedEventArgs e)
        {
            Tools.SetRegistrySetting("CompatibilityCheck", mnuOptionsCompatibilityCheck.IsChecked ? 1 : 0);
        }

        /// <summary>Handles the Click event of the mnuMiscRenameCurrentSeason control. Renames the current season.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscRenameCurrentSeason_Click(object sender, RoutedEventArgs e)
        {
            var curName = GetSeasonName(CurSeason);
            var ibw = new InputBoxWindow("Enter the new name for the current season", curName);
            ibw.ShowDialog();

            if (!String.IsNullOrWhiteSpace(InputBoxWindow.UserInput))
            {
                setSeasonName(CurSeason, InputBoxWindow.UserInput);
                cmbSeasonNum.SelectedValue = CurSeason;
            }
        }

        /// <summary>Sets the name of the specified season.</summary>
        /// <param name="season">The season.</param>
        /// <param name="name">The name.</param>
        private static void setSeasonName(int season, string name)
        {
            for (var i = 0; i < SeasonList.Count; i++)
            {
                if (SeasonList[i].Key == season)
                {
                    SeasonList[i] = new KeyValuePair<int, string>(season, name);
                    break;
                }
            }

            SQLiteIO.SaveSeasonName(season);
        }

        /// <summary>Gets the name of the specified season.</summary>
        /// <param name="season">The season.</param>
        /// <returns></returns>
        public static string GetSeasonName(int season)
        {
            return SeasonList.Single(
                delegate(KeyValuePair<int, string> kvp)
                    {
                        if (kvp.Key == season)
                        {
                            return true;
                        }
                        return false;
                    }).Value;
        }

        /// <summary>Handles the Click event of the mnuMiscLiveBoxScore control. Allows the user to keep track of the box score of a live game.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private async void mnuMiscLiveBoxScore_Click(object sender, RoutedEventArgs e)
        {
            var lbsw = new LiveBoxScoreWindow();
            if (lbsw.ShowDialog() == true)
            {
                var bsw = new BoxScoreWindow(TempLiveBSE);
                bsw.ShowDialog();

                await parseBoxScoreResult();
            }
        }

        /// <summary>
        ///     Handles the Click event of the btnDownloadBoxScore control. Allows the user to download and import a box score from
        ///     Basketball-Reference.com.
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
                    var result = BR.ImportBoxScore(InputBoxWindow.UserInput);
                    if (result == -1)
                    {
                        MessageBox.Show(
                            "The Box Score was saved, but some required players were missing.\nMake sure you're using an up-to-date downloaded database."
                            + "\n\nThe database wasn't saved in case you want to reload it and try again.");
                    }
                    else
                    {
                        SQLiteIO.SaveSeasonToDatabase();
                        UpdateStatus("Box score downloaded and season saved!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Couldn't download & import box score.\n\nError: " + ex.Message);
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEnablePlayers control. Allows the user to enable/disable (i.e. show/hide) specific
        ///     players in the current season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEnablePlayers_Click(object sender, RoutedEventArgs e)
        {
            AddInfo = "";
            var etw = new DualListWindow(CurrentDB, CurSeason, SQLiteIO.GetMaxSeason(CurrentDB), DualListWindow.Mode.HiddenPlayers);
            etw.ShowDialog();

            if (AddInfo == "$$PLAYERSENABLED")
            {
                PST = SQLiteIO.GetPlayersFromDatabase(CurrentDB, TST, TSTOpp, CurSeason, SQLiteIO.GetMaxSeason(CurrentDB));
                UpdateStatus("Players were enabled/disabled. Database saved.");
            }
        }

        /// <summary>Copies the team & player stats dictionaries to the corresponding local MainWindow instances.</summary>
        /// <param name="teamStats">The team stats.</param>
        /// <param name="oppStats">The opp stats.</param>
        /// <param name="playerStats">The player stats.</param>
        public static void CopySeasonToMainWindow(
            Dictionary<int, TeamStats> teamStats, Dictionary<int, TeamStats> oppStats, Dictionary<int, PlayerStats> playerStats)
        {
            TST = teamStats;
            TSTOpp = oppStats;
            PST = playerStats;
        }

        /// <summary>Handles the Click event of the mnuHelpDonate control. Shows the user a website prompting for a donation.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuHelpDonate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://users.tellas.gr/~aslan16/donate.html");
        }

        /// <summary>Handles the Click event of the mnuMiscEditDivisions control. Allows the user to edit the divisions and conferences.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEditDivisions_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(CurrentDB))
            {
                return;
            }

            var lw = new ListWindow();
            lw.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscRecalculateOppStats control. Allows the user to recalculate the opponent stats by
        ///     accumulating the stats in the available box scores.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscRecalculateOppStats_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            recalculateOpponentStats();

            UpdateStatus("Opponent stats for the current season have been recalculated. You should save the current season now.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEditGameLength control. Allows the user to change the default game length in minutes
        ///     used in statistical calculations and in the Box Score window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEditGameLength_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            var ibw = new InputBoxWindow(
                "Insert the length of each game in minutes, without overtime (e.g. 48):", GameLength.ToString());
            if (ibw.ShowDialog() == true)
            {
                try
                {
                    GameLength = Convert.ToInt32(InputBoxWindow.UserInput);
                }
                catch (Exception)
                {
                    return;
                }
            }

            SQLiteIO.SetSetting("Game Length", GameLength);

            UpdateStatus("Game Length saved. Database updated.");
        }

        /// <summary>
        ///     Handles the Click event of the mnuMiscEditSeasonLength control. Allows the user to edit the season length used in statistical
        ///     calculations.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void mnuMiscEditSeasonLength_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            var ibw = new InputBoxWindow("Insert the length of the season in games (etc. 82):", SeasonLength.ToString());
            if (ibw.ShowDialog() == true)
            {
                try
                {
                    SeasonLength = Convert.ToInt32(InputBoxWindow.UserInput);
                }
                catch (Exception)
                {
                    return;
                }
            }

            SQLiteIO.SetSetting("Season Length", SeasonLength);

            UpdateStatus("Season Length saved. Database updated.");
        }

        private void btnAdvStatCalc_Click(object sender, RoutedEventArgs e)
        {
            var ascw = new AdvancedStatCalculatorWindow();
            ascw.ShowDialog();
        }

        public static async Task UpdateAllData(bool leaveProgressWindowOpen = false, bool onlyPopulate = false)
        {
            MWInstance.startProgressWatchTimer();
            var dbData = new DBData();
            dbData = await Task.Run(() => SQLiteIO.PopulateAll(Tf));
            parseDBData(dbData);
            if (onlyPopulate)
            {
                return;
            }
            GameLength = SQLiteIO.GetSetting("Game Length", 48);
            SeasonLength = SQLiteIO.GetSetting("Season Length", 82);
            NumberOfPeriods = SQLiteIO.GetSetting("NumberOfPeriods", 4);
            ShotClockDuration = SQLiteIO.GetSetting("Shot Clock Duration", 24);

            Interlocked.Exchange(ref ProgressHelper.Progress, new ProgressInfo(ProgressHelper.Progress, "Updating notables..."));
            //MessageBox.Show(SQLiteIO.Progress.CurrentStage.ToString());
            updateNotables();
            Interlocked.Exchange(ref ProgressHelper.Progress, new ProgressInfo(ProgressHelper.Progress, "Updating search cache..."));
            MWInstance.UpdateSearchCache();

            if (_notables.Count > 0)
            {
                MWInstance._marqueeTimer.Start();
            }

            MWInstance.mnuTools.IsEnabled = true;
            MWInstance.grdAnalysis.IsEnabled = true;
            MWInstance.grdUpdate.IsEnabled = true;

            if (!leaveProgressWindowOpen)
            {
                MWInstance.StopProgressWatchTimer();
                MWInstance.IsEnabled = true;
            }

            MWInstance.UpdateStatus(String.Format("{0} teams & {1} players loaded successfully!", TST.Count, PST.Count));
            LoadingSeason = false;
            MWInstance.mainGrid.Visibility = Visibility.Visible;
        }

        private static void updateNotables()
        {
            Dictionary<int, PlayerStats> pstLeaders;
            SeasonLeadersRankings = PlayerRankings.CalculateLeadersRankings(out pstLeaders);
            _notables = new List<string>();

            Dictionary<int, PlayerStats> temp;
            PlayoffsLeadersRankings = PlayerRankings.CalculateLeadersRankings(out temp, true);

            if (pstLeaders.Count == 0)
            {
                return;
            }

            var leadersPSRList = new List<PlayerStatsRow>();
            pstLeaders.Values.ToList().ForEach(
                delegate(PlayerStats ps)
                    {
                        var psr = new PlayerStatsRow(ps, calcRatings: false);
                        leadersPSRList.Add(psr);
                    });
            var psrList = new Dictionary<int, PlayerStatsRow>();
            PST.Values.ToList().ForEach(
                delegate(PlayerStats ps)
                    {
                        var psr = new PlayerStatsRow(ps, calcRatings: false);
                        psrList.Add(psr.ID, psr);
                    });

            //PlayerStatsRow curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.PPG).First().ID);
            PlayerStatsRow curL;
            string m, s;
            float ppg;
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.PPG][1]];

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 3, PAbbr.PPG);
                s = String.Format(
                    "PPG Leader: {0} {1} ({2}) ({3:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.PPG,
                    m);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.FGp).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.FGp][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.FGp);
                s = String.Format(
                    "FG% Leader: {0} {1} ({2}) ({3:F3} FG%, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.FGp,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.RPG).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.RPG][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.RPG);
                s = String.Format(
                    "RPG Leader: {0} {1} ({2}) ({3:F1} RPG, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.RPG,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.BPG).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.BPG][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.BPG);
                s = String.Format(
                    "BPG Leader: {0} {1} ({2}) ({3:F1} BPG, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.BPG,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.APG).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.APG][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.APG);
                s = String.Format(
                    "APG Leader: {0} {1} ({2}) ({3:F1} APG, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.APG,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.SPG).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.SPG][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.SPG);
                s = String.Format(
                    "SPG Leader: {0} {1} ({2}) ({3:F1} SPG, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.SPG,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.ORPG).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.ORPG][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.ORPG);
                s = String.Format(
                    "ORPG Leader: {0} {1} ({2}) ({3:F1} ORPG, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.ORPG,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.DRPG).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.DRPG][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.DRPG);
                s = String.Format(
                    "DRPG Leader: {0} {1} ({2}) ({3:F1} DRPG, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.DRPG,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.TPp).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.TPp][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.TPp);
                s = String.Format(
                    "3P% Leader: {0} {1} ({2}) ({3:F3} 3P%, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.TPp,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            //curL = psrList.Single(psr => psr.ID == leadersPSRList.OrderByDescending(pair => pair.FTp).First().ID);
            try
            {
                curL = psrList[SeasonLeadersRankings.RevRankingsPerGame[PAbbr.FTp][1]];
                ppg = double.IsNaN(curL.PPG) ? PST[curL.ID].PerGame[PAbbr.PPG] : curL.PPG;

                m = getBestStatsForMarquee(curL, SeasonLeadersRankings, 2, PAbbr.FTp);
                s = String.Format(
                    "FT% Leader: {0} {1} ({2}) ({3:F3} FT%, {5:F1} PPG, {4})",
                    curL.FirstName,
                    curL.LastName,
                    TST[curL.TeamF].DisplayName,
                    curL.FTp,
                    m,
                    ppg);
                _notables.Add(s);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e);
            }

            if (_notables.Count > 0)
            {
                _notables.Shuffle();

                MWInstance.txbMarquee.Text = "League Notables";
                MWInstance._marqueeTimer.Start();
            }
        }

        private static string getBestStatsForMarquee(PlayerStatsRow curLr, PlayerRankings rankingsActive, int count, int statToIgnore)
        {
            var s = "";
            var dict = new Dictionary<int, int>();
            for (var k = 0; k < rankingsActive.RankingsPerGame[curLr.ID].Length; k++)
            {
                dict.Add(k, rankingsActive.RankingsPerGame[curLr.ID][k]);
            }
            dict[PAbbr.FPG] = PST.Count + 1 - dict[PAbbr.FPG];
            dict[PAbbr.TPG] = PST.Count + 1 - dict[PAbbr.TPG];
            //dict[t.PAPG] = pst.Count + 1 - dict[t.PAPG];
            var strengths = (from entry in dict orderby entry.Value ascending select entry.Key).ToList();
            var m = 0;
            var j = count;
            while (true)
            {
                if (m == j)
                {
                    break;
                }
                if (strengths[m] == statToIgnore || strengths[m] == PAbbr.PPG || strengths[m] == PAbbr.DRPG)
                {
                    j++;
                    m++;
                    continue;
                }
                switch (strengths[m])
                {
                    case PAbbr.APG:
                        s += String.Format("{0:F1} APG, ", curLr.APG);
                        break;
                    case PAbbr.BPG:
                        s += String.Format("{0:F1} BPG, ", curLr.BPG);
                        break;
                    case PAbbr.DRPG:
                        s += String.Format("{0:F1} DRPG, ", curLr.DRPG);
                        break;
                    case PAbbr.FGp:
                        s += String.Format("{0:F3} FG%, ", curLr.FGp);
                        break;
                    case PAbbr.FPG:
                        s += String.Format("{0:F1} FPG, ", curLr.FPG);
                        break;
                    case PAbbr.FTp:
                        s += String.Format("{0:F3} FT%, ", curLr.FTp);
                        break;
                    case PAbbr.ORPG:
                        s += String.Format("{0:F1} ORPG, ", curLr.ORPG);
                        break;
                    case PAbbr.PPG:
                        s += String.Format("{0:F1} PPG, ", curLr.PPG);
                        break;
                    case PAbbr.RPG:
                        s += String.Format("{0:F1} RPG, ", curLr.RPG);
                        break;
                    case PAbbr.SPG:
                        s += String.Format("{0:F1} SPG, ", curLr.SPG);
                        break;
                    case PAbbr.TPG:
                        s += String.Format("{0:F1} TPG, ", curLr.TPG);
                        break;
                    case PAbbr.TPp:
                        s += String.Format("{0:F3} 3P%, ", curLr.TPp);
                        break;
                    default:
                        j++;
                        break;
                }
                m++;
            }
            s = s.TrimEnd(new[] { ' ', ',' });
            return s;
        }

        private void mnuMiscImportPrevYear2K12_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(
                    "Are you sure you want to import the stats of the previous season from your NBA 2K12 save? This "
                    + "will overwrite any data in the current season of the database.",
                    "NBA Stats Tracker",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            var fbd = new FolderBrowserDialog
                {
                    Description = "Select folder with REDitor-exported CSVs",
                    ShowNewFolderButton = false,
                    SelectedPath = Tools.GetRegistrySetting("LastImportDir", "")
                };
            var dr = fbd.ShowDialog(this.GetIWin32Window());

            if (dr != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                return;
            }

            Tools.SetRegistrySetting("LastImportDir", fbd.SelectedPath);

            var result = REDitor.ImportLastYear(ref TST, ref TSTOpp, ref PST, fbd.SelectedPath);

            if (result != 0)
            {
                MessageBox.Show(
                    "Import failed! Please reload your database immediately to avoid saving corrupt data.",
                    "NBA Stats Tracker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            UpdateStatus("NBA 2K12 stats imported successfully! Verify that you want this by saving the current season.");
        }

        private void mnuOptionsIsImperial_Click(object sender, RoutedEventArgs e)
        {
            IsImperial = mnuOptionsIsImperial.IsChecked;
            Tools.SetRegistrySetting("IsImperial", IsImperial ? 1 : 0);
        }

        private void mnuMiscPreferNBALeaders_Checked(object sender, RoutedEventArgs e)
        {
            if (LoadingSeason)
            {
                return;
            }
            mnuMiscPreferMyLeaders.IsChecked = false;
            SQLiteIO.SetSetting("Leaders", "NBA");
            LoadMyLeadersCriteria();
            updateNotables();
        }

        private void mnuMiscPreferMyLeaders_Checked(object sender, RoutedEventArgs e)
        {
            if (LoadingSeason)
            {
                return;
            }
            mnuMiscPreferNBALeaders.IsChecked = false;
            SQLiteIO.SetSetting("Leaders", "My");
            LoadMyLeadersCriteria();
            updateNotables();
        }

        private async void mnuMiscImportOldPlayerStats2K12_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog
                {
                    Description = "Select folder with REDitor-exported CSVs",
                    ShowNewFolderButton = false,
                    SelectedPath = Tools.GetRegistrySetting("LastImportDir", "")
                };
            var dr = fbd.ShowDialog(this.GetIWin32Window());

            if (dr != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                return;
            }

            Tools.SetRegistrySetting("LastImportDir", fbd.SelectedPath);

            mainGrid.Visibility = Visibility.Hidden;

            await REDitor.ImportOldPlayerCareerStats(PST, TST, fbd.SelectedPath);
        }

        public void OnImportOldPlayerStatsCompleted(int result)
        {
            mainGrid.Visibility = Visibility.Visible;
            if (result == -1)
            {
                MessageBox.Show(
                    "Import failed! Please reload your database immediately to avoid saving corrupt data.",
                    "NBA Stats Tracker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            else if (result == -2)
            {
                UpdateStatus("Failed to import player career stats from 2K12 save.");
                return;
            }

            UpdateStatus("NBA 2K12 player career stats imported successfully and saved into the database.");
        }

        private void mnuMiscErasePastTeamStats_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(
                    "Are you sure you want to erase past team stats?\nThis doesn't erase any seasons from the "
                    + "database, but rather erases past team stats entered manually via the Yearly Report tab" + "in Team Overview.",
                    "NBA Stats Tracker",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            DB.ClearTable("PastTeamStats");

            UpdateStatus("Erased all past team stats. Database saved.");
        }

        private void mnuMiscErasePastPlayerStats_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(
                    "Are you sure you want to erase past player stats?\nThis doesn't erase any seasons from the "
                    + "database, but rather erases past player stats either entered manually via the Yearly Report tab"
                    + "in Player Overview, or imported from an NBA 2K save.",
                    "NBA Stats Tracker",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            DB.ClearTable("PastPlayerStats");

            UpdateStatus("Erased all past player stats. Database saved.");
        }

        private void Window_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsEnabled)
            {
                txbMarquee.Text = "";
                _marqueeTimer.Stop();
            }
            else
            {
                _marqueeTimer.Start();
            }
        }

        private void window_Activated(object sender, EventArgs e)
        {
            try
            {
                _marqueeTimer.Start();
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Marquee Timer not yet initizalized.");
            }
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            txbMarquee.Text = "";
            _marqueeTimer.Stop();
        }

        private void mnuMiscEditPeriods_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            while (true)
            {
                var ibw = new InputBoxWindow(
                    "Insert the number of periods (e.g. quarters, halves, etc.) in a game (e.g. 4):", NumberOfPeriods.ToString());
                if (ibw.ShowDialog() == true)
                {
                    try
                    {
                        var input = Convert.ToInt32(InputBoxWindow.UserInput);
                        if (input > 0)
                        {
                            NumberOfPeriods = input;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }

            SQLiteIO.SetSetting("NumberOfPeriods", NumberOfPeriods);

            UpdateStatus("Number of Periods saved. Database updated.");
        }

        private void mnuMiscEditShotClockDuration_Click(object sender, RoutedEventArgs e)
        {
            if (SQLiteIO.IsTSTEmpty())
            {
                return;
            }

            while (true)
            {
                var ibw = new InputBoxWindow("Insert the shot clock duration (e.g. 24):", ShotClockDuration.ToString());
                if (ibw.ShowDialog() == true)
                {
                    try
                    {
                        var input = Convert.ToInt32(InputBoxWindow.UserInput);
                        if (input > 0)
                        {
                            ShotClockDuration = input;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }

            SQLiteIO.SetSetting("Shot Clock Duration", ShotClockDuration);

            UpdateStatus("Shot Clock Duration saved. Database updated.");
        }

        #region Nested type: UpdateInfoContainer

        private struct UpdateInfoContainer
        {
            public string CurVersion;
            public string Message;
            public string[] UpdateInfo;
        }

        #endregion
    }
}