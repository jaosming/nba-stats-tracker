﻿#region Copyright Notice

// Created by Lefteris Aslanoglou, (c) 2011-2013
// 
// Initial development until v1.0 done as part of the implementation of thesis
// "Application Development for Basketball Statistical Analysis in Natural Language"
// under the supervision of Prof. Athanasios Tsakalidis & MSc Alexandros Georgiou
// 
// All rights reserved. Unless specifically stated otherwise, the code in this file should 
// not be reproduced, edited and/or republished without explicit permission from the 
// author.

#endregion

#region Using Directives

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LeftosCommonLibrary;
using NBA_Stats_Tracker.Data.BoxScores;
using NBA_Stats_Tracker.Data.Other;
using NBA_Stats_Tracker.Data.PastStats;
using NBA_Stats_Tracker.Data.Players;
using NBA_Stats_Tracker.Data.Teams;
using NBA_Stats_Tracker.Windows;
using SQLite_Database;

#endregion

namespace NBA_Stats_Tracker.Data.SQLiteIO
{
    /// <summary>
    ///     Implements all SQLite-related input/output methods.
    /// </summary>
    internal static class SQLiteIO
    {
        private const string CreateCareerHighsQuery =
            "CREATE TABLE \"CareerHighs\" (\"PlayerID\" INTEGER ,\"MINS\" INTEGER , \"PTS\" INTEGER ,\"REB\" INTEGER ," +
            "\"AST\" INTEGER ,\"STL\" INTEGER ,\"BLK\" INTEGER ,\"TOS\" INTEGER ,\"FGM\" INTEGER ,\"FGA\" INTEGER ," +
            "\"TPM\" INTEGER ,\"TPA\" INTEGER ,\"FTM\" INTEGER ,\"FTA\" INTEGER ,\"OREB\" INTEGER , \"DREB\" INTEGER, " +
            "\"FOUL\" INTEGER, PRIMARY KEY (\"PlayerID\") )";

        private static bool _upgrading;

        /// <summary>
        ///     Saves the database to a new file.
        /// </summary>
        /// <param name="file">The file to save to.</param>
        /// <returns>
        ///     <c>true</c> if the operation succeeded, <c>false</c> otherwise.
        /// </returns>
        public static bool SaveDatabaseAs(string file)
        {
            string oldDB = MainWindow.CurrentDB + ".tmp";
            File.Copy(MainWindow.CurrentDB, oldDB, true);
            MainWindow.CurrentDB = oldDB;
            try
            {
                File.Delete(file);
            }
            catch
            {
                MessageBox.Show("Error while trying to overwrite file. Make sure the file is not in use by another program.");
                return false;
            }
            SaveAllSeasons(file);
            SetSetting(file, "Game Length", MainWindow.GameLength);
            SetSetting(file, "Season Length", MainWindow.SeasonLength);
            File.Delete(oldDB);
            return true;
        }

        /// <summary>
        ///     Saves all seasons to the specified database.
        /// </summary>
        /// <param name="file">The file.</param>
        public static void SaveAllSeasons(string file)
        {
            string oldDB = MainWindow.CurrentDB;
            int oldSeason = MainWindow.CurSeason;

            int maxSeason = GetMaxSeason(oldDB);

            if (MainWindow.Tf.IsBetween)
            {
                MainWindow.Tf = new Timeframe(oldSeason);
                MainWindow.UpdateAllData();
            }
            SaveSeasonToDatabase(file, MainWindow.TST, MainWindow.TSTOpp, MainWindow.PST, MainWindow.CurSeason, maxSeason);

            for (int i = 1; i <= maxSeason; i++)
            {
                if (i != oldSeason)
                {
                    LoadSeason(oldDB, i, doNotLoadBoxScores: true);
                    SaveSeasonToDatabase(file, MainWindow.TST, MainWindow.TSTOpp, MainWindow.PST, MainWindow.CurSeason, maxSeason,
                                         doNotSaveBoxScores: true);
                }
            }
            LoadSeason(file, oldSeason, doNotLoadBoxScores: true);
        }

        /// <summary>
        ///     Saves the conferences and divisions to a specified database.
        /// </summary>
        /// <param name="file">The database.</param>
        public static void SaveConferencesAndDivisions(string file)
        {
            var db = new SQLiteDatabase(file);
            db.ClearTable("Conferences");
            foreach (var conf in MainWindow.Conferences)
            {
                db.Insert("Conferences", new Dictionary<string, string> {{"ID", conf.ID.ToString()}, {"Name", conf.Name}});
            }
            db.ClearTable("Divisions");
            foreach (var div in MainWindow.Divisions)
            {
                db.Insert("Divisions",
                          new Dictionary<string, string>
                          {
                              {"ID", div.ID.ToString()},
                              {"Name", div.Name},
                              {"Conference", div.ConferenceID.ToString()}
                          });
            }
        }

        /// <summary>
        ///     Saves the season to the current database.
        /// </summary>
        public static void SaveSeasonToDatabase()
        {
            SaveSeasonToDatabase(MainWindow.CurrentDB, MainWindow.TST, MainWindow.TSTOpp, MainWindow.PST, MainWindow.CurSeason,
                                 GetMaxSeason(MainWindow.CurrentDB));
        }

        /// <summary>
        ///     Saves the season to a specified database.
        /// </summary>
        /// <param name="file">The database.</param>
        /// <param name="tstToSave">The TeamStats dictionary to save.</param>
        /// <param name="tstOppToSave">The opposing TeamStats dictionary to save.</param>
        /// <param name="pstToSave">The PlayerStats dictionary to save.</param>
        /// <param name="season">The season ID.</param>
        /// <param name="maxSeason">The max season ID.</param>
        /// <param name="doNotSaveBoxScores">
        ///     if set to <c>true</c>, will not save box scores.
        /// </param>
        /// <param name="partialUpdate">
        ///     if set to <c>true</c>, a partial update will be made (i.e. any pre-existing data won't be cleared before writing the current data).
        /// </param>
        public static void SaveSeasonToDatabase(string file, Dictionary<int, TeamStats> tstToSave, Dictionary<int, TeamStats> tstOppToSave,
                                                Dictionary<int, PlayerStats> pstToSave, int season, int maxSeason,
                                                bool doNotSaveBoxScores = false, bool partialUpdate = false)
        {
            // Delete the file and create it from scratch. If partial updating is implemented later, maybe
            // we won't delete the file before all this.
            //File.Delete(file); 

            // Isn't really needed since we delete the file, but is left for partial updating efforts later.
            bool fileExists = File.Exists(file);

            // SQLite
            //try
            //{
            MainWindow.DB = new SQLiteDatabase(file);
            if (!fileExists)
                PrepareNewDB(MainWindow.DB, season, maxSeason);

            SaveConferencesAndDivisions(file);

            SaveSeasonName(season);

            saveTeamsToDatabase(file, tstToSave, tstOppToSave, season, maxSeason);

            #region Save Player Stats

            SavePlayersToDatabase(file, pstToSave, season, maxSeason, partialUpdate);

            #endregion

            #region Save Box Scores

            if (!doNotSaveBoxScores)
            {
                const string q = "select GameID from GameResults;";
                DataTable res = MainWindow.DB.GetDataTable(q);
                List<int> idList = (from DataRow r in res.Rows
                                    select Convert.ToInt32(r[0].ToString())).ToList();

                var sqlinsert = new List<Dictionary<string, string>>();
                foreach (var bse in MainWindow.BSHist)
                {
                    string q2 = "select HASH from GameResults";
                    List<string> hashes = MainWindow.DB.GetDataTable(q2).Rows.Cast<DataRow>().Select(dr => dr[0].ToString()).ToList();
                    string md5;
                    do
                    {
                        md5 = Tools.GetMD5(MainWindow.Random.Next().ToString());
                    } while (hashes.Contains(md5));
                    if ((!fileExists) || (bse.BS.ID == -1) || (!idList.Contains(bse.BS.ID)) || (bse.MustUpdate))
                    {
                        var dict2 = new Dictionary<string, string>
                                    {
                                        {"Team1ID", bse.BS.Team1ID.ToString()},
                                        {"Team2ID", bse.BS.Team2ID.ToString()},
                                        {"Date", String.Format("{0:yyyy-MM-dd HH:mm:ss}", bse.BS.GameDate)},
                                        {"SeasonNum", bse.BS.SeasonNum.ToString()},
                                        {"IsPlayoff", bse.BS.IsPlayoff.ToString()},
                                        {"T1PTS", bse.BS.PTS1.ToString()},
                                        {"T1REB", bse.BS.REB1.ToString()},
                                        {"T1AST", bse.BS.AST1.ToString()},
                                        {"T1STL", bse.BS.STL1.ToString()},
                                        {"T1BLK", bse.BS.BLK1.ToString()},
                                        {"T1TOS", bse.BS.TOS1.ToString()},
                                        {"T1FGM", bse.BS.FGM1.ToString()},
                                        {"T1FGA", bse.BS.FGA1.ToString()},
                                        {"T13PM", bse.BS.TPM1.ToString()},
                                        {"T13PA", bse.BS.TPA1.ToString()},
                                        {"T1FTM", bse.BS.FTM1.ToString()},
                                        {"T1FTA", bse.BS.FTA1.ToString()},
                                        {"T1OREB", bse.BS.OREB1.ToString()},
                                        {"T1FOUL", bse.BS.FOUL1.ToString()},
                                        {"T1MINS", bse.BS.MINS1.ToString()},
                                        {"T2PTS", bse.BS.PTS2.ToString()},
                                        {"T2REB", bse.BS.REB2.ToString()},
                                        {"T2AST", bse.BS.AST2.ToString()},
                                        {"T2STL", bse.BS.STL2.ToString()},
                                        {"T2BLK", bse.BS.BLK2.ToString()},
                                        {"T2TOS", bse.BS.TOS2.ToString()},
                                        {"T2FGM", bse.BS.FGM2.ToString()},
                                        {"T2FGA", bse.BS.FGA2.ToString()},
                                        {"T23PM", bse.BS.TPM2.ToString()},
                                        {"T23PA", bse.BS.TPA2.ToString()},
                                        {"T2FTM", bse.BS.FTM2.ToString()},
                                        {"T2FTA", bse.BS.FTA2.ToString()},
                                        {"T2OREB", bse.BS.OREB2.ToString()},
                                        {"T2FOUL", bse.BS.FOUL2.ToString()},
                                        {"T2MINS", bse.BS.MINS2.ToString()},
                                        {"HASH", md5}
                                    };

                        if (idList.Contains(bse.BS.ID))
                        {
                            MainWindow.DB.Update("GameResults", dict2, "GameID = " + bse.BS.ID);
                        }
                        else
                        {
                            MainWindow.DB.Insert("GameResults", dict2);

                            int lastid =
                                Convert.ToInt32(
                                    MainWindow.DB.GetDataTable("select GameID from GameResults where HASH LIKE \"" + md5 + "\"").Rows[0][
                                        "GameID"].ToString());
                            bse.BS.ID = lastid;
                        }
                        hashes.Add(md5);
                        MainWindow.DB.Delete("PlayerResults", "GameID = " + bse.BS.ID);

                        foreach (var pbs in bse.PBSList)
                        {
                            dict2 = new Dictionary<string, string>
                                    {
                                        {"GameID", bse.BS.ID.ToString()},
                                        {"PlayerID", pbs.PlayerID.ToString()},
                                        {"TeamID", pbs.TeamID.ToString()},
                                        {"isStarter", pbs.IsStarter.ToString()},
                                        {"playedInjured", pbs.PlayedInjured.ToString()},
                                        {"isOut", pbs.IsOut.ToString()},
                                        {"MINS", pbs.MINS.ToString()},
                                        {"PTS", pbs.PTS.ToString()},
                                        {"REB", pbs.REB.ToString()},
                                        {"AST", pbs.AST.ToString()},
                                        {"STL", pbs.STL.ToString()},
                                        {"BLK", pbs.BLK.ToString()},
                                        {"TOS", pbs.TOS.ToString()},
                                        {"FGM", pbs.FGM.ToString()},
                                        {"FGA", pbs.FGA.ToString()},
                                        {"TPM", pbs.TPM.ToString()},
                                        {"TPA", pbs.TPA.ToString()},
                                        {"FTM", pbs.FTM.ToString()},
                                        {"FTA", pbs.FTA.ToString()},
                                        {"OREB", pbs.OREB.ToString()},
                                        {"FOUL", pbs.FOUL.ToString()}
                                    };

                            sqlinsert.Add(dict2);
                        }
                    }
                }
                if (sqlinsert.Count > 0)
                {
                    MainWindow.DB.InsertManyTransaction("PlayerResults", sqlinsert);
                    //int linesAffected = MainWindow.db.InsertMany("PlayerResults", sqlinsert);
                    //Thread.Sleep(500);
                    //DataTable dt = MainWindow.db.GetDataTable("SELECT * FROM PlayerResults");
                    //Debug.Print(dt.Rows.Count.ToString() + " " + linesAffected.ToString());
                }
            }

            #endregion

            MainWindow.MWInstance.txtFile.Text = file;
            MainWindow.CurrentDB = file;

            //}
            //catch (Exception ex)
            //{
            //App.errorReport(ex, "Trying to save team stats - SQLite");
            //}
        }

        /// <summary>
        ///     Saves the name of the season.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <exception cref="System.Exception">Raised if the specified season ID doesn't correspond to a season existing in the database.</exception>
        public static void SaveSeasonName(int season)
        {
            var dict = new Dictionary<string, string> {{"ID", season.ToString()}};
            try
            {
                dict.Add("Name", MainWindow.GetSeasonName(season));
            }
            catch
            {
                dict.Add("Name", season.ToString());
            }
            try
            {
                int result = MainWindow.DB.Update("SeasonNames", dict, "ID = " + season.ToString());
                if (result < 1)
                    throw (new Exception());
            }
            catch (Exception)
            {
                MainWindow.DB.Insert("SeasonNames", dict);
            }
        }

