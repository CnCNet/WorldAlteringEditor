using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to configure which 
    /// object types should be copied when copying parts of a map.
    /// </summary>
    public class CopiedEntryTypesWindow : INItializableWindow
    {
        public CopiedEntryTypesWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private XNACheckBox chkTerrainTiles;
        private XNACheckBox chkOverlay;
        private XNACheckBox chkSmudges;
        private XNACheckBox chkTerrainObjects;
        private XNACheckBox chkStructures;
        private XNACheckBox chkVehicles;
        private XNACheckBox chkInfantry;


        public override void Initialize()
        {
            Name = nameof(CopiedEntryTypesWindow);
            base.Initialize();

            chkTerrainTiles = FindChild<XNACheckBox>(nameof(chkTerrainTiles));
            chkOverlay = FindChild<XNACheckBox>(nameof(chkOverlay));
            chkSmudges = FindChild<XNACheckBox>(nameof(chkSmudges));
            chkTerrainObjects = FindChild<XNACheckBox>(nameof(chkTerrainObjects));
            chkStructures = FindChild<XNACheckBox>(nameof(chkStructures));
            chkVehicles = FindChild<XNACheckBox>(nameof(chkVehicles));
            chkInfantry = FindChild<XNACheckBox>(nameof(chkInfantry));
        }

        public void Open()
        {
            Show();
        }

        public CopiedEntryType GetEnabledEntryTypes()
        {
            CopiedEntryType entryType = CopiedEntryType.Invalid;

            if (chkTerrainTiles.Checked)
                entryType |= CopiedEntryType.Terrain;
            if (chkOverlay.Checked)
                entryType |= CopiedEntryType.Overlay;
            if (chkSmudges.Checked)
                entryType |= CopiedEntryType.Smudge;
            if (chkTerrainObjects.Checked)
                entryType |= CopiedEntryType.TerrainObject;
            if (chkStructures.Checked)
                entryType |= CopiedEntryType.Structure;
            if (chkVehicles.Checked)
                entryType |= CopiedEntryType.Vehicle;
            if (chkInfantry.Checked)
                entryType |= CopiedEntryType.Infantry;

            return entryType;
        }
    }
}
