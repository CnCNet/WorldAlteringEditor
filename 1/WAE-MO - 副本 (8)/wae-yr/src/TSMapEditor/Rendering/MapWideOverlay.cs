using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System.IO;
using TSMapEditor.Settings;

#if WINDOWS
using System.Windows.Forms;
#endif

namespace TSMapEditor.Rendering
{
    public class MapWideOverlay
    {
        const string MapWideOverlayTextureName = "mapwideoverlay.png";

        public MapWideOverlay()
        {
            if (AssetLoader.AssetExists(MapWideOverlayTextureName))
                texture = AssetLoader.LoadTextureUncached(MapWideOverlayTextureName);

            Opacity = UserSettings.Instance.MapWideOverlayOpacity / 255.0f;
        }

        private Texture2D texture;

        public bool Enabled { get; set; }

        public float Opacity { get; private set; }

        public bool HasTexture => texture != null;

        public void Clear()
        {
            if (texture != null)
                texture.Dispose();

            texture = null;
        }

        public void LoadMapWideOverlay(GraphicsDevice graphicsDevice)
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : UserSettings.Instance.LastScenarioPath.GetValue();

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                openFileDialog.FileName = string.Empty;
                openFileDialog.Filter = "PNG images|*.png";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string texturePath = openFileDialog.FileName;
                    using (var stream = File.OpenRead(texturePath))
                    {
                        texture = Texture2D.FromStream(graphicsDevice, stream);
                    }
                }
            }
#else
            // TODO implement
#endif
        }

        public void Draw(Rectangle cameraRectangle)
        {
            if (Enabled && texture != null)
            {
                Renderer.DrawTexture(texture,
                    cameraRectangle,
                    Color.White * Opacity);
            }
        }
    }
}
