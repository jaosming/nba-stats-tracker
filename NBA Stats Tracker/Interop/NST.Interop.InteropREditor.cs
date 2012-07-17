﻿#region Copyright Notice

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LeftosCommonLibrary;
using NBA_Stats_Tracker.Data;
using NBA_Stats_Tracker.Windows;
using MessageBox = System.Windows.MessageBox;

namespace NBA_Stats_Tracker.Interop
{
    public static class InteropREditor
    {
        private static readonly Dictionary<string, string> Positions = new Dictionary<string, string>
                                                                           {
                                                                               {"0", "PG"},
                                                                               {"1", "SG"},
                                                                               {"2", "SF"},
                                                                               {"3", "PF"},
                                                                               {"4", "C"},
                                                                               {"5", " "}
                                                                           };

        public static void CreateSettingsFile(List<Dictionary<string,string>> activeTeams, string folder)
        {
            string s1 = "Folder$$" + folder + "\n";
            string s2 = activeTeams.Aggregate("Active$$", (current, team) => current + (team["Name"] + "$%"));
            s2 = s2.Substring(0, s2.Length - 2);
            s2 += "\n";

            string stg = s1 + s2;

            SaveFileDialog sfd = new SaveFileDialog
                                     {
                                         Title = "Save Active Teams List",
                                         Filter = "Active Teams List (*.red)|*.red",
                                         DefaultExt = "red",
                                         InitialDirectory = App.AppDocsPath
                                     };
            sfd.ShowDialog();

            if (String.IsNullOrWhiteSpace(sfd.FileName)) return;

            StreamWriter sw = new StreamWriter(sfd.FileName);
            sw.Write(stg);
            sw.Close();
        }

