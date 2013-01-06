using System.Data;
using LeftosCommonLibrary;

namespace NBA_Stats_Tracker.Data.PastStats
{
    public class PastTeamStats
    {
        public int TeamID { get; set; }
        public int ID { get; set; }
        public string SeasonName { get; set; }
        public int Order { get; set; }
        public bool isPlayoff { get; set; }
        public uint Wins { get; set; }
        public uint Losses { get; set; }
        public uint MINS { get; set; }
        public uint PF { get; set; }
        public uint PA { get; set; }
        public uint FGM { get; set; }
        public uint FGA { get; set; }
        public uint TPM { get; set; }
        public uint TPA { get; set; }
        public uint FTM { get; set; }
        public uint FTA { get; set; }
        public uint REB { get; set; }
        public uint OREB { get; set; }
        public uint DREB { get; set; }
        public uint STL { get; set; }
        public uint TOS { get; set; }
        public uint BLK { get; set; }
        public uint AST { get; set; }
        public uint FOUL { get; set; }

        public PastTeamStats()
        {
        }

        public PastTeamStats(DataRow dr)
        {
            Wins = Tools.getUInt32(dr, "WIN");
            Losses = Tools.getUInt32(dr, "LOSS");
            TeamID = Tools.getInt(dr, "TeamID");

            MINS = Tools.getUInt32(dr, "MINS");
            PF = Tools.getUInt32(dr, "PF");
            PA = Tools.getUInt32(dr, "PA");
            FGM = Tools.getUInt32(dr, "FGM");
            FGA = Tools.getUInt32(dr, "FGA");
            TPM = Tools.getUInt32(dr, "TPM");
            TPA = Tools.getUInt32(dr, "TPA");
            FTM = Tools.getUInt32(dr, "FTM");
            FTA = Tools.getUInt32(dr, "FTA");
            OREB = Tools.getUInt32(dr, "OREB");
            DREB = Tools.getUInt32(dr, "DREB");
            REB = OREB + DREB;
            STL = Tools.getUInt32(dr, "STL");
            TOS = Tools.getUInt32(dr, "TOS");
            BLK = Tools.getUInt32(dr, "BLK");
            AST = Tools.getUInt32(dr, "AST");
            FOUL = Tools.getUInt32(dr, "FOUL");

            SeasonName = Tools.getString(dr, "SeasonName");
            Order = Tools.getInt(dr, "SOrder");
            isPlayoff = Tools.getBoolean(dr, "isPlayoff");
            ID = Tools.getInt(dr, "ID");
        }

        public void EndEdit()
        {
            REB = OREB + DREB;
            PF = (FGM - TPM)*2 + TPM*3 + FTM;
        }
    }
}