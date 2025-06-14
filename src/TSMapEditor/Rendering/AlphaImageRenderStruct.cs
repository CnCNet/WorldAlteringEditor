using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering
{
    internal struct AlphaImageRenderStruct
    {
        public Point2D Point;
        public ShapeImage AlphaImage;
        public GameObject OwnerObject;

        public AlphaImageRenderStruct(Point2D point, ShapeImage alphaImage, GameObject ownerObject)
        {
            Point = point;
            AlphaImage = alphaImage;
            OwnerObject = ownerObject;
        }
    }
}
