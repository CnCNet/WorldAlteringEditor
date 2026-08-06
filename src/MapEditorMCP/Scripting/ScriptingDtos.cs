using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MapEditorMCP.Scripting;

/// <summary>
/// The scripting element kinds supported by the scripting MCP surface.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScriptingElementKind
{
    TaskForce,
    Script,
    TeamType,
    AITrigger,
    LocalVariable,
    Trigger,
    Tag,

    /// <summary>
    /// A read-only reference source representing a placed aircraft, infantry, vehicle, or structure.
    /// </summary>
    MapTechno,

    /// <summary>
    /// A read-only reference source representing a cell tag on the map.
    /// </summary>
    CellTag
}

/// <summary>
/// Identifies either an existing scripting element or an element being created in the same change set.
/// Exactly one of <see cref="Id"/>, <see cref="Index"/>, and <see cref="TemporaryKey"/> should be supplied.
/// </summary>
[Description("Identifies a scripting element. Supply exactly one selector: id for an existing permanent element, " +
    "index for a LocalVariable, or temporaryKey for an element created in the same change set.")]
internal sealed class ScriptingElementReference
{
    [JsonRequired]
    public ScriptingElementKind Kind { get; set; }

    [Description("Permanent INI name or ID of an existing element. Not used for local variables.")]
    public string Id { get; set; }

    [Description("Permanent numeric index of an existing local variable.")]
    public int? Index { get; set; }

    [Description("Request-scoped key for an element created in the same atomic change set; it is not stored as the element's name.")]
    public string TemporaryKey { get; set; }
}

/// <summary>
/// A semantic parameter value supplied for a script action, trigger event, or trigger action.
/// The server interprets <see cref="Value"/> according to the configured parameter definition.
/// </summary>
internal sealed class ScriptingParameterValueInput
{
    [Description("Zero-based parameter position in the action or event definition.")]
    [JsonRequired]
    public int Index { get; set; }

    [Description("Semantic value, such as a number, preset label/value, INI name, or waypoint number. " +
        "For unknown definitions this is preserved as the raw value.")]
    public string Value { get; set; }

    [Description("Optional scripting-element reference for reference-valued parameters. Use this instead of value when applicable.")]
    public ScriptingElementReference Reference { get; set; }
}

internal sealed class ScriptingElementIdentityInfo
{
    public ScriptingElementKind Kind { get; set; }
    public string Id { get; set; }
    public int? Index { get; set; }
    public string Name { get; set; }
    public string ContentHash { get; set; }
}

internal sealed class ScriptingReferenceInfo
{
    public ScriptingElementIdentityInfo Source { get; set; }
    public ScriptingElementIdentityInfo Target { get; set; }

    [Description("Stable property path describing where the source refers to the target.")]
    public string Path { get; set; }
}

internal sealed class ScriptingReferencesResult
{
    public ScriptingElementIdentityInfo Element { get; set; }
    public List<ScriptingReferenceInfo> IncomingReferences { get; set; } = new List<ScriptingReferenceInfo>();
    public List<ScriptingReferenceInfo> OutgoingReferences { get; set; } = new List<ScriptingReferenceInfo>();
}

internal sealed class TaskForceMemberInfo
{
    public int Index { get; set; }
    public string TechnoType { get; set; }
    public string DisplayName { get; set; }
    public int Count { get; set; }
}

internal sealed class TaskForceInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Group { get; set; }
    public bool IsGlobal { get; set; }
    public List<TaskForceMemberInfo> Members { get; set; } = new List<TaskForceMemberInfo>();
    public string ContentHash { get; set; }
}

internal sealed class TaskForcesResult
{
    public List<TaskForceInfo> TaskForces { get; set; } = new List<TaskForceInfo>();
}

internal sealed class ScriptActionInfo
{
    public int Index { get; set; }
    public int ActionId { get; set; }
    public string ActionName { get; set; }
    public int RawArgument { get; set; }
    public string Value { get; set; }
    public ScriptingElementReference Reference { get; set; }
}

