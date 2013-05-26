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

namespace NBA_Stats_Tracker.Data.Other
{
    #region Using Directives

    using System.Collections.Generic;

    using NBA_Stats_Tracker.Data.BoxScores;
    using NBA_Stats_Tracker.Data.Players;
    using NBA_Stats_Tracker.Data.Teams;

    #endregion

    public class DBData
    {
        public readonly List<BoxScoreEntry> BSHist;
        public readonly Dictionary<int, string> DisplayNames;
        public readonly Dictionary<int, PlayerStats> PST;
        public readonly PlayerRankings PlayoffPlayerRankings;
        public readonly TeamRankings PlayoffTeamRankings;
        public readonly PlayerRankings SeasonPlayerRankings;
        public readonly TeamRankings SeasonTeamRankings;
        public readonly Dictionary<int, Dictionary<string, PlayerStats>> SplitPlayerStats;
        public readonly Dictionary<int, Dictionary<string, TeamStats>> SplitTeamStats;
        public readonly Dictionary<int, TeamStats> TST;
        public readonly Dictionary<int, TeamStats> TSTOpp;

        public DBData(
            Dictionary<int, TeamStats> tst,
            Dictionary<int, TeamStats> tstOpp,
            Dictionary<int, Dictionary<string, TeamStats>> splitTeamStats,
            TeamRankings seasonTeamRankings,
            TeamRankings playoffTeamRankings,
            Dictionary<int, PlayerStats> pst,
            Dictionary<int, Dictionary<string, PlayerStats>> splitPlayerStats,
            PlayerRankings seasonPlayerRankings,
            PlayerRankings playoffPlayerRankings,
            List<BoxScoreEntry> bsHist,
            Dictionary<int, string> displayNames)
        {
            BSHist = bsHist;
            DisplayNames = displayNames;
            PST = pst;
            SeasonPlayerRankings = seasonPlayerRankings;
            PlayoffPlayerRankings = playoffPlayerRankings;
            PlayoffTeamRankings = playoffTeamRankings;
            SplitPlayerStats = splitPlayerStats;
            SplitTeamStats = splitTeamStats;
            TST = tst;
            TSTOpp = tstOpp;
            SeasonTeamRankings = seasonTeamRankings;
        }
    }
}