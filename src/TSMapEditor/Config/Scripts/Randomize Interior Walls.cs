// Script for randomizing interior walls
// in Dawn of the Tiberium Age maps.

// Using clauses.
// Unless you know what's in the WAE code-base, you want to always include
// these "standard usings".
using System;
using TSMapEditor;
using TSMapEditor.Models;
using TSMapEditor.CCEngine;
using TSMapEditor.Rendering;
using TSMapEditor.GameMath;
using System.Collections.Generic;
using TSMapEditor.Misc;

namespace WAEScript
{
    public class RandomizeInteriorWallsScript
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => Translator.Translate("MapScripts.RandomizeInteriorWalls.Description", "This script will randomize all interior walls on the map with random variants of interior walls to reduce repetition. Continue?");

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            if (error == null)
                return string.Format(Translator.Translate("MapScripts.RandomizeInteriorWalls.SuccessMessage",
                    "Successfully randomized {0} interior walls in the map."), modifiedWallsCount);

            return error;
        }

        private string error;

        private int modifiedWallsCount = 0;
        private const string interiorWallOverlayTypeName = "INTWALL1";
        private readonly List<int> frontInteriorWallFrames = new List<int> { 21, 22, 23 };
        private readonly List<int> backInteriorWallFrames = new List<int> { 27, 28, 29 };

        /// <summary>
        /// The function that actually does the magic.
        /// </summary>
        /// <param name="map">Map argument that allows us to access map data.</param>
        public void Perform(Map map)
        {
            var interiorWallOverlayType = map.Rules.OverlayTypes.Find(overlayType => overlayType.ININame == interiorWallOverlayTypeName);
            if (interiorWallOverlayType == null)
            {
                error = Translator.Translate("MapScripts.RandomizeInteriorWalls.Errors.NoInteriorWallOverlay", "Interior wall overlay collection was not found");
            }

            map.DoForAllValidTiles(mapCell =>
            {
                if (mapCell.Overlay == null)
                    return;

                if (mapCell.Overlay.OverlayType == null)
                    return;

                if (mapCell.Overlay.OverlayType.ININame != interiorWallOverlayType.ININame)
                    return;

                int overlayTypeFrameIndex = mapCell.Overlay.FrameIndex;

                if (!frontInteriorWallFrames.Contains(overlayTypeFrameIndex) && !backInteriorWallFrames.Contains(overlayTypeFrameIndex))
                    return;

                var random = new Random();
                bool isFrontInteriorWallFrame = frontInteriorWallFrames.Contains(overlayTypeFrameIndex);
                int overlayTypeFrameLength = isFrontInteriorWallFrame ? frontInteriorWallFrames.Count : backInteriorWallFrames.Count;

                int chosenElementIndex = random.Next(overlayTypeFrameLength);

                mapCell.Overlay = new Overlay()
                {
                    Position = mapCell.CoordsToPoint(),
                    OverlayType = interiorWallOverlayType,
                    FrameIndex = isFrontInteriorWallFrame ? frontInteriorWallFrames[chosenElementIndex] : backInteriorWallFrames[chosenElementIndex]
                };

                modifiedWallsCount++;
            });
        }
    }
}