internal sealed class ScriptInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string EditorColor { get; set; }
    public bool IsGlobal { get; set; }
    public List<ScriptActionInfo> Actions { get; set; } = new List<ScriptActionInfo>();
    public string ContentHash { get; set; }
}

internal sealed class ScriptsResult
{
    public List<ScriptInfo> Scripts { get; set; } = new List<ScriptInfo>();
}

internal sealed class TeamTypeInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Group { get; set; }
    public string HouseType { get; set; }
    public ScriptingElementReference Script { get; set; }
    public ScriptingElementReference TaskForce { get; set; }
    public ScriptingElementReference Tag { get; set; }
    public int Max { get; set; }
    public int Priority { get; set; }
    public int? Waypoint { get; set; }
    public int? TransportWaypoint { get; set; }
    public string RawWaypoint { get; set; }
    public string RawTransportWaypoint { get; set; }
    public int? MindControlDecision { get; set; }
    public int TechLevel { get; set; }
    public int VeteranLevel { get; set; }
    public List<string> EnabledFlags { get; set; } = new List<string>();
    public string EditorColor { get; set; }
    public bool IsGlobal { get; set; }
    public string ContentHash { get; set; }
}

internal sealed class TeamTypesResult
{
    public List<TeamTypeInfo> TeamTypes { get; set; } = new List<TeamTypeInfo>();
}

internal sealed class AITriggerComparatorInfo
{
    public int Operator { get; set; }
    public int Quantity { get; set; }
}

internal sealed class AITriggerInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ScriptingElementReference PrimaryTeam { get; set; }
    public string OwnerName { get; set; }
    public int TechLevel { get; set; }
    public int ConditionType { get; set; }
    public string ConditionObject { get; set; }
    public AITriggerComparatorInfo Comparator { get; set; }
    public double InitialWeight { get; set; }
    public double MinimumWeight { get; set; }
    public double MaximumWeight { get; set; }
    public bool EnabledInMultiplayer { get; set; }
    public bool Unused { get; set; }
    public int Side { get; set; }
    public bool IsBaseDefense { get; set; }
    public ScriptingElementReference SecondaryTeam { get; set; }
    public bool Easy { get; set; }
    public bool Medium { get; set; }
    public bool Hard { get; set; }
    public bool Enabled { get; set; }
    public string ContentHash { get; set; }
}

internal sealed class AITriggersResult
{
    public List<AITriggerInfo> AITriggers { get; set; } = new List<AITriggerInfo>();
}

internal sealed class LocalVariableInfo
{
    public int Index { get; set; }
    public string Name { get; set; }
    public int InitialState { get; set; }
    public string ContentHash { get; set; }
}

internal sealed class LocalVariablesResult
{
    public List<LocalVariableInfo> LocalVariables { get; set; } = new List<LocalVariableInfo>();
}

internal sealed class TriggerParameterInfo
{
    public int Index { get; set; }
    public string RawValue { get; set; }
    public string Value { get; set; }
    public ScriptingElementReference Reference { get; set; }
}

internal sealed class TriggerEventInfo
{
    public int Index { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; }
    public List<TriggerParameterInfo> Parameters { get; set; } = new List<TriggerParameterInfo>();
}

internal sealed class TriggerActionInfo
{
    public int Index { get; set; }
    public int ActionId { get; set; }
    public string ActionName { get; set; }
    public List<TriggerParameterInfo> Parameters { get; set; } = new List<TriggerParameterInfo>();
}

internal sealed class TriggerInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string HouseType { get; set; }
    public ScriptingElementReference LinkedTrigger { get; set; }
    public bool Disabled { get; set; }
    public bool Easy { get; set; }
    public bool Normal { get; set; }
    public bool Hard { get; set; }
    public string EditorColor { get; set; }
    public List<TriggerEventInfo> Events { get; set; } = new List<TriggerEventInfo>();
    public List<TriggerActionInfo> Actions { get; set; } = new List<TriggerActionInfo>();
    public List<string> TagIds { get; set; } = new List<string>();
    public string ContentHash { get; set; }
}

