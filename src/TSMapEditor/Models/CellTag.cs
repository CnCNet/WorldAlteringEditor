using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
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

        public Point2D Position { get; set; }
        public Tag Tag { get; set; }

        public override RTTIType WhatAmI() => RTTIType.CellTag;
    }
}
