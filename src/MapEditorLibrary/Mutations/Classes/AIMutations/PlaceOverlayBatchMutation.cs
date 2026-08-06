using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes.AIMutations;

/// <summary>
/// Places multiple explicitly selected overlays on individual map cells as one mutation.
/// </summary>
public sealed class PlaceOverlayBatchMutation : OverlayBatchMutationBase
{
    public PlaceOverlayBatchMutation(
        IMutationTarget mutationTarget,
        List<(OverlayType OverlayType, Point2D CellCoords, int? FrameIndex)> placements)
        : base(mutationTarget)
    {
        this.placements = placements ?? throw new ArgumentNullException(nameof(placements));

        if (placements.Count == 0)
            throw new ArgumentException("At least one overlay placement must be provided.", nameof(placements));
    }

    private readonly List<(OverlayType OverlayType, Point2D CellCoords, int? FrameIndex)> placements;

    public override string GetDisplayString()
    {
        return $"Place {placements.Count} overlay(s)";
    }

    public override void Perform()
    {
        var distinctCellCoords = new HashSet<Point2D>();
        foreach ((OverlayType overlayType, Point2D cellCoords, int? _) in placements)
        {
            if (overlayType == null)
                throw new InvalidOperationException("An overlay placement has no overlay type.");
            if (Map.GetTile(cellCoords) == null)
                throw new InvalidOperationException($"Cell {cellCoords} does not exist.");
            if (!distinctCellCoords.Add(cellCoords))
                throw new InvalidOperationException($"Cell {cellCoords} is included more than once.");
        }

        List<Point2D> automaticallyFramedCellCoords = placements
            .Where(placement => !placement.FrameIndex.HasValue)
            .Select(placement => placement.CellCoords)
            .ToList();
        HashSet<Point2D> cellsToSmooth = GetCellsWithinRadius(automaticallyFramedCellCoords, 2);
        HashSet<Point2D> cellsToCapture = new(distinctCellCoords);
        cellsToCapture.UnionWith(cellsToSmooth);
        CaptureOriginalOverlays(cellsToCapture);

        try
        {
            foreach ((OverlayType overlayType, Point2D cellCoords, int? frameIndex) in placements)
            {
                Map.GetTile(cellCoords).Overlay = new Overlay
                {
                    Position = cellCoords,
                    OverlayType = overlayType,
                    FrameIndex = frameIndex ?? 0
                };
            }

            var manuallyFramedCellCoords = placements
                .Where(placement => placement.FrameIndex.HasValue)
                .Select(placement => placement.CellCoords)
                .ToHashSet();
            SmoothResourceOverlays(cellsToSmooth, manuallyFramedCellCoords);
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
