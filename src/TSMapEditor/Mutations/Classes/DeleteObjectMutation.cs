using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class DeleteObjectMutation : Mutation
    {
        public DeleteObjectMutation(IMutationTarget mutationTarget, Point2D cellCoords, BrushSize brushSize, DeletionMode deletionMode) : base(mutationTarget)
        {
            this.cellCoords = cellCoords;
            this.brushSize = brushSize;
            this.deletionMode = deletionMode;
        }

        private readonly Point2D cellCoords;
        private readonly BrushSize brushSize;
        private readonly DeletionMode deletionMode;

        private List<AbstractObject> deletedObjects = new List<AbstractObject>();

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString",
                "Delete {0} objects at {1} with a brush size of {2}"),
                    DeletedCount, cellCoords, brushSize);
        }

        public int DeletedCount => deletedObjects.Count;

        private DeletionMode DeletionModeFromObject(AbstractObject obj)
        {
            switch (obj.WhatAmI())
            {
                case RTTIType.CellTag:
                    return DeletionMode.CellTags;
                case RTTIType.Waypoint:
                    return DeletionMode.Waypoints;
                case RTTIType.Infantry:
                    return DeletionMode.Infantry;
                case RTTIType.Aircraft:
                    return DeletionMode.Aircraft;
                case RTTIType.Unit:
                    return DeletionMode.Vehicles;
                case RTTIType.Building:
                    return DeletionMode.Structures;
                case RTTIType.Terrain:
                    return DeletionMode.TerrainObjects;
                default:
                    throw new Exception($"{nameof(DeleteObjectMutation)}: Cannot set deletion mode from object of type " + obj.WhatAmI());
            }
        }

        public override void Perform()
        {
            // We adjust the deletion mode based on the first object we delete.
            // This is to prevent the mutation from deleting many objects of different types
            // when deleting objects over a larger area, as that kind of behaviour could be seen
            // untuitive for the user. It is better to only delete objects of one type with one
            // pass of the mutation, since that's how it works with the default 1x1 brush size
            // and we want to keep the behaviour consistent across brush sizes.
            DeletionMode adjustedDeletionMode = deletionMode;

            for (int yOffset = (brushSize.Height - 1) / -2; yOffset <= (brushSize.Height / 2); yOffset++)
            {
                for (int xOffset = (brushSize.Width - 1) / -2; xOffset <= (brushSize.Width / 2); xOffset++)
                {
                    Point2D adjustedCellCoords = cellCoords + new Point2D(xOffset, yOffset);

                    var deletedObject = Map.DeleteObjectFromCell(adjustedCellCoords, adjustedDeletionMode);
                    if (deletedObject != null)
                    {
                        adjustedDeletionMode = DeletionModeFromObject(deletedObject);
                        deletedObjects.Add(deletedObject);
                    }
                }
            }

            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brushSize.Width, brushSize.Height));
        }

        public override void Undo()
        {
            deletedObjects.ForEach(deletedObject =>
            {
                switch (deletedObject.WhatAmI())
                {
                    case RTTIType.CellTag:
                        Map.AddCellTag(deletedObject as CellTag);
                        break;
                    case RTTIType.Waypoint:
                        Map.AddWaypoint(deletedObject as Waypoint);
                        break;
                    case RTTIType.Infantry:
                        Map.PlaceInfantry(deletedObject as Infantry);
                        break;
                    case RTTIType.Aircraft:
                        Map.PlaceAircraft(deletedObject as Aircraft);
                        break;
                    case RTTIType.Unit:
                        Map.PlaceUnit(deletedObject as Unit);
                        break;
                    case RTTIType.Building:
                        Map.PlaceBuilding(deletedObject as Structure);
                        break;
                    case RTTIType.Terrain:
                        Map.AddTerrainObject(deletedObject as TerrainObject);
                        break;
                }
            });

            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brushSize.Width, brushSize.Height));
        }
    }
}
