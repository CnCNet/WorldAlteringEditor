using TSMapEditor.Extensions;

namespace TSMapEditor.I18N
{
    /// <summary>
    /// Example usage of L10N translation methods.
    /// </summary>
    public static class TranslationExample
    {
        /// <summary>
        /// Example L10N usage.
        /// </summary>
        public static void ExampleUsage()
        {
            // Basic usage
            string fileMenu = "File".L10N();
            string editMenu = "Edit".L10N();
            string viewMenu = "View".L10N();
            
            // With explicit key
            string newFile = "New".L10N("menu_file_new");
            string openFile = "Open".L10N("menu_file_open");
            string saveFile = "Save".L10N("menu_file_save");
            
            // INI-style keys
            string buttonText = "OK".L10N("MainWindow", "OKButton");
            string labelText = "Name:".L10N("PropertyPanel", "NameLabel");
            
            // Common UI strings
            string ok = "OK".L10N();
            string cancel = "Cancel".L10N();
            string yes = "Yes".L10N();
            string no = "No".L10N();
            string apply = "Apply".L10N();
            string close = "Close".L10N();
            
            // Menu items
            string tools = "Tools".L10N();
            string help = "Help".L10N();
            string options = "Options".L10N();
            string about = "About".L10N();
            
            // Common actions
            string undo = "Undo".L10N();
            string redo = "Redo".L10N();
            string cut = "Cut".L10N();
            string copy = "Copy".L10N();
            string paste = "Paste".L10N();
            string delete = "Delete".L10N();
            
            // Status messages
            string loading = "Loading...".L10N();
            string saving = "Saving...".L10N();
            string complete = "Complete".L10N();
            string error = "Error".L10N();
            string warning = "Warning".L10N();
            
            // Map editor specific strings
            string mapEditor = "Map Editor".L10N();
            string newMap = "New Map".L10N();
            string openMap = "Open Map".L10N();
            string saveMap = "Save Map".L10N();
            string mapProperties = "Map Properties".L10N();
            string terrain = "Terrain".L10N();
            string objects = "Objects".L10N();
            string triggers = "Triggers".L10N();
            string lighting = "Lighting".L10N();
        }
    }
}