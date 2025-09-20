// Script for activating a mission assignment cursor

// Using clauses.
// Unless you know what's in the WAE code-base, you want to always include
// these "standard usings".
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using TSMapEditor;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.Scripts;
using TSMapEditor.UI;
using TSMapEditor.UI.Windows;
using TSMapEditor.Misc;

namespace WAEScript
{
    public class ActivateMissionAssignmentCursorAction
    {

        /// <summary>
        /// Our custom cursor action class activated by this script.
        /// Needs to be a class within a class, as the script runner
        /// only creates an instance of the first top-level class in a script file.
        /// </summary>
        public class AssignMissionCursorAction : CursorAction
        {
            public AssignMissionCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
            {
            }

            /// <summary>
            /// The name of the mission that this script assigns to units.
            /// You can change it to make this script apply a different action.
            /// </summary>
            private string missionName = "Area Guard";

            public override string GetName() => Translator.Translate("MapScripts.AreaGuardMissionCursorAction.Name", "Apply Area Guard Mission To Units");

            public override void LeftClick(Point2D cellCoords)
            {
                base.LeftClick(cellCoords);
                LeftDown(cellCoords);
            }

            public override void LeftDown(Point2D cellCoords)
            {
                var tile = Map.GetTile(cellCoords);
                tile.DoForAllVehicles(unit => unit.Mission = missionName);
                tile.DoForAllInfantry(infantry => infantry.Mission = missionName);
                tile.DoForAllAircraft(aircraft => aircraft.Mission = missionName);
            }

            /// <summary>
            /// Draws the preview for the cursor action.
            /// </summary>
            public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
            {
                // Draw rectangle for the brush (always 1x1).

                Func<Point2D, Map, Point2D> func = Is2DMode ? CellMath.CellTopLeftPointFromCellCoords : CellMath.CellTopLeftPointFromCellCoords_3D;

                Point2D topPixelPoint = func(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, 0);
                Point2D bottomPixelPoint = func(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY);
                Point2D leftPixelPoint = func(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(0, Constants.CellSizeY / 2);
                Point2D rightPixelPoint = func(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX, Constants.CellSizeY / 2);

                topPixelPoint = topPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
                bottomPixelPoint = bottomPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
                rightPixelPoint = rightPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
                leftPixelPoint = leftPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

                Color lineColor = Color.Orange;
                int thickness = 2;
                Renderer.DrawLine(topPixelPoint.ToXNAVector(), rightPixelPoint.ToXNAVector(), lineColor, thickness);
                Renderer.DrawLine(topPixelPoint.ToXNAVector(), leftPixelPoint.ToXNAVector(), lineColor, thickness);
                Renderer.DrawLine(rightPixelPoint.ToXNAVector(), bottomPixelPoint.ToXNAVector(), lineColor, thickness);
                Renderer.DrawLine(leftPixelPoint.ToXNAVector(), bottomPixelPoint.ToXNAVector(), lineColor, thickness);

                // Draw "Assign Mission" in the center of the area.
                Point2D cellCenterPoint;

                if (CursorActionTarget.Is2DMode)
                    cellCenterPoint = CellMath.CellCenterPointFromCellCoords(cellCoords, Map) - cameraTopLeftPoint;
                else
                    cellCenterPoint = CellMath.CellCenterPointFromCellCoords_3D(cellCoords, Map) - cameraTopLeftPoint;

                cellCenterPoint = cellCenterPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

                string text = Translator.Translate("MapScripts.AreaGuardMissionCursorAction.CursorActionText", "Assign Mission");
                var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
                int x = cellCenterPoint.X - (int)(textDimensions.X / 2);
                int y = cellCenterPoint.Y - (int)(textDimensions.Y / 2);

                Renderer.DrawStringWithShadow(text,
                    Constants.UIBoldFont,
                    new Vector2(x, y),
                    lineColor);
            }
        }

        /// <summary>
        /// This needs to be declared as 2 so the script runner knows we support ScriptDependencies.
        /// </summary>
        public int ApiVersion { get; } = 2;

        /// <summary>
        /// Script dependencies object that is assigned by editor when the script is run.
        /// Contains Map, CursorActionTarget (MapView instance), EditorState, WindowManager, and WindowController.
        /// </summary>
        public ScriptDependencies ScriptDependencies { get; set; }

        /// <summary>
        /// Main function for executing the script.
        /// </summary>
        public void Perform()
        {
            ScriptDependencies.EditorState.CursorAction = new AssignMissionCursorAction(ScriptDependencies.CursorActionTarget);
        }
    }
}