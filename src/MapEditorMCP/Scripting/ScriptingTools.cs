using ModelContextProtocol;
using ModelContextProtocol.Server;
using Rampastring.Tools;
using System.ComponentModel;

namespace MapEditorMCP.Scripting;

[McpServerToolType]
internal sealed class ScriptingTools
{
    public ScriptingTools(ScriptingFacade scriptingFacade, GameThreadDispatcher gameThreadDispatcher)
    {
        this.scriptingFacade = scriptingFacade;
        this.gameThreadDispatcher = gameThreadDispatcher;
    }

    private readonly ScriptingFacade scriptingFacade;
    private readonly GameThreadDispatcher gameThreadDispatcher;

    [McpServerTool(Name = "get_house_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns HouseTypes available to map scripting elements in the active game configuration.")]
    public Task<ScriptingHouseTypesResult> GetHouseTypes(
        [Description("Optional case-insensitive filter matched against INI and display names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetHouseTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetHouseTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_scripting_definitions", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns the active game configuration's script actions, trigger events and actions, TeamType flags, " +
        "AITrigger conditions and comparators, and sides.")]
    public Task<ScriptingDefinitionCatalog> GetScriptingDefinitions(CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetScriptingDefinitions)}");
        return gameThreadDispatcher.InvokeAsync(scriptingFacade.GetScriptingDefinitions, cancellationToken);
    }

    [McpServerTool(Name = "get_scripting_parameter_options", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns valid semantic options for a scripting parameter type in the current map and game configuration.")]
    public async Task<ScriptingParameterOptionsResult> GetScriptingParameterOptions(
        [Description("TriggerParamType name returned by get_scripting_definitions, such as TeamType, LocalVariable, " +
            "HouseType, Waypoint, or WaypointZZ.")] string parameterType,
        [Description("Optional case-insensitive filter matched against option values and display names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetScriptingParameterOptions)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => scriptingFacade.GetScriptingParameterOptions(parameterType, nameFilter), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "get_task_forces", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns map-local TaskForces and, optionally, read-only global TaskForces from the active rules.")]
    public Task<TaskForcesResult> GetTaskForces(
        [Description("Optional case-insensitive filter matched against IDs, names, and member types.")] string nameFilter = null,
        [Description("Whether to include read-only global TaskForces in addition to map-local entries.")] bool includeGlobal = false,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetTaskForces)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetTaskForces(nameFilter, includeGlobal), cancellationToken);
    }

    [McpServerTool(Name = "get_scripts", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns map-local Scripts and, optionally, read-only global Scripts with decoded action information.")]
    public Task<ScriptsResult> GetScripts(
        [Description("Optional case-insensitive filter matched against IDs and names.")] string nameFilter = null,
        [Description("Whether to include read-only global Scripts in addition to map-local entries.")] bool includeGlobal = false,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetScripts)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetScripts(nameFilter, includeGlobal), cancellationToken);
    }

    [McpServerTool(Name = "get_team_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns map-local TeamTypes and, optionally, read-only global TeamTypes with their scripting references and flags.")]
    public Task<TeamTypesResult> GetTeamTypes(
        [Description("Optional case-insensitive filter matched against IDs and names.")] string nameFilter = null,
        [Description("Whether to include read-only global TeamTypes in addition to map-local entries.")] bool includeGlobal = false,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetTeamTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetTeamTypes(nameFilter, includeGlobal), cancellationToken);
    }

    [McpServerTool(Name = "get_ai_triggers", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns map-local AITriggers with decoded team, condition, comparator, weight, and difficulty information.")]
    public Task<AITriggersResult> GetAITriggers(
        [Description("Optional case-insensitive filter matched against IDs and names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetAITriggers)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetAITriggers(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_local_variables", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns the map's local variables, including their indices, initial states, and content hashes.")]
    public Task<LocalVariablesResult> GetLocalVariables(
        [Description("Optional case-insensitive filter matched against indices and names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetLocalVariables)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetLocalVariables(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_triggers", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns map Triggers with decoded events, actions, parameters, linked triggers, difficulties, and associated Tag IDs.")]
    public Task<TriggersResult> GetTriggers(
        [Description("Optional case-insensitive filter matched against IDs and names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetTriggers)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetTriggers(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_tags", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns map Tags with their persistence setting, Trigger reference, and content hash.")]
    public Task<TagsResult> GetTags(
        [Description("Optional case-insensitive filter matched against IDs and names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetTags)}");
        return gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetTags(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_scripting_references", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns incoming and outgoing references for one existing permanent element in the current map or rules context. " +
        "Use this before deletion to discover dependants.")]
    public async Task<ScriptingReferencesResult> GetScriptingReferences(
        [Description("Existing scripting element whose references should be inspected.")] ScriptingElementReference element,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(GetScriptingReferences)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => scriptingFacade.GetScriptingReferences(element), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "validate_scripting_changes", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Validates and normalizes an atomic scripting change set without modifying the map. " +
        "Use current content hashes for updates, deletions, and preconditions.")]
    public async Task<ScriptingChangeSetResult> ValidateScriptingChanges(
        [Description("Typed scripting create, update, delete, and precondition operations to validate.")] ScriptingChangeSetRequest request,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(ValidateScriptingChanges)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => scriptingFacade.ValidateScriptingChanges(request), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "apply_scripting_changes", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically applies a validated scripting change set. Changes are not added to map undo/redo history. " +
        "Updates and deletions require current content hashes.")]
    public async Task<ScriptingChangeSetResult> ApplyScriptingChanges(
        [Description("Typed scripting create, update, delete, and precondition operations to apply atomically.")] ScriptingChangeSetRequest request,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(ScriptingTools)}.{nameof(ApplyScriptingChanges)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => scriptingFacade.ApplyScriptingChanges(request), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
