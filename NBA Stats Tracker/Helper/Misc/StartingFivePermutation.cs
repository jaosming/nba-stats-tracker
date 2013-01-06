using System.Collections.Generic;

namespace NBA_Stats_Tracker.Helper.Misc
{
    /// <summary>
    /// Implements a list of five players. Used in determining the best starting five in a specific scope.
    /// </summary>
    public class StartingFivePermutation
    {
        public List<int> idList = new List<int>(5);
        public int PlayersInPrimaryPosition = 0;
        public double Sum = 0;
    }
}