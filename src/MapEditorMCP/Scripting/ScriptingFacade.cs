using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using System.Globalization;

namespace MapEditorMCP.Scripting;

/// <summary>
/// Provides game-thread-only access to map scripting data for the MCP tools.
/// Scripting changes intentionally bypass the terrain/object mutation history.
/// </summary>
internal sealed class ScriptingFacade
{
    public ScriptingFacade(Map map)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        parameterCodec = new ScriptingParameterCodec(map);
        catalogService = new ScriptingCatalogService(map);
        referenceService = new ScriptingReferenceService(map);
    }

    private readonly Map map;
    private readonly ScriptingParameterCodec parameterCodec;
    private readonly ScriptingCatalogService catalogService;
    private readonly ScriptingReferenceService referenceService;

    public ScriptingHouseTypesResult GetHouseTypes(string nameFilter = null)
    {
        var result = new ScriptingHouseTypesResult();
        foreach (HouseType houseType in map.GetHouseTypes())
        {
            string displayName = houseType.ININame;
            if (!MatchesFilter(nameFilter, houseType.ININame, displayName))
                continue;

            result.HouseTypes.Add(new ScriptingHouseTypeInfo
            {
                ININame = houseType.ININame,
                DisplayName = displayName,
                Index = houseType.Index
            });
        }

        return result;
    }

    public ScriptingDefinitionCatalog GetScriptingDefinitions() => catalogService.GetDefinitions();

    public ScriptingParameterOptionsResult GetScriptingParameterOptions(string parameterType, string nameFilter = null)
        => catalogService.GetParameterOptions(parameterType, nameFilter, true);

    public TaskForcesResult GetTaskForces(string nameFilter = null, bool includeGlobal = false)
    {
        var result = new TaskForcesResult();
        var localIds = new HashSet<string>(map.TaskForces.Select(taskForce => taskForce.ININame), StringComparer.OrdinalIgnoreCase);

        foreach (TaskForce taskForce in map.TaskForces)
            AddTaskForceInfo(result, taskForce, false, nameFilter);

        if (includeGlobal)
        {
            foreach (TaskForce taskForce in map.Rules.TaskForces.Where(taskForce => !localIds.Contains(taskForce.ININame)))
                AddTaskForceInfo(result, taskForce, true, nameFilter);
        }

        return result;
    }

    public ScriptsResult GetScripts(string nameFilter = null, bool includeGlobal = false)
    {
        var result = new ScriptsResult();
        var localIds = new HashSet<string>(map.Scripts.Select(script => script.ININame), StringComparer.OrdinalIgnoreCase);

        foreach (Script script in map.Scripts)
            AddScriptInfo(result, script, false, nameFilter);

        if (includeGlobal)
        {
            foreach (Script script in map.Rules.Scripts.Where(script => !localIds.Contains(script.ININame)))
                AddScriptInfo(result, script, true, nameFilter);
        }

        return result;
    }

    public TeamTypesResult GetTeamTypes(string nameFilter = null, bool includeGlobal = false)
    {
        var result = new TeamTypesResult();
        var localIds = new HashSet<string>(map.TeamTypes.Select(teamType => teamType.ININame), StringComparer.OrdinalIgnoreCase);

        foreach (TeamType teamType in map.TeamTypes)
            AddTeamTypeInfo(result, teamType, false, nameFilter);

        if (includeGlobal)
        {
            foreach (TeamType teamType in map.Rules.TeamTypes.Where(teamType => !localIds.Contains(teamType.ININame)))
                AddTeamTypeInfo(result, teamType, true, nameFilter);
        }

        return result;
    }

    public AITriggersResult GetAITriggers(string nameFilter = null)
    {
        var result = new AITriggersResult();
        foreach (AITriggerType aiTrigger in map.AITriggerTypes)
        {
            if (!MatchesFilter(nameFilter, aiTrigger.ININame, aiTrigger.Name))
                continue;

            result.AITriggers.Add(new AITriggerInfo
            {
                Id = aiTrigger.ININame,
                Name = aiTrigger.Name,
                PrimaryTeam = CreateIdReference(ScriptingElementKind.TeamType, aiTrigger.PrimaryTeam?.ININame),
                OwnerName = aiTrigger.OwnerName,
                TechLevel = aiTrigger.TechLevel,
                ConditionType = (int)aiTrigger.ConditionType,
                ConditionObject = aiTrigger.ConditionObject?.ININame,
                Comparator = new AITriggerComparatorInfo
                {
                    Operator = (int)aiTrigger.Comparator.ComparatorOperator,
                    Quantity = aiTrigger.Comparator.Quantity
                },
                InitialWeight = aiTrigger.InitialWeight,
                MinimumWeight = aiTrigger.MinimumWeight,
                MaximumWeight = aiTrigger.MaximumWeight,
                EnabledInMultiplayer = aiTrigger.EnabledInMultiplayer,
                Unused = aiTrigger.Unused,
                Side = aiTrigger.Side,
                IsBaseDefense = aiTrigger.IsBaseDefense,
                SecondaryTeam = CreateIdReference(ScriptingElementKind.TeamType, aiTrigger.SecondaryTeam?.ININame),
                Easy = aiTrigger.Easy,
                Medium = aiTrigger.Medium,
                Hard = aiTrigger.Hard,
                Enabled = aiTrigger.Enabled,
                ContentHash = ScriptingContentHasher.Compute(aiTrigger)
            });
        }

        return result;
    }

    public LocalVariablesResult GetLocalVariables(string nameFilter = null)
    {
        var result = new LocalVariablesResult();
        foreach (LocalVariable variable in map.LocalVariables.OrderBy(variable => variable.Index))
        {
            if (!MatchesFilter(nameFilter, variable.Index.ToString(CultureInfo.InvariantCulture), variable.Name))
                continue;

            result.LocalVariables.Add(new LocalVariableInfo
            {
                Index = variable.Index,
                Name = variable.Name,
                InitialState = variable.InitialState,
                ContentHash = ScriptingContentHasher.Compute(variable)
            });
        }

        return result;
    }

    public TriggersResult GetTriggers(string nameFilter = null)
    {
        var result = new TriggersResult();
        foreach (Trigger trigger in map.Triggers)
        {
            if (!MatchesFilter(nameFilter, trigger.ID, trigger.Name))
                continue;

            var triggerInfo = new TriggerInfo
            {
                Id = trigger.ID,
                Name = trigger.Name,
                HouseType = trigger.HouseType?.ININame,
                LinkedTrigger = CreateIdReference(ScriptingElementKind.Trigger, trigger.LinkedTrigger?.ID),
                Disabled = trigger.Disabled,
                Easy = trigger.Easy,
                Normal = trigger.Normal,
                Hard = trigger.Hard,
                EditorColor = trigger.EditorColor,
                TagIds = map.Tags.Where(tag => tag.Trigger == trigger).Select(tag => tag.ID).ToList(),
                ContentHash = ScriptingContentHasher.Compute(trigger, map.EditorConfig)
            };

            for (int eventIndex = 0; eventIndex < trigger.Conditions.Count; eventIndex++)
                triggerInfo.Events.Add(CreateTriggerEventInfo(trigger.Conditions[eventIndex], eventIndex));
            for (int actionIndex = 0; actionIndex < trigger.Actions.Count; actionIndex++)
                triggerInfo.Actions.Add(CreateTriggerActionInfo(trigger.Actions[actionIndex], actionIndex));

            result.Triggers.Add(triggerInfo);
        }

        return result;
    }

    public TagsResult GetTags(string nameFilter = null)
    {
        var result = new TagsResult();
        foreach (Tag tag in map.Tags)
        {
            if (!MatchesFilter(nameFilter, tag.ID, tag.Name))
                continue;

            result.Tags.Add(new TagInfo
            {
                Id = tag.ID,
                Name = tag.Name,
                Repeating = tag.Repeating,
                Trigger = CreateIdReference(ScriptingElementKind.Trigger, tag.Trigger?.ID),
                ContentHash = ScriptingContentHasher.Compute(tag)
            });
        }

        return result;
    }

    public ScriptingReferencesResult GetScriptingReferences(ScriptingElementReference element)
        => referenceService.GetReferences(element);

    public ScriptingChangeSetResult ValidateScriptingChanges(ScriptingChangeSetRequest request)
        => new ScriptingChangePlanner(map, parameterCodec).Process(request, false);

    public ScriptingChangeSetResult ApplyScriptingChanges(ScriptingChangeSetRequest request)
        => new ScriptingChangePlanner(map, parameterCodec).Process(request, true);

    private void AddTaskForceInfo(TaskForcesResult result, TaskForce taskForce, bool isGlobal, string nameFilter)
    {
        if (!MatchesFilter(
                nameFilter,
                taskForce.ININame,
                taskForce.Name,
                string.Join(" ", taskForce.TechnoTypes.Where(entry => entry != null).Select(entry => entry.TechnoType?.ININame))))
        {
            return;
        }

        var info = new TaskForceInfo
        {
            Id = taskForce.ININame,
            Name = taskForce.Name,
            Group = taskForce.Group,
            IsGlobal = isGlobal,
            ContentHash = ScriptingContentHasher.Compute(taskForce)
        };

        for (int i = 0; i < taskForce.TechnoTypes.Length; i++)
        {
            TaskForceTechnoEntry entry = taskForce.TechnoTypes[i];
            if (entry == null)
                continue;

            info.Members.Add(new TaskForceMemberInfo
            {
                Index = i,
                TechnoType = entry.TechnoType?.ININame,
                DisplayName = entry.TechnoType?.GetEditorDisplayName(),
                Count = entry.Count
            });
        }

        result.TaskForces.Add(info);
    }

    private void AddScriptInfo(ScriptsResult result, Script script, bool isGlobal, string nameFilter)
    {
        if (!MatchesFilter(nameFilter, script.ININame, script.Name))
            return;

        var info = new ScriptInfo
        {
            Id = script.ININame,
            Name = script.Name,
            EditorColor = script.EditorColor,
            IsGlobal = isGlobal,
            ContentHash = ScriptingContentHasher.Compute(script)
        };

        for (int i = 0; i < script.Actions.Count; i++)
        {
            ScriptActionEntry entry = script.Actions[i];
            map.EditorConfig.ScriptActions.TryGetValue(entry.Action, out ScriptAction definition);
            var decoded = parameterCodec.Decode(definition?.ParamType ?? TriggerParamType.Unknown, entry.Argument.ToString(CultureInfo.InvariantCulture));
            info.Actions.Add(new ScriptActionInfo
            {
                Index = i,
                ActionId = entry.Action,
                ActionName = definition?.Name,
                RawArgument = entry.Argument,
                Value = decoded.Value,
                Reference = decoded.Reference
            });
        }

        result.Scripts.Add(info);
    }

    private void AddTeamTypeInfo(TeamTypesResult result, TeamType teamType, bool isGlobal, string nameFilter)
    {
        if (!MatchesFilter(nameFilter, teamType.ININame, teamType.Name))
            return;

        result.TeamTypes.Add(new TeamTypeInfo
        {
            Id = teamType.ININame,
            Name = teamType.Name,
            Group = teamType.Group,
            HouseType = teamType.HouseType?.ININame,
            Script = CreateIdReference(ScriptingElementKind.Script, teamType.Script?.ININame),
            TaskForce = CreateIdReference(ScriptingElementKind.TaskForce, teamType.TaskForce?.ININame),
            Tag = CreateIdReference(ScriptingElementKind.Tag, teamType.Tag?.ID),
            Max = teamType.Max,
            Priority = teamType.Priority,
            Waypoint = DecodeWaypoint(teamType.Waypoint),
            TransportWaypoint = DecodeWaypoint(teamType.TransportWaypoint),
            RawWaypoint = teamType.Waypoint,
            RawTransportWaypoint = teamType.TransportWaypoint,
            MindControlDecision = teamType.MindControlDecision,
            TechLevel = teamType.TechLevel,
            VeteranLevel = teamType.VeteranLevel,
            EnabledFlags = new List<string>(teamType.EnabledTeamTypeFlags),
            EditorColor = teamType.EditorColor,
            IsGlobal = isGlobal,
            ContentHash = ScriptingContentHasher.Compute(teamType)
        });
    }

    private TriggerEventInfo CreateTriggerEventInfo(TriggerCondition condition, int index)
    {
        map.EditorConfig.TriggerEventTypes.TryGetValue(condition.ConditionIndex, out TriggerEventType definition);
        var info = new TriggerEventInfo
        {
            Index = index,
            EventId = condition.ConditionIndex,
            EventName = definition?.Name
        };

        for (int parameterIndex = 0; parameterIndex < condition.Parameters.Length; parameterIndex++)
        {
            TriggerParamType parameterType = definition?.Parameters[parameterIndex]?.TriggerParamType ?? TriggerParamType.Unknown;
            info.Parameters.Add(CreateTriggerParameterInfo(parameterIndex, parameterType, condition.Parameters[parameterIndex]));
        }

        return info;
    }

    private TriggerActionInfo CreateTriggerActionInfo(TriggerAction action, int index)
    {
        map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out TriggerActionType definition);
        var info = new TriggerActionInfo
        {
            Index = index,
            ActionId = action.ActionIndex,
            ActionName = definition?.Name
        };

        for (int parameterIndex = 0; parameterIndex < action.Parameters.Length; parameterIndex++)
        {
            TriggerParamType parameterType = definition?.Parameters[parameterIndex]?.TriggerParamType ?? TriggerParamType.Unknown;
            info.Parameters.Add(CreateTriggerParameterInfo(parameterIndex, parameterType, action.Parameters[parameterIndex]));
        }

        return info;
    }

    private TriggerParameterInfo CreateTriggerParameterInfo(int index, TriggerParamType parameterType, string rawValue)
    {
        var decoded = parameterCodec.Decode(parameterType, rawValue);
        return new TriggerParameterInfo
        {
            Index = index,
            RawValue = rawValue,
            Value = decoded.Value,
            Reference = decoded.Reference
        };
    }

    private static ScriptingElementReference CreateIdReference(ScriptingElementKind kind, string id)
        => id == null ? null : new ScriptingElementReference { Kind = kind, Id = id };

    private static int? DecodeWaypoint(string storedWaypoint)
    {
        if (string.IsNullOrWhiteSpace(storedWaypoint))
            return null;

        try
        {
            return Helpers.GetWaypointNumberFromAlphabeticalString(storedWaypoint);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool MatchesFilter(string filter, params string[] values)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return values.Any(value => value?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
