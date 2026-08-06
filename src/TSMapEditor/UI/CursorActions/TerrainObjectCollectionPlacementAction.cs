using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using System;

namespace TSMapEditor.UI.CursorActions;

public class TerrainObjectCollectionPlacementAction : LineAndRegularPaintingAction
{
    public TerrainObjectCollectionPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    public override string GetName() => Translate("Name", "Place TerrainObject Collection");

    protected override bool PreventInputEventsOnPreviousCell => false; // Not needed, PlaceTerrainObjectCollectionMutation.ShouldPerform does the job already

    private TerrainObject terrainObject;
    private TerrainObjectCollection _terrainObjectCollection;
    public TerrainObjectCollection TerrainObjectCollection
    {
        get => _terrainObjectCollection;
        set
        {
            if (value.Entries.Length == 0)
            {
                throw new InvalidOperationException($"Terrain object collection {value.Name} has no terrain object entries!");
            }

            _terrainObjectCollection = value;
            terrainObject = new TerrainObject(_terrainObjectCollection.Entries[0].TerrainType);
        }
    }

    public override void OnActionExit()
    {
        ClearLinePreview();
        base.OnActionExit();
    }

    public override void PreMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ApplyLinePreview(cellCoords);
            return;
        }

        var cell = CursorActionTarget.Map.GetTile(cellCoords);
        if (cell.TerrainObject == null)
        {
            terrainObject.Position = cell.CoordsToPoint();
            cell.TerrainObject = terrainObject;
        }

        CursorActionTarget.AddRefreshPoint(cellCoords);
    }

    public override void PostMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ClearLinePreview();
            return;
        }

        var cell = CursorActionTarget.Map.GetTile(cellCoords);
        if (cell.TerrainObject == terrainObject)
        {
            cell.TerrainObject = null;
        }

        CursorActionTarget.AddRefreshPoint(cellCoords);
    }

    protected override bool CanDrawLinePreview() => _terrainObjectCollection != null;

    protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
    {
        return new PlaceTerrainObjectCollectionMutation(CursorActionTarget.MutationTarget, TerrainObjectCollection, cellCoords);
    }

    protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
    {
        return new PlaceTerrainObjectCollectionLineMutation(MutationTarget, TerrainObjectCollection, LineSourceCell.Value, direction, length);
    }

    protected override void ApplyLine(Point2D cellCoords)
    {
        if (_terrainObjectCollection != null)
        {
            (Direction direction, int length) = GetLineInformation(cellCoords);
            var mutation = CreateLinePlacementMutation(direction, length);
            PerformMutation(mutation);
        }
    }
}