internal sealed class TriggersResult
{
    public List<TriggerInfo> Triggers { get; set; } = new List<TriggerInfo>();
}

internal sealed class TagInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Repeating { get; set; }
    public ScriptingElementReference Trigger { get; set; }
    public string ContentHash { get; set; }
}

internal sealed class TagsResult
{
    public List<TagInfo> Tags { get; set; } = new List<TagInfo>();
}

internal sealed class TaskForceMemberInput
{
    [Description("Optional TaskForce member slot from 0 through 5. Omit to use the first free slot.")]
    public int? Index { get; set; }
    [JsonRequired]
    public string TechnoType { get; set; }
    [JsonRequired]
    public int Count { get; set; }
}

internal sealed class TaskForceDataInput
{
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public int Group { get; set; } = -1;
    [JsonRequired]
    public List<TaskForceMemberInput> Members { get; set; } = new List<TaskForceMemberInput>();
}

internal sealed class TaskForceCreate
{
    public string TemporaryKey { get; set; }
    [JsonRequired]
    public TaskForceDataInput Value { get; set; }
}

internal sealed class TaskForceUpdate
{
    [JsonRequired]
    public string Id { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
    [JsonRequired]
    [Description("Complete replacement value. Omitted optional properties are cleared or reset to their documented defaults.")]
    public TaskForceDataInput Replacement { get; set; }
}

internal sealed class ScriptActionInput
{
    [JsonRequired]
    public int ActionId { get; set; }

    [Description("Semantic argument value. For an unknown action ID, this must be the exact raw integer value represented as text.")]
    public string Value { get; set; }

    public ScriptingElementReference Reference { get; set; }
}

internal sealed class ScriptDataInput
{
    [JsonRequired]
    public string Name { get; set; }
    public string EditorColor { get; set; }
    [JsonRequired]
    public List<ScriptActionInput> Actions { get; set; } = new List<ScriptActionInput>();
}

internal sealed class ScriptCreate
{
    public string TemporaryKey { get; set; }
    [JsonRequired]
    public ScriptDataInput Value { get; set; }
}

internal sealed class ScriptUpdate
{
    [JsonRequired]
    public string Id { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
    [JsonRequired]
    [Description("Complete replacement value. Omitted optional properties are cleared or reset to their documented defaults.")]
    public ScriptDataInput Replacement { get; set; }
}

internal sealed class TeamTypeDataInput
{
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public int Group { get; set; } = -1;
    [Description("Optional owner HouseType INI name. Null represents no owner.")]
    public string HouseType { get; set; }
    [JsonRequired]
    public ScriptingElementReference Script { get; set; }
    [JsonRequired]
    public ScriptingElementReference TaskForce { get; set; }
    public ScriptingElementReference Tag { get; set; }
    [JsonRequired]
    public int Max { get; set; }
    [JsonRequired]
    public int Priority { get; set; }
    public int? Waypoint { get; set; }
    public int? TransportWaypoint { get; set; }
    public int? MindControlDecision { get; set; }
    [JsonRequired]
    public int TechLevel { get; set; }
    [JsonRequired]
    public int VeteranLevel { get; set; } = 1;
    [Description("Enabled TeamType flag names. When omitted for a create, active configuration defaults are used; " +
        "use an empty list to explicitly enable none.")]
    public List<string> EnabledFlags { get; set; }
    public string EditorColor { get; set; }
}

internal sealed class TeamTypeCreate
{
    public string TemporaryKey { get; set; }
    [JsonRequired]
    public TeamTypeDataInput Value { get; set; }
}

internal sealed class TeamTypeUpdate
{
    [JsonRequired]
    public string Id { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
    [JsonRequired]
    [Description("Complete replacement value. Omitted optional properties are cleared or reset to their documented defaults.")]
    public TeamTypeDataInput Replacement { get; set; }
}

internal sealed class AITriggerDataInput
{
    [JsonRequired]
    public string Name { get; set; }
    public ScriptingElementReference PrimaryTeam { get; set; }
    public string OwnerName { get; set; }
    [JsonRequired]
    public int TechLevel { get; set; }
    [JsonRequired]
    public int ConditionType { get; set; } = -1;
    public string ConditionObject { get; set; }
    [JsonRequired]
    public int ComparatorOperator { get; set; }
    [JsonRequired]
    public int ComparatorQuantity { get; set; }
    [JsonRequired]
    public double InitialWeight { get; set; } = 50.0;
    [JsonRequired]
    public double MinimumWeight { get; set; } = 30.0;
    [JsonRequired]
    public double MaximumWeight { get; set; } = 70.0;
    [JsonRequired]
    public int Side { get; set; }
    public ScriptingElementReference SecondaryTeam { get; set; }
    [JsonRequired]
    public bool Easy { get; set; } = true;
    [JsonRequired]
    public bool Medium { get; set; } = true;
    [JsonRequired]
    public bool Hard { get; set; } = true;
    [JsonRequired]
    public bool Enabled { get; set; } = true;
}

internal sealed class AITriggerCreate
{
    public string TemporaryKey { get; set; }
    [JsonRequired]
    public AITriggerDataInput Value { get; set; }
}

internal sealed class AITriggerUpdate
{
    [JsonRequired]
    public string Id { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
    [JsonRequired]
    [Description("Complete replacement value. Omitted optional properties are cleared or reset to their documented defaults.")]
    public AITriggerDataInput Replacement { get; set; }
}

internal sealed class LocalVariableDataInput
{
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public int InitialState { get; set; }
}

internal sealed class LocalVariableCreate
{
    public string TemporaryKey { get; set; }

