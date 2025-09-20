// Using clauses.
// Unless you know what's in the WAE code-base, you want to always include
// these "standard usings".
using System;
using TSMapEditor;
using TSMapEditor.Models;
using TSMapEditor.CCEngine;
using TSMapEditor.Rendering;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;

namespace WAEScript
{
    public class ReplaceAutumnGrassWithTallGrass
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => Translator.Translate("MapScripts.ReplaceAutumnGrassTallGrass.Description", "This script replaces all autumn grass terrain with summer tall grass terrain. Continue?");

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            return error ?? string.Format(Translator.Translate("MapScripts.ReplaceAutumnGrassTallGrass.SuccessMessage", 
                "Successfully replaced the terrain of {0} cells."), count);
        }

        int count = 0;
        string error = null;

        /// <summary>
        /// The function that actually does the magic.
        /// </summary>
        /// <param name="map">Map argument that allows us to access map data.</param>
        public void Perform(Map map)
        {
            // Fetch the tile sets.
            var tallGrassTileSet = map.TheaterInstance.Theater.TileSets.Find(tileSet => tileSet.SetName == "Tall Grass");
            var tallGrassLATTileSet = map.TheaterInstance.Theater.TileSets.Find(tileSet => tileSet.SetName == "Tall/Short Grass LAT");
            var autumnGrassTileSet = map.TheaterInstance.Theater.TileSets.Find(tileSet => tileSet.SetName == "Autumn Grass");
            var autumnGrassLATTileSet = map.TheaterInstance.Theater.TileSets.Find(tileSet => tileSet.SetName == "Autumn Grass/Clear LAT");

            if (tallGrassTileSet == null)
            {
                error = Translator.Translate("MapScripts.ReplaceAutumnGrassTallGrass.Errors.TallGrassTileSet", "Failed to find tall grass TileSet!");
                return;
            }

            if (tallGrassLATTileSet == null)
            {
                error = Translator.Translate("MapScripts.ReplaceAutumnGrassTallGrass.Errors.TallGrassTileSetLAT", "Failed to find tall grass LAT transition TileSet!");
                return;
            }

            if (autumnGrassTileSet == null)
            {
                error = Translator.Translate("MapScripts.ReplaceAutumnGrassTallGrass.Errors.AutumnGrassTileSet", "Failed to find autumn grass TileSet!");
                return;
            }

            if (autumnGrassLATTileSet == null)
            {
                error = Translator.Translate("MapScripts.ReplaceAutumnGrassTallGrass.Errors.AutumnGrassTileSetLAT", "Failed to find autumn grass LAT transition TileSet!");
                return;
            }

            // Go through all cells on the map.
            map.DoForAllValidTiles(cell =>
            {
                // Check whether the cell in question has autumn grass.
                // If it does, replace its tile with tall grass.
                if (autumnGrassTileSet.ContainsTile(cell.TileIndex))
                {
                    byte subTileIndex = 0; // LAT terrain does not use sub-tiles.
                    cell.ChangeTileIndex(tallGrassTileSet.StartTileIndex, subTileIndex);
                    count++;
                    return;
                }

                // If the cell instead contains autumn-to-grass transition / LAT,
                // replace it with the respective tall-grass-to-grass transition tile.
                if (autumnGrassLATTileSet.ContainsTile(cell.TileIndex))
                {
                    // Calculate the index of the tile within the LAT transition tileset.
                    int indexWithinTileSet = cell.TileIndex - autumnGrassLATTileSet.StartTileIndex;

                    // Based on the index above, calculate the new tile index.
                    int newTileIndex = tallGrassLATTileSet.StartTileIndex + indexWithinTileSet;

                    byte subTileIndex = 0; // LAT terrain does not use sub-tiles.
                    cell.ChangeTileIndex(newTileIndex, subTileIndex);
                    count++;
                }
            });
        }
    }
}