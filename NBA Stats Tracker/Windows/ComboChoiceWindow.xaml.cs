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

using System.Collections.Generic;
using System.Windows;
using NBA_Stats_Tracker.Data;
using NBA_Stats_Tracker.Data.Misc;

#endregion

namespace NBA_Stats_Tracker.Windows
{
    /// <summary>
    /// Implements a multi-function combo-box choice window.
    /// </summary>
    public partial class ComboChoiceWindow
    {
        #region Mode enum

        /// <summary>
        /// Used to determine what choices the window should offer, and its functions
        /// </summary>
        public enum Mode
        {
            OneTeam,
            Versus,
            ImportCompatibility,
            Division
        }

        #endregion

        private readonly Mode mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboChoiceWindow" /> class.
        /// </summary>
        /// <param name="mode">The Mode enum instance which determines what choices should be offered.</param>
        /// <param name="index">The default choice.</param>
        public ComboChoiceWindow(Mode mode, int index = 0)
        {
            InitializeComponent();
            this.mode = mode;

            if (mode == Mode.ImportCompatibility)
            {
                cmbTeams1.Items.Add("Mode 0");
                cmbTeams1.Items.Add("Mode 1");
                cmbTeams1.Items.Add("Mode 2");
                cmbTeams1.Items.Add("Mode 3");
                cmbTeams1.Items.Add("Mode 4");
                cmbTeams1.Items.Add("Mode 5");
                cmbTeams1.Items.Add("Mode 6");
                cmbTeams2.Visibility = Visibility.Hidden;
            }
            else if (mode == Mode.Versus)
            {
                label1.Content = "Pick the two teams";
                cmbTeams2.Visibility = Visibility.Visible;
                foreach (var kvp in MainWindow.TeamOrder)
                {
                    cmbTeams1.Items.Add(kvp.Key);
                    cmbTeams2.Items.Add(kvp.Key);
                }
            }
            else if (mode == Mode.Division)
            {
                label1.Content = "Pick the new division for the team:";
                cmbTeams2.Visibility = Visibility.Hidden;
                foreach (Division div in MainWindow.Divisions)
                {
                    Conference conf = MainWindow.Conferences.Find(conference => conference.ID == div.ConferenceID);
                    cmbTeams1.Items.Add(string.Format("{0}: {1}", conf.Name, div.Name));
                }
            }
            cmbTeams1.SelectedIndex = index;
            cmbTeams2.SelectedIndex = index != 0 ? 0 : 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboChoiceWindow" /> class.
        /// Used for when a player is set to active while previously inactive.
        /// </summary>
        /// <param name="teams">The available teams to sign the player to.</param>
        public ComboChoiceWindow(IEnumerable<string> teams)
        {
            InitializeComponent();

            mode = Mode.OneTeam;

            label1.Content = "Sign the player to which team?";
            cmbTeams1.ItemsSource = teams;
            cmbTeams2.Visibility = Visibility.Hidden;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (mode == Mode.ImportCompatibility)
            {
                App.mode = cmbTeams1.SelectedItem.ToString();
            }
            else if (mode == Mode.OneTeam)
            {
                if (cmbTeams1.SelectedIndex == -1)
                    return;
                PlayerOverviewWindow.askedTeam = cmbTeams1.SelectedItem.ToString();
            }
            else if (mode == Mode.Division)
            {
                MainWindow.input = cmbTeams1.SelectedItem.ToString();
            }
            Close();
        }
    }
}