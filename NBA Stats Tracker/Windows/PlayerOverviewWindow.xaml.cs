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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using LeftosCommonLibrary;
using NBA_Stats_Tracker.Data;
using NBA_Stats_Tracker.Helper;
using SQLite_Database;
using Swordfish.WPF.Charts;

#endregion

namespace NBA_Stats_Tracker.Windows
{
    /// <summary>
    /// Shows player information and stats.
    /// </summary>
    public partial class PlayerOverviewWindow
    {
        public static string askedTeam;
        private readonly SQLiteDatabase db = new SQLiteDatabase(MainWindow.currentDB);
        private readonly int maxSeason = SQLiteIO.getMaxSeason(MainWindow.currentDB);
        private int SelectedPlayerID = -1;
        private List<string> Teams;

        private ObservableCollection<KeyValuePair<int, string>> _playersList = new ObservableCollection<KeyValuePair<int, string>>();

        private string _selectedPlayer;
        private bool changingTimeframe;
        private int curSeason = MainWindow.curSeason;
        private DataTable dt_ov;
        private List<PlayerBoxScore> hthAllPBS;
        private List<PlayerBoxScore> hthOppPBS;
        private List<PlayerBoxScore> hthOwnPBS;

        private ObservableCollection<KeyValuePair<int, string>> oppPlayersList = new ObservableCollection<KeyValuePair<int, string>>();

        private ObservableCollection<PlayerBoxScore> pbsList;
        private string pl_playersT = "PlayoffPlayers";
        private PlayerStatsRow pl_psr;
        private PlayerRankings pl_rankingsActive;
        private PlayerRankings pl_rankingsPosition;
        private PlayerRankings pl_rankingsTeam;

        private Dictionary<int, PlayerStats> playersActive;
        private Dictionary<int, PlayerStats> playersSamePosition;
        private Dictionary<int, PlayerStats> playersSameTeam;
        private string playersT = "Players";
        private PlayerStats psBetween;
        private PlayerStatsRow psr;
        private PlayerRankings rankingsActive;
        private PlayerRankings rankingsPosition;
        private PlayerRankings rankingsTeam;
        private ObservableCollection<PlayerStatsRow> splitPSRs;
        private SortedDictionary<string, int> teamOrder = MainWindow.TeamOrder;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerOverviewWindow" /> class.
        /// </summary>
        public PlayerOverviewWindow()
        {
            InitializeComponent();

            prepareWindow();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerOverviewWindow" /> class.
        /// Automatically switches to view a specific player.
        /// </summary>
        /// <param name="team">The player's team name.</param>
        /// <param name="playerID">The player ID.</param>
        public PlayerOverviewWindow(string team, int playerID) : this()
        {
            cmbTeam.SelectedItem = GetDisplayNameFromTeam(team);
            cmbPlayer.SelectedValue = playerID.ToString();
        }

        private ObservableCollection<KeyValuePair<int, string>> PlayersList
        {
            get { return _playersList; }
            set
            {
                _playersList = value;
                OnPropertyChanged("PlayersList");
            }
        }

        public string SelectedPlayer
        {
            get { return _selectedPlayer; }
            set
            {
                _selectedPlayer = value;
                OnPropertyChanged("SelectedPlayer");
            }
        }

        private int SelectedOppPlayerID { get; set; }

        /// <summary>
        /// Finds a team's name by its displayName.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Requested team that is hidden.</exception>
        private string GetCurTeamFromDisplayName(string displayName)
        {
            if (displayName == "- Inactive -")
                return displayName;
            foreach (int kvp in MainWindow.tst.Keys)
            {
                if (MainWindow.tst[kvp].displayName == displayName)
                {
                    if (MainWindow.tst[kvp].isHidden)
                        throw new Exception("Requested team that is hidden: " + MainWindow.tst[kvp].name);

                    return MainWindow.tst[kvp].name;
                }
            }
            throw new Exception("Team not found: " + displayName);
        }

        /// <summary>
        /// Finds a team's displayName by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Requested team that is hidden.</exception>
        private string GetDisplayNameFromTeam(string name)
        {
            foreach (int kvp in MainWindow.tst.Keys)
            {
                if (MainWindow.tst[kvp].name == name)
                {
                    if (MainWindow.tst[kvp].isHidden)
                        throw new Exception("Requested team that is hidden: " + MainWindow.tst[kvp].name);

                    return MainWindow.tst[kvp].displayName;
                }
            }
            throw new Exception("Team not found: " + name);
        }

        /// <summary>
        /// Populates the teams combo.
        /// </summary>
        private void PopulateTeamsCombo()
        {
            Teams = new List<string>();
            foreach (var kvp in teamOrder)
            {
                if (!MainWindow.tst[kvp.Value].isHidden)
                    Teams.Add(MainWindow.tst[kvp.Value].displayName);
            }

            Teams.Sort();

            Teams.Add("- Inactive -");

            cmbTeam.ItemsSource = Teams;
            cmbOppTeam.ItemsSource = Teams;
        }

        /// <summary>
        /// Prepares the window: populates data tables, sets DataGrid properties, populates combos and calculates metrics.
        /// </summary>
        private void prepareWindow()
        {
            DataContext = this;

            PopulateSeasonCombo();

            var Positions = new List<string> {" ", "PG", "SG", "SF", "PF", "C"};
            var Positions2 = new List<string> {" ", "PG", "SG", "SF", "PF", "C"};
            cmbPosition1.ItemsSource = Positions;
            cmbPosition2.ItemsSource = Positions2;

            PopulateTeamsCombo();

            dt_ov = new DataTable();
            dt_ov.Columns.Add("Type");
            dt_ov.Columns.Add("GP");
            dt_ov.Columns.Add("GS");
            dt_ov.Columns.Add("MINS");
            dt_ov.Columns.Add("PTS");
            dt_ov.Columns.Add("FG");
            dt_ov.Columns.Add("FGeff");
            dt_ov.Columns.Add("3PT");
            dt_ov.Columns.Add("3Peff");
            dt_ov.Columns.Add("FT");
            dt_ov.Columns.Add("FTeff");
            dt_ov.Columns.Add("REB");
            dt_ov.Columns.Add("OREB");
            dt_ov.Columns.Add("DREB");
            dt_ov.Columns.Add("AST");
            dt_ov.Columns.Add("TO");
            dt_ov.Columns.Add("STL");
            dt_ov.Columns.Add("BLK");
            dt_ov.Columns.Add("FOUL");

            dtpStart.SelectedDate = DateTime.Now.AddMonths(-1).AddDays(1);
            dtpEnd.SelectedDate = DateTime.Now;

            rbStatsAllTime.IsChecked = true;

            dgvBoxScores.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvHTH.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvHTHBoxScores.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvOverviewStats.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvSplitStats.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvYearly.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;

            GetActivePlayers();
            PopulateGraphStatCombo();

            PlayerStats.CalculateAllMetrics(ref MainWindow.pst, MainWindow.tst, MainWindow.tstopp, MainWindow.TeamOrder);
            PlayerStats.CalculateAllMetrics(ref MainWindow.pst, MainWindow.tst, MainWindow.tstopp, MainWindow.TeamOrder, playoffs: true);
        }

        /// <summary>
        /// Gets a player stats dictionary of only the active players, and calculates their rankings.
        /// </summary>
        private void GetActivePlayers()
        {
            playersActive = new Dictionary<int, PlayerStats>();

            string q = "select * from " + playersT + " where isActive LIKE \"True\"";
            q += " AND isHidden LIKE \"False\"";
            DataTable res = db.GetDataTable(q);
            foreach (DataRow r in res.Rows)
            {
                string q2 = "select * from " + pl_playersT + " where ID = " + Tools.getInt(r, "ID");
                DataTable pl_res = db.GetDataTable(q2);

                var ps = new PlayerStats(r);
                playersActive.Add(ps.ID, ps);
                playersActive[ps.ID].UpdatePlayoffStats(pl_res.Rows[0]);
            }

            rankingsActive = new PlayerRankings(playersActive);
            pl_rankingsActive = new PlayerRankings(playersActive, true);
        }

        /// <summary>
        /// Populates the season combo.
        /// </summary>
        private void PopulateSeasonCombo()
        {
            cmbSeasonNum.ItemsSource = MainWindow.SeasonList;

            cmbSeasonNum.SelectedValue = curSeason;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the cmbTeam control.
        /// Populates the player combo, resets all relevant DataGrid DataContext and ItemsSource properties, and calculates the in-team player rankings.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void cmbTeam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dgvOverviewStats.DataContext = null;
            grdOverview.DataContext = null;
            cmbPosition1.SelectedIndex = -1;
            cmbPosition2.SelectedIndex = -1;
            dgvBoxScores.ItemsSource = null;
            dgvHTH.ItemsSource = null;
            dgvHTHBoxScores.ItemsSource = null;
            dgvSplitStats.ItemsSource = null;
            dgvYearly.ItemsSource = null;

            if (cmbTeam.SelectedIndex == -1)
                return;

            cmbPlayer.ItemsSource = null;

            PlayersList = new ObservableCollection<KeyValuePair<int, string>>();
            string q;
            if (cmbTeam.SelectedItem.ToString() != "- Inactive -")
            {
                q = "select * from " + playersT + " where TeamFin LIKE \"" + GetCurTeamFromDisplayName(cmbTeam.SelectedItem.ToString()) +
                    "\" AND isActive LIKE \"True\"";
            }
            else
            {
                q = "select * from " + playersT + " where isActive LIKE \"False\"";
            }
            q += " AND isHidden LIKE \"False\"";
            q += " ORDER BY LastName ASC";
            DataTable res = db.GetDataTable(q);

            playersSameTeam = new Dictionary<int, PlayerStats>();

            foreach (DataRow r in res.Rows)
            {
                int id = Tools.getInt(r, "ID");
                PlayersList.Add(new KeyValuePair<int, string>(id,
                                                              Tools.getString(r, "LastName") + ", " + Tools.getString(r, "FirstName") + " (" +
                                                              Tools.getString(r, "Position1") + ")"));

                string q2 = "select * from " + pl_playersT + " where ID = " + id;
                DataTable pl_res = db.GetDataTable(q2);

                var ps = new PlayerStats(r);
                playersSameTeam.Add(ps.ID, ps);
                playersSameTeam[ps.ID].UpdatePlayoffStats(pl_res.Rows[0]);
            }
            rankingsTeam = new PlayerRankings(playersSameTeam);
            pl_rankingsTeam = new PlayerRankings(playersSameTeam, true);

            cmbPlayer.ItemsSource = PlayersList;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the cmbPlayer control.
        /// Updates the PlayerStatsRow instance and all DataGrid controls with this player's information and stats.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void cmbPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == -1)
                return;

            SelectedPlayerID = ((KeyValuePair<int, string>) (((cmbPlayer)).SelectedItem)).Key;

            string q = "select * from " + playersT + " where ID = " + SelectedPlayerID.ToString();
            DataTable res = db.GetDataTable(q);

            if (res.Rows.Count == 0) // Player not found in this year's database
            {
                cmbTeam_SelectionChanged(null, null); // Reload this team's players
                return;
            }
            /*
            string q2 = "select * from " + pl_playersT + " where ID = " + SelectedPlayerID.ToString();
            DataTable pl_res = db.GetDataTable(q2);

            psr = new PlayerStatsRow(new PlayerStats(res.Rows[0]));
            pl_psr = new PlayerStatsRow(new PlayerStats(pl_res.Rows[0], true), true);
            */
            psr = new PlayerStatsRow(MainWindow.pst[SelectedPlayerID]);
            pl_psr = new PlayerStatsRow(MainWindow.pst[SelectedPlayerID], true);

            UpdateOverviewAndBoxScores();

            UpdateSplitStats();

            UpdateYearlyReport();

            //if (tbcPlayerOverview.SelectedItem == tabHTH)
            //{
            cmbOppPlayer_SelectionChanged(null, null);
            //}

            if (cmbGraphStat.SelectedIndex == -1)
                cmbGraphStat.SelectedIndex = 0;
        }

        /// <summary>
        /// Updates the tab viewing the year-by-year overview of the player's stats.
        /// </summary>
        private void UpdateYearlyReport()
        {
            var psrList = new List<PlayerStatsRow>();
            var psCareer = new PlayerStats(new Player(psr.ID, psr.TeamF, psr.LastName, psr.FirstName, psr.Position1, psr.Position2));

            for (int i = 1; i <= maxSeason; i++)
            {
                string pT = "Players";
                if (i != maxSeason)
                    pT += "S" + i;

                string q = "select * from " + pT + " where ID = " + SelectedPlayerID;
                DataTable res = db.GetDataTable(q);
                if (res.Rows.Count == 1)
                {
                    var ps = new PlayerStats(res.Rows[0]);
                    var psr2 = new PlayerStatsRow(ps, "Season " + i);
                    psrList.Add(psr2);
                    psCareer.AddPlayerStats(ps);
                }

                pT = "PlayoffPlayers";
                if (i != maxSeason)
                    pT += "S" + i;

                q = "select * from " + pT + " where ID = " + SelectedPlayerID;
                res = db.GetDataTable(q);
                if (res.Rows.Count == 1)
                {
                    var ps = new PlayerStats(res.Rows[0], true);
                    if (ps.pl_stats[p.GP] > 0)
                    {
                        var psr2 = new PlayerStatsRow(ps, "Playoffs " + i, true);
                        psrList.Add(psr2);
                        psCareer.AddPlayerStats(ps, true);
                    }
                }
            }

            psrList.Add(new PlayerStatsRow(psCareer, "Career", "Career"));

            var psrListCollection = new ListCollectionView(psrList);
            Debug.Assert(psrListCollection.GroupDescriptions != null, "psrListCollection.GroupDescriptions != null");
            psrListCollection.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            dgvYearly.ItemsSource = psrListCollection;
        }

        /// <summary>
        /// Updates the overview tab and prepares the available box scores for the current timeframe.
        /// </summary>
        private void UpdateOverviewAndBoxScores()
        {
            var ts = new TeamStats("Team");
            var tsopp = new TeamStats("Opponents");

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                grdOverview.DataContext = psr;

                playersSamePosition = new Dictionary<int, PlayerStats>();

                string q = "select * from " + playersT + " where Position1 LIKE \"" + psr.Position1 + "\" AND isActive LIKE \"True\"";
                DataTable res = db.GetDataTable(q);

                foreach (DataRow r in res.Rows)
                {
                    string q2 = "select * from " + pl_playersT + " where ID = " + Tools.getInt(r, "ID");
                    DataTable pl_res = db.GetDataTable(q2);

                    var ps = new PlayerStats(r);
                    playersSamePosition.Add(ps.ID, ps);
                    playersSamePosition[ps.ID].UpdatePlayoffStats(pl_res.Rows[0]);
                }
                rankingsPosition = new PlayerRankings(playersSamePosition);
                pl_rankingsPosition = new PlayerRankings(playersSamePosition, true);

                q = "select * from PlayerResults INNER JOIN GameResults ON (PlayerResults.GameID = GameResults.GameID) " +
                    "where PlayerID = " + SelectedPlayerID.ToString() + " AND SeasonNum = " + curSeason + " ORDER BY Date DESC";
                res = db.GetDataTable(q);

                pbsList = new ObservableCollection<PlayerBoxScore>();
                foreach (DataRow r in res.Rows)
                {
                    var pbs = new PlayerBoxScore(r);
                    pbsList.Add(pbs);
                }
            }
            else
            {
                string q = "select * from PlayerResults INNER JOIN GameResults ON (PlayerResults.GameID = GameResults.GameID) " +
                           "where PlayerID = " + SelectedPlayerID.ToString();
                q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                          dtpEnd.SelectedDate.GetValueOrDefault());
                q += " ORDER BY Date DESC";
                DataTable res = db.GetDataTable(q);

                psBetween = new PlayerStats(new Player(psr.ID, psr.TeamF, psr.LastName, psr.FirstName, psr.Position1, psr.Position2));

                pbsList = new ObservableCollection<PlayerBoxScore>();

                TeamOverviewWindow.AddToTeamStatsFromSQLBoxScores(res, ref ts, ref tsopp);

                foreach (DataRow r in res.Rows)
                {
                    bool isPlayoff = Tools.getBoolean(r, "isPlayoff");
                    var pbs = new PlayerBoxScore(r);
                    pbsList.Add(pbs);

                    psBetween.AddBoxScore(pbs, isPlayoff);
                }

                psr = new PlayerStatsRow(psBetween);
                pl_psr = new PlayerStatsRow(psBetween, true);
            }