        /// <summary>
        ///     Saves the teams to a specified database.
        /// </summary>
        /// <param name="file">The database.</param>
        /// <param name="tstToSave">The TeamStats dictionary to save.</param>
        /// <param name="tstOppToSave">The opposing TeamStats dictionary to save.</param>
        /// <param name="season">The season ID.</param>
        /// <param name="maxSeason">The max season ID.</param>
        private static void saveTeamsToDatabase(string file, Dictionary<int, TeamStats> tstToSave, Dictionary<int, TeamStats> tstOppToSave,
                                                int season, int maxSeason)
        {
            var db = new SQLiteDatabase(file);
            string teamsT = "Teams";
            string plTeamsT = "PlayoffTeams";
            string oppT = "Opponents";
            string plOppT = "PlayoffOpponents";

            if (season != maxSeason)
            {
                teamsT += "S" + season;
                plTeamsT += "S" + season;
                oppT += "S" + season;
                plOppT += "S" + season;
            }

            db.ClearTable(teamsT);
            db.ClearTable(plTeamsT);
            db.ClearTable(oppT);
            db.ClearTable(plOppT);

            String q = "select Name from " + teamsT + ";";

            try
            {
                db.GetDataTable(q);
            }
            catch
            {
                PrepareNewDB(db, season, maxSeason, onlyNewSeason: true);
                db.GetDataTable(q);
            }

            var seasonList = new List<Dictionary<string, string>>(500);
            var playoffList = new List<Dictionary<string, string>>(500);
            int i = 0;
            foreach (var key in tstToSave.Keys)
            {
                //bool found = false;

                var dict = new Dictionary<string, string>
                           {
                               {"ID", tstToSave[key].ID.ToString()},
                               {"Name", tstToSave[key].Name},
                               {"DisplayName", tstToSave[key].DisplayName},
                               {"isHidden", tstToSave[key].IsHidden.ToString()},
                               {"Division", tstToSave[key].Division.ToString()},
                               {"Conference", tstToSave[key].Conference.ToString()},
                               {"WIN", tstToSave[key].Record[0].ToString()},
                               {"LOSS", tstToSave[key].Record[1].ToString()},
                               {"MINS", tstToSave[key].Totals[TAbbr.MINS].ToString()},
                               {"PF", tstToSave[key].Totals[TAbbr.PF].ToString()},
                               {"PA", tstToSave[key].Totals[TAbbr.PA].ToString()},
                               {"FGM", tstToSave[key].Totals[TAbbr.FGM].ToString()},
                               {"FGA", tstToSave[key].Totals[TAbbr.FGA].ToString()},
                               {"TPM", tstToSave[key].Totals[TAbbr.TPM].ToString()},
                               {"TPA", tstToSave[key].Totals[TAbbr.TPA].ToString()},
                               {"FTM", tstToSave[key].Totals[TAbbr.FTM].ToString()},
                               {"FTA", tstToSave[key].Totals[TAbbr.FTA].ToString()},
                               {"OREB", tstToSave[key].Totals[TAbbr.OREB].ToString()},
                               {"DREB", tstToSave[key].Totals[TAbbr.DREB].ToString()},
                               {"STL", tstToSave[key].Totals[TAbbr.STL].ToString()},
                               {"TOS", tstToSave[key].Totals[TAbbr.TOS].ToString()},
                               {"BLK", tstToSave[key].Totals[TAbbr.BLK].ToString()},
                               {"AST", tstToSave[key].Totals[TAbbr.AST].ToString()},
                               {"FOUL", tstToSave[key].Totals[TAbbr.FOUL].ToString()},
                               {"OFFSET", tstToSave[key].Offset.ToString()}
                           };

                seasonList.Add(dict);

                var plDict = new Dictionary<string, string>
                             {
                                 {"ID", MainWindow.TeamOrder[tstToSave[key].Name].ToString()},
                                 {"Name", tstToSave[key].Name},
                                 {"DisplayName", tstToSave[key].DisplayName},
                                 {"isHidden", tstToSave[key].IsHidden.ToString()},
                                 {"Division", tstToSave[key].Division.ToString()},
                                 {"Conference", tstToSave[key].Conference.ToString()},
                                 {"WIN", tstToSave[key].PlRecord[0].ToString()},
                                 {"LOSS", tstToSave[key].PlRecord[1].ToString()},
                                 {"MINS", tstToSave[key].PlTotals[TAbbr.MINS].ToString()},
                                 {"PF", tstToSave[key].PlTotals[TAbbr.PF].ToString()},
                                 {"PA", tstToSave[key].PlTotals[TAbbr.PA].ToString()},
                                 {"FGM", tstToSave[key].PlTotals[TAbbr.FGM].ToString()},
                                 {"FGA", tstToSave[key].PlTotals[TAbbr.FGA].ToString()},
                                 {"TPM", tstToSave[key].PlTotals[TAbbr.TPM].ToString()},
                                 {"TPA", tstToSave[key].PlTotals[TAbbr.TPA].ToString()},
                                 {"FTM", tstToSave[key].PlTotals[TAbbr.FTM].ToString()},
                                 {"FTA", tstToSave[key].PlTotals[TAbbr.FTA].ToString()},
                                 {"OREB", tstToSave[key].PlTotals[TAbbr.OREB].ToString()},
                                 {"DREB", tstToSave[key].PlTotals[TAbbr.DREB].ToString()},
                                 {"STL", tstToSave[key].PlTotals[TAbbr.STL].ToString()},
                                 {"TOS", tstToSave[key].PlTotals[TAbbr.TOS].ToString()},
                                 {"BLK", tstToSave[key].PlTotals[TAbbr.BLK].ToString()},
                                 {"AST", tstToSave[key].PlTotals[TAbbr.AST].ToString()},
                                 {"FOUL", tstToSave[key].PlTotals[TAbbr.FOUL].ToString()},
                                 {"OFFSET", tstToSave[key].PlOffset.ToString()}
                             };

                playoffList.Add(plDict);

                i++;

                if (i == 500)
                {
                    db.InsertManyUnion(teamsT, seasonList);
                    db.InsertManyUnion(plTeamsT, playoffList);
                    i = 0;
                    seasonList.Clear();
                    playoffList.Clear();
                }
                /*
                foreach (DataRow r in res.Rows)
                {
                    if (r[0].ToString().Equals(ts.name))
                    {
                        _db.Update(teamsT, dict, "Name LIKE \'" + ts.name + "\'");
                        _db.Update(pl_teamsT, pl_dict, "Name LIKE \'" + ts.name + "\'");
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _db.Insert(teamsT, dict);
                    _db.Insert(pl_teamsT, pl_dict);
                }
                */
            }
            if (i > 0)
            {
                db.InsertManyUnion(teamsT, seasonList);
                db.InsertManyUnion(plTeamsT, playoffList);
            }

            seasonList = new List<Dictionary<string, string>>(500);
            playoffList = new List<Dictionary<string, string>>(500);
            i = 0;
            foreach (var key in tstOppToSave.Keys)
            {
                //bool found = false;

                var dict = new Dictionary<string, string>
                           {
                               {"ID", MainWindow.TeamOrder[tstOppToSave[key].Name].ToString()},
                               {"Name", tstOppToSave[key].Name},
                               {"DisplayName", tstOppToSave[key].DisplayName},
                               {"isHidden", tstOppToSave[key].IsHidden.ToString()},
                               {"Division", tstOppToSave[key].Division.ToString()},
                               {"Conference", tstOppToSave[key].Conference.ToString()},
                               {"WIN", tstOppToSave[key].Record[0].ToString()},
                               {"LOSS", tstOppToSave[key].Record[1].ToString()},
                               {"MINS", tstOppToSave[key].Totals[TAbbr.MINS].ToString()},
                               {"PF", tstOppToSave[key].Totals[TAbbr.PF].ToString()},
                               {"PA", tstOppToSave[key].Totals[TAbbr.PA].ToString()},
                               {"FGM", tstOppToSave[key].Totals[TAbbr.FGM].ToString()},
                               {"FGA", tstOppToSave[key].Totals[TAbbr.FGA].ToString()},
                               {"TPM", tstOppToSave[key].Totals[TAbbr.TPM].ToString()},
                               {"TPA", tstOppToSave[key].Totals[TAbbr.TPA].ToString()},
                               {"FTM", tstOppToSave[key].Totals[TAbbr.FTM].ToString()},
                               {"FTA", tstOppToSave[key].Totals[TAbbr.FTA].ToString()},
                               {"OREB", tstOppToSave[key].Totals[TAbbr.OREB].ToString()},
                               {"DREB", tstOppToSave[key].Totals[TAbbr.DREB].ToString()},
                               {"STL", tstOppToSave[key].Totals[TAbbr.STL].ToString()},
                               {"TOS", tstOppToSave[key].Totals[TAbbr.TOS].ToString()},
                               {"BLK", tstOppToSave[key].Totals[TAbbr.BLK].ToString()},
                               {"AST", tstOppToSave[key].Totals[TAbbr.AST].ToString()},
                               {"FOUL", tstOppToSave[key].Totals[TAbbr.FOUL].ToString()},
                               {"OFFSET", tstOppToSave[key].Offset.ToString()}
                           };

                seasonList.Add(dict);

                var plDict = new Dictionary<string, string>
                             {
                                 {"ID", MainWindow.TeamOrder[tstOppToSave[key].Name].ToString()},
                                 {"Name", tstOppToSave[key].Name},
                                 {"DisplayName", tstOppToSave[key].DisplayName},
                                 {"isHidden", tstOppToSave[key].IsHidden.ToString()},
                                 {"Division", tstOppToSave[key].Division.ToString()},
                                 {"Conference", tstOppToSave[key].Conference.ToString()},
                                 {"WIN", tstOppToSave[key].PlRecord[0].ToString()},
                                 {"LOSS", tstOppToSave[key].PlRecord[1].ToString()},
                                 {"MINS", tstOppToSave[key].PlTotals[TAbbr.MINS].ToString()},
                                 {"PF", tstOppToSave[key].PlTotals[TAbbr.PF].ToString()},
                                 {"PA", tstOppToSave[key].PlTotals[TAbbr.PA].ToString()},
                                 {"FGM", tstOppToSave[key].PlTotals[TAbbr.FGM].ToString()},
                                 {"FGA", tstOppToSave[key].PlTotals[TAbbr.FGA].ToString()},
                                 {"TPM", tstOppToSave[key].PlTotals[TAbbr.TPM].ToString()},
                                 {"TPA", tstOppToSave[key].PlTotals[TAbbr.TPA].ToString()},
                                 {"FTM", tstOppToSave[key].PlTotals[TAbbr.FTM].ToString()},
                                 {"FTA", tstOppToSave[key].PlTotals[TAbbr.FTA].ToString()},
                                 {"OREB", tstOppToSave[key].PlTotals[TAbbr.OREB].ToString()},
                                 {"DREB", tstOppToSave[key].PlTotals[TAbbr.DREB].ToString()},
                                 {"STL", tstOppToSave[key].PlTotals[TAbbr.STL].ToString()},
                                 {"TOS", tstOppToSave[key].PlTotals[TAbbr.TOS].ToString()},
                                 {"BLK", tstOppToSave[key].PlTotals[TAbbr.BLK].ToString()},
                                 {"AST", tstOppToSave[key].PlTotals[TAbbr.AST].ToString()},
                                 {"FOUL", tstOppToSave[key].PlTotals[TAbbr.FOUL].ToString()},
                                 {"OFFSET", tstOppToSave[key].PlOffset.ToString()}
                             };

                playoffList.Add(plDict);

                i++;

                if (i == 500)
                {
                    db.InsertManyUnion(oppT, seasonList);
                    db.InsertManyUnion(plOppT, playoffList);
                    i = 0;
                    seasonList.Clear();
                    playoffList.Clear();
                }

                /*
                foreach (DataRow r in res.Rows)
                {
                    if (r[0].ToString().Equals(ts.name))
                    {
                        _db.Update(oppT, dict, "Name LIKE \'" + ts.name + "\'");
                        _db.Update(pl_oppT, pl_dict, "Name LIKE \'" + ts.name + "\'");
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _db.Insert(oppT, dict);
                    _db.Insert(pl_oppT, pl_dict);
                }
                */
            }
            if (i > 0)
            {
                db.InsertManyUnion(oppT, seasonList);
                db.InsertManyUnion(plOppT, playoffList);
            }
        }

