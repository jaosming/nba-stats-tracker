#region Copyright Notice

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

namespace NBA_Stats_Tracker.Data.Players
{
    #region Using Directives

    using System.Collections.Generic;
    using System.Linq;

    using NBA_Stats_Tracker.Windows.MainInterface;

    #endregion

    /// <summary>Used to determine the player ranking for each stat.</summary>
    public class PlayerRankings
    {
        private static readonly int PerGameCount = new PlayerStats().PerGame.Length;
        public readonly Dictionary<int, Dictionary<string, int>> RankingsMetrics = new Dictionary<int, Dictionary<string, int>>();
        public readonly Dictionary<int, int[]> RankingsPerGame = new Dictionary<int, int[]>();
        public readonly Dictionary<int, int[]> RankingsTotal = new Dictionary<int, int[]>();
        public readonly Dictionary<string, Dictionary<int, int>> RevRankingsMetrics = new Dictionary<string, Dictionary<int, int>>();
        public readonly Dictionary<int, Dictionary<int, int>> RevRankingsPerGame = new Dictionary<int, Dictionary<int, int>>();
        public readonly Dictionary<int, Dictionary<int, int>> RevRankingsTotals = new Dictionary<int, Dictionary<int, int>>();

        public PlayerRankings()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerRankings" /> class, and calculates the rankingsPerGame.
        /// </summary>
        /// <param name="pst">The PlayerStats dictionary, containing all player information.</param>
        /// <param name="playoffs">
        ///     if set to <c>true</c>, the rankingsPerGame will take only playoff performances into account.
        /// </param>
        public PlayerRankings(Dictionary<int, PlayerStats> pst, bool playoffs = false)
        {
            var validPlayers = pst.Where(ps => ps.Value.Totals[PAbbr.GP] > 0).ToDictionary(a => a.Key, a => a.Value);

            var dummyPS = new PlayerStats();
            //int firstPlayerID = validPlayers.Keys.ToList()[0];
            var totalsCount = dummyPS.Totals.Length;
            var metricsCount = dummyPS.Metrics.Count;

            foreach (var kvp in validPlayers)
            {
                RankingsPerGame.Add(kvp.Key, new int[PerGameCount]);
                RankingsTotal.Add(kvp.Key, new int[totalsCount]);
                RankingsMetrics.Add(kvp.Key, new Dictionary<string, int>());
            }
            foreach (var metricName in PAbbr.MetricsNames)
            {
                RevRankingsMetrics.Add(metricName, new Dictionary<int, int>());
            }
            for (var i = 0; i < PerGameCount; i++)
            {
                RevRankingsPerGame.Add(i, new Dictionary<int, int>());
            }
            for (var i = 0; i < totalsCount; i++)
            {
                RevRankingsTotals.Add(i, new Dictionary<int, int>());
            }

            for (var j = 0; j < PerGameCount; j++)
            {
                var perGame = !playoffs
                                  ? validPlayers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PerGame[j])
                                  : validPlayers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PlPerGame[j]);

                var tempList = new List<KeyValuePair<int, float>>(perGame);
                tempList.Sort((x, y) => x.Value.CompareTo(y.Value));
                if (j != PAbbr.FPG && j != PAbbr.TPG)
                {
                    tempList.Reverse();
                }

                var k = 1;
                foreach (var kvp in tempList)
                {
                    RankingsPerGame[kvp.Key][j] = k;
                    RevRankingsPerGame[j].Add(k, kvp.Key);
                    k++;
                }
            }
            var plCount = pst.Count;
            foreach (var kvp in pst.Where(ps => ps.Value.Totals[PAbbr.GP] == 0))
            {
                RankingsPerGame.Add(kvp.Key, new int[PerGameCount]);
                for (var i = 0; i < PerGameCount; i++)
                {
                    RankingsPerGame[kvp.Key][i] = plCount;
                }
            }

            for (var j = 0; j < totalsCount; j++)
            {
                var totals = !playoffs
                                 ? validPlayers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Totals[j])
                                 : validPlayers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PlTotals[j]);

                var tempList = new List<KeyValuePair<int, uint>>(totals);
                tempList.Sort((x, y) => x.Value.CompareTo(y.Value));
                if (j != PAbbr.FOUL && j != PAbbr.TOS)
                {
                    tempList.Reverse();
                }

                var k = 1;
                foreach (var kvp in tempList)
                {
                    RankingsTotal[kvp.Key][j] = k;
                    RevRankingsTotals[j].Add(k, kvp.Key);
                    k++;
                }
            }
            foreach (var kvp in pst.Where(ps => ps.Value.Totals[PAbbr.GP] == 0))
            {
                RankingsTotal.Add(kvp.Key, new int[totalsCount]);
                for (var i = 0; i < totalsCount; i++)
                {
                    RankingsTotal[kvp.Key][i] = plCount;
                }
            }

            var badMetrics = new List<string> { "TO%", "TOR" };
            var metricsNames = PAbbr.MetricsNames;
            for (var j = 0; j < metricsCount; j++)
            {
                var metrics = !playoffs
                                  ? validPlayers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Metrics[metricsNames[j]])
                                  : validPlayers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PlMetrics[metricsNames[j]]);

                var tempList = new List<KeyValuePair<int, double>>(metrics);
                tempList.Sort((x, y) => x.Value.CompareTo(y.Value));
                if (!badMetrics.Contains(metricsNames[j]))
                {
                    tempList.Reverse();
                }

                var k = 1;
                foreach (var kvp in tempList)
                {
                    RankingsMetrics[kvp.Key][metricsNames[j]] = k;
                    RevRankingsMetrics[metricsNames[j]].Add(k, kvp.Key);
                    k++;
                }
            }
            foreach (var kvp in pst.Where(ps => ps.Value.Totals[PAbbr.GP] == 0))
            {
                RankingsMetrics.Add(kvp.Key, new Dictionary<string, int>());
                for (var i = 0; i < metricsCount; i++)
                {
                    RankingsMetrics[kvp.Key][metricsNames[i]] = plCount;
                }
            }
        }

        public static PlayerRankings CalculateAllRankings(bool playoffs = false)
        {
            var cumRankingsAll =
                new PlayerRankings(MainWindow.PST.Where(ps => !ps.Value.IsHidden).ToDictionary(r => r.Key, r => r.Value), playoffs);
            return cumRankingsAll;
        }

        public static PlayerRankings CalculateActiveRankings(bool playoffs = false)
        {
            var cumRankingsActive =
                new PlayerRankings(MainWindow.PST.Where(ps => ps.Value.IsSigned).ToDictionary(r => r.Key, r => r.Value), playoffs);
            return cumRankingsActive;
        }

        public static PlayerRankings CalculateLeadersRankings(out Dictionary<int, PlayerStats> pstLeaders, bool playoffs = false)
        {
            var pstActive = MainWindow.PST.Where(ps => ps.Value.IsSigned).ToDictionary(ps => ps.Key, ps => ps.Value);
            var listOfKeys = pstActive.Keys.ToList();
            foreach (var key in listOfKeys)
            {
                pstActive[key] = MainWindow.LeadersPrefSetting == "NBA"
                                     ? pstActive[key].ConvertToLeagueLeader(MainWindow.TST, playoffs)
                                     : pstActive[key].ConvertToMyLeagueLeader(MainWindow.TST, playoffs);
            }
            var cumRankingsActive = new PlayerRankings(pstActive, playoffs);
            pstLeaders = pstActive;
            return cumRankingsActive;
        }
    }
}