    [Description("Requested variable index, or null to allocate the first available index.")]
    public int? Index { get; set; }

    [JsonRequired]
    public LocalVariableDataInput Value { get; set; }
}

internal sealed class LocalVariableUpdate
{
    [JsonRequired]
    public int Index { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
    [JsonRequired]
    [Description("Complete replacement value.")]
    public LocalVariableDataInput Replacement { get; set; }
}

internal sealed class TriggerEventInput
{
    [JsonRequired]
    public int EventId { get; set; }
    [JsonRequired]
    public List<ScriptingParameterValueInput> Parameters { get; set; } = new List<ScriptingParameterValueInput>();
}

internal sealed class TriggerActionInput
{
    [JsonRequired]
    public int ActionId { get; set; }
    [JsonRequired]
    public List<ScriptingParameterValueInput> Parameters { get; set; } = new List<ScriptingParameterValueInput>();
}

internal sealed class TriggerDataInput
{
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    [Description("Required owner HouseType INI name.")]
    public string HouseType { get; set; }
    public ScriptingElementReference LinkedTrigger { get; set; }
    [JsonRequired]
    public bool Disabled { get; set; }
    [JsonRequired]
    public bool Easy { get; set; } = true;
    [JsonRequired]
    public bool Normal { get; set; } = true;
    [JsonRequired]
    public bool Hard { get; set; } = true;
    public string EditorColor { get; set; }
    [JsonRequired]
    public List<TriggerEventInput> Events { get; set; } = new List<TriggerEventInput>();
    [JsonRequired]
    public List<TriggerActionInput> Actions { get; set; } = new List<TriggerActionInput>();
}

internal sealed class TriggerCreate
{
    public string TemporaryKey { get; set; }
    [JsonRequired]
    public TriggerDataInput Value { get; set; }
}

internal sealed class TriggerUpdate
{
    [JsonRequired]
    public string Id { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
    [JsonRequired]
    [Description("Complete replacement value. Omitted optional properties are cleared or reset to their documented defaults.")]
    public TriggerDataInput Replacement { get; set; }
}

internal sealed class TagDataInput
{
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public int Repeating { get; set; }
    [JsonRequired]
    public ScriptingElementReference Trigger { get; set; }
}

internal sealed class TagCreate
{
    public string TemporaryKey { get; set; }
    [JsonRequired]
    public TagDataInput Value { get; set; }
}

internal sealed class TagUpdate
{
    [JsonRequired]
    public string Id { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
    [JsonRequired]
    [Description("Complete replacement value.")]
    public TagDataInput Replacement { get; set; }
}

internal sealed class ScriptingCreateOperations
{
    public List<TaskForceCreate> TaskForces { get; set; } = new List<TaskForceCreate>();
    public List<ScriptCreate> Scripts { get; set; } = new List<ScriptCreate>();
    public List<TeamTypeCreate> TeamTypes { get; set; } = new List<TeamTypeCreate>();
    public List<AITriggerCreate> AITriggers { get; set; } = new List<AITriggerCreate>();
    public List<LocalVariableCreate> LocalVariables { get; set; } = new List<LocalVariableCreate>();
    public List<TriggerCreate> Triggers { get; set; } = new List<TriggerCreate>();
    public List<TagCreate> Tags { get; set; } = new List<TagCreate>();
}

internal sealed class ScriptingUpdateOperations
{
    public List<TaskForceUpdate> TaskForces { get; set; } = new List<TaskForceUpdate>();
    public List<ScriptUpdate> Scripts { get; set; } = new List<ScriptUpdate>();
    public List<TeamTypeUpdate> TeamTypes { get; set; } = new List<TeamTypeUpdate>();
    public List<AITriggerUpdate> AITriggers { get; set; } = new List<AITriggerUpdate>();
    public List<LocalVariableUpdate> LocalVariables { get; set; } = new List<LocalVariableUpdate>();
    public List<TriggerUpdate> Triggers { get; set; } = new List<TriggerUpdate>();
    public List<TagUpdate> Tags { get; set; } = new List<TagUpdate>();
}

[Description("Deletes one map-local editable element. Supply id for every kind except LocalVariable, which uses index; " +
    "expectedContentHash must be current.")]
internal sealed class ScriptingDeleteOperation
{
    [Description("Map-local editable kind to delete: TaskForce, Script, TeamType, AITrigger, LocalVariable, Trigger, or Tag. " +
        "MapTechno and CellTag are read-only and cannot be deleted here.")]
    [JsonRequired]
    public ScriptingElementKind Kind { get; set; }
    public string Id { get; set; }
    public int? Index { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
}

[Description("Guards one existing permanent element without modifying it. Supply id for every kind except LocalVariable, which uses index; " +
    "expectedContentHash must be current.")]
internal sealed class ScriptingContentPrecondition
{
    [Description("Permanent element kind to guard. All kinds are supported, including global TaskForces/Scripts/TeamTypes " +
        "and read-only MapTechno/CellTag reference sources.")]
    [JsonRequired]
    public ScriptingElementKind Kind { get; set; }
    public string Id { get; set; }
    public int? Index { get; set; }
    [JsonRequired]
    public string ExpectedContentHash { get; set; }
}

internal sealed class ScriptingChangeSetRequest
{
    public ScriptingCreateOperations Creates { get; set; } = new ScriptingCreateOperations();
    public ScriptingUpdateOperations Updates { get; set; } = new ScriptingUpdateOperations();
    public List<ScriptingDeleteOperation> Deletes { get; set; } = new List<ScriptingDeleteOperation>();

