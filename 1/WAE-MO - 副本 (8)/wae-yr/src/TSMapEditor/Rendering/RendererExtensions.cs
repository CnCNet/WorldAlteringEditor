using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;

namespace TSMapEditor.Rendering
{
    public static class RendererExtensions
    {
        public static void DrawArrow(
            Vector2 start,
            Vector2 end,
            Color color, 
            float angleDiff, 
            float sideLineLength, 
            int thickness = 1)
        {
            Vector2 line = end - start;
            float angle = Helpers.AngleFromVector(line) - (float)Math.PI;
            Renderer.DrawLine(start,
                end, color, thickness);
            Renderer.DrawLine(end, end + Helpers.VectorFromLengthAndAngle(sideLineLength, angle + angleDiff),
                color, thickness);
            Renderer.DrawLine(end, end + Helpers.VectorFromLengthAndAngle(sideLineLength, angle - angleDiff),
                color, thickness);
        }
    }
}