            cmbPosition1.SelectedItem = psr.Position1;
            cmbPosition2.SelectedItem = psr.Position2;

            dt_ov.Clear();

            DataRow dr = dt_ov.NewRow();

            dr["Type"] = "Stats";
            dr["GP"] = psr.GP.ToString();
            dr["GS"] = psr.GS.ToString();
            dr["MINS"] = psr.MINS.ToString();
            dr["PTS"] = psr.PTS.ToString();
            dr["FG"] = psr.FGM.ToString() + "-" + psr.FGA.ToString();
            dr["3PT"] = psr.TPM.ToString() + "-" + psr.TPA.ToString();
            dr["FT"] = psr.FTM.ToString() + "-" + psr.FTA.ToString();
            dr["REB"] = (psr.DREB + psr.OREB).ToString();
            dr["OREB"] = psr.OREB.ToString();
            dr["DREB"] = psr.DREB.ToString();
            dr["AST"] = psr.AST.ToString();
            dr["TO"] = psr.TOS.ToString();
            dr["STL"] = psr.STL.ToString();
            dr["BLK"] = psr.BLK.ToString();
            dr["FOUL"] = psr.FOUL.ToString();

            dt_ov.Rows.Add(dr);

            dr = dt_ov.NewRow();

            dr["Type"] = "Averages";
            dr["MINS"] = String.Format("{0:F1}", psr.MPG);
            dr["PTS"] = String.Format("{0:F1}", psr.PPG);
            dr["FG"] = String.Format("{0:F3}", psr.FGp);
            dr["FGeff"] = String.Format("{0:F2}", psr.FGeff);
            dr["3PT"] = String.Format("{0:F3}", psr.TPp);
            dr["3Peff"] = String.Format("{0:F2}", psr.TPeff);
            dr["FT"] = String.Format("{0:F3}", psr.FTp);
            dr["FTeff"] = String.Format("{0:F2}", psr.FTeff);
            dr["REB"] = String.Format("{0:F1}", psr.RPG);
            dr["OREB"] = String.Format("{0:F1}", psr.ORPG);
            dr["DREB"] = String.Format("{0:F1}", psr.DRPG);
            dr["AST"] = String.Format("{0:F1}", psr.APG);
            dr["TO"] = String.Format("{0:F1}", psr.TPG);
            dr["STL"] = String.Format("{0:F1}", psr.SPG);
            dr["BLK"] = String.Format("{0:F1}", psr.BPG);
            dr["FOUL"] = String.Format("{0:F1}", psr.FPG);

            dt_ov.Rows.Add(dr);

