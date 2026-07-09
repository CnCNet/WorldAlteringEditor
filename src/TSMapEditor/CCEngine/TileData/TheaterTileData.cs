using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.CCEngine.TileData
{
    class OverlayFrameInformation
    {
        private const string SHP_FILE_EXTENSION = ".SHP";
        private const string PNG_FILE_EXTENSION = ".PNG";

        public OverlayFrameInformation(Theater theater, CCFileManager fileManager, IReadOnlyList<OverlayType> overlayTypes)
        {
            this.theater = theater;
            this.fileManager = fileManager;

            ReadOverlayFrameCounts(overlayTypes);
        }

        private readonly Theater theater;
        private readonly CCFileManager fileManager;
        private readonly Dictionary<int, int> overlayFrameCounts = new Dictionary<int, int>();

        public int GetOverlayFrameCount(OverlayType overlayType)
        {
            if (overlayType == null)
                throw new ArgumentNullException(nameof(overlayType));

            if (!overlayFrameCounts.TryGetValue(overlayType.Index, out int frameCount))
                throw new KeyNotFoundException($"Overlay frame count for {overlayType.ININame} has not been loaded.");

            return frameCount;
        }

        public void ReadOverlayFrameCounts(IReadOnlyList<OverlayType> overlayTypes)
        {
            if (overlayTypes == null)
                throw new ArgumentNullException(nameof(overlayTypes));

            Logger.Log("Loading overlay frame counts.");

            overlayFrameCounts.Clear();

            for (int i = 0; i < overlayTypes.Count; i++)
            {
                OverlayType overlayType = overlayTypes[i];

                string imageName = GetOverlayImageName(overlayType);

                byte[] pngData = fileManager.LoadFile(imageName + PNG_FILE_EXTENSION);
                if (pngData != null)
                {
                    overlayFrameCounts[overlayType.Index] = GetLogicalOverlayFrameCount(1, 0);
                    continue;
                }

                (string shpFileName, byte[] shpData) = LoadOverlayShpData(overlayType, imageName);
                if (shpData == null)
                {
                    overlayFrameCounts[overlayType.Index] = 0;
                    continue;
                }

                var shpFile = new ShpFile(shpFileName);
                shpFile.ParseFromBuffer(shpData);

                overlayFrameCounts[overlayType.Index] = GetLogicalOverlayFrameCount(shpFile);
            }

            Logger.Log("Finished loading overlay frame counts.");
        }

        private static string GetOverlayImageName(OverlayType overlayType)
        {
            string imageName = overlayType.ININame;

            if (overlayType.ArtConfig.Image != null)
                imageName = overlayType.ArtConfig.Image;
            else if (overlayType.Image != null)
                imageName = overlayType.Image;

            return imageName;
        }

        private (string FileName, byte[] Data) LoadOverlayShpData(OverlayType overlayType, string imageName)
        {
            if (overlayType.ArtConfig.NewTheater)
            {
                string shpFileName = imageName + SHP_FILE_EXTENSION;
                string newTheaterImageName = shpFileName.Substring(0, 1) + theater.NewTheaterBuildingLetter + shpFileName.Substring(2);
                byte[] shpData = fileManager.LoadFile(newTheaterImageName);

                if (shpData != null)
                    return (newTheaterImageName, shpData);

                newTheaterImageName = shpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + shpFileName.Substring(2);
                shpData = fileManager.LoadFile(newTheaterImageName);
                return (newTheaterImageName, shpData);
            }

            string fileExtension = overlayType.ArtConfig.Theater ? theater.FileExtension : SHP_FILE_EXTENSION;
            string finalShpName = imageName + fileExtension;
            return (finalShpName, fileManager.LoadFile(finalShpName));
        }

        private static int GetLogicalOverlayFrameCount(ShpFile shpFile)
        {
            int frameCount = shpFile.FrameCount;
            int lastValidFrame = -1;

            for (int i = 0; i < frameCount; i++)
            {
                ShpFrameInfo frameInfo = shpFile.GetShpFrameInfo(i);
                if (frameInfo != null && frameInfo.DataOffset != 0)
                    lastValidFrame = i;
            }

            return GetLogicalOverlayFrameCount(frameCount, lastValidFrame);
        }

        private static int GetLogicalOverlayFrameCount(int frameCount, int lastValidFrame)
        {
            if (lastValidFrame == frameCount - 1)
                return frameCount / 2;

            return lastValidFrame + 1;
        }
    }

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

        public TheaterTileData(Theater theater, CCFileManager fileManager, Rules rules)
        {
            Theater = theater ?? throw new ArgumentNullException(nameof(theater));
            this.fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));

            ReadTileData();
            overlayFrameInformation = new OverlayFrameInformation(theater, fileManager, rules.OverlayTypes);
        }

        public Theater Theater { get; }

        public int TileCount => terrainTileDataList.Count;

        public TileImage GetTileImage(int id) => terrainTileDataList[id][0];

        public ITileImage GetTile(int id) => GetTileImage(id);

        public int GetTileSetId(int uniqueTileIndex) => GetTileImage(uniqueTileIndex).TileSetId;

        public int GetOverlayFrameCount(OverlayType overlayType) => overlayFrameInformation.GetOverlayFrameCount(overlayType);


        private readonly OverlayFrameInformation overlayFrameInformation;


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
