using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models.ArtConfig;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Models
{
    public class TerrainType : GameObjectType, IArtConfigContainer
    {
        public TerrainType(string iniName) : base(iniName)
        {
        }

        public override RTTIType WhatAmI() => RTTIType.TerrainType;

        public TerrainArtConfig ArtConfig { get; } = new TerrainArtConfig();
        public IArtConfig GetArtConfig() => ArtConfig;

        public TerrainOccupation TemperateOccupationBits { get; set; }
        public TerrainOccupation SnowOccupationBits { get; set; }

        /// <summary>
        /// If set, this terrain type should be drawn 12 pixels above the 
        /// usual drawing point and it should use the unit palette instead
        /// of the terrain palette.
        /// </summary>
        public bool SpawnsTiberium { get; set; }

        public int YDrawFudge { get; set; }

        /// <summary>
        /// Impassable cell data for automatically placing impassable overlay
        /// under terrain objects.
        /// </summary>
        public List<Point2D> ImpassableCells { get; set; }
    }
}