        /// <summary>
        ///     Saves the players to a specified database.
        /// </summary>
        /// <param name="file">The database.</param>
        /// <param name="playerStats">The player stats.</param>
        /// <param name="season">The season ID.</param>
        /// <param name="maxSeason">The max season ID.</param>
        /// <param name="partialUpdate">
        ///     if set to <c>true</c>, a partial update will be made (i.e. any pre-existing data won't be cleared before writing the current data).
        /// </param>
        public static void SavePlayersToDatabase(string file, Dictionary<int, PlayerStats> playerStats, int season, int maxSeason,
                                                 bool partialUpdate = false)
        {
            var db = new SQLiteDatabase(file);

            string playersT = "Players";
            string plPlayersT = "PlayoffPlayers";

            if (season != maxSeason)
            {
                playersT += "S" + season.ToString();
                plPlayersT += "S" + season.ToString();
            }

            if (!partialUpdate)
            {
                MainWindow.DB.ClearTable(playersT);
                MainWindow.DB.ClearTable(plPlayersT);
                MainWindow.DB.ClearTable("CareerHighs");
            }
            string q = "select ID from " + playersT + ";";
            DataTable res = MainWindow.DB.GetDataTable(q);

            //var idList = (from DataRow dr in res.Rows select Convert.ToInt32(dr["ID"].ToString())).ToList();

            var sqlinsert = new List<Dictionary<string, string>>();
            var plSqlinsert = new List<Dictionary<string, string>>();
            var chSqlinsert = new List<Dictionary<string, string>>();
            int i = 0;

            foreach (var kvp in playerStats)
            {
                PlayerStats ps = kvp.Value;
                if (partialUpdate)
                {
                    db.Delete(playersT, "ID = " + ps.ID);
                    db.Delete(plPlayersT, "ID = " + ps.ID);
                    db.Delete("CareerHighs", "PlayerID = " + ps.ID);
                }
                var dict = new Dictionary<string, string>
                           {
                               {"ID", ps.ID.ToString()},
                               {"LastName", ps.LastName},
                               {"FirstName", ps.FirstName},
                               {"Position1", ps.Position1.ToString()},
                               {"Position2", ps.Position2.ToString()},
                               {"isActive", ps.IsActive.ToString()},
                               {"YearOfBirth", ps.YearOfBirth.ToString()},
                               {"YearsPro", ps.YearsPro.ToString()},
                               {"isHidden", ps.IsHidden.ToString()},
                               {"InjuryType", ps.Injury.InjuryType.ToString()},
                               {"CustomInjuryName", ps.Injury.CustomInjuryName},
                               {"InjuryDaysLeft", ps.Injury.InjuryDaysLeft.ToString()},
                               {"TeamFin", ps.TeamF.ToString()},
                               {"TeamSta", ps.TeamS.ToString()},
                               {"GP", ps.Totals[PAbbr.GP].ToString()},
                               {"GS", ps.Totals[PAbbr.GS].ToString()},
                               {"MINS", ps.Totals[PAbbr.MINS].ToString()},
                               {"PTS", ps.Totals[PAbbr.PTS].ToString()},
                               {"FGM", ps.Totals[PAbbr.FGM].ToString()},
                               {"FGA", ps.Totals[PAbbr.FGA].ToString()},
                               {"TPM", ps.Totals[PAbbr.TPM].ToString()},
                               {"TPA", ps.Totals[PAbbr.TPA].ToString()},
                               {"FTM", ps.Totals[PAbbr.FTM].ToString()},
                               {"FTA", ps.Totals[PAbbr.FTA].ToString()},
                               {"OREB", ps.Totals[PAbbr.OREB].ToString()},
                               {"DREB", ps.Totals[PAbbr.DREB].ToString()},
                               {"STL", ps.Totals[PAbbr.STL].ToString()},
                               {"TOS", ps.Totals[PAbbr.TOS].ToString()},
                               {"BLK", ps.Totals[PAbbr.BLK].ToString()},
                               {"AST", ps.Totals[PAbbr.AST].ToString()},
                               {"FOUL", ps.Totals[PAbbr.FOUL].ToString()},
                               {"isAllStar", ps.IsAllStar.ToString()},
                               {"isNBAChampion", ps.IsNBAChampion.ToString()},
                               {"ContractY1", ps.Contract.TryGetSalary(1).ToString()},
                               {"ContractY2", ps.Contract.TryGetSalary(2).ToString()},
                               {"ContractY3", ps.Contract.TryGetSalary(3).ToString()},
                               {"ContractY4", ps.Contract.TryGetSalary(4).ToString()},
                               {"ContractY5", ps.Contract.TryGetSalary(5).ToString()},
                               {"ContractY6", ps.Contract.TryGetSalary(6).ToString()},
                               {"ContractY7", ps.Contract.TryGetSalary(7).ToString()},
                               {"ContractOption", ((byte) ps.Contract.Option).ToString()},
                               {"Height", ps.Height.ToString()},
                               {"Weight", ps.Weight.ToString()}
                           };
                var plDict = new Dictionary<string, string>
                             {
                                 {"ID", ps.ID.ToString()},
                                 {"GP", ps.PlTotals[PAbbr.GP].ToString()},
                                 {"GS", ps.PlTotals[PAbbr.GS].ToString()},
                                 {"MINS", ps.PlTotals[PAbbr.MINS].ToString()},
                                 {"PTS", ps.PlTotals[PAbbr.PTS].ToString()},
                                 {"FGM", ps.PlTotals[PAbbr.FGM].ToString()},
                                 {"FGA", ps.PlTotals[PAbbr.FGA].ToString()},
                                 {"TPM", ps.PlTotals[PAbbr.TPM].ToString()},
                                 {"TPA", ps.PlTotals[PAbbr.TPA].ToString()},
                                 {"FTM", ps.PlTotals[PAbbr.FTM].ToString()},
                                 {"FTA", ps.PlTotals[PAbbr.FTA].ToString()},
                                 {"OREB", ps.PlTotals[PAbbr.OREB].ToString()},
                                 {"DREB", ps.PlTotals[PAbbr.DREB].ToString()},
                                 {"STL", ps.PlTotals[PAbbr.STL].ToString()},
                                 {"TOS", ps.PlTotals[PAbbr.TOS].ToString()},
                                 {"BLK", ps.PlTotals[PAbbr.BLK].ToString()},
                                 {"AST", ps.PlTotals[PAbbr.AST].ToString()},
                                 {"FOUL", ps.PlTotals[PAbbr.FOUL].ToString()}
                             };
                var chDict = new Dictionary<string, string>
                             {
                                 {"PlayerID", ps.ID.ToString()},
                                 {"MINS", ps.CareerHighs[PAbbr.MINS].ToString()},
                                 {"PTS", ps.CareerHighs[PAbbr.PTS].ToString()},
                                 {"FGM", ps.CareerHighs[PAbbr.FGM].ToString()},
                                 {"FGA", ps.CareerHighs[PAbbr.FGA].ToString()},
                                 {"TPM", ps.CareerHighs[PAbbr.TPM].ToString()},
                                 {"TPA", ps.CareerHighs[PAbbr.TPA].ToString()},
                                 {"FTM", ps.CareerHighs[PAbbr.FTM].ToString()},
                                 {"FTA", ps.CareerHighs[PAbbr.FTA].ToString()},
                                 {"REB", ps.CareerHighs[PAbbr.REB].ToString()},
                                 {"OREB", ps.CareerHighs[PAbbr.OREB].ToString()},
                                 {"DREB", ps.CareerHighs[PAbbr.DREB].ToString()},
                                 {"STL", ps.CareerHighs[PAbbr.STL].ToString()},
                                 {"TOS", ps.CareerHighs[PAbbr.TOS].ToString()},
                                 {"BLK", ps.CareerHighs[PAbbr.BLK].ToString()},
                                 {"AST", ps.CareerHighs[PAbbr.AST].ToString()},
                                 {"FOUL", ps.CareerHighs[PAbbr.FOUL].ToString()}
                             };

                sqlinsert.Add(dict);
                plSqlinsert.Add(plDict);
                chSqlinsert.Add(chDict);
                i++;
            }

            if (i > 0)
            {
                db.InsertManyTransaction(playersT, sqlinsert);
                db.InsertManyTransaction(plPlayersT, plSqlinsert);
                db.InsertManyTransaction("CareerHighs", chSqlinsert);
            }
        }

        public static void SavePastTeamStatsToDatabase(SQLiteDatabase db, List<PastTeamStats> statsList)
        {
            int teamID;
            try
            {
                teamID = statsList[0].TeamID;
            }
            catch
            {
                return;
            }

            db.Delete("PastTeamStats", "TeamID = " + teamID);

            var sqlinsert = new List<Dictionary<string, string>>();
            var usedIDs = new List<int>();
            foreach (var pts in statsList)
            {
                int idToUse = GetFreeID(MainWindow.CurrentDB, "PastTeamStats", used: usedIDs);
                usedIDs.Add(idToUse);
                var dict = new Dictionary<string, string>
                           {
                               {"ID", idToUse.ToString()},
                               {"TeamID", pts.TeamID.ToString()},
                               {"SeasonName", pts.SeasonName},
                               {"SOrder", pts.Order.ToString()},
                               {"isPlayoff", pts.IsPlayoff.ToString()},
                               {"WIN", pts.Wins.ToString()},
                               {"LOSS", pts.Losses.ToString()},
                               {"MINS", pts.MINS.ToString()},
                               {"PF", pts.PF.ToString()},
                               {"PA", pts.PA.ToString()},
                               {"FGM", pts.FGM.ToString()},
                               {"FGA", pts.FGA.ToString()},
                               {"TPM", pts.TPM.ToString()},
                               {"TPA", pts.TPA.ToString()},
                               {"FTM", pts.FTM.ToString()},
                               {"FTA", pts.FTA.ToString()},
                               {"OREB", pts.OREB.ToString()},
                               {"DREB", pts.DREB.ToString()},
                               {"STL", pts.STL.ToString()},
                               {"TOS", pts.TOS.ToString()},
                               {"BLK", pts.BLK.ToString()},
                               {"AST", pts.AST.ToString()},
                               {"FOUL", pts.FOUL.ToString()},
                           };
                sqlinsert.Add(dict);
            }
            db.InsertManyTransaction("PastTeamStats", sqlinsert);
        }

        public static void SavePastPlayerStatsToDatabase(SQLiteDatabase db, List<PastPlayerStats> statsList)
        {
            statsList.GroupBy(stat => stat.PlayerID)
                     .Select(pair => pair.Key)
                     .ToList()
                     .ForEach(playerID => db.Delete("PastPlayerStats", "PlayerID = " + playerID));

            var sqlinsert = new List<Dictionary<string, string>>();
            var usedIDs = new List<int>();
            foreach (var pps in statsList)
            {
                int idToUse = GetFreeID(MainWindow.CurrentDB, "PastPlayerStats", used: usedIDs);
                usedIDs.Add(idToUse);
                var dict = new Dictionary<string, string>
                           {
                               {"ID", idToUse.ToString()},
                               {"PlayerID", pps.PlayerID.ToString()},
                               {"SeasonName", pps.SeasonName},
                               {"SOrder", pps.Order.ToString()},
                               {"isPlayoff", pps.IsPlayoff.ToString()},
                               {"TeamFin", pps.TeamFName},
                               {"TeamSta", pps.TeamSName},
                               {"GP", pps.GP.ToString()},
                               {"GS", pps.GS.ToString()},
                               {"MINS", pps.MINS.ToString()},
                               {"PTS", pps.PTS.ToString()},
                               {"FGM", pps.FGM.ToString()},
                               {"FGA", pps.FGA.ToString()},
                               {"TPM", pps.TPM.ToString()},
                               {"TPA", pps.TPA.ToString()},
                               {"FTM", pps.FTM.ToString()},
                               {"FTA", pps.FTA.ToString()},
                               {"OREB", pps.OREB.ToString()},
                               {"DREB", pps.DREB.ToString()},
                               {"STL", pps.STL.ToString()},
                               {"TOS", pps.TOS.ToString()},
                               {"BLK", pps.BLK.ToString()},
                               {"AST", pps.AST.ToString()},
                               {"FOUL", pps.FOUL.ToString()},
                           };
                sqlinsert.Add(dict);
            }
            db.InsertManyTransaction("PastPlayerStats", sqlinsert);
        }

