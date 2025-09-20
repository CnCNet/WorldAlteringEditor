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
    public class ReplaceAutumnJungleGrassWithSummerGrass
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => Translator.Translate("MapScripts.ReplaceAutumnGrassSummerGrass.Description", "This script replaces all autumn grass overlay (AJGRASS) with summer jungle grass overlay (JGRASS). Continue?");

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            return error ?? string.Format(Translator.Translate("MapScripts.ReplaceAutumnGrassSummerGrass.Description",
                "Successfully replaced {0} AJGRASS overlay with JGRASS overlay."), count);
        }

        int count = 0;
        string error = null;

        /// <summary>
        /// The function that actually does the magic.
        /// </summary>
        /// <param name="map">Map argument that allows us to access map data.</param>
        public void Perform(Map map)
        {
            // Fetch the JGRASS (jungle grass, summer version) overlay.
            var jungleGrassOverlayType = map.Rules.OverlayTypes.Find(ot => ot.ININame == "JGRASS");

            if (jungleGrassOverlayType == null)
            {
                error = Translator.Translate("MapScripts.ReplaceAutumnGrassSummerGrass.Errors.JungleGrassOverlay", "Failed to find jungle grass overlay type (JGRASS)!");
                return;
            }

            // Go through all cells on the map.
            map.DoForAllValidTiles(cell =>
            {
                // Skip cells that have no overlay.
                if (cell.Overlay == null)
                    return;

                // Check whether the cell in question has AJGRASS overlay.
                // If it does, replace it with JGRASS overlay.
                if (cell.Overlay.OverlayType.ININame == "AJGRASS")
                {
                    cell.Overlay.OverlayType = jungleGrassOverlayType;
                    count++;
                }
            });
        }
    }
}