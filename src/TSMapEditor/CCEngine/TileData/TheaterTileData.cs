using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.CCEngine.TileData
{
    public interface ITheaterTileData
    {
        TileImage GetTileImage(int id);
    }

    /// <summary>
    /// Non-graphical equivalent of the TMP tile-loading portion of <see cref="TheaterGraphics"/>.
    /// </summary>
    public class TheaterTileData : ITheater, ITheaterTileData
    {
        private readonly CCFileManager fileManager;
        private readonly List<UntexturedTileImage[]> terrainTileDataList = new List<UntexturedTileImage[]>();

        public TheaterTileData(Theater theater, CCFileManager fileManager)
        {
            Theater = theater ?? throw new ArgumentNullException(nameof(theater));
            this.fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));

            ReadTileData();
        }

        public Theater Theater { get; }

        public int TileCount => terrainTileDataList.Count;

        public TileImage GetTileImage(int id) => terrainTileDataList[id][0];

        public ITileImage GetTile(int id) => GetTileImage(id);

        public int GetTileSetId(int uniqueTileIndex) => GetTileImage(uniqueTileIndex).TileSetId;

        public int GetOverlayFrameCount(OverlayType overlayType)
            => throw new NotSupportedException($"{nameof(TheaterTileData)} does not load overlay graphics.");

        private void ReadTileData()
        {
            Logger.Log("Loading tile data.");

            int currentTileIndex = 0; // Used for setting the starting tile ID of a tileset

            for (int tsId = 0; tsId < Theater.TileSets.Count; tsId++)
            {
                TileSet tileSet = Theater.TileSets[tsId];
                tileSet.StartTileIndex = currentTileIndex;
                tileSet.LoadedTileCount = 0;

                for (int i = 0; i < tileSet.TilesInSet; i++)
                {
                    var tileImages = new List<UntexturedTileImage>();

                    // Handle graphics variation (clear00.tem, clear00a.tem, clear00b.tem etc.).
                    // Even though this class does not create textures, variations can still carry
                    // distinct tile metadata, so we parse them just like TheaterGraphics does.
                    for (int v = 0; v < 'g' - 'a'; v++)
                    {
                        string baseName = tileSet.FileName + (i + 1).ToString("D2", CultureInfo.InvariantCulture);

                        if (v > 0)
                            baseName += (char)('a' + (v - 1));

                        string fileName = baseName + Theater.FileExtension;
                        byte[] data = fileManager.LoadFile(fileName);

                        if (data == null && !string.IsNullOrWhiteSpace(Theater.FallbackTileFileExtension))
                        {
                            // Support for the FA2 NEWURBAN hack. FA2 Marble.mix does not contain Marble
                            // Madness graphics for NEWURBAN, only URBAN. To allow Marble Madness to work
                            // in NEWURBAN, FA2 also loads .urb files for NEWURBAN.
                            fileName = baseName + Theater.FallbackTileFileExtension;
                            data = fileManager.LoadFile(fileName);
                        }

                        if (data == null)
                        {
                            if (v == 0)
                            {
                                tileImages.Add(new UntexturedTileImage(0, 0, tsId, i, currentTileIndex, Array.Empty<TmpImage>()));
                                break;
                            }

                            break;
                        }

                        var tmpFile = new TmpFile(fileName);
                        tmpFile.ParseFromBuffer(data);

                        var tmpImages = new List<TmpImage>();
                        for (int img = 0; img < tmpFile.ImageCount; img++)
                        {
                            TmpImage tmpImage = tmpFile.GetImage(img);
                            tmpImage?.FreeImageData();
                            tmpImages.Add(tmpImage);
                        }

                        tileImages.Add(new UntexturedTileImage(tmpFile.CellsX, tmpFile.CellsY, tsId, i, currentTileIndex, tmpImages.ToArray()));
                    }

                    tileSet.LoadedTileCount++;
                    currentTileIndex++;
                    terrainTileDataList.Add(tileImages.ToArray());
                }
            }

            Logger.Log("Finished loading tile data.");
        }
    }
}
