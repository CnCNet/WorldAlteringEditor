using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.I18N;
using Rampastring.Tools;

namespace TSMapEditor.Extensions
{
    /// <summary>
    /// Extension methods for WindowManager to support INI localization.
    /// </summary>
    public static class WindowManagerExtensions
    {
        /// <summary>
        /// Sets the INI attribute parser for UI controls to enable localization support.
        /// </summary>
        /// <param name="windowManager">The WindowManager instance.</param>
        /// <param name="parser">The INI attribute parser to use for localization.</param>
        public static void SetINIParser(this WindowManager windowManager, IControlINIAttributeParser parser)
        {
            // Access the ControlINIAttributeParsers list field
            var field = typeof(WindowManager).GetField("<ControlINIAttributeParsers>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Logger.Log($"[DEBUG] SetINIParser: ControlINIAttributeParsers field found = {field != null}");
            
            if (field != null)
            {
                var parsersList = field.GetValue(windowManager) as System.Collections.Generic.List<IControlINIAttributeParser>;
                if (parsersList != null)
                {
                    // Clear existing parsers and add our custom parser
                    parsersList.Clear();
                    parsersList.Add(parser);
                    Logger.Log($"[DEBUG] SetINIParser: Custom parser added to list. Total parsers: {parsersList.Count}");
                }
                else
                {
                    Logger.Log("[DEBUG] SetINIParser: ControlINIAttributeParsers field is null");
                }
            }
            else
            {
                Logger.Log("[DEBUG] SetINIParser: ControlINIAttributeParsers field not found");
            }
        }
    }
}