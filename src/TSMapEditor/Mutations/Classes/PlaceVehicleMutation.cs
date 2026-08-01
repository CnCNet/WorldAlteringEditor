using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing a vehicle on the map.
    /// </summary>
    public class PlaceVehicleMutation : Mutation
    {
        public PlaceVehicleMutation(IMutationTarget mutationTarget, UnitType unitType, Point2D cellCoords)
            : this(mutationTarget, unitType, cellCoords, mutationTarget.ObjectOwner)
        {
        }

        public PlaceVehicleMutation(IMutationTarget mutationTarget, UnitType unitType, Point2D cellCoords, House owner) : base(mutationTarget)
        {
            this.unitType = unitType;
            this.cellCoords = cellCoords;
            this.owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
        }

        public PlaceVehicleMutation(IMutationTarget mutationTarget, Unit unit) : base(mutationTarget)
        {
            preconfiguredUnit = unit ?? throw new System.ArgumentNullException(nameof(unit));
            unitType = unit.ObjectType;
            cellCoords = unit.Position;
            owner = unit.Owner ?? throw new System.ArgumentException("The vehicle must have an owner.", nameof(unit));
        }

        private readonly UnitType unitType;
        private readonly Point2D cellCoords;
        private readonly House owner;
        private readonly Unit preconfiguredUnit;
        private Unit unit;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", 
                "Place '{0}' at {1}"),
                    unitType.GetEditorDisplayName(), cellCoords);
        }

        public override void Perform()
        {
            var cell = MutationTarget.Map.GetTileOrFail(cellCoords);

            unit = preconfiguredUnit ?? new Unit(unitType)
            {
                Owner = owner,
                Position = cellCoords
            };
            MutationTarget.Map.PlaceUnit(unit);
            MutationTarget.AddRefreshPoint(cellCoords);
        }

        public override void Undo()
        {
            var cell = MutationTarget.Map.GetTile(cellCoords);
            MutationTarget.Map.RemoveUnit(unit);
            MutationTarget.AddRefreshPoint(cellCoords);
        }
    }
}
