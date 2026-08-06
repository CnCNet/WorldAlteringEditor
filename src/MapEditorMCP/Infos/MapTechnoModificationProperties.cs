using System.ComponentModel;

namespace MapEditorMCP.Infos;

internal class MapTechnoReference
{
    [Description("Stable ID of a techno currently present on the open map, returned by get_technos or inspect_map_region.")]
    public int ObjectId { get; set; }
}

internal class MapTechnoModificationProperties
{
    [Description("INI name of a house returned by get_houses.")]
    public string Owner { get; set; }

    [Description("Health from 1 through 256.")]
    public int? Health { get; set; }

    [Description("Facing from 0 through 255.")]
    public int? Facing { get; set; }

    [Description("Tag ID or unique tag name to attach.")]
    public string AttachedTag { get; set; }

    [Description("Whether to remove the currently attached tag. Cannot be combined with attachedTag.")]
    public bool ClearAttachedTag { get; set; }

    [Description("Mission for Unit, Infantry, or Aircraft objects.")]
    public string Mission { get; set; }

    [Description("Veterancy for Unit, Infantry, or Aircraft objects. Valid values are 0, 50, 100, 150, and 200.")]
    public int? Veterancy { get; set; }

    [Description("Group number for Unit, Infantry, or Aircraft objects.")]
    public int? Group { get; set; }

    [Description("On-bridge state for Unit or Infantry objects. Not valid for Aircraft.")]
    public bool? OnBridge { get; set; }

    public bool? AutocreateNoRecruitable { get; set; }
    public bool? AutocreateYesRecruitable { get; set; }

    public bool? AISellable { get; set; }
    public bool? AIRebuildable { get; set; }
    public bool? Powered { get; set; }
    public bool? AIRepairable { get; set; }
    public bool? Nominal { get; set; }

    [Description("Building spotlight mode: 0 for none, 1 for reciprocating, or 2 for loop.")]
    public int? Spotlight { get; set; }

    public string Upgrade1 { get; set; }
    public string Upgrade2 { get; set; }
    public string Upgrade3 { get; set; }
    public bool ClearUpgrade1 { get; set; }
    public bool ClearUpgrade2 { get; set; }
    public bool ClearUpgrade3 { get; set; }
}

internal class MapTechnoQueryResult
{
    public MapTechnoQueryResult(int revision, List<MapTechnoInfo> technos)
    {
        Revision = revision;
        Technos = technos;
    }

    public int Revision { get; }
    public List<MapTechnoInfo> Technos { get; }
}
