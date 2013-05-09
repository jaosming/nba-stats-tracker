﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NBA_Stats_Tracker.Windows.MainInterface.BoxScores
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Threading;

    using LeftosCommonLibrary;
    using LeftosCommonLibrary.CommonDialogs;

    using NBA_Stats_Tracker.Data.BoxScores;
    using NBA_Stats_Tracker.Data.Players;
    using NBA_Stats_Tracker.Data.SQLiteIO;
    using NBA_Stats_Tracker.Data.Teams;
    using NBA_Stats_Tracker.Helper.ListExtensions;
    using NBA_Stats_Tracker.Helper.Miscellaneous;

    /// <summary>
    /// Interaction logic for PlayByPlayWindow.xaml
    /// </summary>
    public partial class PlayByPlayWindow : Window
    {
        private Dictionary<int, TeamStats> _tst;
        private Dictionary<int, PlayerStats> _pst;
        private BoxScoreEntry _bse;
        private int _t1ID;
        private int _t2ID;
        private double _timeLeft;
        private DispatcherTimer _timeLeftTimer, _shotClockTimer;
        private double _shotClock;
        private ObservableCollection<PlayerStats> AwaySubs { get; set; }
        private ObservableCollection<PlayerStats> HomeSubs { get; set; }
        private ObservableCollection<PlayerStats> AwayActive { get; set; }
        private ObservableCollection<PlayerStats> HomeActive { get; set; }
        private ObservableCollection<ComboBoxItemWithIsEnabled> PlayersComboList { get; set; }

        public PlayByPlayWindow()
        {
            InitializeComponent();
        }

        public PlayByPlayWindow(
            Dictionary<int, TeamStats> tst, Dictionary<int, PlayerStats> pst, BoxScoreEntry bse, int t1ID, int t2ID)
            : this()
        {
            _tst = tst;
            _pst = pst;
            _bse = bse;
            _t1ID = t1ID;
            _t2ID = t2ID;
        }

        private void window_Initialized(object sender, EventArgs e)
        {
            Height = Tools.GetRegistrySetting("PBPHeight", MinHeight);
            Width = Tools.GetRegistrySetting("PBPWidth", MinWidth);
            Left = Tools.GetRegistrySetting("PBPX", Left);
            Top = Tools.GetRegistrySetting("PBPY", Top);

            txbAwayTeam.Text = _tst[_t1ID].DisplayName;
            txbHomeTeam.Text = _tst[_t2ID].DisplayName;
            txtAwayScore.Text = _bse.BS.PTS1.ToString();
            txtHomeScore.Text = _bse.BS.PTS2.ToString();

            txtPeriod.Text = "1";

            resetTimeLeft();

            _timeLeftTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 50) };
            _timeLeftTimer.Tick += _timeLeftTimer_Tick;

            resetShotClock();

            _shotClockTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 50) };
            _shotClockTimer.Tick += _shotClockTimer_Tick;

            var awayPlayersIDs = _bse.PBSList.Where(pbs => pbs.TeamID == _t1ID).Select(pbs => pbs.PlayerID).ToList();
            AwaySubs = new ObservableCollection<PlayerStats>();
            awayPlayersIDs.ForEach(id => AwaySubs.Add(_pst[id]));
            AwaySubs.Sort((ps1, ps2) => ps1.FullName.CompareTo(ps2.FullName));
            lstAwaySubs.ItemsSource = AwaySubs;

            AwayActive = new ObservableCollection<PlayerStats>();
            lstAwayActive.ItemsSource = AwayActive;

            var homePlayersIDs = _bse.PBSList.Where(pbs => pbs.TeamID == _t2ID).Select(pbs => pbs.PlayerID).ToList();
            HomeSubs = new ObservableCollection<PlayerStats>();
            homePlayersIDs.ForEach(id => HomeSubs.Add(_pst[id]));
            HomeSubs.Sort((ps1, ps2) => ps1.FullName.CompareTo(ps2.FullName));
            lstHomeSubs.ItemsSource = HomeSubs;

            HomeActive = new ObservableCollection<PlayerStats>();
            lstHomeActive.ItemsSource = HomeActive;

            cmbEventType.ItemsSource = PlayByPlayEntry.EventTypes.Values;
            cmbEventType.SelectedIndex = 2;

            cmbShotOrigin.ItemsSource = ShotEntry.ShotOrigins.Values;
            cmbShotType.ItemsSource = ShotEntry.ShotTypes.Values;

            PlayersComboList = new ObservableCollection<ComboBoxItemWithIsEnabled>();
            cmbPlayer1.ItemsSource = PlayersComboList;
            cmbPlayer2.ItemsSource = PlayersComboList;
        }

        private void resetShotClock()
        {
            _shotClock = MainWindow.ShotClockDuration;
            updateShotClockIndication(_shotClock);
        }

        private void _shotClockTimer_Tick(object sender, EventArgs e)
        {
            _shotClock -= 0.05;
            if (_shotClock < 0.01)
            {
                _shotClockTimer.Stop();
                _shotClock = 0;
            }
            updateShotClockIndication(_shotClock);
        }

        private void updateShotClockIndication(double shotClock)
        {
            var intPart = Convert.ToInt32(Math.Floor(shotClock));
            var decPart = shotClock - intPart;

            var dispDecPart = Convert.ToInt32(decPart * 10);
            if (dispDecPart == 10)
            {
                dispDecPart = 0;
            }

            txbShotClockLeftInt.Text = String.Format("{0:0}", intPart);
            txbShotClockLeftDec.Text = String.Format(".{0:0}", dispDecPart);
        }

        private void _timeLeftTimer_Tick(object sender, EventArgs e)
        {
            _timeLeft -= 0.05;
            if (_timeLeft < 0.01)
            {
                _timeLeftTimer.Stop();
                _timeLeft = 0;
            }
            updateTimeLeftIndication(_timeLeft);
        }

        private void updateTimeLeftIndication(double timeLeft)
        {
            var intPart = Convert.ToInt32(Math.Floor(timeLeft));
            var decPart = timeLeft - intPart;

            var minutes = intPart / 60;
            var seconds = intPart % 60;

            var dispDecPart = Convert.ToInt32(decPart * 10);
            if (dispDecPart == 10)
            {
                dispDecPart = 0;
            }

            txbTimeLeftInt.Text = String.Format("{0:00}:{1:00}", minutes, seconds);
            txbTimeLeftDec.Text = String.Format(".{0:0}", dispDecPart);
        }

        private double convertTimeStringToDouble(string s)
        {
            var parts = s.Split('.');
            double decPart = 0;
            if (parts.Length == 2)
            {
                decPart = Convert.ToDouble("0." + parts[1]);
            }
            var intParts = parts[0].Split(':');
            double intPart = 0;
            intPart += Convert.ToDouble(intParts[intParts.Length - 1]);
            var factor = 1;
            for (int i = intParts.Length - 2; i >= 0; i--)
            {
                factor *= 60;
                intPart += Convert.ToDouble(intParts[i]) * factor;
            }
            return intPart + decPart;
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            Tools.SetRegistrySetting("PBPHeight", Height);
            Tools.SetRegistrySetting("PBPWidth", Width);
            Tools.SetRegistrySetting("PBPX", Left);
            Tools.SetRegistrySetting("PBPY", Top);
        }

        private void btnTimeLeftStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (_timeLeftTimer.IsEnabled)
            {
                _timeLeftTimer.Stop();
                _shotClockTimer.Stop();
            }
            else
            {
                _timeLeftTimer.Start();
            }
        }

        private void btnShotClockStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (_shotClockTimer.IsEnabled)
            {
                _shotClockTimer.Stop();
            }
            else
            {
                _timeLeftTimer.Start();
                _shotClockTimer.Start();
            }
        }

        private void btnTimeLeftReset_Click(object sender, RoutedEventArgs e)
        {
            resetTimeLeft();
        }

        private void resetTimeLeft()
        {
            _timeLeft = (MainWindow.GameLength / MainWindow.NumberOfPeriods) * 60;
            updateTimeLeftIndication(_timeLeft);
        }

        private void btnShotClockReset_Click(object sender, RoutedEventArgs e)
        {
            resetShotClock();
        }

        private void btnTimeLeftSet_Click(object sender, RoutedEventArgs e)
        {
            InputBoxWindow ibw = new InputBoxWindow(
                "Enter the time left:", SQLiteIO.GetSetting("LastTimeLeftSet", "0:00"), "NBA Stats Tracker");
            if (ibw.ShowDialog() == false)
            {
                return;
            }

            double timeLeft = _timeLeft;
            try
            {
                timeLeft = convertTimeStringToDouble(InputBoxWindow.UserInput);
            }
            catch
            {
                return;
            }

            _timeLeft = timeLeft;
            updateTimeLeftIndication(_timeLeft);
            SQLiteIO.SetSetting("LastTimeLeftSet", InputBoxWindow.UserInput);
        }

        private void btnShotClockSet_Click(object sender, RoutedEventArgs e)
        {
            InputBoxWindow ibw = new InputBoxWindow(
                "Enter the shot clock left:", SQLiteIO.GetSetting("LastShotClockSet", "0.0"), "NBA Stats Tracker");
            if (ibw.ShowDialog() == false)
            {
                return;
            }

            double shotClock = _shotClock;
            try
            {
                shotClock = convertTimeStringToDouble(InputBoxWindow.UserInput);
            }
            catch
            {
                return;
            }

            _shotClock = shotClock;
            updateShotClockIndication(_shotClock);
            SQLiteIO.SetSetting("LastShotClockSet", InputBoxWindow.UserInput);
        }

        private void btnAwayDoSubs_Click(object sender, RoutedEventArgs e)
        {
            var inCount = lstAwaySubs.SelectedItems.Count;
            var outCount = lstAwayActive.SelectedItems.Count;
            var activeCount = lstAwayActive.Items.Count;
            var diff = inCount - outCount;

            if (activeCount + diff != 5)
            {
                return;
            }

            var playersIn = lstAwaySubs.SelectedItems.Cast<PlayerStats>().ToList();
            var playersOut = lstAwayActive.SelectedItems.Cast<PlayerStats>().ToList();
            foreach (var player in playersIn)
            {
                AwaySubs.Remove(player);
                AwayActive.Add(player);
            }
            foreach (var player in playersOut)
            {
                AwaySubs.Add(player);
                AwayActive.Remove(player);
            }
            sortPlayerLists();
        }

        private void sortPlayerLists()
        {
            AwaySubs.Sort((ps1, ps2) => ps1.FullName.CompareTo(ps2.FullName));
            AwayActive.Sort((ps1, ps2) => ps1.FullName.CompareTo(ps2.FullName));
            HomeSubs.Sort((ps1, ps2) => ps1.FullName.CompareTo(ps2.FullName));
            HomeActive.Sort((ps1, ps2) => ps1.FullName.CompareTo(ps2.FullName));

            PlayersComboList.Clear();
            PlayersComboList.Add(new ComboBoxItemWithIsEnabled(txbAwayTeam.Text, false));
            AwayActive.ToList().ForEach(ps => PlayersComboList.Add(new ComboBoxItemWithIsEnabled(ps.ToString(), true, ps.ID)));
            PlayersComboList.Add(new ComboBoxItemWithIsEnabled(txbHomeTeam.Text, false));
            HomeActive.ToList().ForEach(ps => PlayersComboList.Add(new ComboBoxItemWithIsEnabled(ps.ToString(), true, ps.ID)));
        }

        private void btnHomeDoSubs_Click(object sender, RoutedEventArgs e)
        {
            var inCount = lstHomeSubs.SelectedItems.Count;
            var outCount = lstHomeActive.SelectedItems.Count;
            var activeCount = lstHomeActive.Items.Count;
            var diff = inCount - outCount;

            if (activeCount + diff != 5)
            {
                return;
            }

            var playersIn = lstHomeSubs.SelectedItems.Cast<PlayerStats>().ToList();
            var playersOut = lstHomeActive.SelectedItems.Cast<PlayerStats>().ToList();
            foreach (var player in playersIn)
            {
                HomeSubs.Remove(player);
                HomeActive.Add(player);
            }
            foreach (var player in playersOut)
            {
                HomeSubs.Add(player);
                HomeActive.Remove(player);
            }
            sortPlayerLists();
        }

        private void cmbEventType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEventType.SelectedIndex == -1)
            {
                return;
            }

            var curEventKey = PlayByPlayEntry.EventTypes.Single(pair => pair.Value == cmbEventType.SelectedItem.ToString()).Key;

            txtEventDesc.IsEnabled = cmbEventType.SelectedItem.ToString() == "Other";

            stpShotEvent.IsEnabled = curEventKey == 1;
            txbLocationLabel.Text = curEventKey == 1 ? "Shot Distance" : "Location";
            txtLocationDesc.IsEnabled = false;
            cmbLocationShotDistance.ItemsSource = curEventKey == 1
                                                      ? ShotEntry.ShotDistances.Values
                                                      : PlayByPlayEntry.EventLocations.Values;

            try
            {
                var definition = PlayByPlayEntry.Player2Definition[curEventKey];
                txbPlayer2Label.Text = definition;
                cmbPlayer2.IsEnabled = true;
            }
            catch (KeyNotFoundException)
            {
                txbPlayer2Label.Text = "Not Applicable";
                cmbPlayer2.IsEnabled = false;
            }
        }

        private void cmbLocationShotDistance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var curEventKey = PlayByPlayEntry.EventTypes.Single(pair => pair.Value == cmbEventType.SelectedItem.ToString()).Key;
            if (curEventKey != 1)
            {
                var curDistanceKey = PlayByPlayEntry.EventLocations.Single(pair => pair.Value == cmbLocationShotDistance.SelectedItem.ToString()).Key;
                txtLocationDesc.IsEnabled = curDistanceKey == -1;
            }
            else
            {
                txtLocationDesc.IsEnabled = false;
            }
        }
    }
}