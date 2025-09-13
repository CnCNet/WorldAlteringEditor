// Using clauses.
// Unless you know what's in the WAE code-base, you want to always include
// these "standard usings".
using System;
using TSMapEditor;
using TSMapEditor.Models;
using TSMapEditor.CCEngine;
using TSMapEditor.Rendering;
using TSMapEditor.GameMath;

namespace WAEScript
{
    public class ReplaceAutumnTreesWithSummerTrees
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => Translate("MapScripts.ReplaceAutumNTreesSummerTrees.Description", "This script replaces all autumn trees with regular trees. Continue?");

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            return string.Format(Translate("MapScripts.ReplaceAutumNTreesSummerTrees.SuccessMessage", 
                "Replaced {0} terrain objects."), count);
        }

        int count = 0;

        /// <summary>
        /// The function that actually does the magic.
        /// </summary>
        /// <param name="map">Map argument that allows us to access map data.</param>
        public void Perform(Map map)
        {
            // Go through all cells on the map.
            map.DoForAllValidTiles(cell =>
            {
                // Skip cells that have no terrain object.
                if (cell.TerrainObject == null)
                    return;

                // Check if this cell has an autumn tree. Autumn tree terrain object types
                // begin their INI name with "AT".
                if (cell.TerrainObject.TerrainType.ININame.StartsWith("AT"))
                {
                    // To get the name of the respective summer tree,
                    // we simply remove the 'A' from the beginning of the terrain object type's INI name.
                    string summerTreeININame = cell.TerrainObject.TerrainType.ININame.Substring(1);

                    // Find the terrain object type of the summer tree.
                    // If it is not found for some reason, bail.
                    var terrainObjectType = map.Rules.TerrainTypes.Find(terrainType => terrainType.ININame == summerTreeININame);
                    if (terrainObjectType == null)
                        return;

                    // Remove the original terrain object (the autumn tree).
                    map.RemoveTerrainObject(cell.TerrainObject);

                    // Place a new terrain object (the summer tree).
                    var terrainObject = new TerrainObject(terrainObjectType, cell.CoordsToPoint());
                    map.AddTerrainObject(terrainObject);

                    count++;
                }
            });
        }
    }
}