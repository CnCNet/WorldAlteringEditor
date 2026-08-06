using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing infantry on the map.
/// </summary>
public class PlaceInfantryMutation : Mutation
{
    public PlaceInfantryMutation(IMutationTarget mutationTarget, InfantryType infantryType, Point2D cellCoords, SubCell subCell)
        : this(mutationTarget, infantryType, cellCoords, subCell, mutationTarget.ObjectOwner)
    {
    }

    public PlaceInfantryMutation(IMutationTarget mutationTarget, InfantryType infantryType, Point2D cellCoords,
        SubCell subCell, House owner) : base(mutationTarget)
    {
        this.infantryType = infantryType;
        this.cellCoords = cellCoords;
        this.subCell = subCell;
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public PlaceInfantryMutation(IMutationTarget mutationTarget, Infantry infantry) : base(mutationTarget)
    {
        preconfiguredInfantry = infantry ?? throw new ArgumentNullException(nameof(infantry));
        infantryType = infantry.ObjectType;
        cellCoords = infantry.Position;
        subCell = infantry.SubCell;
        owner = infantry.Owner ?? throw new ArgumentException("The infantry must have an owner.", nameof(infantry));
    }

    private readonly InfantryType infantryType;
    private readonly Point2D cellCoords;
    private readonly SubCell subCell;
    private readonly House owner;
    private readonly Infantry preconfiguredInfantry;

    private Infantry placedInfantry;

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", 
            "Place '{0}' at {1}"),
                infantryType.GetEditorDisplayName(), cellCoords);
    }

    public override void Perform()
    {
        var cell = MutationTarget.Map.GetTile(cellCoords);
        if (cell == null)
            throw new InvalidOperationException("Invalid cell coords");

        if (cell.Infantry[(int)subCell] != null)
            throw new InvalidOperationException(nameof(PlaceInfantryMutation) + ": cannot place infantry on an occupied sub-cell spot!");

        var infantry = preconfiguredInfantry ?? new Infantry(infantryType)
        {
            Owner = owner,
            Position = cellCoords,
            SubCell = subCell
        };
        placedInfantry = infantry;

        MutationTarget.Map.PlaceInfantry(infantry);
        MutationTarget.AddRefreshPoint(cellCoords);
    }

    public override void Undo()
    {
        MutationTarget.Map.RemoveInfantry(placedInfantry);
        MutationTarget.AddRefreshPoint(placedInfantry.Position);
    }
}
