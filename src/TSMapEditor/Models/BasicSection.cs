namespace TSMapEditor.Models
{
    /// <summary>
    /// Contains information specified in the [Basic] section of a map.
    /// </summary>
    public class BasicSection : INIDefineable
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Player { get; set; }
        public int Percent { get; set; }
        public string GameMode { get; set; }
        public string GameModes { get; set; }
        public int HomeCell { get; set; } = 98;
        public int AltHomeCell { get; set; } = 99;
        public string Theme { get; set; }
        public int InitTime { get; set; }
        public bool Official { get; set; }
        public bool EndOfGame { get; set; }
        public bool FreeRadar { get; set; }
        public int MaxPlayer { get; set; } = 8;
        public int MinPlayer { get; set; } = 2;
        public bool SkipScore { get; set; }
        public bool TrainCrate { get; set; }
        public bool TruckCrate { get; set; }
        public bool OneTimeOnly { get; set; }
        public int CarryOverCap { get; set; }
        public int NewINIFormat { get; set; } = 4;
        public string NextScenario { get; set; }
        public string AltNextScenario { get; set; }
        public int? RequiredAddOn { get; set; }
        public bool SkipMapSelect { get; set; }
        public double CarryOverMoney { get; set; }
        public bool MultiplayerOnly { get; set; }
        public bool IceGrowthEnabled { get; set; }
        public bool VeinGrowthEnabled { get; set; }
        public bool TiberiumGrowthEnabled { get; set; } = true;
        public bool IgnoreGlobalAITriggers { get; set; }
        public bool TiberiumDeathToVisceroid { get; set; }
    }
}
