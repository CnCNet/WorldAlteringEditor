using System;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using System.Collections.Generic;
using MapEditorLibrary.Mutations.Classes;
using Rampastring.XNAUI.Input;

namespace TSMapEditor.UI.CursorActions;

public class AircraftPlacementAction : CursorAction
{
    public AircraftPlacementAction(ICursorActionTarget cursorActionTarget, RKeyboard keyboard) : base(cursorActionTarget)
    {
        this.keyboard = keyboard;
    }

    public override string GetName() => Translate("Name", "Place Aircraft");

    private Aircraft aircraft;

    private AircraftType _aircraftType;

    private readonly RKeyboard keyboard;

    public AircraftType AircraftType
    {
        get => _aircraftType;
        set
        {
            if (_aircraftType != value)
            {
                _aircraftType = value;

                if (_aircraftType == null)
                {
                    aircraft = null;
                }
                else
                {
                    aircraft = new Aircraft(_aircraftType) { Owner = CursorActionTarget.MutationTarget.ObjectOwner };
                }
            }
        }
    }

    public override void OnActionEnter()
    {
        if (aircraft != null)
            aircraft.Owner = CursorActionTarget.MutationTarget.ObjectOwner;
    }

    public override void PreMapDraw(Point2D cellCoords)
    {
        // Assign preview data
        aircraft.Position = cellCoords;

        bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

        bool canPlace = Map.CanPlaceObjectAt(aircraft, cellCoords, false, overlapObjects);

        if (!canPlace)
            return;

        var tile = CursorActionTarget.Map.GetTile(cellCoords);
        tile.Aircraft.Add(aircraft);
        CursorActionTarget.TechnoUnderCursor = aircraft;
        CursorActionTarget.AddRefreshPoint(cellCoords);
    }

    public override void PostMapDraw(Point2D cellCoords)
    {
        // Clear preview data
        var tile = CursorActionTarget.Map.GetTile(cellCoords);
        if (tile.Aircraft.Contains(aircraft))
        {
            tile.Aircraft.Remove(aircraft);
            CursorActionTarget.TechnoUnderCursor = null;
            CursorActionTarget.AddRefreshPoint(cellCoords);
        }
    }

    public override IEnumerable<(Point2D Cell, bool CanPlace)> GetMirroredPreview(Point2D cellCoords) =>
        GetMirroredTechnoPreview(cellCoords, aircraft);

    public override void LeftDown(Point2D cellCoords)
    {
        if (AircraftType == null)
            throw new InvalidOperationException(nameof(AircraftType) + " cannot be null");

        if (TryPlaceMirroredTechno(cellCoords, aircraft))
            return;

        bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

        bool canPlace = Map.CanPlaceObjectAt(aircraft, cellCoords, false,
            overlapObjects);

        if (!canPlace)
            return;

        var mutation = new PlaceAircraftMutation(CursorActionTarget.MutationTarget, AircraftType, cellCoords);
        CursorActionTarget.MutationManager.PerformMutation(mutation);
    }

    public override void LeftClick(Point2D cellCoords)
    {
        LeftDown(cellCoords);
    }
}
