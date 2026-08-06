using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using System.Globalization;

namespace MapEditorMCP.Scripting;

/// <summary>
/// Projects scripting definitions and parameter choices from the active map configuration.
/// </summary>
internal sealed class ScriptingCatalogService
{
    public ScriptingCatalogService(Map map)
    {
        ArgumentNullException.ThrowIfNull(map);
        this.map = map;
    }

    private readonly Map map;

    public ScriptingDefinitionCatalog GetDefinitions()
    {
        return new ScriptingDefinitionCatalog
        {
            ScriptActions = map.EditorConfig.ScriptActions.Values
                .OrderBy(action => action.ID)
                .Select(CreateScriptActionDefinition)
                .ToList(),
            TriggerEvents = map.EditorConfig.TriggerEventTypes.Values
                .OrderBy(triggerEvent => triggerEvent.ID)
                .Select(CreateTriggerEventDefinition)
                .ToList(),
            TriggerActions = map.EditorConfig.TriggerActionTypes.Values
                .OrderBy(triggerAction => triggerAction.ID)
                .Select(CreateTriggerActionDefinition)
                .ToList(),
            TeamTypeFlags = map.EditorConfig.TeamTypeFlags
                .Select(flag => new TeamTypeFlagDefinitionInfo
                {
                    Name = flag.Name,
                    DisplayName = flag.UIName,
                    DefaultValue = flag.DefaultValue
                })
                .ToList(),
            AITriggerConditions = Enum.GetValues<AITriggerConditionType>()
                .OrderBy(conditionType => (int)conditionType)
                .Select(CreateAITriggerConditionDefinition)
                .ToList(),
            AITriggerComparators = Enum.GetValues<AITriggerComparatorOperator>()
                .OrderBy(comparator => (int)comparator)
                .Select(comparator => new AITriggerComparatorDefinitionInfo
                {
                    Id = (int)comparator,
                    Name = GetAITriggerComparatorName(comparator)
                })
                .ToList(),
            Sides = CreateSideDefinitions()
        };
    }

    public ScriptingParameterOptionsResult GetParameterOptions(string parameterType, string nameFilter, bool includeGlobal)
    {
        string canonicalParameterType = GetCanonicalParameterType(parameterType);
        string normalizedFilter = string.IsNullOrWhiteSpace(nameFilter) ? null : nameFilter.Trim();

        return new ScriptingParameterOptionsResult
        {
            ParameterType = canonicalParameterType,
            Options = CreateParameterOptions(canonicalParameterType, includeGlobal)
                .Where(option => MatchesFilter(option, normalizedFilter))
                .ToList()
        };
    }

    private static ScriptActionDefinitionInfo CreateScriptActionDefinition(ScriptAction action)
    {
        return new ScriptActionDefinitionInfo
        {
            Id = action.ID,
            Name = action.Name,
            Description = action.Description,
            ParameterDescription = action.ParamDescription,
            ParameterType = action.ParamType.ToString(),
            UsesSelectionWindow = action.UseWindowSelection,
            PresetOptions = action.PresetOptions
                .Select(option => new ScriptingPresetOptionInfo
                {
                    Value = option.Value.ToString(CultureInfo.InvariantCulture),
                    DisplayName = string.IsNullOrWhiteSpace(option.Text)
                        ? option.Value.ToString(CultureInfo.InvariantCulture)
                        : option.Text
                })
                .ToList()
        };
    }

    private static TriggerEventDefinitionInfo CreateTriggerEventDefinition(TriggerEventType triggerEvent)
    {
        return new TriggerEventDefinitionInfo
        {
            Id = triggerEvent.ID,
            Name = triggerEvent.Name,
            Description = triggerEvent.Description,
            Available = triggerEvent.Available,
            Parameters = triggerEvent.Parameters
                .Select((parameter, index) => CreateParameterDefinition(
                    index,
                    parameter?.TriggerParamType ?? TriggerParamType.Unused,
                    parameter?.NameOverride,
                    parameter?.PresetOptions))
                .ToList()
        };
    }

    private static TriggerActionDefinitionInfo CreateTriggerActionDefinition(TriggerActionType triggerAction)
    {
        return new TriggerActionDefinitionInfo
        {
            Id = triggerAction.ID,
            Name = triggerAction.Name,
            Description = triggerAction.Description,
            Parameters = triggerAction.Parameters
                .Select((parameter, index) => CreateParameterDefinition(
                    index,
                    parameter?.TriggerParamType ?? TriggerParamType.Unused,
                    parameter?.NameOverride,
                    parameter?.PresetOptions))
                .ToList()
        };
    }

