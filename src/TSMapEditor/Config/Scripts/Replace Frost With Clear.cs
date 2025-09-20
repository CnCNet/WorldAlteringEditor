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
    public class ReplaceFrostWithClearScript
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => Translator.Translate("MapScripts.ReplaceFrostWithClear.Description", "This script replaces all '---Frost' terrain tiles with clear terrain. Continue?");

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            return error ?? string.Format(Translator.Translate("MapScripts.ReplaceFrostWithClear.SuccessMessage", 
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
            // Fetch the tile set.
            var frostTileSet = map.TheaterInstance.Theater.TileSets.Find(tileSet => tileSet.SetName == "---Frost");

            if (frostTileSet == null)
            {
                error = Translator.Translate("MapScripts.ReplaceFrostWithClear.Errors.FrostTileSet", "Failed to find '---Frost' TileSet!");
                return;
            }

            // Go through all cells on the map.
            map.DoForAllValidTiles(cell =>
            {
                // Check whether the cell in question has frost.
                // If it does, replace its tile with clear ground.
                if (frostTileSet.ContainsTile(cell.TileIndex))
                {
                    cell.ChangeTileIndex(0, 0); // 0 as tile index and 0 as sub-tile index, resulting in clear ground
                    count++;
                    return;
                }
            });
        }
    }
}