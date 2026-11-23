using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// Paints overlays and updates resource density after all counterparts have been placed.
/// </summary>
public class PlaceMirroredOverlaysMutation : Mutation
{
    public PlaceMirroredOverlaysMutation(IMutationTarget mutationTarget, IReadOnlyList<(Point2D Cell, OverlayType Type, int Frame)> placements,
        bool updateResourceFrames) : base(mutationTarget)
    {
        this.placements = placements;
        this.updateResourceFrames = updateResourceFrames;
    }

    private readonly IReadOnlyList<(Point2D Cell, OverlayType Type, int Frame)> placements;
    private readonly bool updateResourceFrames;
    private readonly Dictionary<MapTile, (Overlay Overlay, int Frame)> original = new Dictionary<MapTile, (Overlay, int)>();

    public override string GetDisplayString() => Translate("MirroredPlacementMutation.DisplayString", "Mirrored placement");

    public override void Perform()
    {
        original.Clear();

        foreach (var placement in placements)
        {
            var cell = Map.GetTileOrFail(placement.Cell);
            RecordOriginal(cell);
            cell.Overlay = placement.Type == null ? null : new Overlay
            {
                Position = placement.Cell,
                OverlayType = placement.Type,
                FrameIndex = placement.Frame
            };
        }

        if (updateResourceFrames)
            UpdateResourceFrames();

        MutationTarget.InvalidateMap();
    }

    private void UpdateResourceFrames()
    {
        var affected = new HashSet<Point2D>();
        var brush = new BrushSize(1, 1);

        foreach (var placement in placements)
        {
            foreach (Point2D coords in brush.GetCells(placement.Cell, 1))
                affected.Add(coords);
        }

        foreach (Point2D coords in affected)
        {
            var cell = Map.GetTile(coords);
            if (cell?.Overlay?.OverlayType?.Tiberium != true)
                continue;

            RecordOriginal(cell);
            cell.Overlay.FrameIndex = Map.GetOverlayFrameIndex(coords);
        }
    }

    private void RecordOriginal(MapTile cell) => original.TryAdd(cell, (cell.Overlay, cell.Overlay?.FrameIndex ?? 0));

    public override void Undo()
    {
        foreach (var entry in original)
        {
            entry.Key.Overlay = entry.Value.Overlay;
            if (entry.Value.Overlay != null)
                entry.Value.Overlay.FrameIndex = entry.Value.Frame;
        }

        MutationTarget.InvalidateMap();
    }
}
