﻿using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public class VehicleArtConfig : IArtConfig
    {
        public bool Voxel { get; set; }
        public bool Remapable { get; set; }
        public int StartStandFrame { get; set; } = -1;
        public int StandingFrames { get; set; } = 1;
        public int StartWalkFrame { get; set; } = -1;
        public int WalkFrames { get; set; } = 1;
        public int Facings { get; set; } = 1;

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            Voxel = iniSection.GetBooleanValue(nameof(Voxel), Voxel);
            Remapable = iniSection.GetBooleanValue(nameof(Remapable), Remapable);
            StartStandFrame = iniSection.GetIntValue(nameof(StartStandFrame), StartStandFrame);
            StandingFrames = iniSection.GetIntValue(nameof(StandingFrames), StandingFrames);
            StartWalkFrame = iniSection.GetIntValue(nameof(StartWalkFrame), StartWalkFrame);
            WalkFrames = iniSection.GetIntValue(nameof(WalkFrames), WalkFrames);
            Facings = iniSection.GetIntValue(nameof(Facings), Facings);
        }
    }
}