    private static ScriptingParameterDefinitionInfo CreateParameterDefinition(
        int index,
        TriggerParamType parameterType,
        string nameOverride,
        List<string> presetOptions)
    {
        int rawParameterType = (int)parameterType;
        bool isHardcoded = rawParameterType < 0;
        bool isUsed = !isHardcoded && parameterType != TriggerParamType.Unused;

        string parameterTypeName = isHardcoded
            ? $"Hardcoded({Math.Abs(rawParameterType).ToString(CultureInfo.InvariantCulture)})"
            : parameterType.ToString();

        string parameterName = nameOverride;
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            parameterName = isHardcoded
                ? $"Hardcoded value {Math.Abs(rawParameterType).ToString(CultureInfo.InvariantCulture)}"
                : parameterType.ToString();
        }

        return new ScriptingParameterDefinitionInfo
        {
            Index = index,
            Name = parameterName,
            ParameterType = parameterTypeName,
            IsUsed = isUsed,
            IsHardcoded = isHardcoded,
            HardcodedValue = isHardcoded ? Math.Abs(rawParameterType).ToString(CultureInfo.InvariantCulture) : null,
            PresetOptions = presetOptions == null
                ? new List<ScriptingPresetOptionInfo>()
                : presetOptions.Select(CreatePresetOption).ToList()
        };
    }

    private static ScriptingPresetOptionInfo CreatePresetOption(string configuredOption)
    {
        string option = configuredOption?.Trim() ?? string.Empty;
        int separatorIndex = option.IndexOfAny(new[] { ' ', '\t' });

        if (separatorIndex < 0)
        {
            return new ScriptingPresetOptionInfo
            {
                Value = option,
                DisplayName = option
            };
        }

        string value = option[..separatorIndex];
        string displayName = option[(separatorIndex + 1)..].Trim();

        return new ScriptingPresetOptionInfo
        {
            Value = value,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? value : displayName
        };
    }

    private static AITriggerConditionDefinitionInfo CreateAITriggerConditionDefinition(AITriggerConditionType conditionType)
    {
        return new AITriggerConditionDefinitionInfo
        {
            Id = (int)conditionType,
            Name = GetAITriggerConditionName(conditionType),
            RequiresObject = conditionType is AITriggerConditionType.EnemyOwns or
                AITriggerConditionType.OwnerOwns or
                AITriggerConditionType.NeutralHouseOwns,
            Available = Constants.IsRA2YR ||
                (int)conditionType < (int)AITriggerConditionType.OwnerHasIronCurtainReady
        };
    }

    private static string GetAITriggerConditionName(AITriggerConditionType conditionType)
    {
        return conditionType switch
        {
            AITriggerConditionType.None => "None",
            AITriggerConditionType.EnemyOwns => "Enemy Owns",
            AITriggerConditionType.OwnerOwns => "House Owns",
            AITriggerConditionType.EnemyOnYellowPower => "Enemy On Yellow Power",
            AITriggerConditionType.EnemyOnRedPower => "Enemy On Red Power",
            AITriggerConditionType.EnemyHasCredits => "Enemy Has X Credits",
            AITriggerConditionType.OwnerHasIronCurtainReady => "House Has Iron Curtain Ready",
            AITriggerConditionType.OwnerHasChronosphereReady => "House Has Chronosphere Ready",
            AITriggerConditionType.NeutralHouseOwns => "Neutral House Owns",
            _ => conditionType.ToString()
        };
    }

    private static string GetAITriggerComparatorName(AITriggerComparatorOperator comparator)
    {
        return comparator switch
        {
            AITriggerComparatorOperator.LessThan => "Less Than",
            AITriggerComparatorOperator.LessThanOrEqual => "Less Than or Equal To",
            AITriggerComparatorOperator.Equal => "Equal To",
            AITriggerComparatorOperator.MoreThanOrEqual => "More Than or Equal To",
            AITriggerComparatorOperator.MoreThan => "More Than",
            AITriggerComparatorOperator.NotEqual => "Not Equal To",
            _ => comparator.ToString()
        };
    }

    private List<ScriptingSideDefinitionInfo> CreateSideDefinitions()
    {
        var sides = new List<ScriptingSideDefinitionInfo>
        {
            new ScriptingSideDefinitionInfo
            {
                Id = 0,
                Name = "All sides"
            }
        };

        sides.AddRange(map.Rules.Sides.Select((side, index) => new ScriptingSideDefinitionInfo
        {
            Id = index + 1,
            Name = side
        }));

        return sides;
    }

    private string GetCanonicalParameterType(string parameterType)
    {
        if (string.IsNullOrWhiteSpace(parameterType))
            throw new MapFacadeValidationException("Parameter type must be supplied.");

        string trimmedParameterType = parameterType.Trim();

        foreach (string scriptingElementType in AdditionalScriptingElementParameterTypes)
        {
            if (trimmedParameterType.Equals(scriptingElementType, StringComparison.OrdinalIgnoreCase))
                return scriptingElementType;
        }

        if (Enum.TryParse(trimmedParameterType, true, out TriggerParamType parsedParameterType) &&
            Enum.IsDefined(parsedParameterType) &&
            !int.TryParse(trimmedParameterType, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return parsedParameterType.ToString();
        }

        string supportedTypes = string.Join(", ", Enum.GetNames<TriggerParamType>()
            .Concat(AdditionalScriptingElementParameterTypes));
        throw new MapFacadeValidationException(
            $"Unknown scripting parameter type '{parameterType}'. Supported types: {supportedTypes}.");
    }

    private static readonly string[] AdditionalScriptingElementParameterTypes =
    {
        nameof(ScriptingElementKind.TaskForce),
        nameof(ScriptingElementKind.Script),
        nameof(ScriptingElementKind.AITrigger)
    };

    private IEnumerable<ScriptingParameterOptionInfo> CreateParameterOptions(string parameterType, bool includeGlobal)
    {
        if (parameterType.Equals(nameof(ScriptingElementKind.TaskForce), StringComparison.Ordinal))
            return CreateTaskForceOptions(includeGlobal);

        if (parameterType.Equals(nameof(ScriptingElementKind.Script), StringComparison.Ordinal))
            return CreateScriptOptions(includeGlobal);

        if (parameterType.Equals(nameof(ScriptingElementKind.AITrigger), StringComparison.Ordinal))
            return map.AITriggerTypes.Select(aiTrigger => CreateReferenceOption(
                ScriptingElementKind.AITrigger,
                aiTrigger.ININame,
                null,
                aiTrigger.Name));

        var triggerParameterType = Enum.Parse<TriggerParamType>(parameterType);

        return triggerParameterType switch
        {
            TriggerParamType.LocalVariable => CreateLocalVariableOptions(),
            TriggerParamType.TeamType => CreateTeamTypeOptions(includeGlobal),
            TriggerParamType.Techno => map.GetAllTechnoTypes().Select(CreateININameObjectOption),
            TriggerParamType.Building => map.Rules.BuildingTypes.Select(CreateIndexedObjectOption),
            TriggerParamType.BuildingName => map.Rules.BuildingTypes.Select(CreateININameObjectOption),
            TriggerParamType.BuildingWithProperty => CreateBuildingWithPropertyOptions(),
            TriggerParamType.Aircraft => map.Rules.AircraftTypes.Select(CreateIndexedObjectOption),
            TriggerParamType.Infantry => map.Rules.InfantryTypes.Select(CreateIndexedObjectOption),
            TriggerParamType.Unit => map.Rules.UnitTypes.Select(CreateIndexedObjectOption),
            TriggerParamType.Tag => map.Tags.Select(tag => CreateReferenceOption(
                ScriptingElementKind.Tag,
                tag.ID,
                null,
                tag.Name)),
            TriggerParamType.Trigger => map.Triggers.Select(trigger => CreateReferenceOption(
                ScriptingElementKind.Trigger,
                trigger.ID,
                null,
                trigger.Name)),
            TriggerParamType.Boolean => CreateBooleanOptions(),
            TriggerParamType.Sound => CreateSoundOptions(),
            TriggerParamType.Theme => CreateThemeOptions(),
            TriggerParamType.Speech => CreateSpeechOptions(),
            TriggerParamType.SuperWeapon => map.Rules.SuperWeaponTypes.Select(superWeapon => CreateIndexedOption(
                superWeapon.Index,
                GetNamedINIObjectDisplayName(superWeapon.Name, superWeapon.ININame))),
            TriggerParamType.SuperWeaponName => map.Rules.SuperWeaponTypes.Select(superWeapon => CreateValueOption(
                superWeapon.ININame,
                GetNamedINIObjectDisplayName(superWeapon.Name, superWeapon.ININame))),
            TriggerParamType.Animation => map.Rules.AnimTypes.Select(CreateIndexedObjectOption),
            TriggerParamType.ParticleSystem => map.Rules.ParticleSystemTypes.Select(particleSystem => CreateIndexedOption(
                particleSystem.Index,
                particleSystem.ININame)),
            TriggerParamType.Waypoint => CreateWaypointOptions(),
            TriggerParamType.WaypointZZ => CreateWaypointOptions(),
            TriggerParamType.GlobalVariable => CreateGlobalVariableOptions(),
            TriggerParamType.HouseType => CreateHouseTypeOptions(),
            TriggerParamType.House => map.GetHouses().Select((house, index) => CreateIndexedOption(
                index,
                house.ININame)),
            TriggerParamType.Weapon => map.Rules.Weapons.Select((weapon, index) => CreateIndexedOption(
                index,
                weapon.ININame)),
            TriggerParamType.StringTableEntry => CreateStringTableOptions(),
            TriggerParamType.Text => CreateTutorialTextOptions(),
            TriggerParamType.Color => CreateColorOptions(),
            _ => Enumerable.Empty<ScriptingParameterOptionInfo>()
        };
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateTaskForceOptions(bool includeGlobal)
    {
        IEnumerable<TaskForce> taskForces = map.TaskForces;
        if (includeGlobal)
            taskForces = taskForces.UnionBy(
                map.Rules.TaskForces,
                taskForce => taskForce.ININame,
                StringComparer.OrdinalIgnoreCase);

        return taskForces.Select(taskForce => CreateReferenceOption(
            ScriptingElementKind.TaskForce,
            taskForce.ININame,
            null,
            taskForce.Name));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateScriptOptions(bool includeGlobal)
    {
        IEnumerable<Script> scripts = map.Scripts;
        if (includeGlobal)
            scripts = scripts.UnionBy(
                map.Rules.Scripts,
                script => script.ININame,
                StringComparer.OrdinalIgnoreCase);

        return scripts.Select(script => CreateReferenceOption(
            ScriptingElementKind.Script,
            script.ININame,
            null,
            script.Name));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateTeamTypeOptions(bool includeGlobal)
    {
        IEnumerable<TeamType> teamTypes = map.TeamTypes;
        if (includeGlobal)
            teamTypes = teamTypes.UnionBy(
                map.Rules.TeamTypes,
                teamType => teamType.ININame,
                StringComparer.OrdinalIgnoreCase);

        return teamTypes.Select(teamType => CreateReferenceOption(
            ScriptingElementKind.TeamType,
            teamType.ININame,
            null,
            teamType.Name));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateLocalVariableOptions()
    {
        return map.LocalVariables
            .OrderBy(variable => variable.Index)
            .Select(variable => CreateReferenceOption(
                ScriptingElementKind.LocalVariable,
                null,
                variable.Index,
                variable.Name));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateGlobalVariableOptions()
    {
        return map.Rules.GlobalVariables
            .OrderBy(variable => variable.Index)
            .Select(variable => CreateIndexedOption(variable.Index, variable.Name));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateHouseTypeOptions()
    {
        yield return CreateValueOption("-1", "-1 - Any house");
        foreach (HouseType houseType in map.GetHouseTypes())
            yield return CreateIndexedOption(houseType.Index, houseType.ININame);
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateColorOptions()
    {
        yield return CreateValueOption("-1", "-1 - Player's house color");
        foreach (RulesColor color in map.Rules.Colors)
            yield return CreateIndexedOption(color.Index, color.Name);
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateWaypointOptions()
    {
        return map.Waypoints
            .OrderBy(waypoint => waypoint.Identifier)
            .Select(waypoint => CreateValueOption(
                waypoint.Identifier.ToString(CultureInfo.InvariantCulture),
                $"Waypoint {waypoint.Identifier.ToString(CultureInfo.InvariantCulture)} " +
                $"({waypoint.Position.X.ToString(CultureInfo.InvariantCulture)}, " +
                $"{waypoint.Position.Y.ToString(CultureInfo.InvariantCulture)})"));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateBuildingWithPropertyOptions()
    {
        BuildingWithPropertyType[] properties =
        {
            BuildingWithPropertyType.LeastThreat,
            BuildingWithPropertyType.HighestThreat,
            BuildingWithPropertyType.Nearest,
            BuildingWithPropertyType.Farthest
        };

        return properties.SelectMany(property => map.Rules.BuildingTypes.Select(buildingType =>
        {
            int value = buildingType.Index + (int)property;
            return CreateValueOption(
                value.ToString(CultureInfo.InvariantCulture),
                $"{buildingType.GetEditorDisplayName()} ({buildingType.ININame}) - {property.ToDescription()}");
        }));
    }

    private static IEnumerable<ScriptingParameterOptionInfo> CreateBooleanOptions()
    {
        return new[]
        {
            CreateValueOption("0", "False"),
            CreateValueOption("1", "True")
        };
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateThemeOptions()
    {
        if (map.Rules.Themes?.List == null)
            return Enumerable.Empty<ScriptingParameterOptionInfo>();

        return map.Rules.Themes.List.Select(theme => CreateIndexedOption(
            theme.Index,
            GetNamedINIObjectDisplayName(theme.Name, theme.ININame)));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateSpeechOptions()
    {
        EvaSpeeches speeches = Constants.IsRA2YR ? map.Rules.Speeches : map.EditorConfig.Speeches;
        if (speeches?.List == null)
            return Enumerable.Empty<ScriptingParameterOptionInfo>();

        return speeches.List.Select(speech => CreateValueOption(
            Constants.IsRA2YR
                ? speech.Name
                : speech.Index.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(speech.Text)
                ? speech.Name
                : $"{speech.Name} - {speech.Text}"));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateSoundOptions()
    {
        if (map.Rules.Sounds?.List == null)
            return Enumerable.Empty<ScriptingParameterOptionInfo>();

        return map.Rules.Sounds.List.Select(sound => CreateValueOption(
            Constants.IsRA2YR
                ? sound.Name
                : sound.Index.ToString(CultureInfo.InvariantCulture),
            sound.Name));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateTutorialTextOptions()
    {
        if (map.Rules.TutorialLines == null)
            return Enumerable.Empty<ScriptingParameterOptionInfo>();

        return map.Rules.TutorialLines.GetLines().Select(line => CreateValueOption(
            line.ID.ToString(CultureInfo.InvariantCulture),
            line.Text));
    }

    private IEnumerable<ScriptingParameterOptionInfo> CreateStringTableOptions()
    {
        if (map.StringTable == null)
            return Enumerable.Empty<ScriptingParameterOptionInfo>();

        return map.StringTable.GetStringEnumerator()
            .OrderBy(csfString => csfString.ID, StringComparer.OrdinalIgnoreCase)
            .Select(csfString => CreateValueOption(
                csfString.ID,
                string.IsNullOrWhiteSpace(csfString.Value)
                    ? csfString.ID
                    : $"{csfString.ID} - {csfString.Value}"));
    }

    private static ScriptingParameterOptionInfo CreateIndexedObjectOption(GameObjectType objectType)
    {
        return CreateIndexedOption(
            objectType.Index,
            GetNamedINIObjectDisplayName(objectType.GetEditorDisplayName(), objectType.ININame));
    }

    private static ScriptingParameterOptionInfo CreateININameObjectOption(GameObjectType objectType)
    {
        return CreateValueOption(
            objectType.ININame,
            GetNamedINIObjectDisplayName(objectType.GetEditorDisplayName(), objectType.ININame));
    }

    private static ScriptingParameterOptionInfo CreateIndexedOption(int index, string name)
    {
        string value = index.ToString(CultureInfo.InvariantCulture);
        return CreateValueOption(value, $"{value} - {name}");
    }

    private static ScriptingParameterOptionInfo CreateValueOption(string value, string displayName)
    {
        return new ScriptingParameterOptionInfo
        {
            Value = value,
            DisplayName = displayName
        };
    }

    private static ScriptingParameterOptionInfo CreateReferenceOption(
        ScriptingElementKind kind,
        string id,
        int? index,
        string name)
    {
        string identifier = index?.ToString(CultureInfo.InvariantCulture) ?? id;
        string displayName = string.IsNullOrWhiteSpace(name)
            ? identifier
            : $"{identifier} - {name}";

        return new ScriptingParameterOptionInfo
        {
            DisplayName = displayName,
            Reference = new ScriptingElementReference
            {
                Kind = kind,
                Id = id,
                Index = index
            }
        };
    }

    private static string GetNamedINIObjectDisplayName(string name, string iniName)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Equals(iniName, StringComparison.OrdinalIgnoreCase))
            return iniName;

        return $"{name} ({iniName})";
    }

    private static bool MatchesFilter(ScriptingParameterOptionInfo option, string filter)
    {
        if (filter == null)
            return true;

        return Contains(option.Value, filter) ||
            Contains(option.DisplayName, filter) ||
            Contains(option.Reference?.Id, filter) ||
            (option.Reference?.Index?.ToString(CultureInfo.InvariantCulture)
                .Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static bool Contains(string value, string filter)
    {
        return value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;
    }
}