        public static int ImportAll(ref Dictionary<int, TeamStats> tst, ref Dictionary<int, TeamStats> tstopp,
                                    ref SortedDictionary<string, int> TeamOrder, ref Dictionary<int, PlayerStats> pst,
                                    string folder, bool teamsOnly = false)
        {
            List<Dictionary<string, string>> teams;
            List<Dictionary<string, string>> players;
            List<Dictionary<string, string>> teamStats;
            List<Dictionary<string, string>> playerStats;
            try
            {
                teams = CSV.CreateDictionaryListFromCSV(folder + @"\Teams.csv");
                players = CSV.CreateDictionaryListFromCSV(folder + @"\Players.csv");
                teamStats = CSV.CreateDictionaryListFromCSV(folder + @"\Team_Stats.csv");
                playerStats = CSV.CreateDictionaryListFromCSV(folder + @"\Player_Stats.csv");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                return -1;
            }

            #region Import Teams & Team Stats

            var legalTTypes = new List<string> {"0", "4"};

            List<Dictionary<string, string>> validTeams = teams.FindAll(delegate(Dictionary<string, string> team)
            {
                if (legalTTypes.IndexOf(team["TType"]) != -1) return true;
                return false;
            });

            List<Dictionary<string, string>> activeTeams = validTeams.FindAll(delegate(Dictionary<string, string> team)
                                                                             {
                                                                                 if (team["StatCurS"] != "-1") return true;
                                                                                 return false;
                                                                             });

            if (activeTeams.Count == 0)
            {
                MessageBox.Show("No Team Stats found in save.");
                return -1;
            }

            if (activeTeams.Count < 30)
            {
                DualListWindow dlw = new DualListWindow(validTeams, activeTeams);
                if (dlw.ShowDialog() == false)
                {
                    return -1;
                }

                activeTeams = new List<Dictionary<string, string>>(MainWindow.selectedTeams);

                if (MainWindow.selectedTeamsChanged)
                {
                    CreateSettingsFile(activeTeams, folder);
                }
            }

            bool madeNew = false;

            if (tst.Count != activeTeams.Count)
            {
                tst = new Dictionary<int, TeamStats>();
                tstopp = new Dictionary<int, TeamStats>();
                madeNew = true;
            }
            var activeTeamsIDs = new List<int>();
            var rosters = new Dictionary<int, List<int>>();
            foreach (var team in activeTeams)
            {
                int id = -1;
                string name = team["Name"];
                if (!TeamOrder.ContainsKey(name))
                {
                    for (int i = 0; i < 30; i++)
                    {
                        if (!TeamOrder.ContainsValue(i))
                        {
                            id = i;
                            break;
                        }
                    }
                    TeamOrder.Add(name, id);
                }
                id = TeamOrder[name];
                activeTeamsIDs.Add(Convert.ToInt32(team["ID"]));

                if (madeNew)
                {
                    tst[id] = new TeamStats(name);
                    tstopp[id] = new TeamStats(name);
                }

                int sStatsID = Convert.ToInt32(team["StatCurS"]);
                int pStatsID = Convert.ToInt32(team["StatCurP"]);

                Dictionary<string, string> sStats = teamStats.Find(delegate(Dictionary<string, string> s)
                                                                       {
                                                                           if (s["ID"] == sStatsID.ToString())
                                                                               return true;
                                                                           return false;
                                                                       });

                tst[id].ID = Convert.ToInt32(team["ID"]);

                if (sStats != null)
                {
                    tst[id].winloss[0] = Convert.ToByte(sStats["Wins"]);
                    tst[id].winloss[1] = Convert.ToByte(sStats["Losses"]);
                    tst[id].stats[t.MINS] = Convert.ToUInt16(sStats["Mins"]);
                    tst[id].stats[t.PF] = Convert.ToUInt16(sStats["PtsFor"]);
                    tst[id].stats[t.PA] = Convert.ToUInt16(sStats["PtsAg"]);
                    tst[id].stats[t.FGM] = Convert.ToUInt16(sStats["FGMade"]);
                    tst[id].stats[t.FGA] = Convert.ToUInt16(sStats["FGAtt"]);
                    tst[id].stats[t.TPM] = Convert.ToUInt16(sStats["3PTMade"]);
                    tst[id].stats[t.TPA] = Convert.ToUInt16(sStats["3PTAtt"]);
                    tst[id].stats[t.FTM] = Convert.ToUInt16(sStats["FTMade"]);
                    tst[id].stats[t.FTA] = Convert.ToUInt16(sStats["FTAtt"]);
                    tst[id].stats[t.DREB] = Convert.ToUInt16(sStats["DRebs"]);
                    tst[id].stats[t.OREB] = Convert.ToUInt16(sStats["ORebs"]);
                    tst[id].stats[t.STL] = Convert.ToUInt16(sStats["Steals"]);
                    tst[id].stats[t.BLK] = Convert.ToUInt16(sStats["Blocks"]);
                    tst[id].stats[t.AST] = Convert.ToUInt16(sStats["Assists"]);
                    tst[id].stats[t.FOUL] = Convert.ToUInt16(sStats["Fouls"]);
                    tst[id].stats[t.TO] = Convert.ToUInt16(sStats["TOs"]);
                    //tstopp[id].stats[t.TO] = Convert.ToUInt16(sStats["TOsAg"]);

                    if (pStatsID != -1)
                    {
                        Dictionary<string, string> pStats = teamStats.Find(delegate(Dictionary<string, string> s)
                        {
                            if (s["ID"] == pStatsID.ToString())
                                return true;
                            return false;
                        });
                        tst[id].pl_winloss[0] = Convert.ToByte(pStats["Wins"]);
                        tst[id].pl_winloss[1] = Convert.ToByte(pStats["Losses"]);
                        tst[id].pl_stats[t.MINS] = Convert.ToUInt16(pStats["Mins"]);
                        tst[id].pl_stats[t.PF] = Convert.ToUInt16(pStats["PtsFor"]);
                        tst[id].pl_stats[t.PA] = Convert.ToUInt16(pStats["PtsAg"]);
                        tst[id].pl_stats[t.FGM] = Convert.ToUInt16(pStats["FGMade"]);
                        tst[id].pl_stats[t.FGA] = Convert.ToUInt16(pStats["FGAtt"]);
                        tst[id].pl_stats[t.TPM] = Convert.ToUInt16(pStats["3PTMade"]);
                        tst[id].pl_stats[t.TPA] = Convert.ToUInt16(pStats["3PTAtt"]);
                        tst[id].pl_stats[t.FTM] = Convert.ToUInt16(pStats["FTMade"]);
                        tst[id].pl_stats[t.FTA] = Convert.ToUInt16(pStats["FTAtt"]);
                        tst[id].pl_stats[t.DREB] = Convert.ToUInt16(pStats["DRebs"]);
                        tst[id].pl_stats[t.OREB] = Convert.ToUInt16(pStats["ORebs"]);
                        tst[id].pl_stats[t.STL] = Convert.ToUInt16(pStats["Steals"]);
                        tst[id].pl_stats[t.BLK] = Convert.ToUInt16(pStats["Blocks"]);
                        tst[id].pl_stats[t.AST] = Convert.ToUInt16(pStats["Assists"]);
                        tst[id].pl_stats[t.FOUL] = Convert.ToUInt16(pStats["Fouls"]);
                        tst[id].pl_stats[t.TO] = Convert.ToUInt16(pStats["TOs"]);
                        //tstopp[id].pl_stats[t.TO] = Convert.ToUInt16(pStats["TOsAg"]);
                    }
                }
                
                tst[id].calcAvg();

                rosters[id] = new List<int>
                                  {
                                      Convert.ToInt32(team["Ros_PG"]),
                                      Convert.ToInt32(team["Ros_SG"]),
                                      Convert.ToInt32(team["Ros_SF"]),
                                      Convert.ToInt32(team["Ros_PG"]),
                                      Convert.ToInt32(team["Ros_PG"])
                                  };
                for (int i = 6; i <= 12; i++)
                {
                    int cur = Convert.ToInt32(team["Ros_S" + i.ToString()]);
                    if (cur != -1) rosters[id].Add(cur);
                    else break;
                }
                for (int i = 13; i <= 20; i++)
                {
                    int cur = Convert.ToInt32(team["Ros_R" + i.ToString()]);
                    if (cur != -1) rosters[id].Add(cur);
                    else break;
                }
            }

            #endregion

            #region Import Players & Player Stats

            if (!teamsOnly)
            {
                List<Dictionary<string, string>> activePlayers =
                    players.FindAll(delegate(Dictionary<string, string> player)
                                        {
                                            if (player["PlType"] == "4" || player["PlType"] == "6")
                                            {
                                                if ((player["IsFA"] == "0" && player["TeamID1"] != "-1") ||
                                                    (player["IsFA"] == "1"))
                                                {
                                                    return true;
                                                }
                                            }
                                            return false;
                                        });

                foreach (var player in activePlayers)
                {
                    /*
                    for (int i = 16; i >= 0; i--)
                    {
                        string cur = player["StatY" + i.ToString()];
                        if (cur != "-1") playerStatsID = Convert.ToInt32(cur);
                    }
                    */
                    int playerID = Convert.ToInt32(player["ID"]);

                    int pTeam = Convert.ToInt32(player["TeamID1"]);
                    if (!activeTeamsIDs.Contains(pTeam) && player["IsFA"] != "1") continue;

                    int playerStatsID = Convert.ToInt32(player["StatY0"]);

                    //TODO: Handle this a bit more gracefully
                    //if (playerStatsID == -1) continue;

                    Dictionary<string, string> plStats = playerStats.Find(delegate(Dictionary<string, string> s)
                                                                              {
                                                                                  if (s["ID"] ==
                                                                                      playerStatsID.ToString())
                                                                                      return true;
                                                                                  return false;
                                                                              });


                    if (!pst.ContainsKey(playerID))
                    {
                        pst.Add(playerID, new PlayerStats(new Player
                                                              {
                                                                  ID = Convert.ToInt32(player["ID"]),
                                                                  FirstName = player["First_Name"],
                                                                  LastName = player["Last_Name"],
                                                                  Position = Positions[player["Pos"]],
                                                                  Position2 = Positions[player["SecondPos"]]
                                                              }));
                    }

                    if (plStats != null)
                    {
                        string TeamFName = "";
                        string team1 = plStats["TeamID1"];
                        if (team1 != "-1" && player["IsFA"] != "1")
                        {
                            Dictionary<string, string> TeamF = teams.Find(delegate(Dictionary<string, string> s)
                                                                              {
                                                                                  if (s["ID"] == team1) return true;
                                                                                  return false;
                                                                              });
                            TeamFName = TeamF["Name"];
                        }

                        string TeamSName = "";
                        string team2 = plStats["TeamID2"];
                        if (team2 != "-1")
                        {
                            Dictionary<string, string> TeamS = teams.Find(delegate(Dictionary<string, string> s)
                                                                              {
                                                                                  if (s["ID"] == team2) return true;
                                                                                  return false;
                                                                              });
                            TeamSName = TeamS["Name"];
                        }

                        PlayerStats ps = pst[playerID];
                        ps.TeamF = TeamFName;
                        ps.TeamS = TeamSName;

                        ps.isActive = player["IsFA"] != "1";

                        ps.stats[p.GP] = Convert.ToUInt16(plStats["GamesP"]);
                        ps.stats[p.GS] = Convert.ToUInt16(plStats["GamesS"]);
                        ps.stats[p.MINS] = Convert.ToUInt16(plStats["Minutes"]);
                        ps.stats[p.PTS] = Convert.ToUInt16(plStats["Points"]);
                        ps.stats[p.DREB] = Convert.ToUInt16(plStats["DRebs"]);
                        ps.stats[p.OREB] = Convert.ToUInt16(plStats["ORebs"]);
                        ps.stats[p.AST] = Convert.ToUInt16(plStats["Assists"]);
                        ps.stats[p.STL] = Convert.ToUInt16(plStats["Steals"]);
                        ps.stats[p.BLK] = Convert.ToUInt16(plStats["Blocks"]);
                        ps.stats[p.TO] = Convert.ToUInt16(plStats["TOs"]);
                        ps.stats[p.FOUL] = Convert.ToUInt16(plStats["Fouls"]);
                        ps.stats[p.FGM] = Convert.ToUInt16(plStats["FGMade"]);
                        ps.stats[p.FGA] = Convert.ToUInt16(plStats["FGAtt"]);
                        ps.stats[p.TPM] = Convert.ToUInt16(plStats["3PTMade"]);
                        ps.stats[p.TPA] = Convert.ToUInt16(plStats["3PTAtt"]);
                        ps.stats[p.FTM] = Convert.ToUInt16(plStats["FTMade"]);
                        ps.stats[p.FTA] = Convert.ToUInt16(plStats["FTAtt"]);

                        ps.isAllStar = Convert.ToBoolean(Convert.ToInt32(plStats["IsAStar"]));
                        ps.isNBAChampion = Convert.ToBoolean(Convert.ToInt32(plStats["IsChamp"]));

                        ps.isInjured = player["InjType"] != "0";

                        ps.CalcAvg();

                        pst[playerID] = ps;
                    }
                    else
                    {
                        PlayerStats ps = pst[playerID];

                        string TeamFName = "";
                        string team1 = player["TeamID1"];
                        if (team1 != "-1" && player["IsFA"] != "1")
                        {
                            Dictionary<string, string> TeamF = teams.Find(delegate(Dictionary<string, string> s)
                                                                              {
                                                                                  if (s["ID"] == team1) return true;
                                                                                  return false;
                                                                              });
                            TeamFName = TeamF["Name"];
                        }
                        ps.TeamF = TeamFName;

                        ps.isActive = player["IsFA"] != "1";
                        ps.isInjured = player["InjType"] != "0";

                        ps.CalcAvg();

                        pst[playerID] = ps;
                    }
                }
            }

            #endregion

            return 0;
        }

