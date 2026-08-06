using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework;

namespace TSMapEditor.Rendering.ObjectRenderers;

internal class AircraftRenderer : ObjectRenderer<Aircraft>
{
    public AircraftRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
    {
    }

    protected override Color ReplacementColor => Color.HotPink;

    protected override CommonDrawParams GetDrawParams(Aircraft gameObject)
    {
        return new CommonDrawParams()
        {
            IniName = gameObject.ObjectType.ININame,
            MainVoxel = TheaterGraphics.AircraftModels[gameObject.ObjectType.Index]
        };
    }

    protected override float GetDepthAddition(Aircraft gameObject)
    {
        return Constants.DepthEpsilon * ObjectDepthAdjustments.Aircraft;
    }

    protected override double GetExtraLight(Aircraft gameObject) => Map.Rules.ExtraAircraftLight;

    protected override void Render(Aircraft gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
    {
        DrawVoxelModel(gameObject, drawParams.MainVoxel,
            gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
            Constants.VoxelsAffectedByLighting, drawPoint, 0f);

        if (gameObject.Veterancy > 0)
        {
            DrawShapeImage(gameObject, TheaterGraphics.PipTextures, TheaterGraphics.GetVeterancyFrame(gameObject.Veterancy),
                Color.White, false, Color.White, false, false,
                drawPoint + new Point2D(Constants.CellSizeX / 8, Constants.CellSizeY / 8),
                100.0f);
        }
    }
}
