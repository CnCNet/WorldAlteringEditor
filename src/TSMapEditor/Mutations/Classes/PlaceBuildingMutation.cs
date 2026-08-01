using TSMapEditor.GameMath;
using TSMapEditor.Models;
using System;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{

    /// <summary>
    /// A mutation that allows placing a building on the map.
    /// </summary>
    public class PlaceBuildingMutation : Mutation
    {
        public PlaceBuildingMutation(IMutationTarget mutationTarget, BuildingType buildingType, Point2D cellCoords)
            : this(mutationTarget, buildingType, cellCoords, mutationTarget.ObjectOwner)
        {
        }

        public PlaceBuildingMutation(IMutationTarget mutationTarget, BuildingType buildingType, Point2D cellCoords, House owner) : base(mutationTarget)
        {
            this.buildingType = buildingType;
            this.cellCoords = cellCoords;
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public PlaceBuildingMutation(IMutationTarget mutationTarget, Structure structure) : base(mutationTarget)
        {
            preconfiguredStructure = structure ?? throw new ArgumentNullException(nameof(structure));
            buildingType = structure.ObjectType;
            cellCoords = structure.Position;
            owner = structure.Owner ?? throw new ArgumentException("The building must have an owner.", nameof(structure));
        }

        private readonly BuildingType buildingType;
        private readonly Point2D cellCoords;
        private readonly House owner;
        private readonly Structure preconfiguredStructure;

        private Structure placedBuilding;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", 
                "Place '{0}' at {1}"),
                    buildingType.GetEditorDisplayName(), cellCoords);
        }

        public override void Perform()
        {
            var cell = MutationTarget.Map.GetTileOrFail(cellCoords);

            var structure = preconfiguredStructure ?? new Structure(buildingType)
            {
                Owner = owner,
                Position = cellCoords,
                AIRepairable = buildingType.Repairable && owner.DefaultRepairableStructures
            };
            MutationTarget.Map.PlaceBuilding(structure);
            MutationTarget.AddRefreshPoint(cellCoords);

            placedBuilding = structure;
            MutationTarget.AddRefreshPoint(cellCoords);
        }

        public override void Undo()
        {
            MutationTarget.Map.RemoveBuilding(placedBuilding);
            MutationTarget.AddRefreshPoint(cellCoords);
        }
    }
}