            #region Rankings

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                if (psr.isActive)
                {
                    int id = SelectedPlayerID;

                    dr = dt_ov.NewRow();

                    dr["Type"] = "Rankings";
                    dr["MINS"] = String.Format("{0}", rankingsActive.list[id][p.MPG]);
                    dr["PTS"] = String.Format("{0}", rankingsActive.list[id][p.PPG]);
                    dr["FG"] = String.Format("{0}", rankingsActive.list[id][p.FGp]);
                    dr["FGeff"] = String.Format("{0}", rankingsActive.list[id][p.FGeff]);
                    dr["3PT"] = String.Format("{0}", rankingsActive.list[id][p.TPp]);
                    dr["3Peff"] = String.Format("{0}", rankingsActive.list[id][p.TPeff]);
                    dr["FT"] = String.Format("{0}", rankingsActive.list[id][p.FTp]);
                    dr["FTeff"] = String.Format("{0}", rankingsActive.list[id][p.FTeff]);
                    dr["REB"] = String.Format("{0}", rankingsActive.list[id][p.RPG]);
                    dr["OREB"] = String.Format("{0}", rankingsActive.list[id][p.ORPG]);
                    dr["DREB"] = String.Format("{0}", rankingsActive.list[id][p.DRPG]);
                    dr["AST"] = String.Format("{0}", rankingsActive.list[id][t.PAPG]);
                    dr["TO"] = String.Format("{0}", rankingsActive.list[id][p.TPG]);
                    dr["STL"] = String.Format("{0}", rankingsActive.list[id][p.SPG]);
                    dr["BLK"] = String.Format("{0}", rankingsActive.list[id][p.BPG]);
                    dr["FOUL"] = String.Format("{0}", rankingsActive.list[id][p.FPG]);

                    dt_ov.Rows.Add(dr);

                    dr = dt_ov.NewRow();

                    dr["Type"] = "In-team Rankings";
                    dr["MINS"] = String.Format("{0}", rankingsTeam.list[id][p.MPG]);
                    dr["PTS"] = String.Format("{0}", rankingsTeam.list[id][p.PPG]);
                    dr["FG"] = String.Format("{0}", rankingsTeam.list[id][p.FGp]);
                    dr["FGeff"] = String.Format("{0}", rankingsTeam.list[id][p.FGeff]);
                    dr["3PT"] = String.Format("{0}", rankingsTeam.list[id][p.TPp]);
                    dr["3Peff"] = String.Format("{0}", rankingsTeam.list[id][p.TPeff]);
                    dr["FT"] = String.Format("{0}", rankingsTeam.list[id][p.FTp]);
                    dr["FTeff"] = String.Format("{0}", rankingsTeam.list[id][p.FTeff]);
                    dr["REB"] = String.Format("{0}", rankingsTeam.list[id][p.RPG]);
                    dr["OREB"] = String.Format("{0}", rankingsTeam.list[id][p.ORPG]);
                    dr["DREB"] = String.Format("{0}", rankingsTeam.list[id][p.DRPG]);
                    dr["AST"] = String.Format("{0}", rankingsTeam.list[id][t.PAPG]);
                    dr["TO"] = String.Format("{0}", rankingsTeam.list[id][p.TPG]);
                    dr["STL"] = String.Format("{0}", rankingsTeam.list[id][p.SPG]);
                    dr["BLK"] = String.Format("{0}", rankingsTeam.list[id][p.BPG]);
                    dr["FOUL"] = String.Format("{0}", rankingsTeam.list[id][p.FPG]);

                    dt_ov.Rows.Add(dr);

                    dr = dt_ov.NewRow();

                    dr["Type"] = "Position Rankings";
                    dr["MINS"] = String.Format("{0}", rankingsPosition.list[id][p.MPG]);
                    dr["PTS"] = String.Format("{0}", rankingsPosition.list[id][p.PPG]);
                    dr["FG"] = String.Format("{0}", rankingsPosition.list[id][p.FGp]);
                    dr["FGeff"] = String.Format("{0}", rankingsPosition.list[id][p.FGeff]);
                    dr["3PT"] = String.Format("{0}", rankingsPosition.list[id][p.TPp]);
                    dr["3Peff"] = String.Format("{0}", rankingsPosition.list[id][p.TPeff]);
                    dr["FT"] = String.Format("{0}", rankingsPosition.list[id][p.FTp]);
                    dr["FTeff"] = String.Format("{0}", rankingsPosition.list[id][p.FTeff]);
                    dr["REB"] = String.Format("{0}", rankingsPosition.list[id][p.RPG]);
                    dr["OREB"] = String.Format("{0}", rankingsPosition.list[id][p.ORPG]);
                    dr["DREB"] = String.Format("{0}", rankingsPosition.list[id][p.DRPG]);
                    dr["AST"] = String.Format("{0}", rankingsPosition.list[id][t.PAPG]);
                    dr["TO"] = String.Format("{0}", rankingsPosition.list[id][p.TPG]);
                    dr["STL"] = String.Format("{0}", rankingsPosition.list[id][p.SPG]);
                    dr["BLK"] = String.Format("{0}", rankingsPosition.list[id][p.BPG]);
                    dr["FOUL"] = String.Format("{0}", rankingsPosition.list[id][p.FPG]);

                    dt_ov.Rows.Add(dr);
                }
            }
            else
            {
                dr = dt_ov.NewRow();

                dr["Type"] = "Team Avg";
                dr["PTS"] = String.Format("{0:F1}", ts.averages[t.PPG]);
                dr["FG"] = String.Format("{0:F3}", ts.averages[t.FGp]);
                dr["FGeff"] = String.Format("{0:F2}", ts.averages[t.FGeff]);
                dr["3PT"] = String.Format("{0:F3}", ts.averages[t.TPp]);
                dr["3Peff"] = String.Format("{0:F2}", ts.averages[t.TPeff]);
                dr["FT"] = String.Format("{0:F3}", ts.averages[t.FTp]);
                dr["FTeff"] = String.Format("{0:F2}", ts.averages[t.FTeff]);
                dr["REB"] = String.Format("{0:F1}", ts.averages[t.RPG]);
                dr["OREB"] = String.Format("{0:F1}", ts.averages[t.ORPG]);
                dr["DREB"] = String.Format("{0:F1}", ts.averages[t.DRPG]);
                dr["AST"] = String.Format("{0:F1}", ts.averages[t.APG]);
                dr["TO"] = String.Format("{0:F1}", ts.averages[t.TPG]);
                dr["STL"] = String.Format("{0:F1}", ts.averages[t.SPG]);
                dr["BLK"] = String.Format("{0:F1}", ts.averages[t.BPG]);
                dr["FOUL"] = String.Format("{0:F1}", ts.averages[t.FPG]);

                dt_ov.Rows.Add(dr);
            }

            #endregion

            dr = dt_ov.NewRow();

            dr["Type"] = " ";

            dt_ov.Rows.Add(dr);

            #region Playoffs

            dr = dt_ov.NewRow();

            dr["Type"] = "Pl Stats";
            dr["GP"] = pl_psr.GP.ToString();
            dr["GS"] = pl_psr.GS.ToString();
            dr["MINS"] = pl_psr.MINS.ToString();
            dr["PTS"] = pl_psr.PTS.ToString();
            dr["FG"] = pl_psr.FGM.ToString() + "-" + pl_psr.FGA.ToString();
            dr["3PT"] = pl_psr.TPM.ToString() + "-" + pl_psr.TPA.ToString();
            dr["FT"] = pl_psr.FTM.ToString() + "-" + pl_psr.FTA.ToString();
            dr["REB"] = (pl_psr.DREB + pl_psr.OREB).ToString();
            dr["OREB"] = pl_psr.OREB.ToString();
            dr["DREB"] = pl_psr.DREB.ToString();
            dr["AST"] = pl_psr.AST.ToString();
            dr["TO"] = pl_psr.TOS.ToString();
            dr["STL"] = pl_psr.STL.ToString();
            dr["BLK"] = pl_psr.BLK.ToString();
            dr["FOUL"] = pl_psr.FOUL.ToString();

            dt_ov.Rows.Add(dr);

            dr = dt_ov.NewRow();

            dr["Type"] = "Pl Avg";
            dr["MINS"] = String.Format("{0:F1}", pl_psr.MPG);
            dr["PTS"] = String.Format("{0:F1}", pl_psr.PPG);
            dr["FG"] = String.Format("{0:F3}", pl_psr.FGp);
            dr["FGeff"] = String.Format("{0:F2}", pl_psr.FGeff);
            dr["3PT"] = String.Format("{0:F3}", pl_psr.TPp);
            dr["3Peff"] = String.Format("{0:F2}", pl_psr.TPeff);
            dr["FT"] = String.Format("{0:F3}", pl_psr.FTp);
            dr["FTeff"] = String.Format("{0:F2}", pl_psr.FTeff);
            dr["REB"] = String.Format("{0:F1}", pl_psr.RPG);
            dr["OREB"] = String.Format("{0:F1}", pl_psr.ORPG);
            dr["DREB"] = String.Format("{0:F1}", pl_psr.DRPG);
            dr["AST"] = String.Format("{0:F1}", pl_psr.APG);
            dr["TO"] = String.Format("{0:F1}", pl_psr.TPG);
            dr["STL"] = String.Format("{0:F1}", pl_psr.SPG);
            dr["BLK"] = String.Format("{0:F1}", pl_psr.BPG);
            dr["FOUL"] = String.Format("{0:F1}", pl_psr.FPG);

            dt_ov.Rows.Add(dr);

