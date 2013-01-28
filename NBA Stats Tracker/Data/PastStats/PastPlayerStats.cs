#region Copyright Notice

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

using System.Data;
using LeftosCommonLibrary;

#endregion

namespace NBA_Stats_Tracker.Data.PastStats
{
    public class PastPlayerStats
    {
        public PastPlayerStats()
        {
        }

        public PastPlayerStats(DataRow dr)
        {
            GP = Tools.getUInt32(dr, "GP");
            GS = Tools.getUInt32(dr, "GS");
            PlayerID = Tools.getInt(dr, "PlayerID");

            MINS = Tools.getUInt32(dr, "MINS");
            PTS = Tools.getUInt32(dr, "PTS");
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
            TeamFName = Tools.getString(dr, "TeamFin");
            TeamSName = Tools.getString(dr, "TeamSta");
            ID = Tools.getInt(dr, "ID");
        }

        public int PlayerID { get; set; }
        public int ID { get; set; }
        public string SeasonName { get; set; }
        public int Order { get; set; }
        public bool isPlayoff { get; set; }
        public string TeamFName { get; set; }
        public string TeamSName { get; set; }
        public uint GP { get; set; }
        public uint GS { get; set; }
        public uint MINS { get; set; }
        public uint PTS { get; set; }
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

        public void EndEdit()
        {
            REB = OREB + DREB;
            PTS = (FGM - TPM)*2 + TPM*3 + FTM;
        }
    }
}