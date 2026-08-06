using Microsoft.Xna.Framework;

namespace MapEditorMCP
{
    public interface IMapScreenCropper
    {
        bool TryRequestScreenCrop(Rectangle cellRectangle, CancellationToken cancellationToken, out Task<byte[]> screenCropTask);
        bool TryRequestWholeMapPreview(int maxPixelWidth, int maxPixelHeight, CancellationToken cancellationToken, out Task<byte[]> previewTask);
        void StopScreenCropRequests();
    }
}