            #region Rankings

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                if (psr.isActive)
                {
                    int id = Convert.ToInt32(SelectedPlayerID);

                    dr = dt_ov.NewRow();

                    dr["Type"] = "Pl Rank";
                    dr["MINS"] = String.Format("{0}", pl_rankingsActive.list[id][p.MPG]);
                    dr["PTS"] = String.Format("{0}", pl_rankingsActive.list[id][p.PPG]);
                    dr["FG"] = String.Format("{0}", pl_rankingsActive.list[id][p.FGp]);
                    dr["FGeff"] = String.Format("{0}", pl_rankingsActive.list[id][p.FGeff]);
                    dr["3PT"] = String.Format("{0}", pl_rankingsActive.list[id][p.TPp]);
                    dr["3Peff"] = String.Format("{0}", pl_rankingsActive.list[id][p.TPeff]);
                    dr["FT"] = String.Format("{0}", pl_rankingsActive.list[id][p.FTp]);
                    dr["FTeff"] = String.Format("{0}", pl_rankingsActive.list[id][p.FTeff]);
                    dr["REB"] = String.Format("{0}", pl_rankingsActive.list[id][p.RPG]);
                    dr["OREB"] = String.Format("{0}", pl_rankingsActive.list[id][p.ORPG]);
                    dr["DREB"] = String.Format("{0}", pl_rankingsActive.list[id][p.DRPG]);
                    dr["AST"] = String.Format("{0}", pl_rankingsActive.list[id][t.PAPG]);
                    dr["TO"] = String.Format("{0}", pl_rankingsActive.list[id][p.TPG]);
                    dr["STL"] = String.Format("{0}", pl_rankingsActive.list[id][p.SPG]);
                    dr["BLK"] = String.Format("{0}", pl_rankingsActive.list[id][p.BPG]);
                    dr["FOUL"] = String.Format("{0}", pl_rankingsActive.list[id][p.FPG]);

                    dt_ov.Rows.Add(dr);

                    dr = dt_ov.NewRow();

                    dr["Type"] = "Pl In-Team";
                    dr["MINS"] = String.Format("{0}", pl_rankingsTeam.list[id][p.MPG]);
                    dr["PTS"] = String.Format("{0}", pl_rankingsTeam.list[id][p.PPG]);
                    dr["FG"] = String.Format("{0}", pl_rankingsTeam.list[id][p.FGp]);
                    dr["FGeff"] = String.Format("{0}", pl_rankingsTeam.list[id][p.FGeff]);
                    dr["3PT"] = String.Format("{0}", pl_rankingsTeam.list[id][p.TPp]);
                    dr["3Peff"] = String.Format("{0}", pl_rankingsTeam.list[id][p.TPeff]);
                    dr["FT"] = String.Format("{0}", pl_rankingsTeam.list[id][p.FTp]);
                    dr["FTeff"] = String.Format("{0}", pl_rankingsTeam.list[id][p.FTeff]);
                    dr["REB"] = String.Format("{0}", pl_rankingsTeam.list[id][p.RPG]);
                    dr["OREB"] = String.Format("{0}", pl_rankingsTeam.list[id][p.ORPG]);
                    dr["DREB"] = String.Format("{0}", pl_rankingsTeam.list[id][p.DRPG]);
                    dr["AST"] = String.Format("{0}", pl_rankingsTeam.list[id][t.PAPG]);
                    dr["TO"] = String.Format("{0}", pl_rankingsTeam.list[id][p.TPG]);
                    dr["STL"] = String.Format("{0}", pl_rankingsTeam.list[id][p.SPG]);
                    dr["BLK"] = String.Format("{0}", pl_rankingsTeam.list[id][p.BPG]);
                    dr["FOUL"] = String.Format("{0}", pl_rankingsTeam.list[id][p.FPG]);

                    dt_ov.Rows.Add(dr);

                    dr = dt_ov.NewRow();

                    dr["Type"] = "Pl Position";
                    dr["MINS"] = String.Format("{0}", pl_rankingsPosition.list[id][p.MPG]);
                    dr["PTS"] = String.Format("{0}", pl_rankingsPosition.list[id][p.PPG]);
                    dr["FG"] = String.Format("{0}", pl_rankingsPosition.list[id][p.FGp]);
                    dr["FGeff"] = String.Format("{0}", pl_rankingsPosition.list[id][p.FGeff]);
                    dr["3PT"] = String.Format("{0}", pl_rankingsPosition.list[id][p.TPp]);
                    dr["3Peff"] = String.Format("{0}", pl_rankingsPosition.list[id][p.TPeff]);
                    dr["FT"] = String.Format("{0}", pl_rankingsPosition.list[id][p.FTp]);
                    dr["FTeff"] = String.Format("{0}", pl_rankingsPosition.list[id][p.FTeff]);
                    dr["REB"] = String.Format("{0}", pl_rankingsPosition.list[id][p.RPG]);
                    dr["OREB"] = String.Format("{0}", pl_rankingsPosition.list[id][p.ORPG]);
                    dr["DREB"] = String.Format("{0}", pl_rankingsPosition.list[id][p.DRPG]);
                    dr["AST"] = String.Format("{0}", pl_rankingsPosition.list[id][t.PAPG]);
                    dr["TO"] = String.Format("{0}", pl_rankingsPosition.list[id][p.TPG]);
                    dr["STL"] = String.Format("{0}", pl_rankingsPosition.list[id][p.SPG]);
                    dr["BLK"] = String.Format("{0}", pl_rankingsPosition.list[id][p.BPG]);
                    dr["FOUL"] = String.Format("{0}", pl_rankingsPosition.list[id][p.FPG]);

                    dt_ov.Rows.Add(dr);
                }
            }
            else
            {
                dr = dt_ov.NewRow();

                dr["Type"] = "Pl Team Avg";
                dr["PTS"] = String.Format("{0:F1}", ts.pl_averages[t.PPG]);
                dr["FG"] = String.Format("{0:F3}", ts.pl_averages[t.FGp]);
                dr["FGeff"] = String.Format("{0:F2}", ts.pl_averages[t.FGeff]);
                dr["3PT"] = String.Format("{0:F3}", ts.pl_averages[t.TPp]);
                dr["3Peff"] = String.Format("{0:F2}", ts.pl_averages[t.TPeff]);
                dr["FT"] = String.Format("{0:F3}", ts.pl_averages[t.FTp]);
                dr["FTeff"] = String.Format("{0:F2}", ts.pl_averages[t.FTeff]);
                dr["REB"] = String.Format("{0:F1}", ts.pl_averages[t.RPG]);
                dr["OREB"] = String.Format("{0:F1}", ts.pl_averages[t.ORPG]);
                dr["DREB"] = String.Format("{0:F1}", ts.pl_averages[t.DRPG]);
                dr["AST"] = String.Format("{0:F1}", ts.pl_averages[t.APG]);
                dr["TO"] = String.Format("{0:F1}", ts.pl_averages[t.TPG]);
                dr["STL"] = String.Format("{0:F1}", ts.pl_averages[t.SPG]);
                dr["BLK"] = String.Format("{0:F1}", ts.pl_averages[t.BPG]);
                dr["FOUL"] = String.Format("{0:F1}", ts.pl_averages[t.FPG]);

                dt_ov.Rows.Add(dr);
            }

            #endregion

            #endregion

            var dv_ov = new DataView(dt_ov) {AllowNew = false};

            dgvOverviewStats.DataContext = dv_ov;

            #region Prepare Box Scores

            dgvBoxScores.ItemsSource = pbsList;
            UpdateBest();
            cmbGraphStat_SelectionChanged(null, null);

            #endregion
        }

        /// <summary>
        /// Updates the best performances tab with the player's best performances and the most significant stats of each one for the current timeframe.
        /// </summary>
        private void UpdateBest()
        {
            txbGame1.Text = "";
            txbGame2.Text = "";
            txbGame3.Text = "";
            txbGame4.Text = "";
            txbGame5.Text = "";
            txbGame6.Text = "";

            try
            {
                List<PlayerBoxScore> templist = pbsList.ToList();
                /*
                if (double.IsNaN(templist[0].PER))
                {
                    templist.Sort((pmsr1, pmsr2) => pmsr1.GmSc.CompareTo(pmsr2.GmSc));
                }
                else
                {
                    templist.Sort((pmsr1, pmsr2) => pmsr1.PER.CompareTo(pmsr2.PER));
                }
                */
                templist.Sort((pmsr1, pmsr2) => pmsr1.GmSc.CompareTo(pmsr2.GmSc));
                templist.Reverse();

                PlayerBoxScore psr1 = templist[0];
                string text = psr1.GetBestStats(5, psr.Position1);
                txbGame1.Text = "1: " + psr1.Date + " vs " + psr1.OppTeam + " (" + psr1.Result + ")\n\n" + text;

                psr1 = templist[1];
                text = psr1.GetBestStats(5, psr.Position1);
                txbGame2.Text = "2: " + psr1.Date + " vs " + psr1.OppTeam + " (" + psr1.Result + ")\n\n" + text;

                psr1 = templist[2];
                text = psr1.GetBestStats(5, psr.Position1);
                txbGame3.Text = "3: " + psr1.Date + " vs " + psr1.OppTeam + " (" + psr1.Result + ")\n\n" + text;

                psr1 = templist[3];
                text = psr1.GetBestStats(5, psr.Position1);
                txbGame4.Text = "4: " + psr1.Date + " vs " + psr1.OppTeam + " (" + psr1.Result + ")\n\n" + text;

                psr1 = templist[4];
                text = psr1.GetBestStats(5, psr.Position1);
                txbGame5.Text = "5: " + psr1.Date + " vs " + psr1.OppTeam + " (" + psr1.Result + ")\n\n" + text;

                psr1 = templist[5];
                text = psr1.GetBestStats(5, psr.Position1);
                txbGame6.Text = "6: " + psr1.Date + " vs " + psr1.OppTeam + " (" + psr1.Result + ")\n\n" + text;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Updates the split stats tab for the current timeframe.
        /// </summary>
        private void UpdateSplitStats()
        {
            string qr_home =
                String.Format(
                    "select * from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID " +
                    "AND Team = T2Name) " + "WHERE PlayerID = {0}", psr.ID);
            string qr_away =
                String.Format(
                    "select * from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID " +
                    "AND Team = T1Name) " + "WHERE PlayerID = {0}", psr.ID);
            string qr_wins =
                String.Format(
                    "select * from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID) " +
                    "WHERE PlayerID = {0} " + "AND (((Team = T1Name) AND (T1PTS > T2PTS)) OR ((Team = T2Name) AND (T2PTS > T1PTS)))", psr.ID);
            string qr_losses =
                String.Format(
                    "select * from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID) " +
                    "WHERE PlayerID = {0} " + "AND (((Team = T1Name) AND (T1PTS < T2PTS)) OR ((Team = T2Name) AND (T2PTS < T1PTS)))", psr.ID);
            string qr_season =
                String.Format(
                    "select * from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID) " +
                    "WHERE PlayerID = {0} AND IsPlayoff LIKE \"False\"", psr.ID);
            string qr_playoffs =
                String.Format(
                    "select * from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID) " +
                    "WHERE PlayerID = {0} AND IsPlayoff LIKE \"True\"", psr.ID);
            string qr_teams =
                String.Format(
                    "select Team from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID) " +
                    " WHERE PlayerID = {0}", psr.ID);

            if (rbStatsBetween.IsChecked.GetValueOrDefault())
            {
                qr_home = SQLiteDatabase.AddDateRangeToSQLQuery(qr_home, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                dtpEnd.SelectedDate.GetValueOrDefault());
                qr_away = SQLiteDatabase.AddDateRangeToSQLQuery(qr_away, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                dtpEnd.SelectedDate.GetValueOrDefault());
                qr_wins = SQLiteDatabase.AddDateRangeToSQLQuery(qr_wins, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                dtpEnd.SelectedDate.GetValueOrDefault());
                qr_losses = SQLiteDatabase.AddDateRangeToSQLQuery(qr_losses, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                  dtpEnd.SelectedDate.GetValueOrDefault());
                qr_season = SQLiteDatabase.AddDateRangeToSQLQuery(qr_season, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                  dtpEnd.SelectedDate.GetValueOrDefault());
                qr_playoffs = SQLiteDatabase.AddDateRangeToSQLQuery(qr_playoffs, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                    dtpEnd.SelectedDate.GetValueOrDefault());
                qr_teams = SQLiteDatabase.AddDateRangeToSQLQuery(qr_teams, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                 dtpEnd.SelectedDate.GetValueOrDefault());
            }
            else
            {
                string s = " AND SeasonNum = " + cmbSeasonNum.SelectedValue;
                qr_home += s;
                qr_away += s;
                qr_wins += s;
                qr_losses += s;
                qr_season += s;
                qr_playoffs += s;
                qr_teams += s;
            }

            qr_teams += " GROUP BY Team";

            splitPSRs = new ObservableCollection<PlayerStatsRow>();

            //Home
            DataTable res = db.GetDataTable(qr_home);
            var ps = new PlayerStats(new Player(psr.ID, psr.TeamF, psr.LastName, psr.FirstName, psr.Position1, psr.Position2));

            foreach (DataRow r in res.Rows)
            {
                ps.AddBoxScore(new PlayerBoxScore(r));
            }
            splitPSRs.Add(new PlayerStatsRow(ps, "Home"));

            //Away
            res = db.GetDataTable(qr_away);
            ps.ResetStats();

            foreach (DataRow r in res.Rows)
            {
                ps.AddBoxScore(new PlayerBoxScore(r));
            }
            splitPSRs.Add(new PlayerStatsRow(ps, "Away"));

            //Wins
            res = db.GetDataTable(qr_wins);
            ps.ResetStats();

            foreach (DataRow r in res.Rows)
            {
                ps.AddBoxScore(new PlayerBoxScore(r));
            }
            splitPSRs.Add(new PlayerStatsRow(ps, "Wins", "Result"));

            //Losses
            res = db.GetDataTable(qr_losses);
            ps.ResetStats();

            foreach (DataRow r in res.Rows)
            {
                ps.AddBoxScore(new PlayerBoxScore(r));
            }
            splitPSRs.Add(new PlayerStatsRow(ps, "Losses", "Result"));

            //Season
            res = db.GetDataTable(qr_season);
            ps.ResetStats();

            foreach (DataRow r in res.Rows)
            {
                ps.AddBoxScore(new PlayerBoxScore(r));
            }
            splitPSRs.Add(new PlayerStatsRow(ps, "Season", "Part of Season"));

            //Playoffs
            res = db.GetDataTable(qr_playoffs);
            ps.ResetStats();

            foreach (DataRow r in res.Rows)
            {
                ps.AddBoxScore(new PlayerBoxScore(r));
            }
            splitPSRs.Add(new PlayerStatsRow(ps, "Playoffs", "Part of Season"));

            #region Each Team Played In Stats

            res = db.GetDataTable(qr_teams);

            if (res.Rows.Count > 1)
            {
                var teams = new List<string>(res.Rows.Count);
                teams.AddRange(from DataRow r in res.Rows select r["Team"].ToString());

                foreach (string team in teams)
                {
                    string q =
                        String.Format(
                            "select * from PlayerResults INNER JOIN GameResults" + " ON (PlayerResults.GameID = GameResults.GameID)" +
                            " WHERE PlayerID = {0} AND Team = \"{1}\"", psr.ID, team);
                    if (rbStatsBetween.IsChecked.GetValueOrDefault())
                    {
                        q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                                  dtpEnd.SelectedDate.GetValueOrDefault());
                    }
                    else
                    {
                        string s = " AND SeasonNum = " + cmbSeasonNum.SelectedValue;
                        q += s;
                    }
                    res = db.GetDataTable(q);

                    ps.ResetStats();

                    foreach (DataRow r in res.Rows)
                    {
                        ps.AddBoxScore(new PlayerBoxScore(r));
                    }
                    splitPSRs.Add(new PlayerStatsRow(ps, "with " + team, "Team Played For"));
                }
            }

            #endregion

            #region Opponents

            foreach (var oppTeam in teamOrder.Keys)
            {
                string q =
                    String.Format(
                        "select * from PlayerResults INNER JOIN GameResults" +
                        " ON (PlayerResults.GameID = GameResults.GameID)" +
                        " WHERE PlayerID = {0} AND ((T1Name LIKE Team AND T2Name LIKE '{1}') OR (T1Name LIKE Team AND T2Name LIKE '{1}'))",
                        psr.ID, oppTeam);
                if (rbStatsBetween.IsChecked.GetValueOrDefault())
                {
                    q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                              dtpEnd.SelectedDate.GetValueOrDefault());
                }
                else
                {
                    string s = " AND SeasonNum = " + cmbSeasonNum.SelectedValue;
                    q += s;
                }
                res = db.GetDataTable(q);
                ps.ResetStats();

                foreach (DataRow r in res.Rows)
                {
                    ps.AddBoxScore(new PlayerBoxScore(r));
                }
                splitPSRs.Add(new PlayerStatsRow(ps, "vs. " + oppTeam, "Team Played Against"));
            }

            #endregion

            #region Monthly Split Stats

            if (rbStatsBetween.IsChecked.GetValueOrDefault())
            {
                DateTime dStart = dtpStart.SelectedDate.GetValueOrDefault();
                DateTime dEnd = dtpEnd.SelectedDate.GetValueOrDefault();

                DateTime dCur = dStart;
                var qrm = new List<string>();

                while (true)
                {
                    if (new DateTime(dCur.Year, dCur.Month, 1) == new DateTime(dEnd.Year, dEnd.Month, 1))
                    {
                        string s =
                            String.Format(
                                "select * from PlayerResults " + "INNER JOIN GameResults " +
                                "ON (PlayerResults.GameID = GameResults.GameID) " +
                                "WHERE (Date >= '{0}' AND Date <= '{1}') AND PlayerID = {2}", SQLiteDatabase.ConvertDateTimeToSQLite(dCur),
                                SQLiteDatabase.ConvertDateTimeToSQLite(dEnd), psr.ID);

                        qrm.Add(s);
                        break;
                    }
                    else
                    {
                        string s =
                            String.Format(
                                "select * from PlayerResults " + "INNER JOIN GameResults " +
                                "ON (PlayerResults.GameID = GameResults.GameID) " +
                                "WHERE (Date >= '{0}' AND Date <= '{1}') AND PlayerID = {2}", SQLiteDatabase.ConvertDateTimeToSQLite(dCur),
                                SQLiteDatabase.ConvertDateTimeToSQLite(new DateTime(dCur.Year, dCur.Month, 1).AddMonths(1).AddDays(-1)),
                                psr.ID);

                        qrm.Add(s);
                        dCur = dCur.AddMonths(1);
                    }
                }

                int i = 0;
                foreach (string q in qrm)
                {
                    ps.ResetStats();
                    res = db.GetDataTable(q);

                    foreach (DataRow r in res.Rows)
                    {
                        ps.AddBoxScore(new PlayerBoxScore(r));
                    }

                    DateTime label = new DateTime(dStart.Year, dStart.Month, 1).AddMonths(i);
                    splitPSRs.Add(new PlayerStatsRow(ps, label.Year.ToString() + " " + String.Format("{0:MMMM}", label), "Month"));
                    i++;
                }
            }

            #endregion

            var splitPSRsCollection = new ListCollectionView(splitPSRs);
            Debug.Assert(splitPSRsCollection.GroupDescriptions != null, "splitPSRsCollection.GroupDescriptions != null");
            splitPSRsCollection.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            dgvSplitStats.ItemsSource = splitPSRsCollection;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the cmbSeasonNum control.
        /// Loads the specified season's team and player stats and tries to automatically switch to the same player again, if he exists in the specified season and isn't hidden.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void cmbSeasonNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            changingTimeframe = true;
            rbStatsAllTime.IsChecked = true;
            changingTimeframe = false;

            try
            {
                curSeason = ((KeyValuePair<int, string>) (((cmbSeasonNum)).SelectedItem)).Key;
            }
            catch (Exception)
            {
                return;
            }

            MainWindow.ChangeSeason(curSeason);

            if (curSeason != maxSeason)
            {
                playersT += "S" + curSeason;
                pl_playersT += "S" + curSeason;
            }

            if (cmbPlayer.SelectedIndex != -1)
            {
                PlayerStats ps = CreatePlayerStatsFromCurrent();

                SQLiteIO.LoadSeason();

                teamOrder = MainWindow.TeamOrder;

                GetActivePlayers();

                PopulateTeamsCombo();

                string q = "select * from " + playersT + " where ID = " + ps.ID;
                q += " AND isHidden LIKE \"False\"";
                DataTable res = db.GetDataTable(q);

                if (res.Rows.Count > 0)
                {
                    bool nowActive = Tools.getBoolean(res.Rows[0], "isActive");
                    string newTeam = nowActive ? res.Rows[0]["TeamFin"].ToString() : " - Inactive -";
                    cmbTeam.SelectedIndex = -1;
                    if (nowActive)
                    {
                        if (newTeam != "")
                        {
                            try
                            {
                                cmbTeam.SelectedItem = GetDisplayNameFromTeam(newTeam);
                            }
                            catch (Exception)
                            {
                                cmbTeam.SelectedIndex = -1;
                                cmbPlayer.SelectedIndex = -1;
                                return;
                            }
                        }
                    }
                    else
                    {
                        cmbTeam.SelectedItem = "- Inactive -";
                    }
                    cmbPlayer.SelectedIndex = -1;
                    cmbPlayer.SelectedValue = ps.ID;

                    if (cmbOppPlayer.SelectedIndex != -1)
                    {
                        SelectedOppPlayerID = ((KeyValuePair<int, string>) (((cmbOppPlayer)).SelectedItem)).Key;

                        q = "select * from " + playersT + " where ID = " + SelectedOppPlayerID;
                        q += " AND isHidden LIKE \"False\"";
                        res = db.GetDataTable(q);

                        if (res.Rows.Count > 0)
                        {
                            nowActive = Tools.getBoolean(res.Rows[0], "isActive");
                            newTeam = nowActive ? res.Rows[0]["TeamFin"].ToString() : " - Inactive -";
                            cmbOppTeam.SelectedIndex = -1;
                            if (nowActive)
                            {
                                if (newTeam != "")
                                {
                                    try
                                    {
                                        cmbOppTeam.SelectedItem = GetDisplayNameFromTeam(newTeam);
                                    }
                                    catch (Exception)
                                    {
                                        cmbOppTeam.SelectedIndex = -1;
                                        cmbOppPlayer.SelectedIndex = -1;
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                cmbOppTeam.SelectedItem = "- Inactive -";
                            }
                            cmbOppPlayer.SelectedIndex = -1;
                            cmbOppPlayer.SelectedValue = SelectedOppPlayerID;
                        }
                        else
                        {
                            cmbOppTeam.SelectedIndex = -1;
                            cmbOppPlayer.SelectedIndex = -1;
                        }
                    }
                }
                else
                {
                    cmbTeam.SelectedIndex = -1;
                    cmbPlayer.SelectedIndex = -1;
                    cmbOppTeam.SelectedIndex = -1;
                    cmbOppPlayer.SelectedIndex = -1;
                }
            }
            else
            {
                SQLiteIO.GetAllTeamStatsFromDatabase(MainWindow.currentDB, curSeason, out MainWindow.tst, out MainWindow.tstopp,
                                                     out MainWindow.TeamOrder);
                PopulateTeamsCombo();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnScoutingReport control.
        /// Displays a quick overview of the player's performance in a natural language scouting report.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnScoutingReport_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == -1)
                return;

            psr.ScoutingReport(MainWindow.pst, rankingsActive, rankingsTeam, rankingsPosition, pbsList.ToList(), txbGame1.Text);
        }

        /// <summary>
        /// Handles the Click event of the btnSavePlayer control.
        /// Saves the current player's stats to the database.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnSavePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == -1)
                return;

            if (rbStatsBetween.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show(
                    "You can't edit partial stats. You can either edit the total stats (which are kept separately from box-scores" +
                    ") or edit the box-scores themselves.", "NBA Stats Tracker", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PlayerStats ps = CreatePlayerStatsFromCurrent();

            var pslist = new Dictionary<int, PlayerStats> {{ps.ID, ps}};

            SQLiteIO.savePlayersToDatabase(MainWindow.currentDB, pslist, curSeason, maxSeason, true);

            MainWindow.pst = SQLiteIO.GetPlayersFromDatabase(MainWindow.currentDB, MainWindow.tst, MainWindow.tstopp, MainWindow.TeamOrder,
                                                             curSeason, maxSeason);

            GetActivePlayers();
            cmbTeam.SelectedIndex = -1;
            cmbTeam.SelectedItem = ps.isActive ? GetDisplayNameFromTeam(ps.TeamF) : "- Inactive -";
            cmbPlayer.SelectedIndex = -1;
            cmbPlayer.SelectedValue = ps.ID;
            //cmbPlayer.SelectedValue = ps.LastName + " " + ps.FirstName + " (" + ps.Position1 + ")";
        }

        /// <summary>
        /// Creates a PlayerStats instance from the currently displayed information and stats.
        /// </summary>
        /// <returns></returns>
        private PlayerStats CreatePlayerStatsFromCurrent()
        {
            if (cmbPosition2.SelectedItem == null)
                cmbPosition2.SelectedItem = " ";

            string TeamF;
            if (chkIsActive.IsChecked.GetValueOrDefault() == false)
            {
                TeamF = "";
            }
            else
            {
                TeamF = GetCurTeamFromDisplayName(cmbTeam.SelectedItem.ToString());
                if (TeamF == "- Inactive -")
                {
                    askedTeam = "";
                    var atw = new ComboChoiceWindow(Teams);
                    atw.ShowDialog();
                    TeamF = askedTeam;
                }
            }

            var ps = new PlayerStats(psr.ID, txtLastName.Text, txtFirstName.Text, cmbPosition1.SelectedItem.ToString(),
                                     cmbPosition2.SelectedItem.ToString(), Convert.ToInt32(txtYearOfBirth.Text),
                                     Convert.ToInt32(txtYearsPro.Text), TeamF, psr.TeamS,
                                     chkIsActive.IsChecked.GetValueOrDefault(),
                                     false, chkIsInjured.IsChecked.GetValueOrDefault(),
                                     chkIsAllStar.IsChecked.GetValueOrDefault(),
                                     chkIsNBAChampion.IsChecked.GetValueOrDefault(), dt_ov.Rows[0]);
            return ps;
        }

        /// <summary>
        /// Handles the Click event of the btnNext control.
        /// Switches to the next team.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTeam.SelectedIndex == cmbTeam.Items.Count - 1)
                cmbTeam.SelectedIndex = 0;
            else
                cmbTeam.SelectedIndex++;
        }

        /// <summary>
        /// Handles the Click event of the btnPrev control.
        /// Switches to the previous team.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTeam.SelectedIndex == 0)
                cmbTeam.SelectedIndex = cmbTeam.Items.Count - 1;
            else
                cmbTeam.SelectedIndex--;
        }

        /// <summary>
        /// Handles the Click event of the btnNextPlayer control.
        /// Switches to the next player.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnNextPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == cmbPlayer.Items.Count - 1)
                cmbPlayer.SelectedIndex = 0;
            else
                cmbPlayer.SelectedIndex++;
        }

        /// <summary>
        /// Handles the Click event of the btnPrevPlayer control.
        /// Switches to the previous player.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnPrevPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPlayer.SelectedIndex == 0)
                cmbPlayer.SelectedIndex = cmbPlayer.Items.Count - 1;
            else
                cmbPlayer.SelectedIndex--;
        }

        /// <summary>
        /// Handles the Checked event of the rbStatsAllTime control.
        /// Allows the user to display stats from the whole season.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void rbStatsAllTime_Checked(object sender, RoutedEventArgs e)
        {
            if (!changingTimeframe)
                cmbSeasonNum_SelectionChanged(null, null);
        }

        /// <summary>
        /// Handles the Checked event of the rbStatsBetween control.
        /// Allows the user to display stats between the specified timeframe.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void rbStatsBetween_Checked(object sender, RoutedEventArgs e)
        {
            if (!changingTimeframe)
                cmbPlayer_SelectionChanged(null, null);
        }

        /// <summary>
        /// Handles the SelectedDateChanged event of the dtpStart control.
        /// Makes sure the starting date isn't after the ending date, and updates the player's stats based on the new timeframe.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void dtpStart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!changingTimeframe)
            {
                changingTimeframe = true;
                if (dtpEnd.SelectedDate < dtpStart.SelectedDate)
                {
                    dtpEnd.SelectedDate = dtpStart.SelectedDate.GetValueOrDefault().AddMonths(1).AddDays(-1);
                }
                rbStatsBetween.IsChecked = true;
                changingTimeframe = false;

                cmbPlayer_SelectionChanged(null, null);
            }
        }

        /// <summary>
        /// Handles the SelectedDateChanged event of the dtpEnd control.
        /// Makes sure the starting date isn't after the ending date, and updates the player's stats based on the new timeframe.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void dtpEnd_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!changingTimeframe)
            {
                changingTimeframe = true;
                if (dtpEnd.SelectedDate < dtpStart.SelectedDate)
                {
                    dtpStart.SelectedDate = dtpEnd.SelectedDate.GetValueOrDefault().AddMonths(-1).AddDays(1);
                }
                rbStatsBetween.IsChecked = true;
                changingTimeframe = false;

                cmbPlayer_SelectionChanged(null, null);
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the cmbOppTeam control.
        /// Allows the user to change the opposing team, the players of which can be compared to.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void cmbOppTeam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbOppTeam.SelectedIndex == -1)
                return;

            dgvHTH.ItemsSource = null;
            cmbOppPlayer.ItemsSource = null;

            oppPlayersList = new ObservableCollection<KeyValuePair<int, string>>();
            string q;
            if (cmbOppTeam.SelectedItem.ToString() != "- Inactive -")
            {
                q = "select * from " + playersT + " where TeamFin LIKE \"" + cmbOppTeam.SelectedItem + "\" AND isActive LIKE \"True\"";
            }
            else
            {
                q = "select * from " + playersT + " where isActive LIKE \"False\"";
            }
            q += " AND isHidden LIKE \"False\"";
            q += " ORDER BY LastName ASC";
            DataTable res = db.GetDataTable(q);

            foreach (DataRow r in res.Rows)
            {
                oppPlayersList.Add(new KeyValuePair<int, string>(Tools.getInt(r, "ID"),
                                                                 Tools.getString(r, "LastName") + ", " + Tools.getString(r, "FirstName") +
                                                                 " (" + Tools.getString(r, "Position1") + ")"));
            }

            cmbOppPlayer.ItemsSource = oppPlayersList;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the cmbOppPlayer control.
        /// Allows the user to change the opposing player, to whose stats the current player's stats will be compared.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void cmbOppPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbTeam.SelectedIndex == -1 || cmbOppTeam.SelectedIndex == -1 || cmbPlayer.SelectedIndex == -1 ||
                    cmbOppPlayer.SelectedIndex == -1)
                {
                    dgvHTH.ItemsSource = null;
                    dgvHTHBoxScores.ItemsSource = null;
                    return;
                }
            }
            catch
            {
                return;
            }

            dgvHTH.ItemsSource = null;

            SelectedOppPlayerID = ((KeyValuePair<int, string>) (cmbOppPlayer.SelectedItem)).Key;

            var psrList = new ObservableCollection<PlayerStatsRow>();

            hthAllPBS = new List<PlayerBoxScore>();

            string q;
            DataTable res;

            if (SelectedPlayerID == SelectedOppPlayerID)
                return;

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                if (rbHTHStatsAnyone.IsChecked.GetValueOrDefault())
                {
                    /*
                    q = "SELECT * FROM " + playersT + " WHERE ID = " + SelectedPlayerID;
                    res = db.GetDataTable(q);

                    PlayerStats ps = new PlayerStats(res.Rows[0]);
                    PlayerStatsRow ownPSR = new PlayerStatsRow(ps, ps.FirstName + " " + ps.LastName);
                    */
                    psr.Type = psr.FirstName + " " + psr.LastName;
                    psrList.Add(psr);

                    hthOwnPBS = new List<PlayerBoxScore>(pbsList);

                    q = "SELECT * FROM " + playersT + " WHERE ID = " + SelectedOppPlayerID;
                    res = db.GetDataTable(q);

                    var ps = new PlayerStats(res.Rows[0]);
                    var oppPSR = new PlayerStatsRow(ps, ps.FirstName + " " + ps.LastName);

                    oppPSR.Type = oppPSR.FirstName + " " + oppPSR.LastName;
                    psrList.Add(oppPSR);

                    q = "select * from PlayerResults INNER JOIN GameResults ON (PlayerResults.GameID = GameResults.GameID) " +
                        "where PlayerID = " + SelectedOppPlayerID + " AND SeasonNum = " + curSeason;
                    res = db.GetDataTable(q);

                    hthOppPBS = new List<PlayerBoxScore>();
                    foreach (DataRow r in res.Rows)
                    {
                        var pbs = new PlayerBoxScore(r);
                        hthOppPBS.Add(pbs);
                    }
                    var gameIDs = new List<int>();
                    foreach (PlayerBoxScore bs in hthOwnPBS)
                    {
                        hthAllPBS.Add(bs);
                        gameIDs.Add(bs.GameID);
                    }
                    foreach (PlayerBoxScore bs in hthOppPBS)
                    {
                        if (!gameIDs.Contains(bs.GameID))
                        {
                            hthAllPBS.Add(bs);
                        }
                    }
                }
                else
                {
                    q =
                        String.Format(
                            "SELECT * FROM PlayerResults INNER JOIN GameResults " + "ON GameResults.GameID = PlayerResults.GameID " +
                            "WHERE PlayerID = {0} " + "AND PlayerResults.GameID IN " + "(SELECT GameID FROM PlayerResults " +
                            "WHERE PlayerID = {1} " + "AND SeasonNum = {2}) ORDER BY Date DESC", SelectedPlayerID, SelectedOppPlayerID,
                            curSeason);
                    res = db.GetDataTable(q);

                    var p = new Player(psr.ID, psr.TeamF, psr.LastName, psr.FirstName, psr.Position1, psr.Position2);
                    var ps = new PlayerStats(p);

                    foreach (DataRow r in res.Rows)
                    {
                        var pbs = new PlayerBoxScore(r);
                        ps.AddBoxScore(pbs);
                        hthAllPBS.Add(pbs);
                    }

                    psrList.Add(new PlayerStatsRow(ps, ps.FirstName + " " + ps.LastName));


                    // Opponent
                    q = "SELECT * FROM " + playersT + " WHERE ID = " + SelectedOppPlayerID;
                    res = db.GetDataTable(q);

                    p = new Player(res.Rows[0]);

                    q =
                        String.Format(
                            "SELECT * FROM PlayerResults INNER JOIN GameResults " + "ON GameResults.GameID = PlayerResults.GameID " +
                            "WHERE PlayerID = {0} " + "AND PlayerResults.GameID IN " + "(SELECT GameID FROM PlayerResults " +
                            "WHERE PlayerID = {1} " + "AND SeasonNum = {2}) ORDER BY Date DESC", SelectedOppPlayerID, SelectedPlayerID,
                            curSeason);
                    res = db.GetDataTable(q);

                    ps = new PlayerStats(p);

                    foreach (DataRow r in res.Rows)
                    {
                        var pbs = new PlayerBoxScore(r);
                        ps.AddBoxScore(pbs);
                    }

                    psrList.Add(new PlayerStatsRow(ps, ps.FirstName + " " + ps.LastName));
                }
            }
            else
            {
                if (rbHTHStatsAnyone.IsChecked.GetValueOrDefault())
                {
                    psrList.Add(new PlayerStatsRow(psBetween, psBetween.FirstName + " " + psBetween.LastName));

                    var gameIDs = new List<int>();
                    foreach (PlayerBoxScore cur in pbsList)
                    {
                        hthAllPBS.Add(cur);
                        gameIDs.Add(cur.GameID);
                    }

                    q = "SELECT * FROM " + playersT + " WHERE ID = " + SelectedOppPlayerID;
                    res = db.GetDataTable(q);

                    var p = new Player(res.Rows[0]);

                    q = "select * from PlayerResults INNER JOIN GameResults ON (PlayerResults.GameID = GameResults.GameID) " +
                        "where PlayerID = " + SelectedOppPlayerID.ToString();
                    q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                              dtpEnd.SelectedDate.GetValueOrDefault());
                    res = db.GetDataTable(q);

                    var psOppBetween = new PlayerStats(p);
                    foreach (DataRow r in res.Rows)
                    {
                        var pbs = new PlayerBoxScore(r);
                        psOppBetween.AddBoxScore(pbs);

                        if (!gameIDs.Contains(pbs.GameID))
                        {
                            hthAllPBS.Add(pbs);
                        }
                    }

                    psrList.Add(new PlayerStatsRow(psOppBetween, psOppBetween.FirstName + " " + psOppBetween.LastName));
                }
                else
                {
                    q =
                        String.Format(
                            "SELECT * FROM PlayerResults INNER JOIN GameResults " + "ON GameResults.GameID = PlayerResults.GameID " +
                            "WHERE PlayerID = {0} " + "AND PlayerResults.GameID IN " + "(SELECT GameID FROM PlayerResults " +
                            "WHERE PlayerID = {1} ", SelectedPlayerID, SelectedOppPlayerID);
                    q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                              dtpEnd.SelectedDate.GetValueOrDefault());
                    q += ") ORDER BY Date DESC";
                    res = db.GetDataTable(q);

                    var p = new Player(psr.ID, psr.TeamF, psr.LastName, psr.FirstName, psr.Position1, psr.Position2);
                    var ps = new PlayerStats(p);

                    foreach (DataRow r in res.Rows)
                    {
                        var pbs = new PlayerBoxScore(r);
                        ps.AddBoxScore(pbs);
                        hthAllPBS.Add(pbs);
                    }

                    psrList.Add(new PlayerStatsRow(ps, ps.FirstName + " " + ps.LastName));


                    // Opponent
                    q = "SELECT * FROM " + playersT + " WHERE ID = " + SelectedOppPlayerID;
                    res = db.GetDataTable(q);

                    p = new Player(res.Rows[0]);

                    q =
                        String.Format(
                            "SELECT * FROM PlayerResults INNER JOIN GameResults " + "ON GameResults.GameID = PlayerResults.GameID " +
                            "WHERE PlayerID = {0} " + "AND PlayerResults.GameID IN " + "(SELECT GameID FROM PlayerResults " +
                            "WHERE PlayerID = {1} ", SelectedOppPlayerID, SelectedPlayerID);
                    q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                              dtpEnd.SelectedDate.GetValueOrDefault());
                    q += ") ORDER BY Date DESC";
                    res = db.GetDataTable(q);

                    ps = new PlayerStats(p);

                    foreach (DataRow r in res.Rows)
                    {
                        var pbs = new PlayerBoxScore(r);
                        ps.AddBoxScore(pbs);
                    }

                    psrList.Add(new PlayerStatsRow(ps, ps.FirstName + " " + ps.LastName));
                }
            }

            hthAllPBS.Sort((pbs1, pbs2) => String.CompareOrdinal(pbs1.Date, pbs2.Date));
            hthAllPBS.Reverse();

            dgvHTH.ItemsSource = psrList;
            dgvHTHBoxScores.ItemsSource = hthAllPBS;
            //dgvHTHBoxScores.ItemsSource = new ObservableCollection<PlayerBoxScore>(hthAllPBS);
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the dgvBoxScores control.
        /// Allows the user to view a specific box score in the Box Score window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private void dgvBoxScores_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvBoxScores.SelectedCells.Count > 0)
            {
                var row = (PlayerBoxScore) dgvBoxScores.SelectedItems[0];
                int gameID = row.GameID;

                var bsw = new BoxScoreWindow(BoxScoreWindow.Mode.View, gameID);
                try
                {
                    bsw.ShowDialog();

                    MainWindow.UpdateBoxScore();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the dgvHTHBoxScores control.
        /// Allows the user to view a specific box score in the Box Score window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private void dgvHTHBoxScores_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvHTHBoxScores.SelectedCells.Count > 0)
            {
                var row = (PlayerBoxScore) dgvHTHBoxScores.SelectedItems[0];
                int gameID = row.GameID;

                var bsw = new BoxScoreWindow(BoxScoreWindow.Mode.View, gameID);
                try
                {
                    bsw.ShowDialog();

                    MainWindow.UpdateBoxScore();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Handles the Checked event of the rbHTHStatsAnyone control.
        /// Used to include all the players' games in the stat calculations, no matter the opponent.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void rbHTHStatsAnyone_Checked(object sender, RoutedEventArgs e)
        {
            cmbOppPlayer_SelectionChanged(null, null);
        }

        /// <summary>
        /// Handles the Checked event of the rbHTHStatsEachOther control.
        /// Used to include only stats from the games these two players have played against each other.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void rbHTHStatsEachOther_Checked(object sender, RoutedEventArgs e)
        {
            cmbOppPlayer_SelectionChanged(null, null);
        }

        /// <summary>
        /// Handles the Sorting event of the StatColumn control.
        /// Uses a custom Sorting event handler that sorts a stat in descending order, if it's not sorted already.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridSortingEventArgs" /> instance containing the event data.</param>
        private void StatColumn_Sorting(object sender, DataGridSortingEventArgs e)
        {
            EventHandlers.StatColumn_Sorting(e);
        }

        /// <summary>
        /// Handles the PreviewKeyDown event of the dgvOverviewStats control.
        /// Allows the user to paste and import tab-separated values formatted player stats into the current player.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        private void dgvOverviewStats_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                string[] lines = Tools.SplitLinesToArray(Clipboard.GetText());
                List<Dictionary<string, string>> dictList = CSV.DictionaryListFromTSV(lines);

                foreach (var dict in dictList)
                {
                    string type = "Stats";
                    try
                    {
                        type = dict["Type"];
                    }
                    catch (Exception)
                    {
                    }
                    switch (type)
                    {
                        case "Stats":
                            TryChangeRow(0, dict);
                            break;
                    }
                }

                CreateViewAndUpdateOverview();

                //btnSavePlayer_Click(null, null);
            }
        }

        /// <summary>
        /// Tries to change the specified row of the Overview data table using the specified dictionary.
        /// Used when pasting TSV data from the clipboard.
        /// </summary>
        /// <param name="row">The row of dt_ov to try and change.</param>
        /// <param name="dict">The dictionary containing stat-value pairs.</param>
        private void TryChangeRow(int row, Dictionary<string, string> dict)
        {
            dt_ov.Rows[row].TryChangeValue(dict, "GP", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "GS", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "MINS", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "PTS", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "FG", typeof (UInt16), "-");
            dt_ov.Rows[row].TryChangeValue(dict, "3PT", typeof (UInt16), "-");
            dt_ov.Rows[row].TryChangeValue(dict, "FT", typeof (UInt16), "-");
            dt_ov.Rows[row].TryChangeValue(dict, "REB", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "OREB", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "DREB", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "AST", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "TO", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "STL", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "BLK", typeof (UInt16));
            dt_ov.Rows[row].TryChangeValue(dict, "FOUL", typeof (UInt16));
        }

        /// <summary>
        /// Creates a DataView instance based on the dt_ov Overview data table and updates the dgvOverviewStats data context.
        /// </summary>
        private void CreateViewAndUpdateOverview()
        {
            var dv_ov = new DataView(dt_ov) {AllowNew = false, AllowDelete = false};
            dgvOverviewStats.DataContext = dv_ov;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the cmbGraphStat control.
        /// Calculates and displays the player's performance graph for the newly selected stat.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void cmbGraphStat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGraphStat.SelectedIndex == -1 || cmbTeam.SelectedIndex == -1 || cmbPlayer.SelectedIndex == -1 || pbsList.Count < 1)
                return;

            ChartPrimitive cp = new ChartPrimitive();
            double i = 0;

            string propToGet = cmbGraphStat.SelectedItem.ToString();
            propToGet = propToGet.Replace('3', 'T');
            propToGet = propToGet.Replace('%', 'p');

            double sum = 0;
            double games = 0;

            foreach (var pbs in pbsList)
            {
                i++;
                double value =
                    Convert.ToDouble(typeof (PlayerBoxScore).GetProperty(propToGet).GetValue(pbs, null));
                if (!double.IsNaN(value))
                {
                    if (propToGet.Contains("p"))
                        value = Convert.ToDouble(Convert.ToInt32(value*1000)) / 1000;
                    cp.AddPoint(i, value);
                    games++;
                    sum += value;
                }
            }
            cp.Label = cmbGraphStat.SelectedItem.ToString();
            cp.ShowInLegend = false;
            chart.Primitives.Clear();
            if (cp.Points.Count > 0)
            {
                double average = sum / games;
                ChartPrimitive cpavg = new ChartPrimitive();
                for (int j = 1; j <= i; j++)
                {
                    cpavg.AddPoint(j, average);
                }
                cpavg.Color = System.Windows.Media.Color.FromRgb(0, 0, 100);
                cpavg.Dashed = true;
                cpavg.ShowInLegend = false;
                chart.Primitives.Add(cpavg);
                chart.Primitives.Add(cp);
            }
            chart.RedrawPlotLines();
            ChartPrimitive cp2 = new ChartPrimitive();
            cp2.AddPoint(1, 0);
            cp2.AddPoint(i, 1);
            chart.Primitives.Add(cp2);
            chart.ResetPanAndZoom();
        }

        /// <summary>
        /// Populates the graph stat combo.
        /// </summary>
        private void PopulateGraphStatCombo()
        {
            List<string> stats = new List<string>
                                 {
                                     "GmSc", "GmScE", "PTS", "FGM", "FGA", "FG%", "3PM", "3PA", "3P%", "FTM", "FTA", "FT%", "REB", "OREB", "DREB", "AST", "BLK", "STL", "TO", "FOUL"
                                 };

            stats.ForEach(s => cmbGraphStat.Items.Add(s));
        }

        /// <summary>
        /// Handles the Click event of the btnPrevStat control.
        /// Switches to the previous graph stat.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnPrevStat_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGraphStat.SelectedIndex == 0)
                cmbGraphStat.SelectedIndex = cmbGraphStat.Items.Count - 1;
            else
                cmbGraphStat.SelectedIndex--;
        }

        /// <summary>
        /// Handles the Click event of the btnNextStat control.
        /// Switches to the next graph stat.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnNextStat_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGraphStat.SelectedIndex == cmbGraphStat.Items.Count - 1)
                cmbGraphStat.SelectedIndex = 0;
            else
                cmbGraphStat.SelectedIndex++;
        }

        /// <summary>
        /// Handles the Click event of the btnPrevOppTeam control.
        /// Switches to the previous opposing team.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnPrevOppTeam_Click(object sender, RoutedEventArgs e)
        {
            if (cmbOppTeam.SelectedIndex == 0)
                cmbOppTeam.SelectedIndex = cmbOppTeam.Items.Count - 1;
            else
                cmbOppTeam.SelectedIndex--;
        }

        /// <summary>
        /// Handles the Click event of the btnNextOppTeam control.
        /// Switches to the next opposing team.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnNextOppTeam_Click(object sender, RoutedEventArgs e)
        {
            if (cmbOppTeam.SelectedIndex == cmbOppTeam.Items.Count - 1)
                cmbOppTeam.SelectedIndex = 0;
            else
                cmbOppTeam.SelectedIndex++;
        }

        /// <summary>
        /// Handles the Click event of the btnPrevOppPlayer control.
        /// Switches to the previous opposing player.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnPrevOppPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbOppPlayer.SelectedIndex == 0)
                cmbOppPlayer.SelectedIndex = cmbOppPlayer.Items.Count - 1;
            else
                cmbOppPlayer.SelectedIndex--;
        }

        /// <summary>
        /// Handles the Click event of the btnNextOppPlayer control.
        /// Switches to the next opposing player.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnNextOppPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbOppPlayer.SelectedIndex == cmbOppPlayer.Items.Count - 1)
                cmbOppPlayer.SelectedIndex = 0;
            else
                cmbOppPlayer.SelectedIndex++;
        }
    }
}