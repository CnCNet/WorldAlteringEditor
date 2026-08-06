using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.Models;

/// <summary>
/// Attaches a <see cref="Tag"/> to a <see cref="MapTile"/>.
/// </summary>
public class CellTag : AbstractObject, IMovable
{
    public CellTag()
    {
    }

    public CellTag(Point2D position, Tag tag)
    {
        Position = position;
        Tag = tag;
    }

    public bool IsOnBridge() => false;

    public Point2D Position { get; set; }
    public Tag Tag { get; set; }

    public override RTTIType WhatAmI() => RTTIType.CellTag;
}