        /// <summary>
        ///     Prepares a new DB, or adds a new season to a pre-existing database.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="curSeason">The current season ID.</param>
        /// <param name="maxSeason">The max season ID.</param>
        /// <param name="onlyNewSeason">
        ///     if set to <c>true</c>, a new season will be added to a pre-existing database.
        /// </param>
        public static void PrepareNewDB(SQLiteDatabase db, int curSeason, int maxSeason, bool onlyNewSeason = false)
        {
            String qr;

            if (!onlyNewSeason)
            {
                qr = @"DROP TABLE IF EXISTS ""GameResults""";
                db.ExecuteNonQuery(qr);
                qr =
                    @"CREATE TABLE ""GameResults"" (""GameID"" INTEGER PRIMARY KEY NOT NULL ,""Team1ID"" INTEGER NOT NULL ,""Team2ID"" INTEGER NOT NULL, ""Date"" DATE NOT NULL ,""SeasonNum"" INTEGER NOT NULL ,""IsPlayoff"" TEXT NOT NULL DEFAULT ('FALSE') ,""T1PTS"" INTEGER NOT NULL ,""T1REB"" INTEGER NOT NULL ,""T1AST"" INTEGER NOT NULL ,""T1STL"" INTEGER NOT NULL ,""T1BLK"" INTEGER NOT NULL ,""T1TOS"" INTEGER NOT NULL ,""T1FGM"" INTEGER NOT NULL ,""T1FGA"" INTEGER NOT NULL ,""T13PM"" INTEGER NOT NULL ,""T13PA"" INTEGER NOT NULL ,""T1FTM"" INTEGER NOT NULL ,""T1FTA"" INTEGER NOT NULL ,""T1OREB"" INTEGER NOT NULL ,""T1FOUL"" INTEGER NOT NULL,""T1MINS"" INTEGER NOT NULL ,""T2PTS"" INTEGER NOT NULL ,""T2REB"" INTEGER NOT NULL ,""T2AST"" INTEGER NOT NULL ,""T2STL"" INTEGER NOT NULL ,""T2BLK"" INTEGER NOT NULL ,""T2TOS"" INTEGER NOT NULL ,""T2FGM"" INTEGER NOT NULL ,""T2FGA"" INTEGER NOT NULL ,""T23PM"" INTEGER NOT NULL ,""T23PA"" INTEGER NOT NULL ,""T2FTM"" INTEGER NOT NULL ,""T2FTA"" INTEGER NOT NULL ,""T2OREB"" INTEGER NOT NULL ,""T2FOUL"" INTEGER NOT NULL,""T2MINS"" INTEGER NOT NULL, ""HASH"" TEXT )";
                db.ExecuteNonQuery(qr);
                qr = @"DROP TABLE IF EXISTS ""PlayerResults""";
                db.ExecuteNonQuery(qr);
                qr =
                    @"CREATE TABLE ""PlayerResults"" (""GameID"" INTEGER NOT NULL ,""PlayerID"" INTEGER NOT NULL ,""TeamID"" INTEGER NOT NULL ,""isStarter"" TEXT, ""playedInjured"" TEXT, ""isOut"" TEXT, ""MINS"" INTEGER NOT NULL DEFAULT (0), ""PTS"" INTEGER NOT NULL ,""REB"" INTEGER NOT NULL ,""AST"" INTEGER NOT NULL ,""STL"" INTEGER NOT NULL ,""BLK"" INTEGER NOT NULL ,""TOS"" INTEGER NOT NULL ,""FGM"" INTEGER NOT NULL ,""FGA"" INTEGER NOT NULL ,""TPM"" INTEGER NOT NULL ,""TPA"" INTEGER NOT NULL ,""FTM"" INTEGER NOT NULL ,""FTA"" INTEGER NOT NULL ,""OREB"" INTEGER NOT NULL ,""FOUL"" INTEGER NOT NULL DEFAULT (0), PRIMARY KEY (""GameID"", ""PlayerID"") )";
                db.ExecuteNonQuery(qr);
                qr = @"DROP TABLE IF EXISTS ""Misc""";
                db.ExecuteNonQuery(qr);
                qr = @"CREATE TABLE ""Misc"" (""Setting"" TEXT PRIMARY KEY,""Value"" TEXT)";
                db.ExecuteNonQuery(qr);
                qr = @"DROP TABLE IF EXISTS ""SeasonNames""";
                db.ExecuteNonQuery(qr);
                qr = @"CREATE TABLE ""SeasonNames"" (""ID"" INTEGER PRIMARY KEY NOT NULL , ""Name"" TEXT)";
                db.ExecuteNonQuery(qr);
                db.Insert("SeasonNames", new Dictionary<string, string> {{"ID", curSeason.ToString()}, {"Name", curSeason.ToString()}});
                qr = @"DROP TABLE IF EXISTS ""Divisions""";
                db.ExecuteNonQuery(qr);
                qr = @"CREATE TABLE ""Divisions"" (""ID"" INTEGER PRIMARY KEY NOT NULL , ""Name"" TEXT, ""Conference"" INTEGER)";
                db.ExecuteNonQuery(qr);
                db.Insert("Divisions", new Dictionary<string, string> {{"ID", "0"}, {"Name", "League"}, {"Conference", "0"}});
                qr = @"DROP TABLE IF EXISTS ""Conferences""";
                db.ExecuteNonQuery(qr);
                qr = @"CREATE TABLE ""Conferences"" (""ID"" INTEGER PRIMARY KEY NOT NULL , ""Name"" TEXT)";
                db.ExecuteNonQuery(qr);
                db.Insert("Conferences", new Dictionary<string, string> {{"ID", "0"}, {"Name", "League"}});
                qr = @"DROP TABLE IF EXISTS ""CareerHighs""";
                db.ExecuteNonQuery(qr);
                qr = CreateCareerHighsQuery;
                db.ExecuteNonQuery(qr);

                createPastPlayerAndTeamStatsTables(db);
            }
            string teamsT = "Teams";
            string plTeamsT = "PlayoffTeams";
            string oppT = "Opponents";
            string plOppT = "PlayoffOpponents";
            string playersT = "Players";
            string plPlayersT = "PlayoffPlayers";
            if (curSeason != maxSeason)
            {
                string s = "S" + curSeason.ToString();
                teamsT += s;
                plTeamsT += s;
                oppT += s;
                plOppT += s;
                playersT += s;
                plPlayersT += s;
            }
            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", plTeamsT);
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY NOT NULL ,""Name"" TEXT NOT NULL ,""DisplayName"" TEXT NOT NULL,""isHidden"" TEXT NOT NULL, ""Division"" INTEGER, ""Conference"" INTEGER, ""WIN"" INTEGER NOT NULL ,""LOSS"" INTEGER NOT NULL ,""MINS"" INTEGER, ""PF"" INTEGER NOT NULL ,""PA"" INTEGER NOT NULL ,""FGM"" INTEGER NOT NULL ,""FGA"" INTEGER NOT NULL ,""TPM"" INTEGER NOT NULL ,""TPA"" INTEGER NOT NULL ,""FTM"" INTEGER NOT NULL ,""FTA"" INTEGER NOT NULL ,""OREB"" INTEGER NOT NULL ,""DREB"" INTEGER NOT NULL ,""STL"" INTEGER NOT NULL ,""TOS"" INTEGER NOT NULL ,""BLK"" INTEGER NOT NULL ,""AST"" INTEGER NOT NULL ,""FOUL"" INTEGER NOT NULL ,""OFFSET"" INTEGER)",
                    plTeamsT);
            db.ExecuteNonQuery(qr);

            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", teamsT);
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY NOT NULL ,""Name"" TEXT NOT NULL ,""DisplayName"" TEXT NOT NULL,""isHidden"" TEXT NOT NULL, ""Division"" INTEGER, ""Conference"" INTEGER, ""WIN"" INTEGER NOT NULL ,""LOSS"" INTEGER NOT NULL ,""MINS"" INTEGER, ""PF"" INTEGER NOT NULL ,""PA"" INTEGER NOT NULL ,""FGM"" INTEGER NOT NULL ,""FGA"" INTEGER NOT NULL ,""TPM"" INTEGER NOT NULL ,""TPA"" INTEGER NOT NULL ,""FTM"" INTEGER NOT NULL ,""FTA"" INTEGER NOT NULL ,""OREB"" INTEGER NOT NULL ,""DREB"" INTEGER NOT NULL ,""STL"" INTEGER NOT NULL ,""TOS"" INTEGER NOT NULL ,""BLK"" INTEGER NOT NULL ,""AST"" INTEGER NOT NULL ,""FOUL"" INTEGER NOT NULL ,""OFFSET"" INTEGER)",
                    teamsT);
            db.ExecuteNonQuery(qr);

            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", plOppT);
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY NOT NULL ,""Name"" TEXT NOT NULL ,""DisplayName"" TEXT NOT NULL,""isHidden"" TEXT NOT NULL, ""Division"" INTEGER, ""Conference"" INTEGER, ""WIN"" INTEGER NOT NULL ,""LOSS"" INTEGER NOT NULL ,""MINS"" INTEGER, ""PF"" INTEGER NOT NULL ,""PA"" INTEGER NOT NULL ,""FGM"" INTEGER NOT NULL ,""FGA"" INTEGER NOT NULL ,""TPM"" INTEGER NOT NULL ,""TPA"" INTEGER NOT NULL ,""FTM"" INTEGER NOT NULL ,""FTA"" INTEGER NOT NULL ,""OREB"" INTEGER NOT NULL ,""DREB"" INTEGER NOT NULL ,""STL"" INTEGER NOT NULL ,""TOS"" INTEGER NOT NULL ,""BLK"" INTEGER NOT NULL ,""AST"" INTEGER NOT NULL ,""FOUL"" INTEGER NOT NULL ,""OFFSET"" INTEGER)",
                    plOppT);
            db.ExecuteNonQuery(qr);

            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", oppT);
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY NOT NULL ,""Name"" TEXT NOT NULL ,""DisplayName"" TEXT NOT NULL,""isHidden"" TEXT NOT NULL, ""Division"" INTEGER, ""Conference"" INTEGER, ""WIN"" INTEGER NOT NULL ,""LOSS"" INTEGER NOT NULL ,""MINS"" INTEGER, ""PF"" INTEGER NOT NULL ,""PA"" INTEGER NOT NULL ,""FGM"" INTEGER NOT NULL ,""FGA"" INTEGER NOT NULL ,""TPM"" INTEGER NOT NULL ,""TPA"" INTEGER NOT NULL ,""FTM"" INTEGER NOT NULL ,""FTA"" INTEGER NOT NULL ,""OREB"" INTEGER NOT NULL ,""DREB"" INTEGER NOT NULL ,""STL"" INTEGER NOT NULL ,""TOS"" INTEGER NOT NULL ,""BLK"" INTEGER NOT NULL ,""AST"" INTEGER NOT NULL ,""FOUL"" INTEGER NOT NULL ,""OFFSET"" INTEGER)",
                    oppT);
            db.ExecuteNonQuery(qr);

            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", playersT);
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY NOT NULL ,""LastName"" TEXT NOT NULL ,""FirstName"" TEXT NOT NULL ,""Position1"" TEXT,""Position2"" TEXT,""isActive"" TEXT,""YearOfBirth"" INTEGER,""YearsPro"" INTEGER, ""isHidden"" TEXT,""InjuryType"" INTEGER, ""CustomInjuryName"" TEXT, ""InjuryDaysLeft"" INTEGER,""TeamFin"" INTEGER,""TeamSta"" INTEGER,""GP"" INTEGER,""GS"" INTEGER,""MINS"" INTEGER NOT NULL DEFAULT (0) ,""PTS"" INTEGER NOT NULL ,""FGM"" INTEGER NOT NULL ,""FGA"" INTEGER NOT NULL ,""TPM"" INTEGER NOT NULL ,""TPA"" INTEGER NOT NULL ,""FTM"" INTEGER NOT NULL ,""FTA"" INTEGER NOT NULL ,""OREB"" INTEGER NOT NULL ,""DREB"" INTEGER NOT NULL ,""STL"" INTEGER NOT NULL ,""TOS"" INTEGER NOT NULL ,""BLK"" INTEGER NOT NULL ,""AST"" INTEGER NOT NULL ,""FOUL"" INTEGER NOT NULL ,""isAllStar"" TEXT,""isNBAChampion"" TEXT, ""ContractY1"" INTEGER, ""ContractY2"" INTEGER, ""ContractY3"" INTEGER, ""ContractY4"" INTEGER, ""ContractY5"" INTEGER, ""ContractY6"" INTEGER, ""ContractY7"" INTEGER, ""ContractOption"" TEXT, ""Height"" REAL, ""Weight"" REAL)",
                    playersT);
            db.ExecuteNonQuery(qr);

            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", plPlayersT);
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY NOT NULL ,""GP"" INTEGER,""GS"" INTEGER,""MINS"" INTEGER NOT NULL DEFAULT (0) ,""PTS"" INTEGER NOT NULL ,""FGM"" INTEGER NOT NULL ,""FGA"" INTEGER NOT NULL ,""TPM"" INTEGER NOT NULL ,""TPA"" INTEGER NOT NULL ,""FTM"" INTEGER NOT NULL ,""FTA"" INTEGER NOT NULL ,""OREB"" INTEGER NOT NULL ,""DREB"" INTEGER NOT NULL ,""STL"" INTEGER NOT NULL ,""TOS"" INTEGER NOT NULL ,""BLK"" INTEGER NOT NULL ,""AST"" INTEGER NOT NULL ,""FOUL"" INTEGER NOT NULL)",
                    plPlayersT);
            db.ExecuteNonQuery(qr);
        }

        private static void createPastPlayerAndTeamStatsTables(SQLiteDatabase db)
        {
            string qr;
            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", "PastPlayerStats");
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY , ""PlayerID"" INTEGER, ""SeasonName"" TEXT, ""SOrder"" TEXT, ""isPlayoff"" TEXT , ""TeamFin"" TEXT,""TeamSta"" TEXT,""GP"" INTEGER,""GS"" INTEGER,""MINS"" INTEGER  DEFAULT (0) ,""PTS"" INTEGER  ,""FGM"" INTEGER  ,""FGA"" INTEGER  ,""TPM"" INTEGER  ,""TPA"" INTEGER  ,""FTM"" INTEGER  ,""FTA"" INTEGER  ,""OREB"" INTEGER  ,""DREB"" INTEGER  ,""STL"" INTEGER  ,""TOS"" INTEGER  ,""BLK"" INTEGER  ,""AST"" INTEGER  ,""FOUL"" INTEGER)",
                    "PastPlayerStats");
            db.ExecuteNonQuery(qr);

            qr = string.Format(@"DROP TABLE IF EXISTS ""{0}""", "PastTeamStats");
            db.ExecuteNonQuery(qr);
            qr =
                string.Format(
                    @"CREATE TABLE ""{0}"" (""ID"" INTEGER PRIMARY KEY  , ""TeamID"" INTEGER, ""SeasonName"" TEXT, ""SOrder"" TEXT, ""isPlayoff"" TEXT , ""WIN"" INTEGER  ,""LOSS"" INTEGER  ,""MINS"" INTEGER, ""PF"" INTEGER  ,""PA"" INTEGER  ,""FGM"" INTEGER  ,""FGA"" INTEGER  ,""TPM"" INTEGER  ,""TPA"" INTEGER  ,""FTM"" INTEGER  ,""FTA"" INTEGER  ,""OREB"" INTEGER  ,""DREB"" INTEGER  ,""STL"" INTEGER  ,""TOS"" INTEGER  ,""BLK"" INTEGER  ,""AST"" INTEGER  ,""FOUL"" INTEGER)",
                    "PastTeamStats");
            db.ExecuteNonQuery(qr);
        }

        /// <summary>
        ///     Gets the max season ID in a database.
        /// </summary>
        /// <param name="file">The database.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">The database requested doesn't exist.</exception>
        public static int GetMaxSeason(string file)
        {
            try
            {
                if (!File.Exists(file))
                    throw (new Exception("The database requested doesn't exist."));

                var db = new SQLiteDatabase(file);

                const string q = "select Name from sqlite_master";
                DataTable res = db.GetDataTable(q);

                int maxseason = (from DataRow r in res.Rows
                                 select r["Name"].ToString()
                                 into name where name.Length > 5 && name.Substring(0, 5) == "Teams"
                                 select Convert.ToInt32(name.Substring(6, 1))).Concat(new[] {0}).Max();

                maxseason++;

                return maxseason;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        ///     Sets a setting value in the current database.
        /// </summary>
        /// <typeparam name="T">The type of value to save.</typeparam>
        /// <param name="setting">The setting.</param>
        /// <param name="value">The value.</param>
        public static void SetSetting<T>(string setting, T value)
        {
            SetSetting(MainWindow.CurrentDB, setting, value);
        }

        /// <summary>
        ///     Sets a setting value in the specified database.
        /// </summary>
        /// <typeparam name="T">The type of value to save.</typeparam>
        /// <param name="file">The file.</param>
        /// <param name="setting">The setting.</param>
        /// <param name="value">The value.</param>
        public static void SetSetting<T>(string file, string setting, T value)
        {
            var db = new SQLiteDatabase(file);

            string val = value.ToString();
            string q = "select * from Misc where Setting LIKE \"" + setting + "\"";

            int rowCount = db.GetDataTable(q).Rows.Count;

            if (rowCount == 1)
            {
                db.Update("Misc", new Dictionary<string, string> {{"Value", val}}, "Setting LIKE \"" + setting + "\"");
            }
            else
            {
                db.Insert("Misc", new Dictionary<string, string> {{"Setting", setting}, {"Value", val}});
            }
        }

        /// <summary>
        ///     Gets a setting value from the current database.
        /// </summary>
        /// <typeparam name="T">The type of value to get.</typeparam>
        /// <param name="setting">The setting.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static T GetSetting<T>(string setting, T defaultValue)
        {
            return GetSetting(MainWindow.CurrentDB, setting, defaultValue);
        }

        /// <summary>
        ///     Gets a setting value from the specified database.
        /// </summary>
        /// <typeparam name="T">The type of value to get.</typeparam>
        /// <param name="file">The file.</param>
        /// <param name="setting">The setting.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static T GetSetting<T>(string file, string setting, T defaultValue)
        {
            var db = new SQLiteDatabase(file);

            string q = "select Value from Misc where Setting LIKE \"" + setting + "\"";
            string value = db.ExecuteScalar(q);

            if (String.IsNullOrEmpty(value))
                return defaultValue;

            return (T) Convert.ChangeType(value, typeof (T));
        }

        /// <summary>
        ///     Gets the team stats from a specified database.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="teamID">The team.</param>
        /// <param name="season">The season.</param>
        /// <param name="ts">The resulting team stats.</param>
        /// <param name="tsopp">The resulting opposing team stats.</param>
        public static void GetTeamStatsFromDatabase(string file, int teamID, int season, out TeamStats ts, out TeamStats tsopp)
        {
            var db = new SQLiteDatabase(file);

            String q;
            int maxSeason = GetMaxSeason(file);

            if (season == 0)
                season = maxSeason;

            if (maxSeason == season)
            {
                q = "select * from Teams where ID = " + teamID;
            }
            else
            {
                q = "select * from TeamsS" + season.ToString() + " where ID = " + teamID;
            }

            DataTable res = db.GetDataTable(q);

            ts = new TeamStats();

            DataRow r = res.Rows[0];
            ts = new TeamStats(teamID, r["Name"].ToString());

            // For compatibility reasons, if properties added after v0.10.5.1 don't exist,
            // we create them without error.
            try
            {
                ts.DisplayName = r["DisplayName"].ToString();
            }
            catch (Exception)
            {
                ts.DisplayName = ts.Name;
            }

            try
            {
                ts.IsHidden = DataRowCellParsers.GetBoolean(r, "isHidden");
            }
            catch (Exception)
            {
                ts.IsHidden = false;
            }

            try
            {
                ts.Division = DataRowCellParsers.GetInt32(r, "Division");
            }
            catch (Exception)
            {
                ts.Division = 0;
            }

            ts.Offset = Convert.ToInt32(r["OFFSET"].ToString());

            GetTeamStatsFromDataRow(ref ts, r);


            if (maxSeason == season)
            {
                q = "select * from PlayoffTeams where ID = " + teamID;
            }
            else
            {
                q = "select * from PlayoffTeamsS" + season.ToString() + " where ID = " + teamID;
            }
            res = db.GetDataTable(q);

            r = res.Rows[0];
            ts.PlOffset = Convert.ToInt32(r["OFFSET"].ToString());
            ts.PlRecord[0] = Convert.ToByte(r["WIN"].ToString());
            ts.PlRecord[1] = Convert.ToByte(r["LOSS"].ToString());
            ts.PlTotals[TAbbr.MINS] = Convert.ToUInt16(r["MINS"].ToString());
            ts.PlTotals[TAbbr.PF] = Convert.ToUInt16(r["PF"].ToString());
            ts.PlTotals[TAbbr.PA] = Convert.ToUInt16(r["PA"].ToString());
            ts.PlTotals[TAbbr.FGM] = Convert.ToUInt16(r["FGM"].ToString());
            ts.PlTotals[TAbbr.FGA] = Convert.ToUInt16(r["FGA"].ToString());
            ts.PlTotals[TAbbr.TPM] = Convert.ToUInt16(r["TPM"].ToString());
            ts.PlTotals[TAbbr.TPA] = Convert.ToUInt16(r["TPA"].ToString());
            ts.PlTotals[TAbbr.FTM] = Convert.ToUInt16(r["FTM"].ToString());
            ts.PlTotals[TAbbr.FTA] = Convert.ToUInt16(r["FTA"].ToString());
            ts.PlTotals[TAbbr.OREB] = Convert.ToUInt16(r["OREB"].ToString());
            ts.PlTotals[TAbbr.DREB] = Convert.ToUInt16(r["DREB"].ToString());
            ts.PlTotals[TAbbr.STL] = Convert.ToUInt16(r["STL"].ToString());
            ts.PlTotals[TAbbr.TOS] = Convert.ToUInt16(r["TOS"].ToString());
            ts.PlTotals[TAbbr.BLK] = Convert.ToUInt16(r["BLK"].ToString());
            ts.PlTotals[TAbbr.AST] = Convert.ToUInt16(r["AST"].ToString());
            ts.PlTotals[TAbbr.FOUL] = Convert.ToUInt16(r["FOUL"].ToString());

            ts.CalcAvg();

            if (maxSeason == season)
            {
                q = "select * from Opponents where ID = " + teamID;
            }
            else
            {
                q = "select * from OpponentsS" + season.ToString() + " where ID = " + teamID;
            }

            res = db.GetDataTable(q);

            r = res.Rows[0];
            tsopp = new TeamStats(teamID, r["Name"].ToString());

            // For compatibility reasons, if properties added after v0.10.5.1 don't exist,
            // we create them without error.

            try
            {
                tsopp.DisplayName = r["DisplayName"].ToString();
            }
            catch (Exception)
            {
                tsopp.DisplayName = tsopp.Name;
            }

            try
            {
                tsopp.IsHidden = DataRowCellParsers.GetBoolean(r, "isHidden");
            }
            catch (Exception)
            {
                tsopp.IsHidden = false;
            }

            try
            {
                tsopp.Division = DataRowCellParsers.GetInt32(r, "Division");
            }
            catch (Exception)
            {
                tsopp.Division = 0;
            }

            tsopp.Offset = Convert.ToInt32(r["OFFSET"].ToString());
            tsopp.Record[0] = Convert.ToByte(r["WIN"].ToString());
            tsopp.Record[1] = Convert.ToByte(r["LOSS"].ToString());
            tsopp.Totals[TAbbr.MINS] = Convert.ToUInt16(r["MINS"].ToString());
            tsopp.Totals[TAbbr.PF] = Convert.ToUInt16(r["PF"].ToString());
            tsopp.Totals[TAbbr.PA] = Convert.ToUInt16(r["PA"].ToString());
            tsopp.Totals[TAbbr.FGM] = Convert.ToUInt16(r["FGM"].ToString());
            tsopp.Totals[TAbbr.FGA] = Convert.ToUInt16(r["FGA"].ToString());
            tsopp.Totals[TAbbr.TPM] = Convert.ToUInt16(r["TPM"].ToString());
            tsopp.Totals[TAbbr.TPA] = Convert.ToUInt16(r["TPA"].ToString());
            tsopp.Totals[TAbbr.FTM] = Convert.ToUInt16(r["FTM"].ToString());
            tsopp.Totals[TAbbr.FTA] = Convert.ToUInt16(r["FTA"].ToString());
            tsopp.Totals[TAbbr.OREB] = Convert.ToUInt16(r["OREB"].ToString());
            tsopp.Totals[TAbbr.DREB] = Convert.ToUInt16(r["DREB"].ToString());
            tsopp.Totals[TAbbr.STL] = Convert.ToUInt16(r["STL"].ToString());
            tsopp.Totals[TAbbr.TOS] = Convert.ToUInt16(r["TOS"].ToString());
            tsopp.Totals[TAbbr.BLK] = Convert.ToUInt16(r["BLK"].ToString());
            tsopp.Totals[TAbbr.AST] = Convert.ToUInt16(r["AST"].ToString());
            tsopp.Totals[TAbbr.FOUL] = Convert.ToUInt16(r["FOUL"].ToString());

            if (maxSeason == season)
            {
                q = "select * from PlayoffOpponents where ID = " + teamID;
            }
            else
            {
                q = "select * from PlayoffOpponentsS" + season.ToString() + " where ID = " + teamID;
            }
            res = db.GetDataTable(q);

            r = res.Rows[0];
            tsopp.PlOffset = Convert.ToInt32(r["OFFSET"].ToString());
            tsopp.PlRecord[0] = Convert.ToByte(r["WIN"].ToString());
            tsopp.PlRecord[1] = Convert.ToByte(r["LOSS"].ToString());
            tsopp.PlTotals[TAbbr.MINS] = Convert.ToUInt16(r["MINS"].ToString());
            tsopp.PlTotals[TAbbr.PF] = Convert.ToUInt16(r["PF"].ToString());
            tsopp.PlTotals[TAbbr.PA] = Convert.ToUInt16(r["PA"].ToString());
            tsopp.PlTotals[TAbbr.FGM] = Convert.ToUInt16(r["FGM"].ToString());
            tsopp.PlTotals[TAbbr.FGA] = Convert.ToUInt16(r["FGA"].ToString());
            tsopp.PlTotals[TAbbr.TPM] = Convert.ToUInt16(r["TPM"].ToString());
            tsopp.PlTotals[TAbbr.TPA] = Convert.ToUInt16(r["TPA"].ToString());
            tsopp.PlTotals[TAbbr.FTM] = Convert.ToUInt16(r["FTM"].ToString());
            tsopp.PlTotals[TAbbr.FTA] = Convert.ToUInt16(r["FTA"].ToString());
            tsopp.PlTotals[TAbbr.OREB] = Convert.ToUInt16(r["OREB"].ToString());
            tsopp.PlTotals[TAbbr.DREB] = Convert.ToUInt16(r["DREB"].ToString());
            tsopp.PlTotals[TAbbr.STL] = Convert.ToUInt16(r["STL"].ToString());
            tsopp.PlTotals[TAbbr.TOS] = Convert.ToUInt16(r["TOS"].ToString());
            tsopp.PlTotals[TAbbr.BLK] = Convert.ToUInt16(r["BLK"].ToString());
            tsopp.PlTotals[TAbbr.AST] = Convert.ToUInt16(r["AST"].ToString());
            tsopp.PlTotals[TAbbr.FOUL] = Convert.ToUInt16(r["FOUL"].ToString());

            tsopp.CalcAvg();
        }

        public static void GetTeamStatsFromDataRow(ref TeamStats ts, DataRow r, bool isPlayoff = false)
        {
            ts.ID = Convert.ToInt32(r["ID"].ToString());
            if (!isPlayoff)
            {
                ts.Record[0] = Convert.ToByte(r["WIN"].ToString());
                ts.Record[1] = Convert.ToByte(r["LOSS"].ToString());
                ts.Totals[TAbbr.MINS] = Convert.ToUInt16(r["MINS"].ToString());
                ts.Totals[TAbbr.PF] = Convert.ToUInt16(r["PF"].ToString());
                ts.Totals[TAbbr.PA] = Convert.ToUInt16(r["PA"].ToString());
                ts.Totals[TAbbr.FGM] = Convert.ToUInt16(r["FGM"].ToString());
                ts.Totals[TAbbr.FGA] = Convert.ToUInt16(r["FGA"].ToString());
                ts.Totals[TAbbr.TPM] = Convert.ToUInt16(r["TPM"].ToString());
                ts.Totals[TAbbr.TPA] = Convert.ToUInt16(r["TPA"].ToString());
                ts.Totals[TAbbr.FTM] = Convert.ToUInt16(r["FTM"].ToString());
                ts.Totals[TAbbr.FTA] = Convert.ToUInt16(r["FTA"].ToString());
                ts.Totals[TAbbr.OREB] = Convert.ToUInt16(r["OREB"].ToString());
                ts.Totals[TAbbr.DREB] = Convert.ToUInt16(r["DREB"].ToString());
                ts.Totals[TAbbr.STL] = Convert.ToUInt16(r["STL"].ToString());
                ts.Totals[TAbbr.TOS] = Convert.ToUInt16(r["TOS"].ToString());
                ts.Totals[TAbbr.BLK] = Convert.ToUInt16(r["BLK"].ToString());
                ts.Totals[TAbbr.AST] = Convert.ToUInt16(r["AST"].ToString());
                ts.Totals[TAbbr.FOUL] = Convert.ToUInt16(r["FOUL"].ToString());
            }
            else
            {
                ts.PlRecord[0] = Convert.ToByte(r["WIN"].ToString());
                ts.PlRecord[1] = Convert.ToByte(r["LOSS"].ToString());
                ts.PlTotals[TAbbr.MINS] = Convert.ToUInt16(r["MINS"].ToString());
                ts.PlTotals[TAbbr.PF] = Convert.ToUInt16(r["PF"].ToString());
                ts.PlTotals[TAbbr.PA] = Convert.ToUInt16(r["PA"].ToString());
                ts.PlTotals[TAbbr.FGM] = Convert.ToUInt16(r["FGM"].ToString());
                ts.PlTotals[TAbbr.FGA] = Convert.ToUInt16(r["FGA"].ToString());
                ts.PlTotals[TAbbr.TPM] = Convert.ToUInt16(r["TPM"].ToString());
                ts.PlTotals[TAbbr.TPA] = Convert.ToUInt16(r["TPA"].ToString());
                ts.PlTotals[TAbbr.FTM] = Convert.ToUInt16(r["FTM"].ToString());
                ts.PlTotals[TAbbr.FTA] = Convert.ToUInt16(r["FTA"].ToString());
                ts.PlTotals[TAbbr.OREB] = Convert.ToUInt16(r["OREB"].ToString());
                ts.PlTotals[TAbbr.DREB] = Convert.ToUInt16(r["DREB"].ToString());
                ts.PlTotals[TAbbr.STL] = Convert.ToUInt16(r["STL"].ToString());
                ts.PlTotals[TAbbr.TOS] = Convert.ToUInt16(r["TOS"].ToString());
                ts.PlTotals[TAbbr.BLK] = Convert.ToUInt16(r["BLK"].ToString());
                ts.PlTotals[TAbbr.AST] = Convert.ToUInt16(r["AST"].ToString());
                ts.PlTotals[TAbbr.FOUL] = Convert.ToUInt16(r["FOUL"].ToString());
            }
            ts.CalcAvg();
        }

        /// <summary>
        ///     Gets all team stats from the specified database.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="season">The season.</param>
        /// <param name="tst">The resulting team stats dictionary.</param>
        /// <param name="tstOpp">The resulting opposing team stats dictionary.</param>
        /// <param name="teamOrder">The resulting team order.</param>
        public static void GetAllTeamStatsFromDatabase(string file, int season, out Dictionary<int, TeamStats> tst,
                                                       out Dictionary<int, TeamStats> tstOpp, out SortedDictionary<string, int> teamOrder)
        {
            var db = new SQLiteDatabase(file);

            String q;
            int maxSeason = GetMaxSeason(file);

            if (season == 0)
                season = maxSeason;

            if (maxSeason == season)
            {
                q = "select ID from Teams;";
            }
            else
            {
                q = "select ID from TeamsS" + season.ToString() + ";";
            }

            DataTable res = db.GetDataTable(q);

            tst = new Dictionary<int, TeamStats>();
            tstOpp = new Dictionary<int, TeamStats>();
            teamOrder = new SortedDictionary<string, int>();
            int i = 0;

            foreach (DataRow r in res.Rows)
            {
                TeamStats ts;
                TeamStats tsopp;
                int teamID = DataRowCellParsers.GetInt32(r, "ID");
                GetTeamStatsFromDatabase(file, teamID, season, out ts, out tsopp);
                tst[i] = ts;
                tstOpp[i] = tsopp;
                teamOrder.Add(ts.Name, i);
                i++;
            }
        }

        /// <summary>
        ///     Default implementation of LoadSeason; calls LoadSeason to update MainWindow data
        /// </summary>
        public static void LoadSeason(int season = 0, bool doNotLoadBoxScores = false)
        {
            LoadSeason(MainWindow.CurrentDB, out MainWindow.TST, out MainWindow.TSTOpp, out MainWindow.PST, out MainWindow.TeamOrder,
                       ref MainWindow.BSHist, out MainWindow.SplitTeamStats, out MainWindow.SplitPlayerStats,
                       out MainWindow.SeasonTeamRankings, out MainWindow.SeasonPlayerRankings, out MainWindow.PlayoffTeamRankings,
                       out MainWindow.PlayoffPlayerRankings, out MainWindow.DisplayNames, season == 0 ? MainWindow.CurSeason : season,
                       doNotLoadBoxScores);
        }

        /// <summary>
        ///     Default implementation of LoadSeason; calls LoadSeason to update MainWindow data
        /// </summary>
        public static void LoadSeason(string file, int season = 0, bool doNotLoadBoxScores = false)
        {
            LoadSeason(file, out MainWindow.TST, out MainWindow.TSTOpp, out MainWindow.PST, out MainWindow.TeamOrder, ref MainWindow.BSHist,
                       out MainWindow.SplitTeamStats, out MainWindow.SplitPlayerStats, out MainWindow.SeasonTeamRankings,
                       out MainWindow.SeasonPlayerRankings, out MainWindow.PlayoffTeamRankings, out MainWindow.PlayoffPlayerRankings,
                       out MainWindow.DisplayNames, season == 0 ? MainWindow.CurSeason : season, doNotLoadBoxScores);
        }

        /// <summary>
        ///     Loads a specific season from the specified database.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="tst">The resulting team stats dictionary.</param>
        /// <param name="tstOpp">The resulting opposing team stats dictionary.</param>
        /// <param name="pst">The resulting player stats dictionary.</param>
        /// <param name="teamOrder">The resulting team order.</param>
        /// <param name="bsHist">The box score history container.</param>
        /// <param name="curSeason">The current season ID.</param>
        /// <param name="doNotLoadBoxScores">
        ///     if set to <c>true</c>, box scores will not be parsed.
        /// </param>
        public static void LoadSeason(string file, out Dictionary<int, TeamStats> tst, out Dictionary<int, TeamStats> tstOpp,
                                      out Dictionary<int, PlayerStats> pst, out SortedDictionary<string, int> teamOrder,
                                      ref List<BoxScoreEntry> bsHist, out Dictionary<int, Dictionary<string, TeamStats>> splitTeamStats,
                                      out Dictionary<int, Dictionary<string, PlayerStats>> splitPlayerStats, out TeamRankings teamRankings,
                                      out PlayerRankings playerRankings, out TeamRankings playoffTeamRankings,
                                      out PlayerRankings playoffPlayerRankings, out Dictionary<int, string> displayNames, int curSeason = 0,
                                      bool doNotLoadBoxScores = false)
        {
            MainWindow.LoadingSeason = true;

            bool mustSave = false;
            if (!_upgrading)
            {
                mustSave = checkIfDBNeedsUpgrade(file);
            }

            int maxSeason = GetMaxSeason(file);

            if (curSeason == 0)
            {
                curSeason = maxSeason;
                if (MainWindow.Tf.SeasonNum == 0)
                {
                    MainWindow.Tf.SeasonNum = maxSeason;
                }
            }

            LoadDivisionsAndConferences(file);

            MainWindow.Tf.SeasonNum = curSeason;
            if (mustSave)
            {
                GetAllTeamStatsFromDatabase(file, curSeason, out tst, out tstOpp, out teamOrder);

                pst = GetPlayersFromDatabase(file, tst, tstOpp, teamOrder, curSeason, maxSeason);

                if (!doNotLoadBoxScores)
                    bsHist = GetSeasonBoxScoresFromDatabase(file, curSeason, maxSeason, tst);

                splitTeamStats = null;
                splitPlayerStats = null;
                teamRankings = null;
                playerRankings = null;
                playoffTeamRankings = null;
                playoffPlayerRankings = null;
                displayNames = null;
            }
            else
            {
                PopulateAll(MainWindow.Tf, out tst, out tstOpp, out teamOrder, out pst, out splitTeamStats, out splitPlayerStats, out bsHist,
                            out teamRankings, out playerRankings, out playoffTeamRankings, out playoffPlayerRankings, out displayNames);
            }

            MainWindow.CurrentDB = file;

            MainWindow.ChangeSeason(curSeason);

            if (mustSave)
            {
                _upgrading = true;
                string backupName = Tools.GetFullPathWithoutExtension(file) + ".UpgradeBackup.tst";
                try
                {
                    File.Delete(backupName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't delete previous upgrade backup (if any): " + backupName);
                    Console.WriteLine(ex.Message);
                }
                try
                {
                    File.Copy(file, backupName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't copy old file to upgrade backup at " + backupName);
                    Console.WriteLine(ex.Message);
                }
                SaveDatabaseAs(file);
                File.Delete(backupName);
                _upgrading = false;
            }

            MainWindow.LoadingSeason = false;
        }

        /// <summary>
        ///     Loads the divisions and conferences.
        /// </summary>
        /// <param name="file">The file.</param>
        public static void LoadDivisionsAndConferences(string file)
        {
            var db = new SQLiteDatabase(file);

            /*
            string q = "SELECT Divisions.ID As DivID, Conferences.ID As ConfID, Divisions.Name As DivName, " +
            "Conferences.Name as ConfName, Divisions.Conference As DivConf FROM Divisions " +
            "INNER JOIN Conferences ON Conference = Conferences.ID";
            */
            string q = "SELECT * FROM Divisions";
            DataTable res = db.GetDataTable(q);

            MainWindow.Divisions.Clear();
            foreach (DataRow row in res.Rows)
            {
                MainWindow.Divisions.Add(new Division
                                         {
                                             ID = DataRowCellParsers.GetInt32(row, "ID"),
                                             Name = DataRowCellParsers.GetString(row, "Name"),
                                             ConferenceID = DataRowCellParsers.GetInt32(row, "Conference")
                                         });
            }

            q = "SELECT * FROM Conferences";
            res = db.GetDataTable(q);

            MainWindow.Conferences.Clear();
            foreach (DataRow row in res.Rows)
            {
                MainWindow.Conferences.Add(new Conference
                                           {
                                               ID = DataRowCellParsers.GetInt32(row, "ID"),
                                               Name = DataRowCellParsers.GetString(row, "Name")
                                           });
            }
        }

        /// <summary>
        ///     Checks for missing and changed fields in older databases and upgrades them to the current format.
        /// </summary>
        /// <param name="file">The path to the database.</param>
        private static bool checkIfDBNeedsUpgrade(string file)
        {
            var db = new SQLiteDatabase(file);

            bool mustSave = false;

            #region SeasonNames

            // Check for missing SeasonNames table (v0.11)

            string qr = "SELECT * FROM SeasonNames";
            DataTable dt;
            try
            {
                dt = db.GetDataTable(qr);
            }
            catch (Exception)
            {
                int maxSeason = GetMaxSeason(file);
                qr = "CREATE TABLE \"SeasonNames\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL , \"Name\" TEXT)";
                db.ExecuteNonQuery(qr);

                for (int i = 1; i <= maxSeason; i++)
                {
                    db.Insert("SeasonNames", new Dictionary<string, string> {{"ID", i.ToString()}, {"Name", i.ToString()}});
                }
            }

            #endregion

            #region PastPlayerAndTeamStats

            qr = "SELECT * FROM PastPlayerStats";
            try
            {
                dt = db.GetDataTable(qr);
            }
            catch (Exception)
            {
                createPastPlayerAndTeamStatsTables(db);
            }

            #endregion

            #region Misc

            qr = "SELECT * FROM sqlite_master WHERE name = \"Misc\"";
            dt = db.GetDataTable(qr);
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["name"].ToString() == "Misc")
                {
                    if (dr["sql"].ToString().Contains("CurSeason"))
                    {
                        qr = "DROP TABLE IF EXISTS \"Misc\"";
                        db.ExecuteNonQuery(qr);
                        qr = "CREATE TABLE \"Misc\" (\"Setting\" TEXT PRIMARY KEY,\"Value\" TEXT)";
                        db.ExecuteNonQuery(qr);
                    }
                    break;
                }
            }

            #endregion

            #region PlayerResults

            qr = "SELECT * FROM sqlite_master WHERE name = \"PlayerResults\"";
            dt = db.GetDataTable(qr);
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["name"].ToString() == "PlayerResults")
                {
                    if (dr["sql"].ToString().Contains("\"ID\""))
                    {
                        mustSave = true;
                    }
                    break;
                }
            }

            #endregion

            #region Teams

            #endregion

            #region Players

            qr = "SELECT * FROM sqlite_master WHERE name = \"Players\"";
            dt = db.GetDataTable(qr);
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["name"].ToString() == "Players")
                {
                    if (!dr["sql"].ToString().Contains("\"isHidden\""))
                    {
                        mustSave = true;
                    }
                    else if (!dr["sql"].ToString().Contains("\"YearOfBirth\""))
                    {
                        mustSave = true;
                        if (dr["sql"].ToString().Contains("\"Age\""))
                        {
                            var ibw =
                                new InputBoxWindow(
                                    "NBA Stats Tracker has replaced the 'Age' field for players with 'Year of Birth'.\n" +
                                    "Please enter the year by which all players' year of birth should be calculated.",
                                    DateTime.Now.Year.ToString());
                            if (ibw.ShowDialog() == false)
                            {
                                MainWindow.Input = DateTime.Now.Year.ToString();
                            }
                        }
                    }
                    else if (!dr["sql"].ToString().Contains("\"ContractY1\""))
                    {
                        mustSave = true;
                    }
                    else if (!dr["sql"].ToString().Contains("\"Height\""))
                    {
                        mustSave = true;
                    }
                    else if (dr["sql"].ToString().Contains("\"TeamFin\" TEXT"))
                    {
                        mustSave = true;
                    }
                    else if (dr["sql"].ToString().Contains("\"isInjured\""))
                    {
                        mustSave = true;
                    }
                    break;
                }
            }

            #endregion

            #region Playoff Players

            qr = "SELECT * FROM PlayoffPlayers";
            try
            {
                dt = db.GetDataTable(qr);
            }
            catch (Exception)
            {
                mustSave = true;
            }

            #endregion

            #region Divisions and Conferences

            qr = "SELECT * FROM sqlite_master WHERE name = \"Teams\"";
            try
            {
                dt = db.GetDataTable(qr);
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["name"].ToString() == "Teams")
                    {
                        if (!dr["sql"].ToString().Contains("\"Division\""))
                        {
                            mustSave = true;
                            qr = "DROP TABLE IF EXISTS \"Divisions\"";
                            db.ExecuteNonQuery(qr);
                            qr = "CREATE TABLE \"Divisions\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL , \"Name\" TEXT, \"Conference\" INTEGER)";
                            db.ExecuteNonQuery(qr);
                            db.Insert("Divisions", new Dictionary<string, string> {{"ID", "0"}, {"Name", "League"}, {"Conference", "0"}});
                            qr = "DROP TABLE IF EXISTS \"Conferences\"";
                            db.ExecuteNonQuery(qr);
                            qr = "CREATE TABLE \"Conferences\" (\"ID\" INTEGER PRIMARY KEY  NOT NULL , \"Name\" TEXT)";
                            db.ExecuteNonQuery(qr);
                            db.Insert("Conferences", new Dictionary<string, string> {{"ID", "0"}, {"Name", "League"}});
                        }
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }

            #endregion

            #region CareerHighs

            qr = "SELECT * FROM CareerHighs";
            try
            {
                db.GetDataTable(qr);
            }
            catch (Exception)
            {
                qr = CreateCareerHighsQuery;
                db.ExecuteNonQuery(qr);
            }

            #endregion

            return mustSave;
        }

        /// <summary>
        ///     Gets all box scores from the specified database.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        public static List<BoxScoreEntry> GetAllBoxScoresFromDatabase(string file, Dictionary<int, TeamStats> tst)
        {
            int maxSeason = GetMaxSeason(file);

            var bsHist = new List<BoxScoreEntry>();

            for (int i = maxSeason; i >= 1; i--)
            {
                List<BoxScoreEntry> temp = GetSeasonBoxScoresFromDatabase(MainWindow.CurrentDB, i, maxSeason, tst);

                bsHist.AddRange(temp);
            }

            return bsHist;
        }

        /// <summary>
        ///     Gets the season's box scores from the specified database.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="curSeason">The current season ID.</param>
        /// <param name="maxSeason">The max season ID.</param>
        /// <returns></returns>
        public static List<BoxScoreEntry> GetSeasonBoxScoresFromDatabase(string file, int curSeason, int maxSeason,
                                                                         Dictionary<int, TeamStats> tst)
        {
            var db = new SQLiteDatabase(file);

            string q = "select * from GameResults WHERE SeasonNum = " + curSeason + " ORDER BY Date DESC;";
            DataTable res2 = db.GetDataTable(q);

            string teamsT = "Teams";
            if (curSeason != maxSeason)
                teamsT += "S" + curSeason;

            DataTable res;
            var displayNames = new Dictionary<int, string>();
            try
            {
                q = "select ID, DisplayName from " + teamsT;
                res = db.GetDataTable(q);

                foreach (DataRow r in res.Rows)
                {
                    displayNames.Add(Convert.ToInt32(r["ID"].ToString()), r["DisplayName"].ToString());
                }
            }
            catch
            {
                q = "select ID, Name from " + teamsT;
                res = db.GetDataTable(q);

                foreach (DataRow r in res.Rows)
                {
                    displayNames.Add(Convert.ToInt32(r["ID"].ToString()), r["Name"].ToString());
                }
            }

            var bsHist = new List<BoxScoreEntry>(res2.Rows.Count);
            Parallel.ForEach(res2.Rows.Cast<DataRow>(), r =>
                                                        {
                                                            var bs = new TeamBoxScore(r, tst);

                                                            var bse = new BoxScoreEntry(bs)
                                                                      {
                                                                          Date = bs.GameDate,
                                                                          Team1Display = displayNames[bs.Team1ID],
                                                                          Team2Display = displayNames[bs.Team2ID]
                                                                      };

                                                            string q2 = "select * from PlayerResults WHERE GameID = " + bs.ID.ToString();
                                                            DataTable res3 = db.GetDataTable(q2);
                                                            bse.PBSList = new List<PlayerBoxScore>(res3.Rows.Count);

                                                            Parallel.ForEach(res3.Rows.Cast<DataRow>(),
                                                                             r3 => bse.PBSList.Add(new PlayerBoxScore(r3, tst)));

                                                            bsHist.Add(bse);
                                                        });
            return bsHist;
        }

        public static List<BoxScoreEntry> GetTimeframedBoxScoresFromDatabase(string file, DateTime startDate, DateTime endDate,
                                                                             Dictionary<int, TeamStats> tst)
        {
            var db = new SQLiteDatabase(file);

            string q = "select * from GameResults";
            q = SQLiteDatabase.AddDateRangeToSQLQuery(q, startDate, endDate, true);
            DataTable res2 = db.GetDataTable(q);
            Dictionary<int, string> displayNames = GetTimeframedDisplayNames(file, startDate, endDate);

            var bsHist = new List<BoxScoreEntry>(res2.Rows.Count);
            Parallel.ForEach(res2.Rows.Cast<DataRow>(), r =>
                                                        {
                                                            var bs = new TeamBoxScore(r, tst);

                                                            var bse = new BoxScoreEntry(bs)
                                                                      {
                                                                          Date = bs.GameDate,
                                                                          Team1Display = displayNames[bs.Team1ID],
                                                                          Team2Display = displayNames[bs.Team2ID]
                                                                      };

                                                            string q2 = "select * from PlayerResults WHERE GameID = " + bs.ID.ToString();
                                                            DataTable res3 = db.GetDataTable(q2);
                                                            bse.PBSList = new List<PlayerBoxScore>(res3.Rows.Count);

                                                            Parallel.ForEach(res3.Rows.Cast<DataRow>(),
                                                                             r3 => bse.PBSList.Add(new PlayerBoxScore(r3, tst)));

                                                            bsHist.Add(bse);
                                                        });
            return bsHist;
        }

        private static Dictionary<int, string> getAllDisplayNames(string file)
        {
            var displayNames = new Dictionary<int, string>();

            int maxSeason = GetMaxSeason(file);
            for (int i = maxSeason; i >= 0; i--)
            {
                GetSeasonDisplayNames(file, i, ref displayNames);
            }
            return displayNames;
        }

        public static Dictionary<int, string> GetTimeframedDisplayNames(string file, DateTime startDate, DateTime endDate)
        {
            var displayNames = new Dictionary<int, string>();

            List<int> seasons = getSeasonsInTimeframe(startDate, endDate);
            seasons.Reverse();

            foreach (var i in seasons)
            {
                GetSeasonDisplayNames(file, i, ref displayNames);
            }
            return displayNames;
        }

        private static List<int> getSeasonsInTimeframe(DateTime startDate, DateTime endDate)
        {
            string q = "SELECT SeasonNum FROM GameResults";
            q = SQLiteDatabase.AddDateRangeToSQLQuery(q, startDate, endDate, true);
            q += " GROUP BY SeasonNum";
            var seasons = new List<int>();
            MainWindow.DB.GetDataTable(q)
                      .Rows.Cast<DataRow>()
                      .ToList()
                      .ForEach(row => seasons.Add(DataRowCellParsers.GetInt32(row, "SeasonNum")));
            if (seasons.Count == 0)
            {
                seasons.Add(1);
            }
            else
            {
                seasons.Sort();
                seasons.Reverse();
            }
            return seasons;
        }

        public static void GetSeasonDisplayNames(string file, int curSeason, ref Dictionary<int, string> DisplayNames)
        {
            string teamsT = "Teams";
            if (curSeason != GetMaxSeason(file))
                teamsT += "S" + curSeason;
            string q;

            DisplayNames = new Dictionary<int, string>();
            var db = new SQLiteDatabase(file);

            DataTable res;
            try
            {
                q = "select ID, DisplayName from " + teamsT;
                res = db.GetDataTable(q);

                foreach (DataRow r in res.Rows)
                {
                    int id = Convert.ToInt32(r["ID"].ToString());
                    string displayName = r["DisplayName"].ToString();
                    if (!DisplayNames.Keys.Contains(id))
                    {
                        DisplayNames.Add(id, displayName);
                    }
                    else
                    {
                        string cur = DisplayNames[id];
                        string[] parts = cur.Split(new[] {", "}, StringSplitOptions.None);
                        if (!parts.Contains(displayName))
                            DisplayNames[id] += ", " + displayName;
                    }
                }
            }
            catch
            {
                q = "select ID, Name from " + teamsT;
                res = db.GetDataTable(q);

                foreach (DataRow r in res.Rows)
                {
                    int id = Convert.ToInt32(r["ID"].ToString());
                    string displayName = r["Name"].ToString();
                    if (!DisplayNames.Keys.Contains(id))
                    {
                        DisplayNames.Add(id, displayName);
                    }
                    else
                    {
                        string cur = DisplayNames[id];
                        string[] parts = cur.Split(new[] {"/"}, StringSplitOptions.None);
                        if (!parts.Contains(displayName))
                            DisplayNames[id] += "/" + displayName;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the players from database.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="tst">The team stats dictionary.</param>
        /// <param name="tstOpp">The opposing team stats dictionary.</param>
        /// <param name="teamOrder">The team order.</param>
        /// <param name="curSeason">The current season ID.</param>
        /// <param name="maxSeason">The maximum season ID.</param>
        /// <returns></returns>
        public static Dictionary<int, PlayerStats> GetPlayersFromDatabase(string file, Dictionary<int, TeamStats> tst,
                                                                          Dictionary<int, TeamStats> tstOpp,
                                                                          SortedDictionary<string, int> teamOrder, int curSeason,
                                                                          int maxSeason)
        {
            string q;

            var db = new SQLiteDatabase(file);

            if (curSeason == maxSeason)
            {
                q = "select * from Players;";
            }
            else
            {
                q = "select * from PlayersS" + curSeason.ToString() + ";";
            }
            DataTable res = db.GetDataTable(q);

            Dictionary<int, PlayerStats> pst = (from DataRow r in res.Rows.AsParallel()
                                                select new PlayerStats(r, tst)).ToDictionary(ps => ps.ID);
            PlayerStats.CalculateAllMetrics(ref pst, tst, tstOpp);

            if (curSeason == maxSeason)
            {
                q = "select * from PlayoffPlayers;";
            }
            else
            {
                q = "select * from PlayoffPlayersS" + curSeason.ToString() + ";";
            }

            try
            {
                res = db.GetDataTable(q);
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLowerInvariant().Contains("no such table"))
                    return pst;
            }

            foreach (DataRow r in res.Rows)
            {
                int id = DataRowCellParsers.GetInt32(r, "ID");
                pst[id].UpdatePlayoffStats(r);
            }

            q = "SELECT * FROM CareerHighs";

            try
            {
                res = db.GetDataTable(q);
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLowerInvariant().Contains("no such table"))
                    return pst;
            }

            foreach (DataRow r in res.Rows)
            {
                int id = DataRowCellParsers.GetInt32(r, "PlayerID");
                if (pst.Keys.Contains(id))
                {
                    pst[id].UpdateCareerHighs(r);
                }
            }

            PlayerStats.CalculateAllMetrics(ref pst, tst, tstOpp, playoffs: true);

            return pst;
        }

        /// <summary>
        ///     Determines whether the TeamStats dictionary is empty.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the TeamStats dictionary is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsTSTEmpty()
        {
            if (String.IsNullOrWhiteSpace(MainWindow.CurrentDB))
                return true;

            MainWindow.DB = new SQLiteDatabase(MainWindow.CurrentDB);

            string teamsT = "Teams";
            if (MainWindow.CurSeason != GetMaxSeason(MainWindow.CurrentDB))
                teamsT += "S" + MainWindow.CurSeason;
            string q = "select Name from " + teamsT;
            DataTable res = MainWindow.DB.GetDataTable(q);

            try
            {
                if (res.Rows[0]["Name"].ToString() == "$$NewDB")
                    return true;

                return false;
            }
            catch
            {
                return true;
            }
        }

        public static void RepairDB(ref Dictionary<int, PlayerStats> pst)
        {
            List<int> list = pst.Keys.ToList();
            foreach (var key in list)
            {
                PlayerStats ps = pst[key];
                if (ps.IsActive && ps.TeamF == -1)
                {
                    ps.IsActive = false;
                }
            }
        }

        /// <summary>
        ///     Gets the max player ID.
        /// </summary>
        /// <param name="dbFile">The db file.</param>
        /// <returns></returns>
        public static int GetMaxPlayerID(string dbFile)
        {
            var db = new SQLiteDatabase(dbFile);
            int max = GetMaxSeason(dbFile);

            string q;
            DataTable res;

            var maxList = new List<int>();

            for (int i = 1; i < max; i++)
            {
                q = "select ID from PlayersS" + i + " ORDER BY ID DESC LIMIT 1;";
                res = db.GetDataTable(q);
                maxList.Add(Convert.ToInt32(res.Rows[0]["ID"].ToString()));
            }
            q = "select ID from Players ORDER BY ID DESC LIMIT 1;";
            res = db.GetDataTable(q);

            try
            {
                maxList.Add(Convert.ToInt32(res.Rows[0]["ID"].ToString()));
                maxList.Sort();
                maxList.Reverse();
                return maxList[0];
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        ///     Gets a free player result ID.
        /// </summary>
        /// <param name="dbFile">The db file.</param>
        /// <param name="used">Additional player result IDs to assume used.</param>
        /// <returns></returns>
        private static int getFreePlayerResultID(string dbFile, List<int> used)
        {
            var db = new SQLiteDatabase(dbFile);

            const string q = "select ID from PlayerResults ORDER BY ID ASC;";
            DataTable res = db.GetDataTable(q);

            int i;
            for (i = 0; i < res.Rows.Count; i++)
            {
                if (Convert.ToInt32(res.Rows[i]["ID"].ToString()) != i)
                {
                    if (!used.Contains(i))
                        return i;
                }
            }
            i = res.Rows.Count;
            while (true)
            {
                if (!used.Contains(i))
                    return i;

                i++;
            }
        }

        /// <summary>
        ///     Gets the first free ID from the specified table.
        /// </summary>
        /// <param name="dbFile">The db file.</param>
        /// <param name="table">The table.</param>
        /// <param name="columnName">Name of the column; "ID" by default.</param>
        /// <returns></returns>
        public static int GetFreeID(string dbFile, string table, string columnName = "ID", List<int> used = null)
        {
            var db = new SQLiteDatabase(dbFile);
            if (used == null)
                used = new List<int>();

            string q = "select " + columnName + " from " + table + " ORDER BY " + columnName + " ASC;";
            DataTable res = db.GetDataTable(q);
            res.Rows.Cast<DataRow>().ToList().ForEach(r => used.Add(Convert.ToInt32(r["ID"].ToString())));
            int i = 0;
            while (true)
            {
                if (used.Contains(i))
                    i++;
                else
                    return i;
            }
        }

        /// <summary>
        ///     Saves the current team stats dictionaries to the current database.
        /// </summary>
        public static void SaveTeamsToDatabase()
        {
            saveTeamsToDatabase(MainWindow.CurrentDB, MainWindow.TST, MainWindow.TSTOpp, MainWindow.CurSeason,
                                GetMaxSeason(MainWindow.CurrentDB));
        }

        public static void PopulateAll(Timeframe tf, out Dictionary<int, TeamStats> tst, out Dictionary<int, TeamStats> tstOpp,
                                       out SortedDictionary<string, int> teamOrder, out Dictionary<int, PlayerStats> pst,
                                       out Dictionary<int, Dictionary<string, TeamStats>> splitTeamStats,
                                       out Dictionary<int, Dictionary<string, PlayerStats>> splitPlayerStats, out List<BoxScoreEntry> bsHist,
                                       out TeamRankings teamRankings, out PlayerRankings playerRankings,
                                       out TeamRankings playoffTeamRankings, out PlayerRankings playoffPlayerRankings,
                                       out Dictionary<int, string> displayNames)
        {
            tst = new Dictionary<int, TeamStats>();
            tstOpp = new Dictionary<int, TeamStats>();
            teamOrder = new SortedDictionary<string, int>();
            pst = new Dictionary<int, PlayerStats>();
            splitTeamStats = new Dictionary<int, Dictionary<string, TeamStats>>();
            splitPlayerStats = new Dictionary<int, Dictionary<string, PlayerStats>>();
            int curSeason = tf.SeasonNum;
            int maxSeason = GetMaxSeason(MainWindow.CurrentDB);
            SQLiteDatabase db = MainWindow.DB;

            string q;
            DataTable res;

            #region Prepare Teams & Players Dictionaries

            displayNames = new Dictionary<int, string>();

            if (!tf.IsBetween)
            {
                GetAllTeamStatsFromDatabase(MainWindow.CurrentDB, tf.SeasonNum, out tst, out tstOpp, out teamOrder);
                foreach (var ts in tst)
                {
                    displayNames.Add(ts.Value.ID, ts.Value.DisplayName);
                }
                pst = GetPlayersFromDatabase(MainWindow.CurrentDB, tst, tstOpp, teamOrder, curSeason, maxSeason);
            }
            else
            {
                List<int> seasons = getSeasonsInTimeframe(tf.StartDate, tf.EndDate);

                foreach (var i in seasons)
                {
                    q = "SELECT * FROM Teams" + AddSuffix(i, maxSeason) + " WHERE isHidden LIKE \"False\"";
                    res = db.GetDataTable(q);
                    foreach (DataRow dr in res.Rows)
                    {
                        int teamID = DataRowCellParsers.GetInt32(dr, "ID");
                        if (!tst.Keys.Contains(teamID))
                        {
                            tst.Add(teamID,
                                    new TeamStats
                                    {
                                        ID = teamID,
                                        Name = DataRowCellParsers.GetString(dr, "Name"),
                                        DisplayName = DataRowCellParsers.GetString(dr, "DisplayName")
                                    });
                            tstOpp.Add(teamID,
                                       new TeamStats
                                       {
                                           ID = teamID,
                                           Name = DataRowCellParsers.GetString(dr, "Name"),
                                           DisplayName = DataRowCellParsers.GetString(dr, "DisplayName")
                                       });
                            teamOrder.Add(DataRowCellParsers.GetString(dr, "Name"), teamID);
                            displayNames.Add(DataRowCellParsers.GetInt32(dr, "ID"), DataRowCellParsers.GetString(dr, "DisplayName"));
                        }
                    }

                    q = "SELECT * FROM Players" + AddSuffix(i, maxSeason) + " WHERE isHidden LIKE \"False\"";
                    res = db.GetDataTable(q);
                    foreach (DataRow dr in res.Rows)
                    {
                        int playerID = DataRowCellParsers.GetInt32(dr, "ID");
                        if (!pst.Keys.Contains(playerID))
                        {
                            pst.Add(playerID, new PlayerStats(dr, tst));
                            pst[playerID].ResetStats();
                        }
                    }

                    q = "SELECT * FROM CareerHighs";
                    res = db.GetDataTable(q);
                    foreach (DataRow dr in res.Rows)
                    {
                        int playerID = DataRowCellParsers.GetInt32(dr, "PlayerID");
                        if (pst.Keys.Contains(playerID))
                        {
                            pst[playerID].UpdateCareerHighs(dr);
                        }
                    }
                }
            }

            displayNames.Add(-1, "");

            RepairDB(ref pst);

            #endregion

            #region Prepare Split Dictionaries

            string better500 = "vs >= .500";
            string worse500 = "vs < .500";
            foreach (var id in teamOrder.Values)
            {
                splitTeamStats.Add(id, new Dictionary<string, TeamStats>());
                splitTeamStats[id].Add("Wins", new TeamStats());
                splitTeamStats[id].Add("Losses", new TeamStats());
                splitTeamStats[id].Add("Home", new TeamStats());
                splitTeamStats[id].Add("Away", new TeamStats());
                splitTeamStats[id].Add("Season", new TeamStats());
                splitTeamStats[id].Add("Playoffs", new TeamStats());
                foreach (var pair in teamOrder)
                {
                    if (pair.Value != id)
                    {
                        splitTeamStats[id].Add("vs " + displayNames[pair.Value], new TeamStats());
                    }
                }
                if (!tf.IsBetween)
                {
                    string q2 = "SELECT Date FROM GameResults WHERE SeasonNum = " + tf.SeasonNum + " GROUP BY Date ORDER BY Date ASC";
                    DataTable dataTable = db.GetDataTable(q2);
                    if (dataTable.Rows.Count == 0)
                    {
                        tf.StartDate = DateTime.Today.AddMonths(-1).AddDays(1);
                        tf.EndDate = DateTime.Today;
                    }
                    else
                    {
                        tf.StartDate = Convert.ToDateTime(dataTable.Rows[0][0].ToString());
                        tf.EndDate = Convert.ToDateTime(dataTable.Rows[dataTable.Rows.Count - 1][0].ToString());
                    }
                }
                DateTime dCur = tf.StartDate;
                while (true)
                {
                    if (new DateTime(dCur.Year, dCur.Month, 1) == new DateTime(tf.EndDate.Year, tf.EndDate.Month, 1))
                    {
                        splitTeamStats[id].Add("M " + dCur.Year + " " + dCur.Month.ToString().PadLeft(2, '0'), new TeamStats());
                        break;
                    }
                    else
                    {
                        splitTeamStats[id].Add("M " + dCur.Year + " " + dCur.Month.ToString().PadLeft(2, '0'), new TeamStats());
                        dCur = new DateTime(dCur.Year, dCur.Month, 1).AddMonths(1);
                    }
                }
                splitTeamStats[id].Add(better500, new TeamStats());
                splitTeamStats[id].Add(worse500, new TeamStats());
                splitTeamStats[id].Add("Division", new TeamStats());
                splitTeamStats[id].Add("Conference", new TeamStats());
                splitTeamStats[id].Add("Last 10", new TeamStats());
                splitTeamStats[id].Add("Before", new TeamStats());
            }

            foreach (var id in pst.Keys)
            {
                splitPlayerStats.Add(id, new Dictionary<string, PlayerStats>());
                splitPlayerStats[id].Add("Wins", new PlayerStats {ID = id});
                splitPlayerStats[id].Add("Losses", new PlayerStats {ID = id});
                splitPlayerStats[id].Add("Home", new PlayerStats {ID = id});
                splitPlayerStats[id].Add("Away", new PlayerStats {ID = id});
                splitPlayerStats[id].Add("Season", new PlayerStats {ID = id});
                splitPlayerStats[id].Add("Playoffs", new PlayerStats {ID = id});

                string qrTeams =
                    String.Format(
                        "select TeamID from PlayerResults INNER JOIN GameResults ON " + "(PlayerResults.GameID = GameResults.GameID) " +
                        " WHERE PlayerID = {0}", id);
                if (tf.IsBetween)
                {
                    qrTeams = SQLiteDatabase.AddDateRangeToSQLQuery(qrTeams, tf.StartDate, tf.EndDate);
                }
                else
                {
                    string s = " AND SeasonNum = " + tf.SeasonNum;
                    qrTeams += s;
                }
                qrTeams += " GROUP BY TeamID";
                res = db.GetDataTable(qrTeams);
                foreach (DataRow r in res.Rows)
                {
                    splitPlayerStats[id].Add("with " + displayNames[DataRowCellParsers.GetInt32(r, "TeamID")], new PlayerStats {ID = id});
                }

                foreach (var pair in teamOrder)
                {
                    splitPlayerStats[id].Add("vs " + displayNames[pair.Value], new PlayerStats {ID = id});
                }
                DateTime dCur = tf.StartDate;

                while (true)
                {
                    if (new DateTime(dCur.Year, dCur.Month, 1) == new DateTime(tf.EndDate.Year, tf.EndDate.Month, 1))
                    {
                        splitPlayerStats[id].Add("M " + dCur.Year + " " + dCur.Month.ToString().PadLeft(2, '0'), new PlayerStats {ID = id});
                        break;
                    }
                    else
                    {
                        splitPlayerStats[id].Add("M " + dCur.Year + " " + dCur.Month.ToString().PadLeft(2, '0'), new PlayerStats {ID = id});
                        dCur = new DateTime(dCur.Year, dCur.Month, 1).AddMonths(1);
                    }
                }

                splitPlayerStats[id].Add(better500, new PlayerStats {ID = id});
                splitPlayerStats[id].Add(worse500, new PlayerStats {ID = id});
                splitPlayerStats[id].Add("Last 10", new PlayerStats {ID = id});
                splitPlayerStats[id].Add("Before", new PlayerStats {ID = id});
            }

            #endregion

            #region Box Scores

            bsHist = !tf.IsBetween
                         ? GetSeasonBoxScoresFromDatabase(MainWindow.CurrentDB, tf.SeasonNum, maxSeason, tst)
                         : GetTimeframedBoxScoresFromDatabase(MainWindow.CurrentDB, tf.StartDate, tf.EndDate, tst);

            bsHist.Sort((bse1, bse2) => bse1.BS.GameDate.CompareTo(bse2.BS.GameDate));
            bsHist.Reverse();

            if (tf.IsBetween)
            {
                foreach (var bse in bsHist)
                {
                    TeamStats.AddTeamStatsFromBoxScore(bse.BS, ref tst, ref tstOpp, bse.BS.Team1ID, bse.BS.Team2ID);

                    foreach (var pbs in bse.PBSList)
                    {
                        PlayerStats ps = pst.Single(pair => pair.Value.ID == pbs.PlayerID).Value;
                        ps.AddBoxScore(pbs, bse.BS.IsPlayoff);
                    }
                }
                /*
                TeamStats.CalculateAllMetrics(ref tst, tstOpp);
                TeamStats.CalculateAllMetrics(ref tst, tstOpp, playoffs: true);
                TeamStats.CalculateAllMetrics(ref tstOpp, tst);
                TeamStats.CalculateAllMetrics(ref tstOpp, tst, playoffs: true);
                */
                PlayerStats.CalculateAllMetrics(ref pst, tst, tstOpp);
                PlayerStats.CalculateAllMetrics(ref pst, tst, tstOpp, playoffs: true);
            }

            var last10GamesTeams = new Dictionary<int, List<int>>();
            foreach (var pair in teamOrder)
            {
                KeyValuePair<string, int> teamPair = pair;
                List<BoxScoreEntry> teamBSEs =
                    bsHist.Where(bse => bse.BS.Team1ID == teamPair.Value || bse.BS.Team2ID == teamPair.Value).ToList();
                last10GamesTeams.Add(pair.Value, teamBSEs.Select(bse => bse.BS.ID).Take(10).ToList());
                string type = "";
                int length = 0;
                foreach (var bse in teamBSEs)
                {
                    if (bse.BS.Team1ID == teamPair.Value)
                    {
                        if (bse.BS.PTS1 > bse.BS.PTS2)
                        {
                            if (type == "")
                            {
                                type = "W";
                                length = 1;
                            }
                            else if (type == "W")
                            {
                                length++;
                            }
                            else if (type == "L")
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (type == "")
                            {
                                type = "L";
                                length = 1;
                            }
                            else if (type == "W")
                            {
                                break;
                            }
                            else if (type == "L")
                            {
                                length++;
                            }
                        }
                    }
                    else
                    {
                        if (bse.BS.PTS1 < bse.BS.PTS2)
                        {
                            if (type == "")
                            {
                                type = "W";
                                length = 1;
                            }
                            else if (type == "W")
                            {
                                length++;
                            }
                            else if (type == "L")
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (type == "")
                            {
                                type = "L";
                                length = 1;
                            }
                            else if (type == "W")
                            {
                                break;
                            }
                            else if (type == "L")
                            {
                                length++;
                            }
                        }
                    }
                }
                tst[teamPair.Value].CurStreak = type + length;
            }
            var last10GamesPlayers = new Dictionary<int, List<int>>();
            foreach (var playerID in pst.Keys.ToList())
            {
                List<BoxScoreEntry> playerBSEs = bsHist.Where(bse => bse.PBSList.Any(pbs => pbs.PlayerID == playerID)).ToList();
                last10GamesPlayers.Add(playerID, playerBSEs.Select(bse => bse.BS.ID).Take(10).ToList());
            }

            foreach (var bse in bsHist)
            {
                int t1ID = bse.BS.Team1ID;
                int t2ID = bse.BS.Team2ID;
                TeamBoxScore bs = bse.BS;
                TeamStats tsH = splitTeamStats[t2ID]["Home"];
                TeamStats tsA = splitTeamStats[t1ID]["Away"];
                TeamStats.AddTeamStatsFromBoxScore(bs, ref tsA, ref tsH, true);
                TeamStats tsOH = splitTeamStats[t2ID]["vs " + displayNames[t1ID]];
                TeamStats tsOA = splitTeamStats[t1ID]["vs " + displayNames[t2ID]];
                TeamStats.AddTeamStatsFromBoxScore(bs, ref tsOA, ref tsOH, true);
                TeamStats tsDH = splitTeamStats[t2ID]["M " + bs.GameDate.Year + " " + bs.GameDate.Month.ToString().PadLeft(2, '0')];
                TeamStats tsDA = splitTeamStats[t1ID]["M " + bs.GameDate.Year + " " + bs.GameDate.Month.ToString().PadLeft(2, '0')];
                TeamStats.AddTeamStatsFromBoxScore(bs, ref tsDA, ref tsDH, true);
                if (!bse.BS.IsPlayoff)
                {
                    TeamStats tsSH = splitTeamStats[t2ID]["Season"];
                    TeamStats tsSA = splitTeamStats[t1ID]["Season"];
                    TeamStats.AddTeamStatsFromBoxScore(bs, ref tsSA, ref tsSH, true);
                }
                else
                {
                    TeamStats tsSH = splitTeamStats[t2ID]["Playoffs"];
                    TeamStats tsSA = splitTeamStats[t1ID]["Playoffs"];
                    TeamStats.AddTeamStatsFromBoxScore(bs, ref tsSA, ref tsSH, true);
                }
                TeamStats ts1;
                TeamStats ts2;
                if (bs.PTS1 > bs.PTS2)
                {
                    ts2 = splitTeamStats[t2ID]["Losses"];
                    ts1 = splitTeamStats[t1ID]["Wins"];
                    TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2, true);

                    foreach (var pbs in bse.PBSList)
                    {
                        if (pbs.TeamID == t1ID)
                        {
                            splitPlayerStats[pbs.PlayerID]["Wins"].AddBoxScore(pbs);
                        }
                        else
                        {
                            splitPlayerStats[pbs.PlayerID]["Losses"].AddBoxScore(pbs);
                        }
                    }
                }
                else
                {
                    ts1 = splitTeamStats[t1ID]["Losses"];
                    ts2 = splitTeamStats[t2ID]["Wins"];
                    TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2, true);

                    foreach (var pbs in bse.PBSList)
                    {
                        if (pbs.TeamID == t1ID)
                        {
                            splitPlayerStats[pbs.PlayerID]["Losses"].AddBoxScore(pbs);
                        }
                        else
                        {
                            splitPlayerStats[pbs.PlayerID]["Wins"].AddBoxScore(pbs);
                        }
                    }
                }

                if (tst[bs.Team1ID].Conference == tst[bs.Team2ID].Conference)
                {
                    ts1 = splitTeamStats[t1ID]["Conference"];
                    ts2 = splitTeamStats[t2ID]["Conference"];
                    TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2);
                    if (tst[bs.Team1ID].Division == tst[bs.Team2ID].Division)
                    {
                        ts1 = splitTeamStats[t1ID]["Division"];
                        ts2 = splitTeamStats[t2ID]["Division"];
                        TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2);
                    }
                }

                ts1 = last10GamesTeams[bs.Team1ID].Contains(bs.ID) ? splitTeamStats[t1ID]["Last 10"] : splitTeamStats[t1ID]["Before"];
                ts2 = last10GamesTeams[bs.Team2ID].Contains(bs.ID) ? splitTeamStats[t2ID]["Last 10"] : splitTeamStats[t2ID]["Before"];
                TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2);

                string forTeam1, forTeam2;
                if (tst[bs.Team1ID].GetWinningPercentage(Span.Season) >= 0.5)
                {
                    ts2 = splitTeamStats[t2ID][better500];
                    forTeam2 = better500;
                }
                else
                {
                    ts2 = splitTeamStats[t2ID][worse500];
                    forTeam2 = worse500;
                }
                if (tst[bs.Team2ID].GetWinningPercentage(Span.Season) >= 0.5)
                {
                    ts1 = splitTeamStats[t1ID][better500];
                    forTeam1 = better500;
                }
                else
                {
                    ts1 = splitTeamStats[t1ID][worse500];
                    forTeam1 = worse500;
                }
                TeamStats.AddTeamStatsFromBoxScore(bs, ref ts1, ref ts2, true);

                foreach (var pbs in bse.PBSList)
                {
                    if (pbs.TeamID == t1ID)
                    {
                        splitPlayerStats[pbs.PlayerID]["Away"].AddBoxScore(pbs);
                        splitPlayerStats[pbs.PlayerID]["vs " + displayNames[t2ID]].AddBoxScore(pbs);
                        splitPlayerStats[pbs.PlayerID]["with " + displayNames[t1ID]].AddBoxScore(pbs);
                        splitPlayerStats[pbs.PlayerID][forTeam1].AddBoxScore(pbs);
                    }
                    else
                    {
                        splitPlayerStats[pbs.PlayerID]["Home"].AddBoxScore(pbs);
                        splitPlayerStats[pbs.PlayerID]["vs " + displayNames[t1ID]].AddBoxScore(pbs);
                        splitPlayerStats[pbs.PlayerID]["with " + displayNames[t2ID]].AddBoxScore(pbs);
                        splitPlayerStats[pbs.PlayerID][forTeam2].AddBoxScore(pbs);
                    }
                    splitPlayerStats[pbs.PlayerID][bs.IsPlayoff ? "Playoffs" : "Season"].AddBoxScore(pbs);

                    splitPlayerStats[pbs.PlayerID]["M " + bs.GameDate.Year + " " + bs.GameDate.Month.ToString().PadLeft(2, '0')].AddBoxScore
                        (pbs);

                    if (last10GamesPlayers[pbs.PlayerID].Contains(bs.ID))
                    {
                        splitPlayerStats[pbs.PlayerID]["Last 10"].AddBoxScore(pbs);
                    }
                    else
                    {
                        splitPlayerStats[pbs.PlayerID]["Before"].AddBoxScore(pbs);
                    }
                }
            }

            #endregion

            foreach (var ps in pst)
            {
                ps.Value.CalculateSeasonHighs(bsHist);
            }

            teamRankings = new TeamRankings(tst);
            playoffTeamRankings = new TeamRankings(tst, true);
            playerRankings = new PlayerRankings(pst);
            playoffPlayerRankings = new PlayerRankings(pst, true);
        }

        private static void findTeamByName(string teamName, DateTime startDate, DateTime endDate, out TeamStats ts, out TeamStats tsopp,
                                           out int lastInSeason)
        {
            int maxSeason = GetMaxSeason(MainWindow.CurrentDB);

            string q = "SELECT SeasonNum FROM GameResults";
            q = SQLiteDatabase.AddDateRangeToSQLQuery(q, startDate, endDate, true);
            q += " GROUP BY SeasonNum";
            DataTable res = MainWindow.DB.GetDataTable(q);
            List<DataRow> rows = res.Rows.Cast<DataRow>().OrderByDescending(row => row["SeasonNum"]).ToList();

            foreach (var r in rows)
            {
                int curSeason = DataRowCellParsers.GetInt32(r, "SeasonNum");
                q = "SELECT * FROM Teams" + AddSuffix(curSeason, maxSeason);
                DataTable res2 = MainWindow.DB.GetDataTable(q);
                List<DataRow> rows2 = res2.Rows.Cast<DataRow>().ToList();
                try
                {
                    DataRow r2 = rows2.Single(row => row["Name"].ToString() == teamName);
                    GetTeamStatsFromDatabase(MainWindow.CurrentDB, DataRowCellParsers.GetInt32(r2, "ID"), curSeason, out ts, out tsopp);
                    lastInSeason = curSeason;
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine("Couldn't find team " + teamName + " in Season " + curSeason + " by name.");
                }
            }
            ts = null;
            tsopp = null;
            lastInSeason = 0;
        }

        public static string AddSuffix(int curSeason, int maxSeason)
        {
            return (curSeason != maxSeason ? "S" + curSeason : "");
        }
    }
}