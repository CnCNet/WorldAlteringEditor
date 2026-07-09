using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that fills an area with terrain.
    /// </summary>
    public partial class FillTerrainAreaMutation : Mutation
    {
        public FillTerrainAreaMutation(IMutationTarget mutationTarget, MapTile target, TileImage tile) : base(mutationTarget)
        {
            if (tile.Width > 1 || tile.Height > 1)
                throw new InvalidOperationException("Only 1x1 tiles can be used to fill areas.");

            TargetCell = target;
            Tile = tile;
        }

        public MapTile TargetCell { get; }
        public TileImage Tile { get; }

        private OriginalCellTerrainData[] undoData;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", 
                "Flood-fill terrain tiles at {0} with tile from set '{1}'"),
                    TargetCell.CoordsToPoint(), MutationTarget.TheaterGraphics.Theater.TileSets[Tile.TileSetId].TranslatedName);
        }

        public override void Perform()
        {
            var originalData = new List<OriginalCellTerrainData>();
            var tilesToProcess = Helpers.GetFillAreaTiles(TargetCell, MutationTarget.Map, MutationTarget.TheaterGraphics);

            // Process tiles
            foreach (Point2D cellCoords in tilesToProcess)
            {
                var cell = MutationTarget.Map.GetTile(cellCoords);
                originalData.Add(new OriginalCellTerrainData(cellCoords, cell.TileIndex, cell.SubTileIndex, cell.Level));

                cell.ChangeTileIndex(Tile.TileID, 0);
            }

            undoData = originalData.ToArray();
            MutationTarget.InvalidateMap();
        }

        public override void Undo()
        {
            foreach (var originalTerrainData in undoData)
            {
                var cell = MutationTarget.Map.GetTile(originalTerrainData.CellCoords);
                cell.ChangeTileIndex(originalTerrainData.TileIndex, originalTerrainData.SubTileIndex);
            }

            MutationTarget.InvalidateMap();
        }
    }
}
