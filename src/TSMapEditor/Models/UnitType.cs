using System.Collections.Generic;
using TSMapEditor.Models.ArtConfig;

namespace TSMapEditor.Models
{
    /// <summary>
    /// Could also be called 'VehicleType', but let's respect the original game's naming.
    /// </summary>
    public class UnitType : TechnoType, IArtConfigContainer
    {
        public const int STANDARD_STANDING_FRAME_COUNT = 8;

        public UnitType(string iniName) : base(iniName)
        {
        }

        public VehicleArtConfig ArtConfig { get; private set; } = new VehicleArtConfig();
        public IArtConfig GetArtConfig() => ArtConfig;

        public override RTTIType WhatAmI() => RTTIType.UnitType;

        public bool Turret { get; set; }
        public string SpeedType { get; set; }
        public string MovementZone { get; set; }

        public int GetTurretStartFrame()
        {
            if (ArtConfig.StartTurretFrame > -1)
                return ArtConfig.StartTurretFrame;

            if (ArtConfig.StartStandFrame < 0)
            {
                if (ArtConfig.StartWalkFrame < 0)
                {
                    return ArtConfig.Facings * ArtConfig.StandingFrames;
                }

                // Turret frames come after walk frames
                return ArtConfig.StartWalkFrame + (ArtConfig.WalkFrames * ArtConfig.Facings);
            }

            // Turret frames come after stand frames
            return ArtConfig.StartStandFrame + (ArtConfig.StandingFrames * ArtConfig.Facings);
        }

        public List<int> GetIdleFrameIndexes()
        {
            var returnValue = new List<int>();

            if (ArtConfig.FiringFrames > 0)
            {
                int startStandFrame;

                if (ArtConfig.StartStandFrame > -1)
                    startStandFrame = ArtConfig.StartStandFrame;
                else
                    startStandFrame = ArtConfig.WalkFrames * ArtConfig.Facings;

                for (int i = 0; i < ArtConfig.Facings; i++)
                    returnValue.Add(startStandFrame + (i * ArtConfig.StandingFrames));
            }
            else
            {
                if (ArtConfig.StartStandFrame < 0)
                {
                    if (ArtConfig.StartWalkFrame < 0)
                    {
                        for (int i = 0; i < ArtConfig.Facings; i++)
                            returnValue.Add(i * ArtConfig.StandingFrames);
                    }
                    else
                    {
                        // Use walk frames
                        for (int i = 0; i < ArtConfig.Facings; i++)
                            returnValue.Add(ArtConfig.StartWalkFrame + (i * ArtConfig.WalkFrames));
                    }
                }
                else
                {
                    // Use StartStandFrame
                    for (int i = 0; i < ArtConfig.Facings; i++)
                    {
                        returnValue.Add(ArtConfig.StartStandFrame + (i * ArtConfig.StandingFrames));
                    }
                }
            }

            return returnValue;
        }
    }
}