    [Description("Optional content-hash checks for permanent elements read by the operation but not otherwise updated or deleted, " +
        "including read-only global elements, placed technos, and cell tags.")]
    public List<ScriptingContentPrecondition> Preconditions { get; set; } = new List<ScriptingContentPrecondition>();

}

internal sealed class ScriptingCreatedElementInfo
{
    public string TemporaryKey { get; set; }
    public ScriptingElementKind Kind { get; set; }
    public string Id { get; set; }
    public int? Index { get; set; }
    public string ContentHash { get; set; }
}

internal sealed class ScriptingChangedElementInfo
{
    public ScriptingElementKind Kind { get; set; }
    public string Id { get; set; }
    public int? Index { get; set; }
    public string ContentHash { get; set; }
}

internal sealed class ScriptingDiagnosticInfo
{
    public string Severity { get; set; }
    public string Code { get; set; }
    public string Path { get; set; }
    public string Message { get; set; }
}

internal sealed class ScriptingChangeSetResult
{
    public bool IsValid { get; set; }
    public bool Applied { get; set; }
    public List<ScriptingCreatedElementInfo> Created { get; set; } = new List<ScriptingCreatedElementInfo>();
    public List<ScriptingChangedElementInfo> Updated { get; set; } = new List<ScriptingChangedElementInfo>();
    public List<ScriptingElementIdentityInfo> Deleted { get; set; } = new List<ScriptingElementIdentityInfo>();
    public List<ScriptingDiagnosticInfo> Diagnostics { get; set; } = new List<ScriptingDiagnosticInfo>();
}

internal sealed class ScriptingPresetOptionInfo
{
    public string Value { get; set; }
    public string DisplayName { get; set; }
}

internal sealed class ScriptingParameterDefinitionInfo
{
    public int Index { get; set; }
    public string Name { get; set; }
    public string ParameterType { get; set; }
    public bool IsUsed { get; set; }
    public bool IsHardcoded { get; set; }
    public string HardcodedValue { get; set; }
    public List<ScriptingPresetOptionInfo> PresetOptions { get; set; } = new List<ScriptingPresetOptionInfo>();
}

internal sealed class ScriptActionDefinitionInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ParameterDescription { get; set; }
    public string ParameterType { get; set; }
    public bool UsesSelectionWindow { get; set; }
    public List<ScriptingPresetOptionInfo> PresetOptions { get; set; } = new List<ScriptingPresetOptionInfo>();
}

internal sealed class TriggerEventDefinitionInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    [Description("Whether the active configuration allows this event for new authoring. Existing unavailable events may only be preserved unchanged.")]
    public bool Available { get; set; }
    public List<ScriptingParameterDefinitionInfo> Parameters { get; set; } = new List<ScriptingParameterDefinitionInfo>();
}

internal sealed class TriggerActionDefinitionInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ScriptingParameterDefinitionInfo> Parameters { get; set; } = new List<ScriptingParameterDefinitionInfo>();
}

internal sealed class TeamTypeFlagDefinitionInfo
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public bool DefaultValue { get; set; }
}

internal sealed class AITriggerConditionDefinitionInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool RequiresObject { get; set; }
    public bool Available { get; set; }
}

internal sealed class AITriggerComparatorDefinitionInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
}

internal sealed class ScriptingSideDefinitionInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
}

internal sealed class ScriptingDefinitionCatalog
{
    public List<ScriptActionDefinitionInfo> ScriptActions { get; set; } = new List<ScriptActionDefinitionInfo>();
    public List<TriggerEventDefinitionInfo> TriggerEvents { get; set; } = new List<TriggerEventDefinitionInfo>();
    public List<TriggerActionDefinitionInfo> TriggerActions { get; set; } = new List<TriggerActionDefinitionInfo>();
    public List<TeamTypeFlagDefinitionInfo> TeamTypeFlags { get; set; } = new List<TeamTypeFlagDefinitionInfo>();
    public List<AITriggerConditionDefinitionInfo> AITriggerConditions { get; set; } = new List<AITriggerConditionDefinitionInfo>();
    public List<AITriggerComparatorDefinitionInfo> AITriggerComparators { get; set; } = new List<AITriggerComparatorDefinitionInfo>();
    public List<ScriptingSideDefinitionInfo> Sides { get; set; } = new List<ScriptingSideDefinitionInfo>();
}

internal sealed class ScriptingParameterOptionInfo
{
    public string Value { get; set; }
    public string DisplayName { get; set; }
    public ScriptingElementReference Reference { get; set; }
}

internal sealed class ScriptingParameterOptionsResult
{
    public string ParameterType { get; set; }
    public List<ScriptingParameterOptionInfo> Options { get; set; } = new List<ScriptingParameterOptionInfo>();
}

internal sealed class ScriptingHouseTypeInfo
{
    public string ININame { get; set; }
    public string DisplayName { get; set; }
    public int Index { get; set; }
}

internal sealed class ScriptingHouseTypesResult
{
    public List<ScriptingHouseTypeInfo> HouseTypes { get; set; } = new List<ScriptingHouseTypeInfo>();
}
