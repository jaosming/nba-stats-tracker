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
using System.Windows;
using LeftosCommonLibrary;
using NBA_Stats_Tracker.Data;

#endregion

namespace NBA_Stats_Tracker.Windows
{
    /// <summary>
    /// Used for adding Teams and Players to the database.
    /// </summary>
    public partial class AddWindow
    {
        private readonly Dictionary<int, PlayerStats> pst;

        public AddWindow(ref Dictionary<int, PlayerStats> pst)
        {
            InitializeComponent();

            this.pst = pst;

            Teams = new ObservableCollection<string>();
            foreach (var kvp in MainWindow.TeamOrder)
            {
                Teams.Add(kvp.Key);
            }
            
            Players = new ObservableCollection<Player>();

            teamColumn.ItemsSource = Teams;
            dgvAddPlayers.ItemsSource = Players;

            dgvAddPlayers.RowEditEnding += GenericEventHandlers.WPFDataGrid_RowEditEnding_GoToNewRowOnTab;
            dgvAddPlayers.PreviewKeyDown += GenericEventHandlers.Any_PreviewKeyDown_CheckTab;
            dgvAddPlayers.PreviewKeyUp += GenericEventHandlers.Any_PreviewKeyUp_CheckTab;
        }

        private ObservableCollection<Player> Players { get; set; }
        private ObservableCollection<string> Teams { get; set; }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            var newpst = new Dictionary<int, PlayerStats>(pst);

            if (tbcAdd.SelectedItem == tabTeams)
            {
                List<string> lines = Tools.SplitLinesToList(txtTeams.Text, false);
                MainWindow.addInfo = "";
                foreach (string line in lines)
                {
                    MainWindow.addInfo += line + "\n";
                }
            }
            else if (tbcAdd.SelectedItem == tabPlayers)
            {
                int i = SQLiteIO.GetMaxPlayerID(MainWindow.currentDB);
                foreach (Player p in Players)
                {
                    if (String.IsNullOrWhiteSpace(p.LastName) || String.IsNullOrWhiteSpace(p.Team))
                    {
                        MessageBox.Show("You have to enter the Last Name, Position and Team for all players");
                        return;
                    }
                    p.ID = ++i;
                    newpst.Add(p.ID, new PlayerStats(p));
                }
                MainWindow.pst = newpst;
                MainWindow.addInfo = "$$NST Players Added";
            }

            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.addInfo = "";
            Close();
        }
    }
}