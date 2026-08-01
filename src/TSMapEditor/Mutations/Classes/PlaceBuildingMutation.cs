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

        private readonly BuildingType buildingType;
        private readonly Point2D cellCoords;
        private readonly House owner;

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

            var structure = new Structure(buildingType);
            structure.Owner = owner;
            structure.Position = cellCoords;
            structure.AIRepairable = structure.ObjectType.Repairable && structure.Owner.DefaultRepairableStructures;
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
