using System.ComponentModel;

namespace TSMapEditor.AI;

public class MapFootPlacementProperties
{
    [Description("Initial health from 1 through 256. Omit to use full health.")]
    public int? Health { get; set; }

    [Description("Initial facing from 0 through 255. Omit to use 0.")]
    public int? Facing { get; set; }

    [Description("Initial mission. Valid values are Ambush, Area Guard, Attack, Capture, Construction, Enter, Guard, Harmless, Harvest, Hunt, Missile, Move, Open, Patrol, QMove, Repair, Rescue, Retreat, Return, Sabotage, Selling, Sleep, Sticky, Stop, and Unload.")]
    public string Mission { get; set; }

    [Description("Initial veterancy. Valid values are 0, 50, 100, 150, and 200.")]
    public int? Veterancy { get; set; }

    [Description("Initial group number. Omit to use -1.")]
    public int? Group { get; set; }

    [Description("Whether a vehicle or infantry object is on a bridge. Not valid for aircraft.")]
    public bool? OnBridge { get; set; }

    [Description("Whether the object can be recruited when Autocreate is disabled.")]
    public bool? AutocreateNoRecruitable { get; set; }

    [Description("Whether the object can be recruited when Autocreate is enabled.")]
    public bool? AutocreateYesRecruitable { get; set; }

    [Description("Tag ID or unique tag name to attach to the object.")]
    public string AttachedTag { get; set; }

    [Description("Infantry subcell: 2 for Right, 3 for Left, or 4 for Bottom. Not valid for aircraft or vehicles. Omit to use the first free usable subcell.")]
    public int? SubCell { get; set; }
}
