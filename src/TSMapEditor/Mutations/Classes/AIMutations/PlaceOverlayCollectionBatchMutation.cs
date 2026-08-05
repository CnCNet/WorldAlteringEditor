using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.AIMutations;

/// <summary>
/// Places random entries from an overlay collection on multiple individual map cells as one mutation.
/// </summary>
public sealed class PlaceOverlayCollectionBatchMutation : OverlayBatchMutationBase
{
    public PlaceOverlayCollectionBatchMutation(
        IMutationTarget mutationTarget,
        OverlayCollection overlayCollection,
        List<Point2D> cellCoords)
        : base(mutationTarget)
    {
        this.overlayCollection = overlayCollection ?? throw new ArgumentNullException(nameof(overlayCollection));
        this.cellCoords = cellCoords ?? throw new ArgumentNullException(nameof(cellCoords));

        if (overlayCollection.Entries.Length == 0)
            throw new ArgumentException("The overlay collection must contain at least one entry.", nameof(overlayCollection));
        if (cellCoords.Count == 0)
            throw new ArgumentException("At least one cell coordinate must be provided.", nameof(cellCoords));
    }

    private readonly OverlayCollection overlayCollection;
    private readonly List<Point2D> cellCoords;

    public override string GetDisplayString()
    {
        return $"Place overlay collection '{overlayCollection.Name}' on {cellCoords.Count} map cell(s)";
    }

    public override void Perform()
    {
        var distinctCellCoords = new HashSet<Point2D>();
        foreach (Point2D coords in cellCoords)
        {
            if (Map.GetTile(coords) == null)
                throw new InvalidOperationException($"Cell {coords} does not exist.");
            if (!distinctCellCoords.Add(coords))
                throw new InvalidOperationException($"Cell {coords} is included more than once.");
        }

        HashSet<Point2D> affectedCellCoords = GetCellsWithinRadius(distinctCellCoords, 2);
        CaptureOriginalOverlays(affectedCellCoords);

        try
        {
            foreach (Point2D coords in cellCoords)
            {
                MapTile mapTile = Map.GetTile(coords);
                var collectionEntry = overlayCollection.Entries[
                    MutationTarget.Randomizer.GetRandomNumber(0, overlayCollection.Entries.Length - 1)];

                if (collectionEntry.OverlayType.Tiberium)
                {
                    ITileImage tileGraphics = Map.TheaterInstance.GetTile(mapTile.TileIndex);
                    ISubTileImage subCellImage = tileGraphics.GetSubTile(mapTile.SubTileIndex);
                    if (Helpers.IsLandTypeImpassable(subCellImage.TmpImage.TerrainType, true))
                        continue;
                }

                mapTile.Overlay = new Overlay
                {
                    Position = coords,
                    OverlayType = collectionEntry.OverlayType,
                    FrameIndex = collectionEntry.Frame
                };
            }

            SmoothResourceOverlays(affectedCellCoords);
        }
        catch
        {
            RestoreOriginalOverlays();
            MutationTarget.InvalidateMap();
            throw;
        }

        MutationTarget.InvalidateMap();
    }

    public override void Undo()
    {
        RestoreOriginalOverlays();
        MutationTarget.InvalidateMap();
    }
}
