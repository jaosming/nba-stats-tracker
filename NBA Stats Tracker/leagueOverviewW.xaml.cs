﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace NBA_Stats_Tracker
{
    /// <summary>
    /// Interaction logic for leagueOverviewW.xaml
    /// </summary>
    public partial class leagueOverviewW : Window
    {
        private readonly DataTable dt_bs;
        private readonly DataTable dt_ts;
        private Dictionary<int, PlayerStats> pst;
        private TeamStats[] tst;
        private SQLiteDatabase db = new SQLiteDatabase(MainWindow.currentDB);
        private int curSeason = MainWindow.curSeason;
        private int maxSeason = MainWindow.getMaxSeason(MainWindow.currentDB);
        private string q;
        private DataTable res;
        private TeamStats ts;
        private TeamStats tsopp;

        public leagueOverviewW(TeamStats[] tst, Dictionary<int, PlayerStats> pst)
        {
            InitializeComponent();

            #region Prepare DataTables

            dt_ts = new DataTable();

            dt_ts.Columns.Add("Name");
            dt_ts.Columns.Add("Games", typeof (int));
            dt_ts.Columns.Add("Wins", typeof (int));
            dt_ts.Columns.Add("Losses", typeof (int));
            dt_ts.Columns.Add("W%", typeof (float));
            dt_ts.Columns.Add("Weff", typeof (float));
            dt_ts.Columns.Add("PF", typeof (float));
            dt_ts.Columns.Add("PA", typeof (float));
            dt_ts.Columns.Add("PD", typeof (float));
            dt_ts.Columns.Add("FG", typeof (float));
            dt_ts.Columns.Add("FGeff", typeof (float));
            dt_ts.Columns.Add("3PT", typeof (float));
            dt_ts.Columns.Add("3Peff", typeof (float));
            dt_ts.Columns.Add("FT", typeof (float));
            dt_ts.Columns.Add("FTeff", typeof (float));
            dt_ts.Columns.Add("REB", typeof (float));
            dt_ts.Columns.Add("OREB", typeof (float));
            dt_ts.Columns.Add("DREB", typeof (float));
            dt_ts.Columns.Add("AST", typeof (float));
            dt_ts.Columns.Add("TO", typeof (float));
            dt_ts.Columns.Add("STL", typeof (float));
            dt_ts.Columns.Add("BLK", typeof (float));
            dt_ts.Columns.Add("FOUL", typeof (float));


            dt_bs = new DataTable();

            dt_bs.Columns.Add("Date");
            dt_bs.Columns.Add("Away");
            dt_bs.Columns.Add("AS", typeof (int));
            dt_bs.Columns.Add("Home");
            dt_bs.Columns.Add("HS", typeof (int));
            dt_bs.Columns.Add("GameID");

            #endregion

            this.tst = tst;
            this.pst = pst;

            PopulateSeasonCombo();

            dtpEnd.SelectedDate = DateTime.Today;
            dtpStart.SelectedDate = DateTime.Today.AddMonths(-1);
        }

        private void dtpStart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dtpEnd.SelectedDate < dtpStart.SelectedDate)
            {
                dtpEnd.SelectedDate = dtpStart.SelectedDate.GetValueOrDefault().AddMonths(1);
            }
            tbcLeagueOverview_SelectionChanged(null, null);
        }

        private void dtpEnd_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dtpEnd.SelectedDate < dtpStart.SelectedDate)
            {
                dtpStart.SelectedDate = dtpEnd.SelectedDate.GetValueOrDefault().AddMonths(-1);
            }
            tbcLeagueOverview_SelectionChanged(null, null);
        }

        private void PopulateSeasonCombo()
        {
            for (int i = MainWindow.getMaxSeason(MainWindow.currentDB); i > 0; i--)
            {
                cmbSeasonNum.Items.Add(i.ToString());
            }

            cmbSeasonNum.SelectedItem = MainWindow.curSeason.ToString();
        }

        private void tbcLeagueOverview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tbcLeagueOverview.SelectedItem == tabTeamStats)
            {
                PrepareTeamStats();
            }
            else if (tbcLeagueOverview.SelectedItem == tabPlayoffStats)
            {
                PreparePlayoffStats();
            }
            else if (tbcLeagueOverview.SelectedItem == tabPlayerStats)
            {
                PreparePlayerStats();
            }
            else if (tbcLeagueOverview.SelectedItem == tabBoxScores)
            {
                PrepareBoxScores();
            }
        }

        private void PreparePlayerStats()
        {
            string playersT = "Players";
            if (curSeason != maxSeason) playersT += "S" + curSeason;
            List<PlayerStatsRow> psrList = new List<PlayerStatsRow>();

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                q = "select * from " + playersT;
                res = db.GetDataTable(q);

                foreach (DataRow r in res.Rows)
                {
                    PlayerStats ps = new PlayerStats(r);
                    PlayerStatsRow psr = new PlayerStatsRow(ps);

                    psrList.Add(psr);
                }
            }

            dgvPlayerStats.ItemsSource = psrList;
        }

        private void PrepareBoxScores()
        {
            DataTable res;
            string q;
            dt_bs.Clear();

            q = "select * from GameResults";

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                q += " where SeasonNum = " + cmbSeasonNum.SelectedItem;
            }
            else
            {
                q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                          dtpEnd.SelectedDate.GetValueOrDefault(), true);
            }

            res = db.GetDataTable(q);

            foreach (DataRow dr in res.Rows)
            {
                DataRow r = dt_bs.NewRow();

                r["Date"] = dr["Date"].ToString().Split(' ')[0];
                r["Away"] = dr["T1Name"].ToString();
                r["AS"] = Convert.ToInt32(dr["T1PTS"].ToString());
                r["Home"] = dr["T2Name"].ToString();
                r["HS"] = Convert.ToInt32(dr["T2PTS"].ToString());
                r["GameID"] = dr["GameID"].ToString();

                dt_bs.Rows.Add(r);
            }

            var dv_bs = new DataView(dt_bs);
            dv_bs.AllowNew = false;
            dv_bs.AllowEdit = false;
            dv_bs.Sort = "Date DESC";

            dgvBoxScores.DataContext = dv_bs;
        }

        private void PreparePlayoffStats()
        {
            dt_ts.Clear();

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                tst = MainWindow.LoadDatabase(MainWindow.currentDB, ref pst, ref MainWindow.TeamOrder,
                                              ref MainWindow.pt, ref MainWindow.bshist,
                                              _curSeason: Convert.ToInt32(cmbSeasonNum.SelectedItem.ToString()));

                foreach (TeamStats cur in tst)
                {
                    if (cur.getPlayoffGames() == 0) continue;

                    DataRow r = dt_ts.NewRow();

                    teamOverviewW.CreateDataRowFromTeamStats(cur, ref r, cur.name, true);

                    dt_ts.Rows.Add(r);
                }
            }
            else
            {
                foreach (var kvp in MainWindow.TeamOrder)
                {
                    q =
                        String.Format(
                            "select * from GameResults where ((T1Name LIKE '{0}' OR T2Name LIKE '{0}') AND IsPlayoff LIKE 'True');",
                            kvp.Key);
                    q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                              dtpEnd.SelectedDate.GetValueOrDefault());

                    res = db.GetDataTable(q);

                    DataRow r = dt_ts.NewRow();

                    ts = new TeamStats(kvp.Key);
                    tsopp = new TeamStats();
                    teamOverviewW.AddToTeamStatsFromSQLBoxScore(res, ref ts, ref tsopp, true);
                    teamOverviewW.CreateDataRowFromTeamStats(ts, ref r, kvp.Key, true);

                    dt_ts.Rows.Add(r);
                }
            }

            // DataTable's ready, set DataView and fill DataGrid
            var dv_ts = new DataView(dt_ts);
            dv_ts.AllowNew = false;
            dv_ts.AllowEdit = false;
            dv_ts.Sort = "Weff DESC";

            dgvPlayoffStats.DataContext = dv_ts;
        }

        private void PrepareTeamStats()
        {
            dt_ts.Clear();

            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
            {
                tst = MainWindow.LoadDatabase(MainWindow.currentDB, ref pst, ref MainWindow.TeamOrder,
                                              ref MainWindow.pt, ref MainWindow.bshist,
                                              _curSeason: Convert.ToInt32(cmbSeasonNum.SelectedItem.ToString()));

                foreach (TeamStats cur in tst)
                {
                    DataRow r = dt_ts.NewRow();

                    teamOverviewW.CreateDataRowFromTeamStats(cur, ref r, cur.name);

                    dt_ts.Rows.Add(r);
                }
            }
            else
            {
                foreach (var kvp in MainWindow.TeamOrder)
                {
                    q =
                        String.Format(
                            "select * from GameResults where ((T1Name LIKE '{0}' OR T2Name LIKE '{0}') AND IsPlayoff LIKE 'False');",
                            kvp.Key);
                    q = SQLiteDatabase.AddDateRangeToSQLQuery(q, dtpStart.SelectedDate.GetValueOrDefault(),
                                                              dtpEnd.SelectedDate.GetValueOrDefault());

                    res = db.GetDataTable(q);

                    DataRow r = dt_ts.NewRow();

                    ts = new TeamStats(kvp.Key);
                    tsopp = new TeamStats();
                    teamOverviewW.AddToTeamStatsFromSQLBoxScore(res, ref ts, ref tsopp);
                    teamOverviewW.CreateDataRowFromTeamStats(ts, ref r, kvp.Key);

                    dt_ts.Rows.Add(r);
                }
            }

            // DataTable's ready, set DataView and fill DataGrid
            var dv_ts = new DataView(dt_ts);
            dv_ts.AllowNew = false;
            dv_ts.AllowEdit = false;
            dv_ts.Sort = "Weff DESC";

            dgvTeamStats.DataContext = dv_ts;
        }

        private void rbStatsAllTime_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                dtpEnd.IsEnabled = false;
                dtpStart.IsEnabled = false;
                cmbSeasonNum.IsEnabled = true;
            }
            catch
            {
            }
            tbcLeagueOverview_SelectionChanged(null, null);
        }

        private void rbStatsBetween_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                dtpEnd.IsEnabled = true;
                dtpStart.IsEnabled = true;
                cmbSeasonNum.IsEnabled = false;
            }
            catch
            {
            }
            tbcLeagueOverview_SelectionChanged(null, null);
        }

        private void dg_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void cmbSeasonNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            curSeason = Convert.ToInt32(cmbSeasonNum.SelectedItem);
            if (rbStatsAllTime.IsChecked.GetValueOrDefault())
                tbcLeagueOverview_SelectionChanged(null, null);
        }

        private void dgvBoxScores_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgvBoxScores.SelectedCells.Count > 0)
            {
                var row = (DataRowView)dgvBoxScores.SelectedItems[0];
                int gameid = Convert.ToInt32(row["GameID"].ToString());

                int i = 0;

                foreach (BoxScoreEntry bse in MainWindow.bshist)
                {
                    if (bse.bs.id == gameid)
                    {
                        MainWindow.bs = new BoxScore();

                        var bsw = new boxScoreW(boxScoreW.Mode.View, i);
                        bsw.ShowDialog();

                        MainWindow.UpdateBoxScore();
                        break;
                    }
                    i++;
                }
            }
        }

        private void dgvTeamStats_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgvTeamStats.SelectedCells.Count > 0)
            {
                var row = (DataRowView) dgvTeamStats.SelectedItems[0];
                string team = row["Name"].ToString();

                teamOverviewW tow = new teamOverviewW(MainWindow.tst, MainWindow.pst, team);
                tow.ShowDialog();
            }
        }

        private void dgvPlayoffStats_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgvPlayoffStats.SelectedCells.Count > 0)
            {
                var row = (DataRowView)dgvPlayoffStats.SelectedItems[0];
                string team = row["Name"].ToString();

                teamOverviewW tow = new teamOverviewW(MainWindow.tst, MainWindow.pst, team);
                tow.ShowDialog();
            }
        }

        private void tabPlayerStats_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgvPlayerStats.SelectedCells.Count > 0)
            {
                PlayerStatsRow psr = (PlayerStatsRow) dgvPlayerStats.SelectedItems[0];

                playerOverviewW pow = new playerOverviewW(psr.TeamF, psr.ID);
                pow.ShowDialog();
            }
        }
    }
}