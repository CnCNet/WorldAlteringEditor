using MapEditorLibrary.Models.ArtConfig;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.Models;

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
    public bool IsLimpetDrone { get; set; }
    public bool Jellyfish { get; set; }
    public int IdleRate { get; set; }
    public string SpeedType { get; set; }
    public string MovementZone { get; set; }
    public string DeploysInto { get; set; }

    public int GetTurretStartFrame()
    {
        if (ArtConfig.StartTurretFrame > -1)
            return ArtConfig.StartTurretFrame;

        return GetArtFacingCount() * ArtConfig.WalkFrames;
    }

    public List<int> GetIdleFrameIndexes()
    {
        var (startFrame, framesPerFacing, facingCount) = GetIdleFrameLayout();
        var frames = new List<int>();

        for (int facingIndex = 0; facingIndex < facingCount; facingIndex++)
            frames.Add(startFrame + facingIndex * framesPerFacing);

        return frames;
    }

    internal (int StartFrame, int FramesPerFacing, int FacingCount) GetIdleFrameLayout()
    {
        // Limpet drones and jellyfish use their animation stage as the frame index.
        if (!Constants.IsRA2YR && (IsLimpetDrone || Jellyfish))
            return (0, 0, 1);

        int artFacings = GetArtFacingCount();
        int facingCount = artFacings >= STANDARD_STANDING_FRAME_COUNT ? artFacings : 1;
        int standingFrames = ArtConfig.StandingFrames;
        if (standingFrames < 0)
            standingFrames = ArtConfig.FiringFrames > 0 ? 1 : 0;

        // Red Alert 2 uses the walk sequence for idle animation when IdleRate is set in the rules.
        if (standingFrames == 0 || (Constants.IsRA2YR && IdleRate != 0))
            return (ArtConfig.StartWalkFrame, ArtConfig.WalkFrames, facingCount);

        int startStandFrame = ArtConfig.StartStandFrame >= 0 ? ArtConfig.StartStandFrame : artFacings * ArtConfig.WalkFrames;

        return (startStandFrame, standingFrames, facingCount);
    }

    private int GetArtFacingCount()
    {
        if (ArtConfig.Facings >= 0)
            return ArtConfig.Facings;

        return ArtConfig.FiringFrames != 0 || Turret ? STANDARD_STANDING_FRAME_COUNT : 1;
    }
}
