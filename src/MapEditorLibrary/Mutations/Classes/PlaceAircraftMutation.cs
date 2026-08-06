using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing aircraft on the map.
/// </summary>
public class PlaceAircraftMutation : Mutation
{
    public PlaceAircraftMutation(IMutationTarget mutationTarget, AircraftType aircraftType, Point2D cellCoords)
        : this(mutationTarget, aircraftType, cellCoords, mutationTarget.ObjectOwner)
    {
    }

    public PlaceAircraftMutation(IMutationTarget mutationTarget, AircraftType aircraftType, Point2D cellCoords, House owner) : base(mutationTarget)
    {
        this.aircraftType = aircraftType;
        this.cellCoords = cellCoords;
        this.owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
    }

    public PlaceAircraftMutation(IMutationTarget mutationTarget, Aircraft aircraft) : base(mutationTarget)
    {
        preconfiguredAircraft = aircraft ?? throw new System.ArgumentNullException(nameof(aircraft));
        aircraftType = aircraft.ObjectType;
        cellCoords = aircraft.Position;
        owner = aircraft.Owner ?? throw new System.ArgumentException("The aircraft must have an owner.", nameof(aircraft));
    }

    private readonly AircraftType aircraftType;
    private readonly Point2D cellCoords;
    private readonly House owner;
    private readonly Aircraft preconfiguredAircraft;
    private Aircraft aircraft;

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", 
            "Place '{0}' at {1}"),
                aircraftType.GetEditorDisplayName(), cellCoords);
    }

    public override void Perform()
    {
        var cell = MutationTarget.Map.GetTile(cellCoords);
        if (cell == null)
            return;

        aircraft = preconfiguredAircraft ?? new Aircraft(aircraftType)
        {
            Owner = owner,
            Position = cellCoords
        };
        MutationTarget.Map.PlaceAircraft(aircraft);
        MutationTarget.AddRefreshPoint(cellCoords);
    }

    public override void Undo()
    {
        var cell = MutationTarget.Map.GetTile(cellCoords);
        MutationTarget.Map.RemoveAircraft(aircraft);
        MutationTarget.AddRefreshPoint(cellCoords);
    }
}
