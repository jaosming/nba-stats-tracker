﻿#region Copyright Notice

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

#region Using Directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NBA_Stats_Tracker.Data.Teams;
using NBA_Stats_Tracker.Windows.MainInterface;

#endregion

namespace NBA_Stats_Tracker.Helper.Miscellaneous
{
    /// <summary>
    ///     Implements miscellaneous helper methods used all over NBA Stats Tracker.
    /// </summary>
    public static class Misc
    {
        public static int GetTeamIDFromDisplayName(Dictionary<int, TeamStats> teamStats, string displayName)
        {
            if (displayName == "- Inactive -")
                return -1;
            for (var i = 0; i < MainWindow.TST.Count; i++)
            {
                if (teamStats[i].DisplayName == displayName)
                {
                    if (teamStats[i].IsHidden)
                        throw new Exception("Requested team that is hidden: " + MainWindow.TST[i].Name);

                    return teamStats[i].ID;
                }
            }
            throw new Exception("Team not found: " + displayName);
        }

        /// <summary>
        ///     Loads an image into a BitmapImage object.
        /// </summary>
        /// <param name="path">The path to the image file.</param>
        public static BitmapImage LoadImage(string path)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            return bi;
        }

        public static void SetRegistrySetting<T>(string setting, T value)
        {
            var rk = Registry.CurrentUser;
            try
            {
                try
                {
                    rk = rk.OpenSubKey(App.AppRegistryKey, true);
                    if (rk == null)
                        throw new Exception();
                }
                catch (Exception)
                {
                    rk = Registry.CurrentUser;
                    rk.CreateSubKey(App.AppRegistryKey);
                    rk = rk.OpenSubKey(App.AppRegistryKey, true);
                    if (rk == null)
                        throw new Exception();
                }

                rk.SetValue(setting, value);
            }
            catch
            {
                MessageBox.Show("Couldn't save changed setting.");
            }
        }

        public static T GetRegistrySetting<T>(string setting, T defaultValue)
        {
            var rk = Registry.CurrentUser;
            var settingValue = defaultValue;
            try
            {
                if (rk == null)
                    throw new Exception();

                rk = rk.OpenSubKey(App.AppRegistryKey);
                if (rk != null)
                    settingValue = (T)Convert.ChangeType(rk.GetValue(setting, defaultValue), typeof(T));
            }
            catch
            {
                settingValue = defaultValue;
            }

            return settingValue;
        } 

        public static string GetRankingSuffix(int rank)
        {
            if (rank%10 == 1)
            {
                if (rank.ToString(CultureInfo.InvariantCulture).EndsWith("11"))
                {
                    return "th";
                }
                else
                {
                    return "st";
                }
            }
            else if (rank%10 == 2)
            {
                if (rank.ToString(CultureInfo.InvariantCulture).EndsWith("12"))
                {
                    return "th";
                }
                else
                {
                    return "nd";
                }
            }
            else if (rank%10 == 3)
            {
                if (rank.ToString(CultureInfo.InvariantCulture).EndsWith("13"))
                {
                    return "th";
                }
                else
                {
                    return "rd";
                }
            }
            else
            {
                return "th";
            }
        }

        public static string GetDisplayName(Dictionary<int, string> displayNames, int id)
        {
            if (id == -1)
                return "";
            else
            {
                try
                {
                    return displayNames[id];
                }
                catch (KeyNotFoundException)
                {
                    return "Unknown";
                }
            }
        }
    }
}