        public static int ExportAll(Dictionary<int, TeamStats> tst, Dictionary<int, TeamStats> tstopp,
                                    Dictionary<int, PlayerStats> pst, string folder, bool teamsOnly = false)
        {
            List<Dictionary<string, string>> teams;
            List<Dictionary<string, string>> players;
            List<Dictionary<string, string>> teamStats;
            List<Dictionary<string, string>> playerStats;
            try
            {
                teams = CSV.CreateDictionaryListFromCSV(folder + @"\Teams.csv");
                players = CSV.CreateDictionaryListFromCSV(folder + @"\Players.csv");
                teamStats = CSV.CreateDictionaryListFromCSV(folder + @"\Team_Stats.csv");
                playerStats = CSV.CreateDictionaryListFromCSV(folder + @"\Player_Stats.csv");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                return -1;
            }

            foreach (int key in tst.Keys)
            {
                TeamStats ts = tst[key];
                TeamStats tsopp = tstopp[key];

                int id = ts.ID;

                int tindex = teams.FindIndex(delegate(Dictionary<string, string> s)
                                                 {
                                                     if (s["ID"] == id.ToString()) return true;
                                                     return false;
                                                 });

                Dictionary<string, string> team = teams[tindex];

                int sStatsID = Convert.ToInt32(team["StatCurS"]);
                int pStatsID = Convert.ToInt32(team["StatCurP"]);

                int sStatsIndex = teamStats.FindIndex(delegate(Dictionary<string, string> s)
                                                          {
                                                              if (s["ID"] == sStatsID.ToString()) return true;
                                                              return false;
                                                          });

                if (sStatsIndex != -1)
                {
                    teamStats[sStatsIndex]["Wins"] = ts.winloss[0].ToString();
                    teamStats[sStatsIndex]["Losses"] = ts.winloss[1].ToString();
                    teamStats[sStatsIndex]["Mins"] = ts.stats[t.MINS].ToString();
                    teamStats[sStatsIndex]["PtsFor"] = ts.stats[t.PF].ToString();
                    teamStats[sStatsIndex]["PtsAg"] = ts.stats[t.PA].ToString();
                    teamStats[sStatsIndex]["FGMade"] = ts.stats[t.FGM].ToString();
                    teamStats[sStatsIndex]["FGAtt"] = ts.stats[t.FGA].ToString();
                    teamStats[sStatsIndex]["3PTMade"] = ts.stats[t.TPM].ToString();
                    teamStats[sStatsIndex]["3PTAtt"] = ts.stats[t.TPA].ToString();
                    teamStats[sStatsIndex]["FTMade"] = ts.stats[t.FTM].ToString();
                    teamStats[sStatsIndex]["FTAtt"] = ts.stats[t.FTA].ToString();
                    teamStats[sStatsIndex]["DRebs"] = ts.stats[t.DREB].ToString();
                    teamStats[sStatsIndex]["ORebs"] = ts.stats[t.OREB].ToString();
                    teamStats[sStatsIndex]["Steals"] = ts.stats[t.STL].ToString();
                    teamStats[sStatsIndex]["Blocks"] = ts.stats[t.BLK].ToString();
                    teamStats[sStatsIndex]["Assists"] = ts.stats[t.AST].ToString();
                    teamStats[sStatsIndex]["Fouls"] = ts.stats[t.FOUL].ToString();
                    teamStats[sStatsIndex]["TOs"] = ts.stats[t.TO].ToString();
                    //teamStats[sStatsIndex]["TOsAg"] = tsopp.stats[t.TO].ToString();
                }

                if (pStatsID != -1)
                {
                    int pStatsIndex = teamStats.FindIndex(delegate(Dictionary<string, string> s)
                                                              {
                                                                  if (s["ID"] == pStatsID.ToString()) return true;
                                                                  return false;
                                                              });

                    if (pStatsIndex != -1)
                    {
                        teamStats[pStatsIndex]["Wins"] = ts.pl_winloss[0].ToString();
                        teamStats[pStatsIndex]["Losses"] = ts.pl_winloss[1].ToString();
                        teamStats[pStatsIndex]["Mins"] = ts.pl_stats[t.MINS].ToString();
                        teamStats[pStatsIndex]["PtsFor"] = ts.pl_stats[t.PF].ToString();
                        teamStats[pStatsIndex]["PtsAg"] = ts.pl_stats[t.PA].ToString();
                        teamStats[pStatsIndex]["FGMade"] = ts.pl_stats[t.FGM].ToString();
                        teamStats[pStatsIndex]["FGAtt"] = ts.pl_stats[t.FGA].ToString();
                        teamStats[pStatsIndex]["3PTMade"] = ts.pl_stats[t.TPM].ToString();
                        teamStats[pStatsIndex]["3PTAtt"] = ts.pl_stats[t.TPA].ToString();
                        teamStats[pStatsIndex]["FTMade"] = ts.pl_stats[t.FTM].ToString();
                        teamStats[pStatsIndex]["FTAtt"] = ts.pl_stats[t.FTA].ToString();
                        teamStats[pStatsIndex]["DRebs"] = ts.pl_stats[t.DREB].ToString();
                        teamStats[pStatsIndex]["ORebs"] = ts.pl_stats[t.OREB].ToString();
                        teamStats[pStatsIndex]["Steals"] = ts.pl_stats[t.STL].ToString();
                        teamStats[pStatsIndex]["Blocks"] = ts.pl_stats[t.BLK].ToString();
                        teamStats[pStatsIndex]["Assists"] = ts.pl_stats[t.AST].ToString();
                        teamStats[pStatsIndex]["Fouls"] = ts.pl_stats[t.FOUL].ToString();
                        teamStats[pStatsIndex]["TOs"] = ts.pl_stats[t.TO].ToString();
                        //teamStats[pStatsIndex]["TOsAg"] = tsopp.stats[t.TO].ToString();
                    }
                }
            }

            if (!teamsOnly)
            {
                foreach (int key in pst.Keys)
                {
                    PlayerStats ps = pst[key];

                    int id = ps.ID;

                    int pindex = players.FindIndex(delegate(Dictionary<string, string> s)
                                                       {
                                                           if (s["ID"] == id.ToString()) return true;
                                                           return false;
                                                       });

                    Dictionary<string, string> player = players[pindex];

                    /*for (int i = 16; i >= 0; i--)
                    {
                        string cur = player["StatY" + i.ToString()];
                        if (cur != "-1") playerStatsID = Convert.ToInt32(cur);
                    }*/
                    int playerStatsID = Convert.ToInt32(player["StatY0"]);

                    int playerStatsIndex = playerStats.FindIndex(delegate(Dictionary<string, string> s)
                                                                     {
                                                                         if (s["ID"] == playerStatsID.ToString())
                                                                             return true;
                                                                         return false;
                                                                     });

                    if (playerStatsIndex != -1)
                    {
                        playerStats[playerStatsIndex]["GamesP"] = ps.stats[p.GP].ToString();
                        playerStats[playerStatsIndex]["GamesS"] = ps.stats[p.GS].ToString();
                        playerStats[playerStatsIndex]["Minutes"] = ps.stats[p.MINS].ToString();
                        playerStats[playerStatsIndex]["Points"] = ps.stats[p.PTS].ToString();
                        playerStats[playerStatsIndex]["DRebs"] = ps.stats[p.DREB].ToString();
                        playerStats[playerStatsIndex]["ORebs"] = ps.stats[p.OREB].ToString();
                        playerStats[playerStatsIndex]["Assists"] = ps.stats[p.AST].ToString();
                        playerStats[playerStatsIndex]["Steals"] = ps.stats[p.STL].ToString();
                        playerStats[playerStatsIndex]["Blocks"] = ps.stats[p.BLK].ToString();
                        playerStats[playerStatsIndex]["TOs"] = ps.stats[p.TO].ToString();
                        playerStats[playerStatsIndex]["Fouls"] = ps.stats[p.FOUL].ToString();
                        playerStats[playerStatsIndex]["FGMade"] = ps.stats[p.FGM].ToString();
                        playerStats[playerStatsIndex]["FGAtt"] = ps.stats[p.FGA].ToString();
                        playerStats[playerStatsIndex]["3PTMade"] = ps.stats[p.TPM].ToString();
                        playerStats[playerStatsIndex]["3PTAtt"] = ps.stats[p.TPA].ToString();
                        playerStats[playerStatsIndex]["FTMade"] = ps.stats[p.FTM].ToString();
                        playerStats[playerStatsIndex]["FTAtt"] = ps.stats[p.FTA].ToString();
                        playerStats[playerStatsIndex]["IsAStar"] = (ps.isAllStar ? 1 : 0).ToString();
                        playerStats[playerStatsIndex]["IsChamp"] = (ps.isNBAChampion ? 1 : 0).ToString();
                    }
                }
            }

            string path = folder + @"\Team_Stats.csv";
            CSV.CreateCSVFromDictionaryList(teamStats, path);
            path = folder + @"Player_Stats.csv";
            CSV.CreateCSVFromDictionaryList(playerStats, path);

            return 0;
        }
    }
}