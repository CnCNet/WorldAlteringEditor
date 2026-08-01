using System;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.AIMutations
{
    /// <summary>
    /// Directly changes the absolute tile and sub-tile index of one map cell.
    /// </summary>
    public sealed class SetCellTerrainMutation : Mutation, ICheckableMutation
    {
        public SetCellTerrainMutation(IMutationTarget mutationTarget, Point2D cellCoords, int tileIndex, byte subTileIndex)
            : base(mutationTarget)
        {
            this.cellCoords = cellCoords;
            this.tileIndex = tileIndex;
            this.subTileIndex = subTileIndex;
        }

        private readonly Point2D cellCoords;
        private readonly int tileIndex;
        private readonly byte subTileIndex;

        private int originalTileIndex;
        private byte originalSubTileIndex;

        public bool ShouldPerform()
        {
            var mapTile = Map.GetTile(cellCoords);
            return mapTile != null && (mapTile.TileIndex != tileIndex || mapTile.SubTileIndex != subTileIndex);
        }

        public override string GetDisplayString()
        {
            return $"Set terrain at {cellCoords} to tile {tileIndex}, sub-tile {subTileIndex}";
        }

        public override void Perform()
        {
            var mapTile = Map.GetTile(cellCoords);
            if (mapTile == null)
                throw new InvalidOperationException($"Cell {cellCoords} does not exist.");

            originalTileIndex = mapTile.TileIndex;
            originalSubTileIndex = mapTile.SubTileIndex;

            mapTile.ChangeTileIndex(tileIndex, subTileIndex);
            RefreshCellLighting(mapTile);
            MutationTarget.AddRefreshPoint(cellCoords);
        }

        public override void Undo()
        {
            var mapTile = Map.GetTile(cellCoords);
            if (mapTile == null)
                throw new InvalidOperationException($"Cell {cellCoords} does not exist.");

            mapTile.ChangeTileIndex(originalTileIndex, originalSubTileIndex);
            RefreshCellLighting(mapTile);
            MutationTarget.AddRefreshPoint(cellCoords);
        }
    }
}
