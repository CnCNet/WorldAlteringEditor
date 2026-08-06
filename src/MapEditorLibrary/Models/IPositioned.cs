using MapEditorLibrary.GameMath;

namespace MapEditorLibrary.Models;

public interface IPositioned
{
    Point2D Position { get; set; }

    bool IsOnBridge();
}
