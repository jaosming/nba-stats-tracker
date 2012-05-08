﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LeftosCommonLibrary;
using Microsoft.Win32;

namespace NBA_Stats_Tracker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class boxScoreW : Window
    {
        #region Mode enum

        public enum Mode
        {
            Update,
            View
        };

        #endregion

        public static Mode curmode = Mode.Update;

        public static BoxScore curBoxScore;
        private readonly SQLiteDatabase db = new SQLiteDatabase(MainWindow.currentDB);
        private readonly int maxSeason = MainWindow.getMaxSeason(MainWindow.currentDB);
        private int curSeason;
        private Brush defaultBackground;
        private string playersT;
        private bool loading;
        private bool minsUpdating;
        private TeamStats[] tst = new TeamStats[MainWindow.tst.Length];
        private TeamStats[] tstopp = new TeamStats[MainWindow.tst.Length];
        private Dictionary<int, PlayerStats> pst = new Dictionary<int, PlayerStats>();

        public boxScoreW(Mode _curmode = Mode.Update)
        {
            InitializeComponent();

            prepareWindow(_curmode);
        }

        public boxScoreW(Mode _curmode, int id) : this(_curmode)
        {
            cbHistory.SelectedIndex = id;
        }

        private BindingList<PlayerBoxScore> pbsAwayList { get; set; }
        private BindingList<PlayerBoxScore> pbsHomeList { get; set; }
        private ObservableCollection<KeyValuePair<int, string>> PlayersListAway { get; set; }
        private ObservableCollection<KeyValuePair<int, string>> PlayersListHome { get; set; }
        private List<PlayerMetricStatsRow> pmsrListAway { get; set; }
        private List<PlayerMetricStatsRow> pmsrListHome { get; set; } 

        private void UpdateDataGrid(int team)
        {
            string Team1, Team2;
            string q;
            if (team == 1)
            {
                try
                {
                    Team1 = cmbTeam1.SelectedItem.ToString();
                }
                catch (Exception)
                {
                    return;
                }
                q = "select * from " + playersT + " where TeamFin LIKE '" + Team1 + "'";
            }
            else
            {
                try
                {
                    Team2 = cmbTeam2.SelectedItem.ToString();
                }
                catch (Exception)
                {
                    return;
                }
                q = "select * from " + playersT + " where TeamFin LIKE '" + Team2 + "'";
            }
            DataTable res = db.GetDataTable(q);

            if (team == 1)
            {
                PlayersListAway = new ObservableCollection<KeyValuePair<int, string>>();
                if (!loading) pbsAwayList = new BindingList<PlayerBoxScore>();

                foreach (DataRow r in res.Rows)
                {
                    var ps = new PlayerStats(r);
                    PlayersListAway.Add(new KeyValuePair<int, string>(ps.ID, ps.FirstName + " " + ps.LastName));
                }

                foreach (var cur in pbsAwayList)
                {
                    var player = new KeyValuePair<int, string>(cur.PlayerID, MainWindow.pst[cur.PlayerID].FirstName + " " + MainWindow.pst[cur.PlayerID].LastName);
                    if (!PlayersListAway.Contains(player))
                    {
                        PlayersListAway.Add(player);
                    }
                }

                colPlayerAway.ItemsSource = PlayersListAway;
                dgvPlayersAway.ItemsSource = pbsAwayList;
            }
            else
            {
                PlayersListHome = new ObservableCollection<KeyValuePair<int, string>>();
                if (!loading) pbsHomeList = new BindingList<PlayerBoxScore>();

                foreach (DataRow r in res.Rows)
                {
                    var ps = new PlayerStats(r);
                    PlayersListHome.Add(new KeyValuePair<int, string>(ps.ID, ps.FirstName + " " + ps.LastName));
                }

                foreach (var cur in pbsHomeList)
                {
                    var player = new KeyValuePair<int, string>(cur.PlayerID, MainWindow.pst[cur.PlayerID].FirstName + " " + MainWindow.pst[cur.PlayerID].LastName);
                    if (!PlayersListHome.Contains(player))
                    {
                        PlayersListHome.Add(player);
                    }
                }

                colPlayerHome.ItemsSource = PlayersListHome;
                dgvPlayersHome.ItemsSource = pbsHomeList;
            }
        }

        private void prepareWindow(Mode _curmode)
        {
            curSeason = MainWindow.curSeason;

            PopulateSeasonCombo();

            foreach (var kvp in MainWindow.TeamOrder)
            {
                cmbTeam1.Items.Add(kvp.Key);
                cmbTeam2.Items.Add(kvp.Key);
            }
            /*
            if ((MainWindow.pt == null) || (MainWindow.pt.teams[0] == "Invalid"))
            {
                foreach (var kvp in MainWindow.TeamOrder)
                {
                    cmbTeam1.Items.Add(kvp.Key);
                    cmbTeam2.Items.Add(kvp.Key);
                }
            }
            else
            {
                var newteams = new List<string>();
                foreach (string team in MainWindow.pt.teams)
                    newteams.Add(team);
                newteams.Sort();
                foreach (string newteam in newteams)
                {
                    cmbTeam1.Items.Add(newteam);
                    cmbTeam2.Items.Add(newteam);
                }
            }
            */
            defaultBackground = cmbTeam1.Background;

            pbsAwayList = new BindingList<PlayerBoxScore>();
            pbsHomeList = new BindingList<PlayerBoxScore>();

            pbsAwayList.AllowNew = true;
            pbsAwayList.AllowEdit = true;
            pbsAwayList.AllowRemove = true;
            pbsAwayList.RaiseListChangedEvents = true;

            pbsHomeList.AllowNew = true;
            pbsHomeList.AllowEdit = true;
            pbsHomeList.AllowRemove = true;
            pbsHomeList.RaiseListChangedEvents = true;

            dgvPlayersAway.ItemsSource = pbsAwayList;
            dgvPlayersHome.ItemsSource = pbsHomeList;

            dgvPlayersAway.RowEditEnding += EventHandlers.WPFDataGrid_RowEditEnding_GoToNewRowOnTab;
            dgvPlayersAway.PreviewKeyDown += EventHandlers.Any_PreviewKeyDown_CheckTab;
            dgvPlayersAway.PreviewKeyUp += EventHandlers.Any_PreviewKeyUp_CheckTab;
            dgvPlayersHome.RowEditEnding += EventHandlers.WPFDataGrid_RowEditEnding_GoToNewRowOnTab;
            dgvPlayersHome.PreviewKeyDown += EventHandlers.Any_PreviewKeyDown_CheckTab;
            dgvPlayersHome.PreviewKeyUp += EventHandlers.Any_PreviewKeyUp_CheckTab;

            dgvPlayersAway.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvPlayersHome.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvMetricAway.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            dgvMetricHome.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;

            dgvMetricAwayEFFColumn.Visibility = Visibility.Collapsed;
            dgvMetricAwayPERColumn.Visibility = Visibility.Collapsed;
            dgvMetricAwayPPRColumn.Visibility = Visibility.Collapsed;

            dgvMetricHomeEFFColumn.Visibility = Visibility.Collapsed;
            dgvMetricHomePERColumn.Visibility = Visibility.Collapsed;
            dgvMetricHomePPRColumn.Visibility = Visibility.Collapsed;

            cmbTeam1.SelectedIndex = -1;
            cmbTeam2.SelectedIndex = -1;
            cmbTeam1.SelectedIndex = 0;
            cmbTeam2.SelectedIndex = 1;

            MainWindow.bs.done = false;

            dtpGameDate.SelectedDate = DateTime.Today;

            foreach (BoxScoreEntry cur in MainWindow.bshist)
            {
                cbHistory.Items.Add(cur.date.ToShortDateString() + " - " + cur.bs.Team1 + " @ " + cur.bs.Team2);
            }

            curmode = _curmode;

            MainWindow.bs.done = false;

            calculateScore1();
            calculateScore2();

            if (curmode == Mode.View)
            {
                label1.Content = "Select a box score to view";
                chkDoNotUpdate.Visibility = Visibility.Hidden;
                txbDoNotUpdate.Visibility = Visibility.Hidden;
                Title = "View & Edit Box Score";
            }
        }

        private void PopulateSeasonCombo()
        {
            for (int i = maxSeason; i > 0; i--)
            {
                cmbSeasonNum.Items.Add(i.ToString());
            }

            cmbSeasonNum.SelectedIndex = -1;
            cmbSeasonNum.SelectedItem = curSeason.ToString();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (curmode == Mode.Update)
            {
                tryParseBS();
                if (MainWindow.bs.done == false) return;
            }
            else
            {
                if (MainWindow.isCustom)
                {
                    if (cbHistory.SelectedIndex != -1)
                    {
                        MessageBoxResult r = MessageBox.Show("Do you want to save any changes to this Box Score?",
                                                             "NBA Stats Tracker", MessageBoxButton.YesNoCancel,
                                                             MessageBoxImage.Question);
                        if (r == MessageBoxResult.Cancel) return;
                        else if (r == MessageBoxResult.Yes)
                        {
                            tryParseBS();
                            if (MainWindow.bs.done == false) return;

                            MessageBox.Show("It is recommended to save the database for changes to take effect.");
                        }
                        else
                        {
                            MainWindow.bs.done = false;
                        }
                    }
                    else
                    {
                        MainWindow.bs.done = false;
                    }
                }
            }
            Close();
        }

        private void tryParseBS()
        {
            if (cmbTeam1.SelectedItem.ToString() == cmbTeam2.SelectedItem.ToString())
            {
                MessageBox.Show("You can't have the same team in both Home & Away.");
                return;
            }
            if ((txtPTS1.Text == "") || (txtPTS1.Text == "N/A") || (txtPTS2.Text == "") || (txtPTS2.Text == "N/A"))
            {
                //MessageBox.Show("The Box Score is incomplete. Make sure you input all stats.");
                return;
            }
            if (cmbSeasonNum.SelectedIndex == -1)
            {
                MessageBox.Show("You have to choose a season.");
                return;
            }
            try
            {
                try
                {
                    MainWindow.bs.id = curBoxScore.id;
                    MainWindow.bs.bshistid = curBoxScore.bshistid;
                }
                catch
                {
                    MainWindow.bs.id = -1;
                    MainWindow.bs.bshistid = -1;
                }
                MainWindow.bs.isPlayoff = chkIsPlayoff.IsChecked.GetValueOrDefault();
                MainWindow.bs.gamedate = dtpGameDate.SelectedDate.GetValueOrDefault();
                MainWindow.bs.SeasonNum = Convert.ToInt32(cmbSeasonNum.SelectedItem.ToString());
                MainWindow.bs.Team1 = cmbTeam1.SelectedItem.ToString();
                MainWindow.bs.Team2 = cmbTeam2.SelectedItem.ToString();
                MainWindow.bs.MINS2 = MainWindow.bs.MINS1 = Convert.ToUInt16(txtMINS1.Text);

                if (MainWindow.bs.MINS1 <= 0)
                {
                    MessageBox.Show("You have to enter the game's minutes. Usually 48 for 4 quarters, 53 for 1 overtime, 58 for 2 overtimes.");
                    throw (new Exception());
                }
                
                MainWindow.bs.PTS1 = Convert.ToUInt16(txtPTS1.Text);
                MainWindow.bs.REB1 = Convert.ToUInt16(txtREB1.Text);
                MainWindow.bs.AST1 = Convert.ToUInt16(txtAST1.Text);
                MainWindow.bs.STL1 = Convert.ToUInt16(txtSTL1.Text);
                MainWindow.bs.BLK1 = Convert.ToUInt16(txtBLK1.Text);
                MainWindow.bs.TO1 = Convert.ToUInt16(txtTO1.Text);
                MainWindow.bs.FGM1 = Convert.ToUInt16(txtFGM1.Text);
                MainWindow.bs.FGA1 = Convert.ToUInt16(txtFGA1.Text);
                MainWindow.bs.TPM1 = Convert.ToUInt16(txt3PM1.Text);
                MainWindow.bs.TPA1 = Convert.ToUInt16(txt3PA1.Text);

                if (MainWindow.bs.FGA1 < MainWindow.bs.FGM1)
                {
                    MessageBox.Show("The FGM stat can't be higher than the FGA stat.");
                    throw (new Exception());
                }
                if (MainWindow.bs.TPA1 < MainWindow.bs.TPM1)
                {
                    MessageBox.Show("The 3PM stat can't be higher than the 3PA stat.");
                    throw (new Exception());
                }
                if (MainWindow.bs.FGM1 < MainWindow.bs.TPM1)
                {
                    MessageBox.Show("The 3PM stat can't be higher than the FGM stat.");
                    throw (new Exception());
                }

                MainWindow.bs.FTM1 = Convert.ToUInt16(txtFTM1.Text);
                MainWindow.bs.FTA1 = Convert.ToUInt16(txtFTA1.Text);
                if (MainWindow.bs.FTA1 < MainWindow.bs.FTM1)
                {
                    MessageBox.Show("The FTM stat can't be higher than the FTA stat.");
                    throw (new Exception());
                }

                MainWindow.bs.OFF1 = Convert.ToUInt16(txtOFF1.Text);
                if (MainWindow.bs.OFF1 > MainWindow.bs.REB1)
                {
                    MessageBox.Show("The OFF stat can't be higher than the REB stat.");
                    throw (new Exception());
                }
                if (MainWindow.bs.FGA1 < MainWindow.bs.TPA1)
                {
                    MessageBox.Show("The 3PA stat can't be higher than the FGA stat.");
                    throw (new Exception());
                }

                MainWindow.bs.PF1 = Convert.ToUInt16(txtPF1.Text);
                MainWindow.bs.PTS2 = Convert.ToUInt16(txtPTS2.Text);
                MainWindow.bs.REB2 = Convert.ToUInt16(txtREB2.Text);
                MainWindow.bs.AST2 = Convert.ToUInt16(txtAST2.Text);
                MainWindow.bs.STL2 = Convert.ToUInt16(txtSTL2.Text);
                MainWindow.bs.BLK2 = Convert.ToUInt16(txtBLK2.Text);
                MainWindow.bs.TO2 = Convert.ToUInt16(txtTO2.Text);
                MainWindow.bs.FGM2 = Convert.ToUInt16(txtFGM2.Text);
                MainWindow.bs.FGA2 = Convert.ToUInt16(txtFGA2.Text);
                MainWindow.bs.TPM2 = Convert.ToUInt16(txt3PM2.Text);
                MainWindow.bs.TPA2 = Convert.ToUInt16(txt3PA2.Text);

                if (MainWindow.bs.FGA2 < MainWindow.bs.FGM2)
                {
                    MessageBox.Show("The FGM stat can't be higher than the FGA stat.");
                    throw (new Exception());
                }
                if (MainWindow.bs.TPA2 < MainWindow.bs.TPM2)
                {
                    MessageBox.Show("The 3PM stat can't be higher than the 3PA stat.");
                    throw (new Exception());
                }
                if (MainWindow.bs.FGM2 < MainWindow.bs.TPM2)
                {
                    MessageBox.Show("The 3PM stat can't be higher than the FGM stat.");
                    throw (new Exception());
                }
                if (MainWindow.bs.FGA2 < MainWindow.bs.TPA2)
                {
                    MessageBox.Show("The 3PA stat can't be higher than the FGA stat.");
                    throw (new Exception());
                }

                MainWindow.bs.FTM2 = Convert.ToUInt16(txtFTM2.Text);
                MainWindow.bs.FTA2 = Convert.ToUInt16(txtFTA2.Text);
                if (MainWindow.bs.FTA2 < MainWindow.bs.FTM2)
                {
                    MessageBox.Show("The FTM stat can't be higher than the FTA stat.");
                    throw (new Exception());
                }

                MainWindow.bs.OFF2 = Convert.ToUInt16(txtOFF2.Text);

                if (MainWindow.bs.OFF2 > MainWindow.bs.REB2)
                {
                    MessageBox.Show("The OFF stat can't be higher than the REB stat.");
                    throw (new Exception());
                }

                MainWindow.bs.PF2 = Convert.ToUInt16(txtPF2.Text);

                MainWindow.bs.doNotUpdate = chkDoNotUpdate.IsChecked.GetValueOrDefault();

                #region Player Box Scores Check

                string Team1 = cmbTeam1.SelectedItem.ToString();
                string Team2 = cmbTeam2.SelectedItem.ToString();

                foreach (PlayerBoxScore pbs in pbsAwayList)
                    pbs.Team = Team1;

                foreach (PlayerBoxScore pbs in pbsHomeList)
                    pbs.Team = Team2;

                int starters = 0;
                var pbsLists = new List<BindingList<PlayerBoxScore>>(2);
                pbsLists.Add(pbsAwayList);
                pbsLists.Add(pbsHomeList);
                var allPlayers = new Dictionary<int, string>();
                foreach (var kvp in PlayersListAway) allPlayers.Add(kvp.Key, kvp.Value);
                foreach (var kvp in PlayersListHome) allPlayers.Add(kvp.Key, kvp.Value);
                foreach (var pbsList in pbsLists)
                {
                    foreach (PlayerBoxScore pbs in pbsList)
                    {
                        //pbs.PlayerID = 
                        if (pbs.PlayerID == -1) continue;

                        if (pbs.isOut)
                        {
                            pbs.ResetStats();
                            continue;
                        }

                        if (pbs.isStarter)
                        {
                            starters++;
                            if (starters > 5)
                            {
                                string s = "There can't be more than 5 starters in each team.";
                                s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                                MessageBox.Show(s);
                                throw (new Exception());
                            }
                        }

                        if (pbs.FGM > pbs.FGA)
                        {
                            string s = "The FGM stat can't be higher than the FGA stat.";
                            s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                            MessageBox.Show(s);
                            throw (new Exception());
                        }

                        if (pbs.TPM > pbs.TPA)
                        {
                            string s = "The 3PM stat can't be higher than the 3PA stat.";
                            s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                            MessageBox.Show(s);
                            throw (new Exception());
                        }

                        if (pbs.FGM < pbs.TPM)
                        {
                            string s = "The 3PM stat can't be higher than the FGM stat.";
                            s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                            MessageBox.Show(s);
                            throw (new Exception());
                        }

                        if (pbs.FGA < pbs.TPA)
                        {
                            string s = "The TPA stat can't be higher than the FGA stat.";
                            s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                            MessageBox.Show(s);
                            throw (new Exception());
                        }

                        if (pbs.FTM > pbs.FTA)
                        {
                            string s = "The FTM stat can't be higher than the FTA stat.";
                            s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                            MessageBox.Show(s);
                            throw (new Exception());
                        }

                        if (pbs.OREB > pbs.REB)
                        {
                            string s = "The OREB stat can't be higher than the REB stat.";
                            s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                            MessageBox.Show(s);
                            throw (new Exception());
                        }

                        if (pbs.isStarter && pbs.MINS == 0)
                        {
                            string s = "A player can't be a starter but not have any minutes played.";
                            s += "\n\nTeam: " + pbs.Team + "\nPlayer: " + allPlayers[pbs.PlayerID];
                            MessageBox.Show(s);
                            throw (new Exception());
                        }

                        pbs.DREB = (UInt16) (pbs.REB - pbs.OREB);
                    }
                }
                MainWindow.pbsLists = pbsLists;

                #endregion

                MainWindow.bs.done = true;
            }
            catch
            {
                MessageBox.Show("The Box Score seems to be invalid. Check that there's no stats missing.");
                MainWindow.bs.done = false;
            }
        }

        private void cmbTeam1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            checkIfSameTeams();
            tabTeam1.Header = cmbTeam1.SelectedItem;
            UpdateDataGrid(1);
        }

        private void checkIfSameTeams()
        {
            string Team1, Team2;
            try
            {
                Team1 = cmbTeam1.SelectedItem.ToString();
                Team2 = cmbTeam2.SelectedItem.ToString();
            }
            catch (Exception)
            {
                return;
            }


            if (Team1 == Team2)
            {
                cmbTeam1.Background = Brushes.Red;
                cmbTeam2.Background = Brushes.Red;
                return;
            }
            else
            {
                cmbTeam1.Background = defaultBackground;
                cmbTeam2.Background = defaultBackground;
            }

            if (MainWindow.pt != null && MainWindow.pt.teams[0] != "Invalid")
            {
                if (MainWindow.West.Contains(Team1))
                {
                    if (!MainWindow.West.Contains(Team2))
                    {
                        cmbTeam1.Background = Brushes.Red;
                        cmbTeam2.Background = Brushes.Red;
                        return;
                    }
                    else
                    {
                        cmbTeam1.Background = defaultBackground;
                        cmbTeam2.Background = defaultBackground;
                    }
                }
                else
                {
                    if (MainWindow.West.Contains(Team2))
                    {
                        cmbTeam1.Background = Brushes.Red;
                        cmbTeam2.Background = Brushes.Red;
                        return;
                    }
                    else
                    {
                        cmbTeam1.Background = defaultBackground;
                        cmbTeam2.Background = defaultBackground;
                    }
                }
            }
        }

        private void cmbTeam2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            checkIfSameTeams();
            tabTeam2.Header = cmbTeam2.SelectedItem;
            UpdateDataGrid(2);
        }

        private void txtFGM1_TextChanged(object sender, TextChangedEventArgs e)
        {
            calculateScore1();
        }

        private void calculateScore1()
        {
            calculateScore1(null, null);
        }

        private void calculateScore2()
        {
            calculateScore2(null, null);
        }

        private void calculateScore1(object sender, TextChangedEventArgs e)
        {
            try
            {
                int fgm = Convert.ToInt32(txtFGM1.Text);
                int tpm = Convert.ToInt32(txt3PM1.Text);
                int ftm = Convert.ToInt32(txtFTM1.Text);
                txtPTS1.Text = ((fgm - tpm)*2 + tpm*3 + ftm).ToString();

                int fga = Convert.ToInt32(txtFGA1.Text);
                int tpa = Convert.ToInt32(txt3PA1.Text);
                int fta = Convert.ToInt32(txtFTA1.Text);
                txbT1Avg.Text = String.Format("FG%: {0:F3}\t3P%: {1:F3}\tFT%: {2:F3}", (float) fgm/fga, (float) tpm/tpa,
                                              (float) ftm/fta);
            }
            catch
            {
                txtPTS1.Text = "N/A";
                txbT1Avg.Text = "";
            }
        }

        private void txt3PM1_TextChanged(object sender, TextChangedEventArgs e)
        {
            calculateScore1();
        }

        private void txtFTM1_TextChanged(object sender, TextChangedEventArgs e)
        {
            calculateScore1();
        }

        private void txtFGM2_TextChanged(object sender, TextChangedEventArgs e)
        {
            calculateScore2();
        }

        private void calculateScore2(object sender, TextChangedEventArgs e)
        {
            try
            {
                int fgm = Convert.ToInt32(txtFGM2.Text);
                int tpm = Convert.ToInt32(txt3PM2.Text);
                int ftm = Convert.ToInt32(txtFTM2.Text);
                txtPTS2.Text = ((fgm - tpm)*2 + tpm*3 + ftm).ToString();

                int fga = Convert.ToInt32(txtFGA2.Text);
                int tpa = Convert.ToInt32(txt3PA2.Text);
                int fta = Convert.ToInt32(txtFTA2.Text);
                txbT2Avg.Text = String.Format("FG%: {0:F3}\t3P%: {1:F3}\tFT%: {2:F3}", (float) fgm/fga, (float) tpm/tpa,
                                              (float) ftm/fta);
            }
            catch
            {
                txtPTS2.Text = "N/A";
                txbT2Avg.Text = "";
            }
        }

        private void txt3PM2_TextChanged(object sender, TextChangedEventArgs e)
        {
            calculateScore2();
        }

        private void txtFTM2_TextChanged(object sender, TextChangedEventArgs e)
        {
            calculateScore2();
        }

        private void btnCSVOK_Click(object sender, RoutedEventArgs e)
        {
            tryParseBS();
            if (MainWindow.bs.done)
            {
                /*
                string header1 = 
                    //"Team,PTS,REB,AST,STL,BLK,TO,FGM,FGA,FG%,3PM,3PA,3P%,FTM,FTA,FT%,OREB,FOUL";
                    "Team\tPTS\tREB\tAST\tSTL\tBLK\tTO\tFGM\tFGA\tFG%\t3PM\t3PA\t3P%\tFTM\tFTA\tFT%\tOREB\tFOUL";
                */

                string data1 =
                    String.Format(
                        //"{0},{1},{2},{5},{6},{7},{8},{9},{10},{11:F3},{12},{13},{14:F3},{15},{16},{17:F3},{3},{18}",
                        //"{0}\t{1}\t{2}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11:F3}\t{12}\t{13}\t{14:F3}\t{15}\t{16}\t{17:F3}\t{3}\t{18}",
                        "{0}\t\t\t\t\t{1}\t{2}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11:F3}\t{12}\t{13}\t{14:F3}\t{15}\t{16}\t{17:F3}\t{3}\t{18}",
                        cmbTeam1.SelectedItem, MainWindow.bs.PTS1, MainWindow.bs.REB1, MainWindow.bs.OFF1,
                        MainWindow.bs.REB1 - MainWindow.bs.OFF1,
                        MainWindow.bs.AST1, MainWindow.bs.STL1, MainWindow.bs.BLK1, MainWindow.bs.TO1,
                        MainWindow.bs.FGM1,
                        MainWindow.bs.FGA1, MainWindow.bs.FGM1/(float) MainWindow.bs.FGA1, MainWindow.bs.TPM1,
                        MainWindow.bs.TPA1, MainWindow.bs.TPM1/(float) MainWindow.bs.TPA1,
                        MainWindow.bs.FTM1, MainWindow.bs.FTA1, MainWindow.bs.FTM1/(float) MainWindow.bs.FTA1,
                        MainWindow.bs.PF1);

                string data2 =
                    String.Format(
                        //"{0},{1},{2},{5},{6},{7},{8},{9},{10},{11:F3},{12},{13},{14:F3},{15},{16},{17:F3},{3},{18}",
                        //"{0}\t{1}\t{2}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11:F3}\t{12}\t{13}\t{14:F3}\t{15}\t{16}\t{17:F3}\t{3}\t{18}",
                        "{0}\t\t\t\t\t{1}\t{2}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11:F3}\t{12}\t{13}\t{14:F3}\t{15}\t{16}\t{17:F3}\t{3}\t{18}",
                        cmbTeam2.SelectedItem, MainWindow.bs.PTS2, MainWindow.bs.REB2, MainWindow.bs.OFF2,
                        MainWindow.bs.REB2 - MainWindow.bs.OFF2,
                        MainWindow.bs.AST2, MainWindow.bs.STL2, MainWindow.bs.BLK2, MainWindow.bs.TO2,
                        MainWindow.bs.FGM2,
                        MainWindow.bs.FGA2, MainWindow.bs.FGM2/(float) MainWindow.bs.FGA2, MainWindow.bs.TPM2,
                        MainWindow.bs.TPA2, MainWindow.bs.TPM2/(float) MainWindow.bs.TPA2,
                        MainWindow.bs.FTM2, MainWindow.bs.FTA2, MainWindow.bs.FTM2/(float) MainWindow.bs.FTA2,
                        MainWindow.bs.PF2);

                /*
                var sfd = new SaveFileDialog();
                sfd.Filter = "Comma-Separated Values file (*.csv)|*.csv";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                sfd.Title = "Select a file to save the CSV to...";
                sfd.ShowDialog();
                if (sfd.FileName == "") return;

                var sw = new StreamWriter(sfd.FileName);
                sw.WriteLine(header1);
                sw.WriteLine(data1);
                sw.WriteLine(data2);
                sw.Close();
                */
                dgvPlayersAway.SelectAllCells();
                ApplicationCommands.Copy.Execute(null, dgvPlayersAway);
                dgvPlayersAway.UnselectAllCells();
                string result1 = (string) Clipboard.GetData(DataFormats.Text);
                dgvPlayersHome.SelectAllCells();
                ApplicationCommands.Copy.Execute(null, dgvPlayersHome);
                dgvPlayersHome.UnselectAllCells();
                string result2 = (string)Clipboard.GetData(DataFormats.Text);

                //string result = header1 + "\n" + result1 + "\n" + data1 + "\n" + data2;
                string result = result1 + data1 + "\n\n\n" + result2 + data2;
                Clipboard.SetText(result);
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.bs.done = false;
            Close();
        }

        private void _bsAnyTextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox) sender;
            tb.SelectAll();
        }

        private void cbHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = cbHistory.SelectedIndex;
            BoxScore bs = MainWindow.bshist[i].bs;
            curBoxScore = MainWindow.bshist[i].bs;
            curBoxScore.bshistid = i;
            txtPTS1.Text = bs.PTS1.ToString();
            txtREB1.Text = bs.REB1.ToString();
            txtAST1.Text = bs.AST1.ToString();
            txtSTL1.Text = bs.STL1.ToString();
            txtBLK1.Text = bs.BLK1.ToString();
            txtTO1.Text = bs.TO1.ToString();
            txtFGM1.Text = bs.FGM1.ToString();
            txtFGA1.Text = bs.FGA1.ToString();
            txt3PM1.Text = bs.TPM1.ToString();
            txt3PA1.Text = bs.TPA1.ToString();
            txtFTM1.Text = bs.FTM1.ToString();
            txtFTA1.Text = bs.FTA1.ToString();
            txtOFF1.Text = bs.OFF1.ToString();
            txtPF1.Text = bs.PF1.ToString();
            txtMINS1.Text = bs.MINS1.ToString();
            txtPTS2.Text = bs.PTS2.ToString();
            txtREB2.Text = bs.REB2.ToString();
            txtAST2.Text = bs.AST2.ToString();
            txtSTL2.Text = bs.STL2.ToString();
            txtBLK2.Text = bs.BLK2.ToString();
            txtTO2.Text = bs.TO2.ToString();
            txtFGM2.Text = bs.FGM2.ToString();
            txtFGA2.Text = bs.FGA2.ToString();
            txt3PM2.Text = bs.TPM2.ToString();
            txt3PA2.Text = bs.TPA2.ToString();
            txtFTM2.Text = bs.FTM2.ToString();
            txtFTA2.Text = bs.FTA2.ToString();
            txtOFF2.Text = bs.OFF2.ToString();
            txtPF2.Text = bs.PF2.ToString();

            dtpGameDate.SelectedDate = bs.gamedate;
            cmbSeasonNum.SelectedItem = bs.SeasonNum.ToString();
            chkIsPlayoff.IsChecked = bs.isPlayoff;

            calculateScore1();
            calculateScore2();

            pbsAwayList = new BindingList<PlayerBoxScore>();
            pbsHomeList = new BindingList<PlayerBoxScore>();

            pbsAwayList.AllowNew = true;
            pbsAwayList.AllowEdit = true;
            pbsAwayList.AllowRemove = true;
            pbsAwayList.RaiseListChangedEvents = true;

            pbsHomeList.AllowNew = true;
            pbsHomeList.AllowEdit = true;
            pbsHomeList.AllowRemove = true;
            pbsHomeList.RaiseListChangedEvents = true;

            dgvPlayersAway.ItemsSource = pbsAwayList;
            dgvPlayersHome.ItemsSource = pbsHomeList;
            loading = true;
            foreach (PlayerBoxScore pbs in MainWindow.bshist[i].pbsList)
            {
                if (pbs.Team == bs.Team1)
                {
                    pbsAwayList.Add(pbs);
                }
                else
                {
                    pbsHomeList.Add(pbs);
                }
            }

            cmbTeam1.SelectedItem = bs.Team1;
            cmbTeam2.SelectedItem = bs.Team2;
            loading = false;
        }

        private void cmbSeasonNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSeasonNum.SelectedIndex == -1) return;

            curSeason = Convert.ToInt32(cmbSeasonNum.SelectedItem.ToString());
            MainWindow.curSeason = curSeason;

            MainWindow.LoadSeason(MainWindow.currentDB, ref tst, ref tstopp, ref pst, ref MainWindow.TeamOrder,
                                    ref MainWindow.pt, ref MainWindow.bshist, _curSeason: curSeason,
                                    doNotLoadBoxScores: true);

            playersT = "Players";

            if (curSeason != maxSeason)
            {
                playersT += "S" + curSeason;
            }
        }

        private void btnCalculateTeams_Click(object sender, RoutedEventArgs e)
        {
            int REB = 0,
                AST = 0,
                STL = 0,
                TOS = 0,
                BLK = 0,
                FGM = 0,
                FGA = 0,
                TPM = 0,
                TPA = 0,
                FTM = 0,
                FTA = 0,
                OREB = 0,
                FOUL = 0;

            foreach (PlayerBoxScore pbs in pbsAwayList)
            {
                REB += pbs.REB;
                AST += pbs.AST;
                STL += pbs.STL;
                TOS += pbs.TOS;
                BLK += pbs.BLK;
                FGM += pbs.FGM;
                FGA += pbs.FGA;
                TPM += pbs.TPM;
                TPA += pbs.TPA;
                FTM += pbs.FTM;
                FTA += pbs.FTA;
                OREB += pbs.OREB;
                FOUL += pbs.FOUL;
            }

            txtREB1.Text = REB.ToString();
            txtAST1.Text = AST.ToString();
            txtSTL1.Text = STL.ToString();
            txtBLK1.Text = BLK.ToString();
            txtTO1.Text = TOS.ToString();
            txtFGM1.Text = FGM.ToString();
            txtFGA1.Text = FGA.ToString();
            txt3PM1.Text = TPM.ToString();
            txt3PA1.Text = TPA.ToString();
            txtFTM1.Text = FTM.ToString();
            txtFTA1.Text = FTA.ToString();
            txtOFF1.Text = OREB.ToString();
            txtPF1.Text = FOUL.ToString();

            calculateScore1();

            REB = 0;
            AST = 0;
            STL = 0;
            TOS = 0;
            BLK = 0;
            FGM = 0;
            FGA = 0;
            TPM = 0;
            TPA = 0;
            FTM = 0;
            FTA = 0;
            OREB = 0;
            FOUL = 0;

            foreach (PlayerBoxScore pbs in pbsHomeList)
            {
                REB += pbs.REB;
                AST += pbs.AST;
                STL += pbs.STL;
                TOS += pbs.TOS;
                BLK += pbs.BLK;
                FGM += pbs.FGM;
                FGA += pbs.FGA;
                TPM += pbs.TPM;
                TPA += pbs.TPA;
                FTM += pbs.FTM;
                FTA += pbs.FTA;
                OREB += pbs.OREB;
                FOUL += pbs.FOUL;
            }

            txtREB2.Text = REB.ToString();
            txtAST2.Text = AST.ToString();
            txtSTL2.Text = STL.ToString();
            txtBLK2.Text = BLK.ToString();
            txtTO2.Text = TOS.ToString();
            txtFGM2.Text = FGM.ToString();
            txtFGA2.Text = FGA.ToString();
            txt3PM2.Text = TPM.ToString();
            txt3PA2.Text = TPA.ToString();
            txtFTM2.Text = FTM.ToString();
            txtFTA2.Text = FTA.ToString();
            txtOFF2.Text = OREB.ToString();
            txtPF2.Text = FOUL.ToString();

            calculateScore2();
        }

        private void colPlayerAway_CopyingCellClipboardContent(object sender, DataGridCellClipboardEventArgs e)
        {
            try
            {
                foreach (var p in PlayersListAway)
                {
                    if (Convert.ToInt32(e.Content) == p.Key)
                    {
                        e.Content = p.Value;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void colPlayerHome_CopyingCellClipboardContent(object sender, DataGridCellClipboardEventArgs e)
        {
            try
            {
                foreach (var p in PlayersListHome)
                {
                    if (Convert.ToInt32(e.Content) == p.Key)
                    {
                        e.Content = p.Value;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void PercentageColumn_CopyingCellClipboardContent(object sender, DataGridCellClipboardEventArgs e)
        {
            try
            {
                e.Content = String.Format("{0:F3}", e.Content);
            }
            catch (Exception)
            {
                return;
            }
        }

        private void txtMINS1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!minsUpdating)
            {
                minsUpdating = true;
                txtMINS2.Text = txtMINS1.Text;
                minsUpdating = false;
            }
        }

        private void txtMINS2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!minsUpdating)
            {
                minsUpdating = true;
                txtMINS1.Text = txtMINS2.Text;
                minsUpdating = false;
            }
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is TabControl)
            {
                if (tabControl1.SelectedItem == tabAwayMetric)
                {
                    UpdateMetric(1);
                }
                else if (tabControl1.SelectedItem == tabHomeMetric)
                {
                    UpdateMetric(2);
                }
                else if (tabControl1.SelectedItem == tabBest)
                {
                    UpdateMetric(1);
                    UpdateMetric(2);
                    UpdateBest();
                }
            }
        }

        private void UpdateBest()
        {
            try
            {
                if (pmsrListAway.Count == 0 && pmsrListHome.Count == 0) return;

                pmsrListAway.Sort(delegate(PlayerMetricStatsRow pmsr1, PlayerMetricStatsRow pmsr2)
                { return pmsr1.GmSc.CompareTo(pmsr2.GmSc); });
                pmsrListAway.Reverse();

                pmsrListHome.Sort(delegate(PlayerMetricStatsRow pmsr1, PlayerMetricStatsRow pmsr2)
                { return pmsr1.GmSc.CompareTo(pmsr2.GmSc); });
                pmsrListHome.Reverse();
            }
            catch (Exception)
            {
                return;
            }

            string TeamBest;
            int awayid, homeid;
            PlayerBoxScore pbsBest = new PlayerBoxScore();
            PlayerBoxScore pbsAway1 = new PlayerBoxScore(),
                           pbsAway2 = new PlayerBoxScore(),
                           pbsAway3 = new PlayerBoxScore(),
                           pbsHome1 = new PlayerBoxScore(),
                           pbsHome2 = new PlayerBoxScore(),
                           pbsHome3 = new PlayerBoxScore();

            txbMVP.Text = "";
            txbMVPStats.Text = "";
            txbAway1.Text = "";
            txbAway2.Text = "";
            txbAway3.Text = "";
            txbHome1.Text = "";
            txbHome2.Text = "";
            txbHome3.Text = "";

            bool skipaway = pmsrListAway.Count == 0;

            if (!skipaway || pmsrListAway[0].GmSc > pmsrListHome[0].GmSc)
            {
                int bestID = pmsrListAway[0].ID;
                foreach (var pbs in pbsAwayList)
                {
                    if (pbs.PlayerID == bestID)
                    {
                        pbsBest = pbs;
                    }
                }
                TeamBest = cmbTeam1.SelectedItem.ToString();
                awayid = 1;
                homeid = 0;
            }
            else
            {
                int bestID = pmsrListHome[0].ID;
                foreach (var pbs in pbsHomeList)
                {
                    if (pbs.PlayerID == bestID)
                    {
                        pbsBest = pbs;
                    }
                }
                TeamBest = cmbTeam1.SelectedItem.ToString();
                awayid = 0;
                homeid = 1;
            }

            PlayerStats ps = pst[pbsBest.PlayerID];
            string text = pbsBest.GetBestStats(5, ps.Position1);
            txbMVP.Text = ps.FirstName + " " + ps.LastName;
            txbMVPStats.Text = TeamBest + "\n\n" + text;

            if (pmsrListAway.Count > awayid)
            {
                int id2 = pmsrListAway[awayid++].ID;
                foreach (var pbs in pbsAwayList)
                {
                    if (pbs.PlayerID == id2)
                    {
                        pbsAway1 = pbs;
                    }
                }

                ps = pst[pbsAway1.PlayerID];
                text = pbsAway1.GetBestStats(5, ps.Position1);
                txbAway1.Text = ps.FirstName + " " + ps.LastName + "\n\n" + text;
            }

            if (pmsrListAway.Count > awayid)
            {
                int id3 = pmsrListAway[awayid++].ID;
                foreach (var pbs in pbsAwayList)
                {
                    if (pbs.PlayerID == id3)
                    {
                        pbsAway2 = pbs;
                    }
                }

                ps = pst[pbsAway2.PlayerID];
                text = pbsAway2.GetBestStats(5, ps.Position1);
                txbAway2.Text = ps.FirstName + " " + ps.LastName + "\n\n" + text;
            }

            if (pmsrListAway.Count > awayid)
            {
                int id3 = pmsrListAway[awayid++].ID;
                foreach (var pbs in pbsAwayList)
                {
                    if (pbs.PlayerID == id3)
                    {
                        pbsAway3 = pbs;
                    }
                }

                ps = pst[pbsAway3.PlayerID];
                text = pbsAway3.GetBestStats(5, ps.Position1);
                txbAway3.Text = ps.FirstName + " " + ps.LastName + "\n\n" + text;
            }

            if (pmsrListHome.Count > homeid)
            {
                int id2 = pmsrListHome[homeid++].ID;
                foreach (var pbs in pbsHomeList)
                {
                    if (pbs.PlayerID == id2)
                    {
                        pbsHome1 = pbs;
                    }
                }

                ps = pst[pbsHome1.PlayerID];
                text = pbsHome1.GetBestStats(5, ps.Position1);
                txbHome1.Text = ps.FirstName + " " + ps.LastName + "\n\n" + text;
            }

            if (pmsrListHome.Count > homeid)
            {
                int id3 = pmsrListHome[homeid++].ID;
                foreach (var pbs in pbsHomeList)
                {
                    if (pbs.PlayerID == id3)
                    {
                        pbsHome2 = pbs;
                    }
                }

                ps = pst[pbsHome2.PlayerID];
                text = pbsHome2.GetBestStats(5, ps.Position1);
                txbHome2.Text = ps.FirstName + " " + ps.LastName + "\n\n" + text;
            }

            if (pmsrListHome.Count > homeid)
            {
                int id3 = pmsrListHome[homeid++].ID;
                foreach (var pbs in pbsHomeList)
                {
                    if (pbs.PlayerID == id3)
                    {
                        pbsHome3 = pbs;
                    }
                }

                ps = pst[pbsHome3.PlayerID];
                text = pbsHome3.GetBestStats(5, ps.Position1);
                txbHome3.Text = ps.FirstName + " " + ps.LastName + "\n\n" + text;
            }
        }

        private void UpdateMetric(int team)
        {
            PlayerStats ps;
            TeamStats ts = new TeamStats(cmbTeam1.SelectedItem.ToString());
            TeamStats tsopp = new TeamStats(cmbTeam2.SelectedItem.ToString());

            tryParseBS();
            if (!MainWindow.bs.done) return;

            BoxScore bs = MainWindow.bs;

            if (team == 1) MainWindow.AddTeamStatsFromBoxScore(bs, ref ts, ref tsopp);
            else MainWindow.AddTeamStatsFromBoxScore(bs, ref tsopp, ref ts);

            ts.CalcMetrics(tsopp);

            List<PlayerMetricStatsRow> pmsrList = new List<PlayerMetricStatsRow>();
            BindingList<PlayerBoxScore> pbsList;

            if (team == 1) pbsList = pbsAwayList;
            else pbsList = pbsHomeList;

            foreach (PlayerBoxScore pbs in pbsList)
            {
                if (pbs.PlayerID == -1) continue;

                ps = pst[pbs.PlayerID];
                ps.ResetStats();
                ps.AddBoxScore(pbs);
                ps.CalcMetrics(ts, tsopp, new TeamStats("$$Empty"));
                pmsrList.Add(new PlayerMetricStatsRow(ps));
            }

            pmsrList.Sort(delegate(PlayerMetricStatsRow pmsr1, PlayerMetricStatsRow pmsr2)
                              { return pmsr1.GmSc.CompareTo(pmsr2.GmSc); });
            pmsrList.Reverse();

            if (team == 1)
            {
                pmsrListAway = new List<PlayerMetricStatsRow>(pmsrList);
                dgvMetricAway.ItemsSource = pmsrListAway;
            }
            else
            {
                pmsrListHome = new List<PlayerMetricStatsRow>(pmsrList);
                dgvMetricHome.ItemsSource = pmsrListHome;
            }
        }
    }
}