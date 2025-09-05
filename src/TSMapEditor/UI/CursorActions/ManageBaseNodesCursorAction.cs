using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows adding and removing base nodes.
    /// </summary>
    public class ManageBaseNodesCursorAction : CursorAction
    {
        public ManageBaseNodesCursorAction(ICursorActionTarget cursorActionTarget, WindowManager windowManager) : base(cursorActionTarget)
        {
            this.windowManager = windowManager;
        }

        private readonly WindowManager windowManager;

        private BaseNode draggedBaseNode = null;
        private bool isDragging = false;

        public override string GetName() => Translate("Name", "Manage Base Nodes");

        public override bool DrawCellCursor => true;

        public override bool HandlesKeyboardInput => true;

        public override bool OnlyUniqueCellEvents => false;

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            string text = Translate("Text","Placement actions:") + Environment.NewLine +
                Translate("PlaceText","Click on building to place a base node.") + Environment.NewLine +
                Translate("DeleteSourceText", "Hold SHIFT while clicking to also delete the source building.") + Environment.NewLine +
                Translate("EraseText", "Hold CTRL while clicking to erase a base node.") + Environment.NewLine + Environment.NewLine +
                Translate("MoveText", "Hold M while dragging a base node to move it.") + Environment.NewLine + Environment.NewLine +
                Translate("OrderText", "Ordering actions:") + Environment.NewLine +
                Translate("EarlierText", "Press E while hovering over a base node to shift it to be built earlier.") + Environment.NewLine +
                Translate("LaterText", "Press D while hovering over a base node to shift it to be built later.");

            DrawText(cellCoords, cameraTopLeftPoint, 60, -240, text, UISettings.ActiveSettings.AltColor);

            if (isDragging)
            {
                var source = Is2DMode ? CellMath.CellCenterPointFromCellCoords(draggedBaseNode.Position, Map) : CellMath.CellCenterPointFromCellCoords_3D(draggedBaseNode.Position, Map);
                var destination = Is2DMode ? CellMath.CellCenterPointFromCellCoords(cellCoords, Map) : CellMath.CellCenterPointFromCellCoords_3D(cellCoords, Map);
                source = CursorActionTarget.Camera.ScalePointWithZoom(source - cameraTopLeftPoint);
                destination = CursorActionTarget.Camera.ScalePointWithZoom(destination - cameraTopLeftPoint);

                Renderer.DrawLine(source.ToXNAVector(), destination.ToXNAVector(), Color.White);
            }
        }

        public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
        {
            if (cellCoords == Point2D.NegativeOne)
                return;

            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.D)
            {
                ShiftBaseNodeLater(cellCoords);
                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.E)
            {
                ShiftBaseNodeEarlier(cellCoords);
                e.Handled = true;
            }
        }

        public override void LeftDown(Point2D cellCoords)
        {
            if (Keyboard.IsKeyHeldDown(Microsoft.Xna.Framework.Input.Keys.M))
            {
                if (!isDragging)
                {
                    var baseNode = GetBaseNodeFromCellCoords(cellCoords);
                    if (baseNode != null)
                    {
                        StartBaseNodeDrag(baseNode);
                    }
                }
            }
            else
            {
                StopBaseNodeDrag();
            }

            base.LeftDown(cellCoords);
        }

        public override void LeftUpOnMouseMove(Point2D cellCoords)
        {
            if (isDragging && !Keyboard.IsKeyHeldDown(Microsoft.Xna.Framework.Input.Keys.M))
            {
                StopBaseNodeDrag();
            }

            base.LeftUpOnMouseMove(cellCoords);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            if (isDragging)
            {
                draggedBaseNode.Position = cellCoords;

                StopBaseNodeDrag();
                CursorActionTarget.InvalidateMap();
                return;
            }

            if (Keyboard.IsCtrlHeldDown())
            {
                DeleteBaseNode(cellCoords);
            }
            else
            {
                CreateBaseNode(cellCoords);
            }

            base.LeftClick(cellCoords);
        }

        private void StartBaseNodeDrag(BaseNode draggedBaseNode)
        {
            this.draggedBaseNode = draggedBaseNode;
            isDragging = true;
        }

        private void StopBaseNodeDrag()
        {
            isDragging = false;
            draggedBaseNode = null;
        }

        // TODO implement all these manipulations as mutations so they go through the undo/redo system
        private void CreateBaseNode(Point2D cellCoords)
        {
            var mapCell = Map.GetTile(cellCoords);

            if (mapCell.Structures.Count == 0)
                return;

            var structureType = mapCell.Structures[0].ObjectType;
            var cellCoordsToCheck = new List<Point2D>();
            if (structureType.ArtConfig.Foundation.Width == 0 || structureType.ArtConfig.Foundation.Height == 0)
                cellCoordsToCheck.Add(cellCoords);

            for (int y = 0; y < structureType.ArtConfig.Foundation.Height; y++)
            {
                for (int x = 0; x < structureType.ArtConfig.Foundation.Width; x++)
                {
                    cellCoordsToCheck.Add(mapCell.Structures[0].Position + new Point2D(x, y));
                }
            }

            House owner = mapCell.Structures[0].Owner;

            bool overlappingNodes = false;

            // Make sure that a node doesn't already exist in this location for the same house
            foreach (Point2D structureFoundationPoint in cellCoordsToCheck)
            {
                foreach (BaseNode baseNode in owner.BaseNodes)
                {
                    var nodeStructureType = CursorActionTarget.Map.Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName);

                    if (nodeStructureType == null)
                        continue;

                    if (baseNode.Position == cellCoords)
                    {
                        overlappingNodes = true;
                        break;
                    }

                    bool baseNodeExistsOnFoundation = false;
                    nodeStructureType.ArtConfig.DoForFoundationCoords(foundationOffset =>
                    {
                        Point2D foundationCellCoords = baseNode.Position + foundationOffset;
                        if (foundationCellCoords == structureFoundationPoint)
                            baseNodeExistsOnFoundation = true;
                    });

                    if (baseNodeExistsOnFoundation)
                    {
                        overlappingNodes = true;
                        break;
                    }
                }

                if (overlappingNodes)
                    break;
            }

            if (overlappingNodes)
            {
                EditorMessageBox.Show(windowManager, "Error", "The house already has one or more base nodes on the cell!", MessageBoxButtons.OK);
                return;
            }

            // All OK, create the base node

            bool removeBuilding = CursorActionTarget.WindowManager.Keyboard.IsShiftHeldDown();

            if (mapCell.Structures[0].UpgradeCount > 0)
            {
                var messageBox = EditorMessageBox.Show(windowManager, "Create node for upgrades?", "The building has one or more upgrades. Do you also want to create a base node for them?", MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = _ => CreateNode(mapCell.Structures[0], true, removeBuilding);
                messageBox.NoClickedAction = _ => CreateNode(mapCell.Structures[0], false, removeBuilding);
                return;
            }

            CreateNode(mapCell.Structures[0], false, removeBuilding);
        }

        private void CreateNode(Structure structure, bool createForUpgrades, bool delete)
        {
            var baseNode = new BaseNode(structure.ObjectType.ININame, structure.Position);
            structure.Owner.BaseNodes.Add(baseNode);
            CursorActionTarget.Map.RegisterBaseNode(structure.Owner, baseNode);

            if (createForUpgrades)
            {
                for (int i = 0; i < structure.Upgrades.Length; i++)
                {
                    var upgrade = structure.Upgrades[i];
                    if (upgrade == null)
                        continue;

                    var upgradeBaseNode = new BaseNode(upgrade.ININame, structure.Position);
                    structure.Owner.BaseNodes.Add(upgradeBaseNode);
                    CursorActionTarget.Map.RegisterBaseNode(structure.Owner, upgradeBaseNode);
                }
            }

            if (delete)
            {
                CursorActionTarget.Map.RemoveBuilding(structure);
            }

            CursorActionTarget.AddRefreshPoint(structure.Position);
        }

        private int GetBaseNodeIndexForHouse(House house, Point2D cellCoords)
        {
            return house.BaseNodes.FindIndex(baseNode =>
            {
                var structureType = CursorActionTarget.Map.Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName);
                if (structureType == null)
                    return false;

                if (structureType.ArtConfig.Foundation.Width == 0 || structureType.ArtConfig.Foundation.Height == 0)
                    return baseNode.Position == cellCoords;

                bool clickedOnFoundation = false;
                structureType.ArtConfig.DoForFoundationCoords(foundationOffset =>
                {
                    if (foundationOffset + baseNode.Position == cellCoords)
                        clickedOnFoundation = true;
                });

                return clickedOnFoundation;
            });
        }

        private void DeleteBaseNode(Point2D cellCoords)
        {
            foreach (House house in Map.GetHouses())
            {
                int index = GetBaseNodeIndexForHouse(house, cellCoords);

                if (index > -1)
                {
                    var baseNode = house.BaseNodes[index];
                    house.BaseNodes.RemoveAt(index);
                    CursorActionTarget.Map.UnregisterBaseNode(baseNode);
                    CursorActionTarget.AddRefreshPoint(cellCoords);
                    return;
                }
            }
        }

        private void ShiftBaseNodeEarlier(Point2D cellCoords)
        {
            foreach (House house in Map.GetHouses())
            {
                int index = GetBaseNodeIndexForHouse(house, cellCoords);

                if (index > -1)
                {
                    if (index == 0)
                    {
                        house.BaseNodes.Swap(0, house.BaseNodes.Count - 1);
                    }
                    else
                    {
                        house.BaseNodes.Swap(index, index - 1);
                    }

                    CursorActionTarget.InvalidateMap();
                    return;
                }
            }
        }

        private void ShiftBaseNodeLater(Point2D cellCoords)
        {
            foreach (House house in Map.GetHouses())
            {
                int index = GetBaseNodeIndexForHouse(house, cellCoords);

                if (index > -1)
                {
                    if (index == house.BaseNodes.Count - 1)
                    {
                        house.BaseNodes.Swap(0, house.BaseNodes.Count - 1);
                    }
                    else
                    {
                        house.BaseNodes.Swap(index, index + 1);
                    }

                    CursorActionTarget.InvalidateMap();
                    return;
                }
            }
        }

        private BaseNode GetBaseNodeFromCellCoords(Point2D cellCoords)
        {
            foreach (House house in Map.GetHouses())
            {
                int index = GetBaseNodeIndexForHouse(house, cellCoords);

                if (index > -1)
                {
                    return house.BaseNodes[index];
                }
            }

            return null;
        }
    }
}
