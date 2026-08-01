using System.ComponentModel;

namespace TSMapEditor.AI;

public class MapBuildingPlacementProperties
{
    [Description("Initial health from 1 through 256. Omit to use full health.")]
    public int? Health { get; set; }

    [Description("Initial facing from 0 through 255. Omit to use 0.")]
    public int? Facing { get; set; }

    [Description("Tag ID or unique tag name to attach to the building.")]
    public string AttachedTag { get; set; }

    public bool? AISellable { get; set; }
    public bool? AIRebuildable { get; set; }
    public bool? Powered { get; set; }
    public bool? AIRepairable { get; set; }
    public bool? Nominal { get; set; }

    [Description("Spotlight mode: 0 for none, 1 for reciprocating, or 2 for loop.")]
    public int? Spotlight { get; set; }

    [Description("INI name of the first building upgrade.")]
    public string Upgrade1 { get; set; }

    [Description("INI name of the second building upgrade.")]
    public string Upgrade2 { get; set; }

    [Description("INI name of the third building upgrade.")]
    public string Upgrade3 { get; set; }
}
