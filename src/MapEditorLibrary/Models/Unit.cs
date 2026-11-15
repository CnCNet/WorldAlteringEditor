using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.Models;

/// <summary>
/// Could also be called 'vehicle', but let's respect the original game's naming.
/// </summary>
public class Unit : Foot<UnitType>
{
    public Unit(UnitType objectType) : base(objectType)
    {
    }

    public override RTTIType WhatAmI() => RTTIType.Unit;

    public UnitType UnitType => ObjectType;

    /// <summary>
    /// The index of a unit (in the internal unit list) that is set up to
    /// follow this unit. -1 means none. Only fetched and used on map load,
    /// otherwise <see cref="FollowerUnit"/> is used.
    /// </summary>
    public int FollowerID { get; set; } = -1;

    /// <summary>
    /// The unit, if any, that is set up to follow this unit.
    /// </summary>
    public Unit FollowerUnit { get; set; }

    public override int GetFrameIndex(int frameCount)
    {
        var (startFrame, framesPerFacing, facingCount) = UnitType.GetIdleFrameLayout();
        return startFrame + GetFacingIndex(facingCount) * framesPerFacing;
    }

    public int GetTurretFrameIndex() => UnitType.GetTurretStartFrame() + GetFacingIndex(UnitType.ArtConfig.TurretFacings);

    private int GetFacingIndex(int facingCount)
    {
        if (facingCount <= 1)
            return 0;

        const int unitsPerTurn = Constants.FacingMax + 1;
        int facingIndex = (Facing * facingCount + unitsPerTurn / 2) / unitsPerTurn;

        // SHP frames start at visual north, while map facings start at northeast.
        return (facingIndex + facingCount / (int)Direction.Count) % facingCount;
    }

    public override bool Remapable() => ObjectType.ArtConfig.Remapable;

    public override int GetShadowFrameIndex(int frameCount)
    {
        return GetFrameIndex(frameCount) + frameCount / 2;
    }

    public override bool HasShadow() => !ObjectType.NoShadow;
}
