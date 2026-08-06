using System.Globalization;
using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using MapEditorMCP;
using Rampastring.Tools;

namespace MapEditorMCP.Scripting;

internal sealed class ScriptingChangePlanner
{
    public ScriptingChangePlanner(Map map, ScriptingParameterCodec parameterCodec)
    {
        this.map = map;
        this.parameterCodec = parameterCodec;
        referenceService = new ScriptingReferenceService(map);
        idAllocator = new ScriptingIdAllocator(map);
    }

    private readonly Map map;
    private readonly ScriptingParameterCodec parameterCodec;
    private readonly ScriptingReferenceService referenceService;
    private readonly ScriptingIdAllocator idAllocator;
    private readonly Dictionary<string, TemporaryElement> temporaryElements = new Dictionary<string, TemporaryElement>(StringComparer.Ordinal);
    private readonly HashSet<string> touchedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<object> deletedObjects = new HashSet<object>();

    private readonly List<PendingCreate<TaskForceCreate, TaskForce>> taskForceCreates = new();
    private readonly List<PendingCreate<ScriptCreate, Script>> scriptCreates = new();
    private readonly List<PendingCreate<TeamTypeCreate, TeamType>> teamTypeCreates = new();
    private readonly List<PendingCreate<AITriggerCreate, AITriggerType>> aiTriggerCreates = new();
    private readonly List<PendingCreate<LocalVariableCreate, LocalVariable>> localVariableCreates = new();
    private readonly List<PendingCreate<TriggerCreate, Trigger>> triggerCreates = new();
    private readonly List<PendingCreate<TagCreate, Tag>> tagCreates = new();

    private readonly List<PendingUpdate<TaskForceUpdate, TaskForce>> taskForceUpdates = new();
    private readonly List<PendingUpdate<ScriptUpdate, Script>> scriptUpdates = new();
    private readonly List<PendingUpdate<TeamTypeUpdate, TeamType>> teamTypeUpdates = new();
    private readonly List<PendingUpdate<AITriggerUpdate, AITriggerType>> aiTriggerUpdates = new();
    private readonly List<PendingUpdate<LocalVariableUpdate, LocalVariable>> localVariableUpdates = new();
    private readonly List<PendingUpdate<TriggerUpdate, Trigger>> triggerUpdates = new();
    private readonly List<PendingUpdate<TagUpdate, Tag>> tagUpdates = new();

    private readonly Dictionary<TaskForce, TaskForce> taskForceReplacements = new();
    private readonly Dictionary<Script, Script> scriptReplacements = new();
    private readonly Dictionary<TeamType, TeamType> teamTypeReplacements = new();
    private readonly Dictionary<AITriggerType, AITriggerType> aiTriggerReplacements = new();
    private readonly Dictionary<LocalVariable, LocalVariable> localVariableReplacements = new();
    private readonly Dictionary<Trigger, Trigger> triggerReplacements = new();
    private readonly Dictionary<Tag, Tag> tagReplacements = new();
    private readonly List<PendingDelete> deletes = new();

    private ScriptingChangeSetResult result;

    public ScriptingChangeSetResult Process(ScriptingChangeSetRequest request, bool apply)
    {
        result = new ScriptingChangeSetResult();
        if (request == null)
        {
            AddError("invalid_request", "$", "A scripting change set is required.");
            return CompleteResult(false);
        }

        PrepareCreates(request.Creates ?? new ScriptingCreateOperations());
        PrepareUpdates(request.Updates ?? new ScriptingUpdateOperations());
        PrepareDeletes(request.Deletes);
        ValidatePreconditions(request.Preconditions);

        BuildCreates();
        BuildUpdates();
        ValidateProjectedState();

        if (HasErrors())
            return CompleteResult(false);

        PopulatePlannedResult();
        if (apply && HasPlannedChanges())
        {
            ApplyChanges();
            result.Applied = true;
            RefreshChangedHashes();
        }

        return CompleteResult(true);
    }

    private ScriptingChangeSetResult CompleteResult(bool isValid)
    {
        result.IsValid = isValid && !HasErrors();
        if (!result.IsValid)
            result.Applied = false;
        return result;
    }

    private void PrepareCreates(ScriptingCreateOperations creates)
    {
        ForEach(creates.TaskForces, "creates.taskForces", (operation, path) =>
        {
            var model = new TaskForce(idAllocator.AllocateInternalId());
            taskForceCreates.Add(new PendingCreate<TaskForceCreate, TaskForce>(operation, model, path));
            RegisterTemporaryKey(operation?.TemporaryKey, ScriptingElementKind.TaskForce, model, model.ININame, null, path);
        });

        ForEach(creates.Scripts, "creates.scripts", (operation, path) =>
        {
            var model = new Script(idAllocator.AllocateInternalId());
            scriptCreates.Add(new PendingCreate<ScriptCreate, Script>(operation, model, path));
            RegisterTemporaryKey(operation?.TemporaryKey, ScriptingElementKind.Script, model, model.ININame, null, path);
        });

        ForEach(creates.TeamTypes, "creates.teamTypes", (operation, path) =>
        {
            var model = new TeamType(idAllocator.AllocateInternalId());
            teamTypeCreates.Add(new PendingCreate<TeamTypeCreate, TeamType>(operation, model, path));
            RegisterTemporaryKey(operation?.TemporaryKey, ScriptingElementKind.TeamType, model, model.ININame, null, path);
        });

        ForEach(creates.AITriggers, "creates.aiTriggers", (operation, path) =>
        {
            var model = new AITriggerType(idAllocator.AllocateInternalId());
            aiTriggerCreates.Add(new PendingCreate<AITriggerCreate, AITriggerType>(operation, model, path));
            RegisterTemporaryKey(operation?.TemporaryKey, ScriptingElementKind.AITrigger, model, model.ININame, null, path);
        });

        ForEach(creates.LocalVariables, "creates.localVariables", (operation, path) =>
        {
            int index;
            if (operation?.Index is int requestedIndex)
            {
                index = requestedIndex;
                if (index < 0)
                    AddError("invalid_index", path + ".index", "A local-variable index cannot be negative.");
                if (map.LocalVariables.Exists(variable => variable.Index == index) ||
                    localVariableCreates.Exists(pending => pending.Model.Index == index))
                {
                    AddError("duplicate_index", path + ".index", $"Local-variable index {index} is already in use or requested by this change set.");
                }
                idAllocator.ReserveLocalVariableIndex(index);
            }
            else
            {
                index = idAllocator.AllocateLocalVariableIndex();
            }

            var model = new LocalVariable(index);
            localVariableCreates.Add(new PendingCreate<LocalVariableCreate, LocalVariable>(operation, model, path));
            RegisterTemporaryKey(operation?.TemporaryKey, ScriptingElementKind.LocalVariable, model, null, index, path);
        });

        ForEach(creates.Triggers, "creates.triggers", (operation, path) =>
        {
            var model = new Trigger(idAllocator.AllocateInternalId());
            triggerCreates.Add(new PendingCreate<TriggerCreate, Trigger>(operation, model, path));
            RegisterTemporaryKey(operation?.TemporaryKey, ScriptingElementKind.Trigger, model, model.ID, null, path);
        });

        ForEach(creates.Tags, "creates.tags", (operation, path) =>
        {
            var model = new Tag { ID = idAllocator.AllocateInternalId() };
            tagCreates.Add(new PendingCreate<TagCreate, Tag>(operation, model, path));
            RegisterTemporaryKey(operation?.TemporaryKey, ScriptingElementKind.Tag, model, model.ID, null, path);
        });
    }

    private void PrepareUpdates(ScriptingUpdateOperations updates)
    {
        ForEach(updates.TaskForces, "updates.taskForces", (operation, path) =>
            PrepareUpdate(operation, operation?.Id, ScriptingElementKind.TaskForce, map.TaskForces,
                model => model.ININame, model => ScriptingContentHasher.Compute(model), taskForceUpdates, path));
        ForEach(updates.Scripts, "updates.scripts", (operation, path) =>
            PrepareUpdate(operation, operation?.Id, ScriptingElementKind.Script, map.Scripts,
                model => model.ININame, model => ScriptingContentHasher.Compute(model), scriptUpdates, path));
        ForEach(updates.TeamTypes, "updates.teamTypes", (operation, path) =>
            PrepareUpdate(operation, operation?.Id, ScriptingElementKind.TeamType, map.TeamTypes,
                model => model.ININame, model => ScriptingContentHasher.Compute(model), teamTypeUpdates, path));
        ForEach(updates.AITriggers, "updates.aiTriggers", (operation, path) =>
            PrepareUpdate(operation, operation?.Id, ScriptingElementKind.AITrigger, map.AITriggerTypes,
                model => model.ININame, model => ScriptingContentHasher.Compute(model), aiTriggerUpdates, path));
        ForEach(updates.LocalVariables, "updates.localVariables", (operation, path) =>
            PrepareLocalVariableUpdate(operation, path));
        ForEach(updates.Triggers, "updates.triggers", (operation, path) =>
            PrepareUpdate(operation, operation?.Id, ScriptingElementKind.Trigger, map.Triggers,
                model => model.ID, model => ScriptingContentHasher.Compute(model, map.EditorConfig), triggerUpdates, path));
        ForEach(updates.Tags, "updates.tags", (operation, path) =>
            PrepareUpdate(operation, operation?.Id, ScriptingElementKind.Tag, map.Tags,
                model => model.ID, model => ScriptingContentHasher.Compute(model), tagUpdates, path));
    }

    private void PrepareUpdate<TOperation, TModel>(
        TOperation operation,
        string id,
        ScriptingElementKind kind,
        List<TModel> models,
        Func<TModel, string> getId,
        Func<TModel, string> computeHash,
        List<PendingUpdate<TOperation, TModel>> pendingUpdates,
        string path)
        where TOperation : class
        where TModel : class
    {
        if (operation == null)
        {
            AddError("null_operation", path, "An update operation cannot be null.");
            return;
        }
        if (string.IsNullOrWhiteSpace(id))
        {
            AddError("missing_id", path + ".id", "An existing ID is required.");
            return;
        }

        TModel model = models.Find(candidate => string.Equals(getId(candidate), id.Trim(), StringComparison.OrdinalIgnoreCase));
        if (model == null)
        {
            AddError("not_found", path + ".id", $"{kind} '{id}' does not exist as a map-local element.");
            return;
        }
        if (!RegisterTouched(kind, getId(model), null, path))
            return;

        string expectedHash = GetExpectedHash(operation);
        ValidateExpectedHash(expectedHash, computeHash(model), path + ".expectedContentHash");
        pendingUpdates.Add(new PendingUpdate<TOperation, TModel>(operation, model, path));
    }

    private void PrepareLocalVariableUpdate(LocalVariableUpdate operation, string path)
    {
        if (operation == null)
        {
            AddError("null_operation", path, "An update operation cannot be null.");
            return;
        }

        LocalVariable variable = map.LocalVariables.Find(candidate => candidate.Index == operation.Index);
        if (variable == null)
        {
            AddError("not_found", path + ".index", $"Local variable {operation.Index} does not exist.");
            return;
        }
        if (!RegisterTouched(ScriptingElementKind.LocalVariable, null, variable.Index, path))
            return;

        ValidateExpectedHash(operation.ExpectedContentHash, ScriptingContentHasher.Compute(variable), path + ".expectedContentHash");
        localVariableUpdates.Add(new PendingUpdate<LocalVariableUpdate, LocalVariable>(operation, variable, path));
    }

    private void PrepareDeletes(List<ScriptingDeleteOperation> operations)
    {
        ForEach(operations, "deletes", (operation, path) =>
        {
            if (operation == null)
            {
                AddError("null_operation", path, "A delete operation cannot be null.");
                return;
            }

            object model = FindLocalElement(operation.Kind, operation.Id, operation.Index, path);
            if (model == null)
                return;
            if (!RegisterTouched(operation.Kind, GetElementId(operation.Kind, model), GetElementIndex(operation.Kind, model), path))
                return;

            ValidateExpectedHash(operation.ExpectedContentHash, ComputeHash(operation.Kind, model), path + ".expectedContentHash");
            deletedObjects.Add(model);
            deletes.Add(new PendingDelete(operation.Kind, model, path));
        });
    }

    private void ValidatePreconditions(List<ScriptingContentPrecondition> preconditions)
    {
        ForEach(preconditions, "preconditions", (precondition, path) =>
        {
            if (precondition == null)
            {
                AddError("null_precondition", path, "A content precondition cannot be null.");
                return;
            }

            if (precondition.Kind is ScriptingElementKind.MapTechno or ScriptingElementKind.CellTag)
            {
                try
                {
                    ScriptingElementIdentityInfo identity = referenceService.GetElementIdentity(new ScriptingElementReference
                    {
                        Kind = precondition.Kind,
                        Id = precondition.Id,
                        Index = precondition.Index
                    });
                    ValidateExpectedHash(precondition.ExpectedContentHash, identity.ContentHash, path + ".expectedContentHash");
                }
                catch (MapFacadeValidationException ex)
                {
                    AddError("not_found", path, ex.Message);
                }
                return;
            }

            object model = FindExistingElement(precondition.Kind, precondition.Id, precondition.Index, true, path);
            if (model == null)
                return;
            ValidateExpectedHash(precondition.ExpectedContentHash, ComputeHash(precondition.Kind, model), path + ".expectedContentHash");
        });
    }

    private void BuildCreates()
    {
        foreach (var pending in taskForceCreates)
            TryBuild(pending.Path, () => PopulateTaskForce(pending.Model, pending.Operation?.Value, pending.Path + ".value"));
        foreach (var pending in scriptCreates)
            TryBuild(pending.Path, () => PopulateScript(pending.Model, pending.Operation?.Value, null, pending.Path + ".value"));
        foreach (var pending in teamTypeCreates)
            TryBuild(pending.Path, () => PopulateTeamType(pending.Model, pending.Operation?.Value, true, pending.Path + ".value"));
        foreach (var pending in aiTriggerCreates)
            TryBuild(pending.Path, () => PopulateAITrigger(pending.Model, pending.Operation?.Value, pending.Path + ".value"));
        foreach (var pending in localVariableCreates)
            TryBuild(pending.Path, () => PopulateLocalVariable(pending.Model, pending.Operation?.Value, pending.Path + ".value"));
        foreach (var pending in triggerCreates)
            TryBuild(pending.Path, () => PopulateTrigger(pending.Model, pending.Operation?.Value, null, pending.Path + ".value"));
        foreach (var pending in tagCreates)
            TryBuild(pending.Path, () => PopulateTag(pending.Model, pending.Operation?.Value, pending.Path + ".value"));
    }

    private void BuildUpdates()
    {
        foreach (var pending in taskForceUpdates)
        {
            TryBuild(pending.Path, () =>
            {
                var replacement = new TaskForce(pending.Existing.ININame);
                PopulateTaskForce(replacement, pending.Operation.Replacement, pending.Path + ".replacement");
                taskForceReplacements.Add(pending.Existing, replacement);
            });
        }
        foreach (var pending in scriptUpdates)
        {
            TryBuild(pending.Path, () =>
            {
                var replacement = new Script(pending.Existing.ININame);
                PopulateScript(replacement, pending.Operation.Replacement, pending.Existing, pending.Path + ".replacement");
                scriptReplacements.Add(pending.Existing, replacement);
            });
        }
        foreach (var pending in teamTypeUpdates)
        {
            TryBuild(pending.Path, () =>
            {
                var replacement = new TeamType(pending.Existing.ININame);
                PopulateTeamType(replacement, pending.Operation.Replacement, false, pending.Path + ".replacement");
                teamTypeReplacements.Add(pending.Existing, replacement);
            });
        }
        foreach (var pending in aiTriggerUpdates)
        {
            TryBuild(pending.Path, () =>
            {
                var replacement = new AITriggerType(pending.Existing.ININame);
                PopulateAITrigger(replacement, pending.Operation.Replacement, pending.Path + ".replacement");
                aiTriggerReplacements.Add(pending.Existing, replacement);
            });
        }
        foreach (var pending in localVariableUpdates)
        {
            TryBuild(pending.Path, () =>
            {
                var replacement = new LocalVariable(pending.Existing.Index);
                PopulateLocalVariable(replacement, pending.Operation.Replacement, pending.Path + ".replacement");
                localVariableReplacements.Add(pending.Existing, replacement);
            });
        }
        foreach (var pending in triggerUpdates)
        {
            TryBuild(pending.Path, () =>
            {
                var replacement = new Trigger(pending.Existing.ID);
                PopulateTrigger(replacement, pending.Operation.Replacement, pending.Existing, pending.Path + ".replacement");
                triggerReplacements.Add(pending.Existing, replacement);
            });
        }
        foreach (var pending in tagUpdates)
        {
            TryBuild(pending.Path, () =>
            {
                var replacement = new Tag { ID = pending.Existing.ID };
                PopulateTag(replacement, pending.Operation.Replacement, pending.Path + ".replacement");
                tagReplacements.Add(pending.Existing, replacement);
            });
        }
    }

    private void PopulateTaskForce(TaskForce taskForce, TaskForceDataInput input, string path)
    {
        RequireInput(input, path);
        taskForce.Name = RequireName(input.Name, path + ".name");
        taskForce.Group = input.Group;

        List<TaskForceMemberInput> members = input.Members ?? new List<TaskForceMemberInput>();
        if (members.Count == 0)
            throw new ScriptingValidationException(path + ".members", "A TaskForce must contain at least one member.");
        if (members.Count > TaskForce.MaxTechnoCount)
            throw new ScriptingValidationException(path + ".members", $"A TaskForce can contain at most {TaskForce.MaxTechnoCount} member types.");

        var occupiedSlots = new HashSet<int>();
        for (int i = 0; i < members.Count; i++)
        {
            TaskForceMemberInput member = members[i] ?? throw new ScriptingValidationException(path + $".members[{i}]", "A TaskForce member cannot be null.");
            if (member.Count < 1)
                throw new ScriptingValidationException(path + $".members[{i}].count", "A TaskForce member count must be at least 1.");

            string typeName = RequireName(member.TechnoType, path + $".members[{i}].technoType");
            TechnoType technoType = map.Rules.AircraftTypes.Cast<TechnoType>()
                .Concat(map.Rules.UnitTypes)
                .Concat(map.Rules.InfantryTypes)
                .FirstOrDefault(type => string.Equals(type.ININame, typeName, StringComparison.OrdinalIgnoreCase));
            if (technoType == null)
                throw new ScriptingValidationException(path + $".members[{i}].technoType",
                    $"Aircraft, vehicle, or infantry type '{typeName}' does not exist in the loaded rules.");

            int slot = member.Index ?? Enumerable.Range(0, TaskForce.MaxTechnoCount).First(candidate => !occupiedSlots.Contains(candidate));
            if (slot < 0 || slot >= TaskForce.MaxTechnoCount)
                throw new ScriptingValidationException(path + $".members[{i}].index",
                    $"TaskForce member index must be from 0 through {TaskForce.MaxTechnoCount - 1}.");
            if (!occupiedSlots.Add(slot))
                throw new ScriptingValidationException(path + $".members[{i}].index", $"TaskForce member slot {slot} is used more than once.");

            taskForce.TechnoTypes[slot] = new TaskForceTechnoEntry { TechnoType = technoType, Count = member.Count };
        }
    }

    private void PopulateScript(Script script, ScriptDataInput input, Script existingScript, string path)
    {
        RequireInput(input, path);
        script.Name = RequireName(input.Name, path + ".name");
        script.EditorColor = ResolveEditorColor(input.EditorColor, Script.SupportedColors.Select(color => color.Name), path + ".editorColor");

        List<ScriptActionInput> actions = input.Actions ?? new List<ScriptActionInput>();
        if (actions.Count > 50)
            throw new ScriptingValidationException(path + ".actions", "A Script can contain at most 50 actions.");

        for (int i = 0; i < actions.Count; i++)
        {
            ScriptActionInput action = actions[i] ?? throw new ScriptingValidationException(path + $".actions[{i}]", "A script action cannot be null.");
            map.EditorConfig.ScriptActions.TryGetValue(action.ActionId, out ScriptAction definition);
            if (definition == null)
            {
                bool preservesUnknown = existingScript != null && i < existingScript.Actions.Count &&
                    existingScript.Actions[i].Action == action.ActionId &&
                    int.TryParse(action.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int requestedRawArgument) &&
                    existingScript.Actions[i].Argument == requestedRawArgument && action.Reference == null;
                if (!preservesUnknown)
                {
                    throw new ScriptingValidationException(
                        path + $".actions[{i}].actionId",
                        $"Script action {action.ActionId} is not defined in the active configuration. " +
                        "Unknown existing actions can only be preserved unchanged.");
                }
            }

            int argument = parameterCodec.EncodeScriptArgument(action, definition, ResolveReference, path + $".actions[{i}]");
            script.Actions.Add(new ScriptActionEntry(action.ActionId, argument));
        }
    }

    private void PopulateTeamType(TeamType teamType, TeamTypeDataInput input, bool applyFlagDefaults, string path)
    {
        RequireInput(input, path);
        teamType.Name = RequireName(input.Name, path + ".name");
        teamType.Group = input.Group;
        teamType.HouseType = string.IsNullOrWhiteSpace(input.HouseType)
            ? null
            : ResolveHouseType(input.HouseType, path + ".houseType");
        teamType.Script = (Script)ResolveRequiredReference(input.Script, ScriptingElementKind.Script, path + ".script");
        teamType.TaskForce = (TaskForce)ResolveRequiredReference(input.TaskForce, ScriptingElementKind.TaskForce, path + ".taskForce");
        teamType.Tag = input.Tag == null ? null : (Tag)ResolveReference(input.Tag, ScriptingElementKind.Tag, path + ".tag");
        teamType.Max = input.Max;
        teamType.Priority = input.Priority;
        teamType.Waypoint = EncodeOptionalWaypoint(input.Waypoint, path + ".waypoint");
        teamType.TransportWaypoint = EncodeOptionalWaypoint(input.TransportWaypoint, path + ".transportWaypoint");
        teamType.MindControlDecision = input.MindControlDecision;
        teamType.TechLevel = input.TechLevel;
        teamType.VeteranLevel = input.VeteranLevel;
        teamType.EditorColor = ResolveEditorColor(input.EditorColor, TeamType.SupportedColors.Select(color => color.Name), path + ".editorColor");

        if (teamType.Max < 0)
            throw new ScriptingValidationException(path + ".max", "TeamType max cannot be negative.");
        if (teamType.VeteranLevel < 1 || teamType.VeteranLevel > 3)
            throw new ScriptingValidationException(path + ".veteranLevel", "TeamType veteranLevel must be 1, 2, or 3.");
        if (teamType.MindControlDecision.HasValue && (teamType.MindControlDecision.Value < 0 || teamType.MindControlDecision.Value > 5))
            throw new ScriptingValidationException(path + ".mindControlDecision", "Mind-control decision must be from 0 through 5.");
        if (!Constants.IsRA2YR && teamType.TransportWaypoint != null)
            throw new ScriptingValidationException(path + ".transportWaypoint", "Transport waypoints are only available in Red Alert 2 and Yuri's Revenge.");
        if (!Constants.IsRA2YR && teamType.MindControlDecision.HasValue)
            throw new ScriptingValidationException(path + ".mindControlDecision",
                "Mind-control decisions are only available in Red Alert 2 and Yuri's Revenge.");

        IEnumerable<string> requestedFlags = input.EnabledFlags;
        if (requestedFlags == null && applyFlagDefaults)
            requestedFlags = map.EditorConfig.TeamTypeFlags.Where(flag => flag.DefaultValue).Select(flag => flag.Name);
        requestedFlags ??= Array.Empty<string>();

        var seenFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string requestedFlag in requestedFlags)
        {
            if (string.IsNullOrWhiteSpace(requestedFlag))
                throw new ScriptingValidationException(path + ".enabledFlags", "A TeamType flag name cannot be empty.");
            TeamTypeFlag flag = map.EditorConfig.TeamTypeFlags.Find(candidate =>
                string.Equals(candidate.Name, requestedFlag.Trim(), StringComparison.OrdinalIgnoreCase));
            if (flag == null)
                throw new ScriptingValidationException(path + ".enabledFlags",
                    $"TeamType flag '{requestedFlag}' is not defined in the active configuration.");
            if (seenFlags.Add(flag.Name))
                teamType.EnableFlag(flag.Name);
        }
    }

    private void PopulateAITrigger(AITriggerType aiTrigger, AITriggerDataInput input, string path)
    {
        RequireInput(input, path);
        aiTrigger.Name = RequireCommaSafeName(input.Name, path + ".name");
        aiTrigger.PrimaryTeam = input.PrimaryTeam == null ? null :
            (TeamType)ResolveReference(input.PrimaryTeam, ScriptingElementKind.TeamType, path + ".primaryTeam");
        aiTrigger.OwnerName = string.IsNullOrWhiteSpace(input.OwnerName) ? "<all>" : RequireCommaSafeName(input.OwnerName, path + ".ownerName");
        if (!string.Equals(aiTrigger.OwnerName, "<all>", StringComparison.OrdinalIgnoreCase))
        {
            HouseType ownerHouseType = map.GetHouseTypes().Find(candidate =>
                string.Equals(candidate.ININame, aiTrigger.OwnerName, StringComparison.OrdinalIgnoreCase));
            if (ownerHouseType == null)
                throw new ScriptingValidationException(path + ".ownerName", $"AITrigger owner HouseType '{aiTrigger.OwnerName}' does not exist.");
            aiTrigger.OwnerName = ownerHouseType.ININame;
        }
        else
        {
            aiTrigger.OwnerName = "<all>";
        }

        int maximumCondition = Constants.IsRA2YR ? (int)AITriggerConditionType.NeutralHouseOwns : (int)AITriggerConditionType.EnemyHasCredits;
        if (input.ConditionType < (int)AITriggerConditionType.None || input.ConditionType > maximumCondition)
            throw new ScriptingValidationException(path + ".conditionType",
                $"AITrigger condition type {input.ConditionType} is not available for the active game.");

        aiTrigger.TechLevel = input.TechLevel;
        aiTrigger.ConditionType = (AITriggerConditionType)input.ConditionType;
        bool conditionNeedsObject = input.ConditionType == (int)AITriggerConditionType.EnemyOwns ||
            input.ConditionType == (int)AITriggerConditionType.OwnerOwns ||
            input.ConditionType == (int)AITriggerConditionType.NeutralHouseOwns;
        aiTrigger.ConditionObject = string.IsNullOrWhiteSpace(input.ConditionObject)
            ? null
            : ResolveTechnoType(input.ConditionObject, path + ".conditionObject");
        if (conditionNeedsObject && aiTrigger.ConditionObject == null)
            throw new ScriptingValidationException(path + ".conditionObject", "This AITrigger condition requires a comparison object.");

        if (!Enum.IsDefined(typeof(AITriggerComparatorOperator), input.ComparatorOperator))
            throw new ScriptingValidationException(path + ".comparatorOperator", "AITrigger comparatorOperator must be from 0 through 5.");
        aiTrigger.Comparator = new AITriggerComparator((AITriggerComparatorOperator)input.ComparatorOperator, input.ComparatorQuantity);
        if (!double.IsFinite(input.InitialWeight) || !double.IsFinite(input.MinimumWeight) || !double.IsFinite(input.MaximumWeight))
            throw new ScriptingValidationException(path, "AITrigger weights must be finite numbers.");
        aiTrigger.InitialWeight = ScriptingContentHasher.NormalizeAITriggerWeight(input.InitialWeight);
        aiTrigger.MinimumWeight = ScriptingContentHasher.NormalizeAITriggerWeight(input.MinimumWeight);
        aiTrigger.MaximumWeight = ScriptingContentHasher.NormalizeAITriggerWeight(input.MaximumWeight);

        if (input.Side < 0 || input.Side > map.Rules.Sides.Count)
            throw new ScriptingValidationException(path + ".side", $"AITrigger side must be from 0 through {map.Rules.Sides.Count}.");
        aiTrigger.Side = input.Side;
        aiTrigger.SecondaryTeam = input.SecondaryTeam == null ? null :
            (TeamType)ResolveReference(input.SecondaryTeam, ScriptingElementKind.TeamType, path + ".secondaryTeam");
        aiTrigger.Easy = input.Easy;
        aiTrigger.Medium = input.Medium;
        aiTrigger.Hard = input.Hard;
        aiTrigger.Enabled = input.Enabled;

        // These three fields are normalized by the map writer and are not independently editable.
        aiTrigger.EnabledInMultiplayer = true;
        aiTrigger.Unused = false;
        aiTrigger.IsBaseDefense = false;
    }

    private void PopulateLocalVariable(LocalVariable variable, LocalVariableDataInput input, string path)
    {
        RequireInput(input, path);
        variable.Name = RequireCommaSafeName(input.Name, path + ".name");
        if (!Constants.IntegerVariables && input.InitialState != 0 && input.InitialState != 1)
            throw new ScriptingValidationException(path + ".initialState", "This game configuration supports only local-variable initial states 0 and 1.");
        variable.InitialState = input.InitialState;
    }

    private void PopulateTrigger(Trigger trigger, TriggerDataInput input, Trigger existingTrigger, string path)
    {
        RequireInput(input, path);
        trigger.Name = RequireCommaSafeName(input.Name, path + ".name");
        trigger.HouseType = ResolveHouseType(input.HouseType, path + ".houseType");
        trigger.LinkedTrigger = input.LinkedTrigger == null ? null :
            (Trigger)ResolveReference(input.LinkedTrigger, ScriptingElementKind.Trigger, path + ".linkedTrigger");
        trigger.Disabled = input.Disabled;
        trigger.Easy = input.Easy;
        trigger.Normal = input.Normal;
        trigger.Hard = input.Hard;
        trigger.EditorColor = ResolveEditorColor(input.EditorColor, Trigger.SupportedColors.Select(color => color.Name), path + ".editorColor");

        List<TriggerEventInput> events = input.Events ?? new List<TriggerEventInput>();
        for (int i = 0; i < events.Count; i++)
        {
            TriggerEventInput eventInput = events[i] ?? throw new ScriptingValidationException(path + $".events[{i}]", "A trigger event cannot be null.");
            if (!map.EditorConfig.TriggerEventTypes.TryGetValue(eventInput.EventId, out TriggerEventType definition))
                throw new ScriptingValidationException(path + $".events[{i}].eventId",
                    $"Trigger event {eventInput.EventId} is not defined in the active configuration.");

            var condition = new TriggerCondition(definition);
            PopulateKnownTriggerParameters(
                definition.Parameters.Select(parameter => parameter?.TriggerParamType ?? TriggerParamType.Unused).ToArray(),
                definition.Parameters.Select(parameter => parameter?.PresetOptions).ToArray(),
                eventInput.Parameters,
                condition.Parameters,
                path + $".events[{i}].parameters");

            if (!definition.Available)
            {
                TriggerCondition existingCondition = existingTrigger != null && i < existingTrigger.Conditions.Count
                    ? existingTrigger.Conditions[i]
                    : null;
                bool preservesUnavailableEvent = existingCondition != null &&
                    existingCondition.ConditionIndex == condition.ConditionIndex &&
                    existingCondition.Parameters.SequenceEqual(condition.Parameters, StringComparer.Ordinal);
                if (!preservesUnavailableEvent)
                {
                    throw new ScriptingValidationException(
                        path + $".events[{i}].eventId",
                        $"Trigger event {eventInput.EventId} is unavailable in the active configuration. " +
                        "An existing unavailable event can only be preserved unchanged.");
                }
            }

            trigger.Conditions.Add(condition);
        }

        List<TriggerActionInput> actions = input.Actions ?? new List<TriggerActionInput>();
        if (actions.Count > 18)
            throw new ScriptingValidationException(path + ".actions", "A Trigger can contain at most 18 actions without exceeding the game action buffer.");
        for (int i = 0; i < actions.Count; i++)
        {
            TriggerActionInput actionInput = actions[i] ?? throw new ScriptingValidationException(path + $".actions[{i}]", "A trigger action cannot be null.");
            if (!map.EditorConfig.TriggerActionTypes.TryGetValue(actionInput.ActionId, out TriggerActionType definition))
            {
                TriggerAction existingAction = existingTrigger != null && i < existingTrigger.Actions.Count
                    ? existingTrigger.Actions[i]
                    : null;
                trigger.Actions.Add(PreserveUnknownTriggerAction(actionInput, existingAction, path + $".actions[{i}]"));
                continue;
            }

            var action = new TriggerAction { ActionIndex = actionInput.ActionId };
            PopulateKnownTriggerParameters(
                definition.Parameters.Select(parameter => parameter?.TriggerParamType ?? TriggerParamType.Unused).ToArray(),
                definition.Parameters.Select(parameter => parameter?.PresetOptions).ToArray(),
                actionInput.Parameters,
                action.Parameters,
                path + $".actions[{i}].parameters");
            trigger.Actions.Add(action);
        }
    }

    private void PopulateTag(Tag tag, TagDataInput input, string path)
    {
        RequireInput(input, path);
        tag.Name = RequireCommaSafeName(input.Name, path + ".name");
        if (input.Repeating < 0 || input.Repeating > Tag.REPEAT_TYPE_MAX)
            throw new ScriptingValidationException(path + ".repeating", $"Tag repeating must be from 0 through {Tag.REPEAT_TYPE_MAX}.");
        tag.Repeating = input.Repeating;
        tag.Trigger = (Trigger)ResolveRequiredReference(input.Trigger, ScriptingElementKind.Trigger, path + ".trigger");
    }

    private void PopulateKnownTriggerParameters(
        TriggerParamType[] parameterTypes,
        List<string>[] presetOptions,
        List<ScriptingParameterValueInput> suppliedParameters,
        string[] destination,
        string path)
    {
        var parametersByIndex = new Dictionary<int, ScriptingParameterValueInput>();
        ForEach(suppliedParameters, path, (parameter, parameterPath) =>
        {
            if (parameter == null)
                throw new ScriptingValidationException(parameterPath, "A parameter cannot be null.");
            if (parameter.Index < 0 || parameter.Index >= destination.Length)
                throw new ScriptingValidationException(parameterPath + ".index", $"Parameter index must be from 0 through {destination.Length - 1}.");
            if (!parametersByIndex.TryAdd(parameter.Index, parameter))
                throw new ScriptingValidationException(parameterPath + ".index", $"Parameter index {parameter.Index} was supplied more than once.");
        });

        for (int i = 0; i < destination.Length; i++)
        {
            TriggerParamType parameterType = parameterTypes[i];
            parametersByIndex.TryGetValue(i, out ScriptingParameterValueInput suppliedParameter);
            if ((int)parameterType < 0)
            {
                if (suppliedParameter != null)
                    throw new ScriptingValidationException(path + $"[{i}]", "This parameter is fixed and must not be supplied.");
                destination[i] = Math.Abs((int)parameterType).ToString(CultureInfo.InvariantCulture);
                continue;
            }
            if (parameterType == TriggerParamType.Unused)
            {
                if (suppliedParameter != null)
                    throw new ScriptingValidationException(path + $"[{i}]", "This parameter is unused and must not be supplied.");
                continue;
            }
            if (suppliedParameter == null)
                throw new ScriptingValidationException(path + $"[{i}]", $"Parameter {i} ({parameterType}) is required.");

            destination[i] = parameterCodec.Encode(
                parameterType,
                ResolveConfiguredPresetValue(suppliedParameter.Value, presetOptions[i]),
                suppliedParameter.Reference,
                ResolveReference,
                path + $"[{i}]");
        }
    }

    private static string ResolveConfiguredPresetValue(string value, List<string> presetOptions)
    {
        if (string.IsNullOrWhiteSpace(value) || presetOptions == null || presetOptions.Count == 0)
            return value;

        string requestedValue = value.Trim();
        foreach (string configuredOption in presetOptions)
        {
            string option = configuredOption?.Trim() ?? string.Empty;
            int separatorIndex = option.IndexOfAny(new[] { ' ', '\t' });
            string storedValue = separatorIndex < 0 ? option : option[..separatorIndex];
            string displayName = separatorIndex < 0 ? option : option[(separatorIndex + 1)..].Trim();

            if (string.Equals(requestedValue, option, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requestedValue, storedValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requestedValue, displayName, StringComparison.OrdinalIgnoreCase))
            {
                return storedValue;
            }
        }

        return value;
    }

    private TriggerAction PreserveUnknownTriggerAction(TriggerActionInput input, TriggerAction existingAction, string path)
    {
        if (existingAction == null || existingAction.ActionIndex != input.ActionId)
            throw new ScriptingValidationException(path + ".actionId",
                $"Trigger action {input.ActionId} is not defined. Unknown existing actions can only be preserved unchanged.");

        var parametersByIndex = new Dictionary<int, ScriptingParameterValueInput>();
        ForEach(input.Parameters, path + ".parameters", (parameter, parameterPath) =>
        {
            if (parameter == null)
                throw new ScriptingValidationException(parameterPath, "A parameter cannot be null.");
            if (parameter.Index < 0 || parameter.Index >= TriggerAction.PARAM_COUNT)
            {
                throw new ScriptingValidationException(parameterPath + ".index",
                    $"Parameter index must be from 0 through {TriggerAction.PARAM_COUNT - 1}.");
            }
            if (!parametersByIndex.TryAdd(parameter.Index, parameter))
                throw new ScriptingValidationException(parameterPath + ".index", $"Parameter index {parameter.Index} was supplied more than once.");
        });

        if (parametersByIndex.Count != TriggerAction.PARAM_COUNT)
            throw new ScriptingValidationException(path + ".parameters", "Preserving an unknown trigger action requires all seven exact raw parameter values.");

        var replacement = new TriggerAction { ActionIndex = input.ActionId };
        for (int i = 0; i < TriggerAction.PARAM_COUNT; i++)
        {
            if (!parametersByIndex.TryGetValue(i, out ScriptingParameterValueInput parameter) || parameter.Reference != null ||
                parameter.Value != existingAction.Parameters[i])
            {
                throw new ScriptingValidationException(path + $".parameters[{i}]",
                    "Unknown trigger-action parameters must match the existing raw values exactly.");
            }
            replacement.Parameters[i] = parameter.Value;
        }
        return replacement;
    }

    private void ValidateProjectedState()
    {
        ValidateTriggerLinks();
        ValidateAITriggerTeams();

        foreach (PendingDelete pendingDelete in deletes)
        {
            List<string> incomingPaths = GetProjectedIncomingReferencePaths(pendingDelete.Kind, pendingDelete.Model);
            if (incomingPaths.Count == 0)
                continue;

            string identity = pendingDelete.Kind == ScriptingElementKind.LocalVariable
                ? GetElementIndex(pendingDelete.Kind, pendingDelete.Model).ToString()
                : GetElementId(pendingDelete.Kind, pendingDelete.Model);
            AddError(
                "element_is_referenced",
                pendingDelete.Path,
                $"{pendingDelete.Kind} '{identity}' cannot be deleted because it is still referenced by: {string.Join(", ", incomingPaths)}.");
        }
    }

    private void ValidateTriggerLinks()
    {
        var changedTriggerIds = new HashSet<string>(
            triggerReplacements.Values.Select(trigger => trigger.ID)
                .Concat(triggerCreates.Select(pending => pending.Model.ID)),
            StringComparer.OrdinalIgnoreCase);
        if (changedTriggerIds.Count == 0)
            return;

        List<Trigger> triggers = GetEffectiveTriggers().ToList();
        var byId = triggers.ToDictionary(trigger => trigger.ID, StringComparer.OrdinalIgnoreCase);
        foreach (string changedTriggerId in changedTriggerIds)
        {
            if (!byId.ContainsKey(changedTriggerId))
                continue;
            if (HasTriggerLinkCycle(changedTriggerId, byId, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)))
            {
                AddError("linked_trigger_cycle", "triggers", $"Linked triggers form a cycle reached from changed Trigger '{changedTriggerId}'.");
                return;
            }
        }
    }

    private static bool HasTriggerLinkCycle(string id, Dictionary<string, Trigger> byId, Dictionary<string, int> states)
    {
        if (states.TryGetValue(id, out int state))
            return state == 1;

        states[id] = 1;
        Trigger trigger = byId[id];
        string linkedId = trigger.LinkedTrigger?.ID;
        if (linkedId != null && byId.ContainsKey(linkedId) && HasTriggerLinkCycle(linkedId, byId, states))
            return true;
        states[id] = 2;
        return false;
    }

    private void ValidateAITriggerTeams()
    {
        var changedAITriggers = new HashSet<AITriggerType>(aiTriggerReplacements.Values.Concat(aiTriggerCreates.Select(pending => pending.Model)));
        var changedTeamIds = new HashSet<string>(
            teamTypeReplacements.Values.Select(teamType => teamType.ININame)
                .Concat(teamTypeCreates.Select(pending => pending.Model.ININame)),
            StringComparer.OrdinalIgnoreCase);

        foreach (AITriggerType aiTrigger in GetEffectiveAITriggers())
        {
            bool isRelevant = changedAITriggers.Contains(aiTrigger) ||
                (aiTrigger.PrimaryTeam != null && changedTeamIds.Contains(aiTrigger.PrimaryTeam.ININame)) ||
                (aiTrigger.SecondaryTeam != null && changedTeamIds.Contains(aiTrigger.SecondaryTeam.ININame));
            if (!isRelevant)
                continue;

            ValidateAITriggerTeam(aiTrigger, aiTrigger.PrimaryTeam, "primaryTeam");
            ValidateAITriggerTeam(aiTrigger, aiTrigger.SecondaryTeam, "secondaryTeam");
        }
    }

    private void ValidateAITriggerTeam(AITriggerType aiTrigger, TeamType referencedTeam, string propertyName)
    {
        if (referencedTeam == null)
            return;

        TeamType effectiveTeam = teamTypeReplacements.GetValueOrDefault(referencedTeam, referencedTeam);
        if (effectiveTeam.Max == 0 &&
            (map.TeamTypes.Contains(referencedTeam) || teamTypeCreates.Exists(pending => pending.Model == referencedTeam)))
        {
            AddError(
                "ai_team_max_zero",
                $"aiTriggers[{aiTrigger.ININame}].{propertyName}",
                $"TeamType '{referencedTeam.ININame}' has Max=0, so the AI cannot produce it. Set Max to at least 1.");
        }
    }

    private List<string> GetProjectedIncomingReferencePaths(ScriptingElementKind targetKind, object target)
    {
        var paths = new List<string>();
        string targetId = GetElementId(targetKind, target);
        int? targetIndex = GetElementIndex(targetKind, target);

        foreach (TeamType teamType in GetEffectiveTeamTypes())
        {
            AddIfTarget(paths, targetKind, targetId, targetIndex, ScriptingElementKind.TaskForce,
                teamType.TaskForce?.ININame, null, $"TeamType[{teamType.ININame}].TaskForce");
            AddIfTarget(paths, targetKind, targetId, targetIndex, ScriptingElementKind.Script,
                teamType.Script?.ININame, null, $"TeamType[{teamType.ININame}].Script");
            AddIfTarget(paths, targetKind, targetId, targetIndex, ScriptingElementKind.Tag, teamType.Tag?.ID, null, $"TeamType[{teamType.ININame}].Tag");
        }

        foreach (AITriggerType aiTrigger in GetEffectiveAITriggers())
        {
            AddIfTarget(paths, targetKind, targetId, targetIndex, ScriptingElementKind.TeamType,
                aiTrigger.PrimaryTeam?.ININame, null, $"AITrigger[{aiTrigger.ININame}].PrimaryTeam");
            AddIfTarget(paths, targetKind, targetId, targetIndex, ScriptingElementKind.TeamType,
                aiTrigger.SecondaryTeam?.ININame, null, $"AITrigger[{aiTrigger.ININame}].SecondaryTeam");
        }

        foreach (Tag tag in GetEffectiveTags())
            AddIfTarget(paths, targetKind, targetId, targetIndex, ScriptingElementKind.Trigger, tag.Trigger?.ID, null, $"Tag[{tag.ID}].Trigger");

        foreach (Trigger trigger in GetEffectiveTriggers())
        {
            AddIfTarget(paths, targetKind, targetId, targetIndex, ScriptingElementKind.Trigger,
                trigger.LinkedTrigger?.ID, null, $"Trigger[{trigger.ID}].LinkedTrigger");
            for (int i = 0; i < trigger.Conditions.Count; i++)
            {
                if (!map.EditorConfig.TriggerEventTypes.TryGetValue(trigger.Conditions[i].ConditionIndex, out TriggerEventType definition))
                    continue;
                AddTypedParameterReferences(paths, targetKind, targetId, targetIndex,
                    definition.Parameters.Select(parameter => parameter?.TriggerParamType ?? TriggerParamType.Unused).ToArray(),
                    trigger.Conditions[i].Parameters, $"Trigger[{trigger.ID}].Events[{i}].Parameters");
            }
            for (int i = 0; i < trigger.Actions.Count; i++)
            {
                if (!map.EditorConfig.TriggerActionTypes.TryGetValue(trigger.Actions[i].ActionIndex, out TriggerActionType definition))
                {
                    for (int parameterIndex = 0; parameterIndex < trigger.Actions[i].Parameters.Length; parameterIndex++)
                    {
                        AddUnknownParameterReference(
                            paths,
                            targetKind,
                            targetId,
                            targetIndex,
                            trigger.Actions[i].Parameters[parameterIndex],
                            $"Trigger[{trigger.ID}].Actions[{i}].Parameters[{parameterIndex}] (unknown definition)");
                    }
                    continue;
                }
                AddTypedParameterReferences(paths, targetKind, targetId, targetIndex,
                    definition.Parameters.Select(parameter => parameter?.TriggerParamType ?? TriggerParamType.Unused).ToArray(),
                    trigger.Actions[i].Parameters, $"Trigger[{trigger.ID}].Actions[{i}].Parameters");
            }
        }

        foreach (Script script in GetEffectiveScripts())
        {
            for (int i = 0; i < script.Actions.Count; i++)
            {
                if (!map.EditorConfig.ScriptActions.TryGetValue(script.Actions[i].Action, out ScriptAction definition))
                {
                    AddUnknownParameterReference(
                        paths,
                        targetKind,
                        targetId,
                        targetIndex,
                        script.Actions[i].Argument.ToString(CultureInfo.InvariantCulture),
                        $"Script[{script.ININame}].Actions[{i}].Argument (unknown definition)");
                    continue;
                }
                AddTypedParameterReference(paths, targetKind, targetId, targetIndex, definition.ParamType,
                    script.Actions[i].Argument.ToString(CultureInfo.InvariantCulture), $"Script[{script.ININame}].Actions[{i}].Argument");
            }
        }

        if (targetKind == ScriptingElementKind.Tag)
        {
            foreach (TechnoBase techno in map.Structures.Cast<TechnoBase>().Concat(map.Units).Concat(map.Infantry).Concat(map.Aircraft))
            {
                if (string.Equals(techno.AttachedTag?.ID, targetId, StringComparison.OrdinalIgnoreCase))
                    paths.Add($"MapTechno[{techno.ObjectId}].AttachedTag");
            }
            foreach (CellTag cellTag in map.CellTags)
            {
                if (string.Equals(cellTag.Tag?.ID, targetId, StringComparison.OrdinalIgnoreCase))
                    paths.Add($"CellTag[{cellTag.Position.X},{cellTag.Position.Y}].Tag");
            }
        }

        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddIfTarget(
        List<string> paths,
        ScriptingElementKind targetKind,
        string targetId,
        int? targetIndex,
        ScriptingElementKind referencedKind,
        string referencedId,
        int? referencedIndex,
        string path)
    {
        if (targetKind != referencedKind)
            return;
        bool matches = targetKind == ScriptingElementKind.LocalVariable
            ? targetIndex == referencedIndex
            : referencedId != null && string.Equals(targetId, referencedId, StringComparison.OrdinalIgnoreCase);
        if (matches)
            paths.Add(path);
    }

    private void AddTypedParameterReferences(
        List<string> paths,
        ScriptingElementKind targetKind,
        string targetId,
        int? targetIndex,
        TriggerParamType[] parameterTypes,
        string[] values,
        string path)
    {
        for (int i = 0; i < Math.Min(parameterTypes.Length, values.Length); i++)
            AddTypedParameterReference(paths, targetKind, targetId, targetIndex, parameterTypes[i], values[i], path + $"[{i}]");
    }

    private static void AddTypedParameterReference(
        List<string> paths,
        ScriptingElementKind targetKind,
        string targetId,
        int? targetIndex,
        TriggerParamType parameterType,
        string value,
        string path)
    {
        ScriptingElementKind? referencedKind = parameterType switch
        {
            TriggerParamType.LocalVariable => ScriptingElementKind.LocalVariable,
            TriggerParamType.TeamType => ScriptingElementKind.TeamType,
            TriggerParamType.Trigger => ScriptingElementKind.Trigger,
            TriggerParamType.Tag => ScriptingElementKind.Tag,
            _ => null
        };
        if (!referencedKind.HasValue || referencedKind.Value != targetKind)
            return;

        if (targetKind == ScriptingElementKind.LocalVariable)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int variableIndex) && variableIndex == targetIndex)
                paths.Add(path);
        }
        else if (string.Equals(value, targetId, StringComparison.OrdinalIgnoreCase) ||
            (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawIdNumber) &&
             int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int targetIdNumber) &&
             rawIdNumber == targetIdNumber))
        {
            paths.Add(path);
        }
    }

    private static void AddUnknownParameterReference(
        List<string> paths,
        ScriptingElementKind targetKind,
        string targetId,
        int? targetIndex,
        string rawValue,
        string path)
    {
        if (targetKind is not (ScriptingElementKind.LocalVariable or ScriptingElementKind.TeamType or ScriptingElementKind.Trigger or ScriptingElementKind.Tag))
            return;

        if (targetKind == ScriptingElementKind.LocalVariable)
        {
            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawIndex) && rawIndex == targetIndex)
                paths.Add(path);
            return;
        }

        if (string.Equals(rawValue, targetId, StringComparison.OrdinalIgnoreCase))
        {
            paths.Add(path);
            return;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawNumber) &&
            int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int targetNumber) &&
            rawNumber == targetNumber)
        {
            paths.Add(path);
        }
    }

    private IEnumerable<TaskForce> GetEffectiveTaskForces()
    {
        foreach (TaskForce model in map.TaskForces)
            if (!deletedObjects.Contains(model))
                yield return taskForceReplacements.GetValueOrDefault(model, model);
        foreach (var pending in taskForceCreates)
            yield return pending.Model;
    }

    private IEnumerable<Script> GetEffectiveScripts()
    {
        foreach (Script model in map.Scripts)
            if (!deletedObjects.Contains(model))
                yield return scriptReplacements.GetValueOrDefault(model, model);
        foreach (var pending in scriptCreates)
            yield return pending.Model;
    }

    private IEnumerable<TeamType> GetEffectiveTeamTypes()
    {
        foreach (TeamType model in map.TeamTypes)
            if (!deletedObjects.Contains(model))
                yield return teamTypeReplacements.GetValueOrDefault(model, model);
        foreach (var pending in teamTypeCreates)
            yield return pending.Model;
    }

    private IEnumerable<AITriggerType> GetEffectiveAITriggers()
    {
        foreach (AITriggerType model in map.AITriggerTypes)
            if (!deletedObjects.Contains(model))
                yield return aiTriggerReplacements.GetValueOrDefault(model, model);
        foreach (var pending in aiTriggerCreates)
            yield return pending.Model;
    }

    private IEnumerable<Trigger> GetEffectiveTriggers()
    {
        foreach (Trigger model in map.Triggers)
            if (!deletedObjects.Contains(model))
                yield return triggerReplacements.GetValueOrDefault(model, model);
        foreach (var pending in triggerCreates)
            yield return pending.Model;
    }

    private IEnumerable<Tag> GetEffectiveTags()
    {
        foreach (Tag model in map.Tags)
            if (!deletedObjects.Contains(model))
                yield return tagReplacements.GetValueOrDefault(model, model);
        foreach (var pending in tagCreates)
            yield return pending.Model;
    }

    private object ResolveRequiredReference(ScriptingElementReference reference, ScriptingElementKind expectedKind, string path)
    {
        if (reference == null)
            throw new ScriptingValidationException(path, $"A {expectedKind} reference is required.");
        return ResolveReference(reference, expectedKind, path);
    }

    private object ResolveReference(ScriptingElementReference reference, ScriptingElementKind expectedKind, string path)
    {
        if (reference == null)
            throw new ScriptingValidationException(path, $"A {expectedKind} reference is required.");
        if (reference.Kind != expectedKind)
            throw new ScriptingValidationException(path + ".kind", $"Expected a {expectedKind} reference, but received {reference.Kind}.");

        int selectorCount = (!string.IsNullOrWhiteSpace(reference.Id) ? 1 : 0) +
            (reference.Index.HasValue ? 1 : 0) +
            (!string.IsNullOrWhiteSpace(reference.TemporaryKey) ? 1 : 0);
        if (selectorCount != 1)
            throw new ScriptingValidationException(path, "A reference must specify exactly one of id, index, or temporaryKey.");

        object model;
        if (!string.IsNullOrWhiteSpace(reference.TemporaryKey))
        {
            if (!temporaryElements.TryGetValue(reference.TemporaryKey, out TemporaryElement temporaryElement))
                throw new ScriptingValidationException(path + ".temporaryKey", $"Temporary key '{reference.TemporaryKey}' is not defined by this change set.");
            if (temporaryElement.Kind != expectedKind)
                throw new ScriptingValidationException(path + ".temporaryKey",
                    $"Temporary key '{reference.TemporaryKey}' identifies {temporaryElement.Kind}, not {expectedKind}.");
            model = temporaryElement.Model;
        }
        else
        {
            model = FindExistingElement(expectedKind, reference.Id, reference.Index, true, path);
            if (model == null)
                throw new ScriptingValidationException(path, $"Referenced {expectedKind} does not exist.");
        }

        if (deletedObjects.Contains(model))
            throw new ScriptingValidationException(path, $"The referenced {expectedKind} is deleted by this change set.");
        return model;
    }

    private object FindLocalElement(ScriptingElementKind kind, string id, int? index, string path)
        => FindExistingElement(kind, id, index, false, path);

    private object FindExistingElement(ScriptingElementKind kind, string id, int? index, bool includeGlobal, string path)
    {
        if (kind == ScriptingElementKind.LocalVariable)
        {
            if (!index.HasValue || !string.IsNullOrWhiteSpace(id))
            {
                AddError("invalid_identity", path, "A LocalVariable identity must specify index and no id.");
                return null;
            }

            LocalVariable variable = map.LocalVariables.Find(candidate => candidate.Index == index.Value);
            if (variable == null)
                AddError("not_found", path, $"Local variable {index.Value} does not exist.");
            return variable;
        }

        if (kind == ScriptingElementKind.MapTechno || kind == ScriptingElementKind.CellTag)
        {
            AddError("read_only_kind", path, $"{kind} is only used as a read-only reference source.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(id) || index.HasValue)
        {
            AddError("invalid_identity", path, $"A {kind} identity must specify id and no index.");
            return null;
        }

        string normalizedId = id.Trim();
        object model = kind switch
        {
            ScriptingElementKind.TaskForce => map.TaskForces.Find(candidate => EqualsId(candidate.ININame, normalizedId)),
            ScriptingElementKind.Script => map.Scripts.Find(candidate => EqualsId(candidate.ININame, normalizedId)),
            ScriptingElementKind.TeamType => map.TeamTypes.Find(candidate => EqualsId(candidate.ININame, normalizedId)),
            ScriptingElementKind.AITrigger => map.AITriggerTypes.Find(candidate => EqualsId(candidate.ININame, normalizedId)),
            ScriptingElementKind.Trigger => map.Triggers.Find(candidate => EqualsId(candidate.ID, normalizedId)),
            ScriptingElementKind.Tag => map.Tags.Find(candidate => EqualsId(candidate.ID, normalizedId)),
            _ => null
        };

        if (model == null && int.TryParse(normalizedId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericId))
        {
            model = kind switch
            {
                ScriptingElementKind.TeamType => FindUniqueNumericIdMatch(
                    map.TeamTypes.Cast<object>(), candidate => ((TeamType)candidate).ININame, numericId),
                ScriptingElementKind.Trigger => FindUniqueNumericIdMatch(
                    map.Triggers.Cast<object>(), candidate => ((Trigger)candidate).ID, numericId),
                ScriptingElementKind.Tag => FindUniqueNumericIdMatch(
                    map.Tags.Cast<object>(), candidate => ((Tag)candidate).ID, numericId),
                _ => null
            };
        }

        if (model == null && includeGlobal)
        {
            model = kind switch
            {
                ScriptingElementKind.TaskForce => map.Rules.TaskForces.Find(candidate => EqualsId(candidate.ININame, normalizedId)),
                ScriptingElementKind.Script => map.Rules.Scripts.Find(candidate => EqualsId(candidate.ININame, normalizedId)),
                ScriptingElementKind.TeamType => map.Rules.TeamTypes.Find(candidate => EqualsId(candidate.ININame, normalizedId)),
                _ => null
            };

            if (model == null && kind == ScriptingElementKind.TeamType &&
                int.TryParse(normalizedId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericGlobalId))
            {
                model = FindUniqueNumericIdMatch(
                    map.Rules.TeamTypes.Cast<object>(),
                    candidate => ((TeamType)candidate).ININame,
                    numericGlobalId);
            }
        }

        if (model == null)
            AddError("not_found", path, $"{kind} '{id}' does not exist{(includeGlobal ? string.Empty : " as a map-local element")}.");
        return model;
    }

    private static object FindUniqueNumericIdMatch(IEnumerable<object> candidates, Func<object, string> getId, int numericId)
    {
        List<object> matches = candidates.Where(candidate =>
            int.TryParse(getId(candidate), NumberStyles.Integer, CultureInfo.InvariantCulture, out int candidateId) &&
            candidateId == numericId).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    private HouseType ResolveHouseType(string houseTypeName, string path)
    {
        if (string.IsNullOrWhiteSpace(houseTypeName))
            throw new ScriptingValidationException(path, "A HouseType is required.");
        HouseType houseType = map.GetHouseTypes().Find(candidate =>
            string.Equals(candidate.ININame, houseTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
        return houseType ?? throw new ScriptingValidationException(path, $"HouseType '{houseTypeName}' does not exist.");
    }

    private TechnoType ResolveTechnoType(string technoTypeName, string path)
    {
        TechnoType technoType = map.GetAllTechnoTypes().Find(candidate =>
            string.Equals(candidate.ININame, technoTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
        return technoType ?? throw new ScriptingValidationException(path, $"Techno type '{technoTypeName}' does not exist in the loaded rules.");
    }

    private string EncodeOptionalWaypoint(int? value, string path)
    {
        if (!value.HasValue)
            return null;
        return parameterCodec.Encode(
            TriggerParamType.WaypointZZ,
            value.Value.ToString(CultureInfo.InvariantCulture),
            null,
            ResolveReference,
            path);
    }

    private void PopulatePlannedResult()
    {
        AddCreatedResults(taskForceCreates, ScriptingElementKind.TaskForce, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddCreatedResults(scriptCreates, ScriptingElementKind.Script, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddCreatedResults(teamTypeCreates, ScriptingElementKind.TeamType, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddCreatedResults(aiTriggerCreates, ScriptingElementKind.AITrigger, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddCreatedResults(localVariableCreates, ScriptingElementKind.LocalVariable, null, model => model.Index, model => ScriptingContentHasher.Compute(model));
        AddCreatedResults(triggerCreates, ScriptingElementKind.Trigger, model => model.ID, null,
            model => ScriptingContentHasher.Compute(model, map.EditorConfig));
        AddCreatedResults(tagCreates, ScriptingElementKind.Tag, model => model.ID, null, model => ScriptingContentHasher.Compute(model));

        AddUpdatedResults(taskForceReplacements, ScriptingElementKind.TaskForce, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddUpdatedResults(scriptReplacements, ScriptingElementKind.Script, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddUpdatedResults(teamTypeReplacements, ScriptingElementKind.TeamType, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddUpdatedResults(aiTriggerReplacements, ScriptingElementKind.AITrigger, model => model.ININame, null, model => ScriptingContentHasher.Compute(model));
        AddUpdatedResults(localVariableReplacements, ScriptingElementKind.LocalVariable, null,
            model => model.Index, model => ScriptingContentHasher.Compute(model));
        AddUpdatedResults(triggerReplacements, ScriptingElementKind.Trigger, model => model.ID, null,
            model => ScriptingContentHasher.Compute(model, map.EditorConfig));
        AddUpdatedResults(tagReplacements, ScriptingElementKind.Tag, model => model.ID, null, model => ScriptingContentHasher.Compute(model));

        foreach (PendingDelete pendingDelete in deletes)
            result.Deleted.Add(CreateIdentity(pendingDelete.Kind, pendingDelete.Model));
    }

    private void AddCreatedResults<TOperation, TModel>(
        List<PendingCreate<TOperation, TModel>> creates,
        ScriptingElementKind kind,
        Func<TModel, string> getId,
        Func<TModel, int> getIndex,
        Func<TModel, string> getHash)
        where TOperation : class
        where TModel : class
    {
        foreach (var pending in creates)
        {
            result.Created.Add(new ScriptingCreatedElementInfo
            {
                TemporaryKey = GetTemporaryKey(pending.Operation),
                Kind = kind,
                Id = getId?.Invoke(pending.Model),
                Index = getIndex?.Invoke(pending.Model),
                ContentHash = getHash(pending.Model)
            });
        }
    }

    private void AddUpdatedResults<TModel>(
        Dictionary<TModel, TModel> replacements,
        ScriptingElementKind kind,
        Func<TModel, string> getId,
        Func<TModel, int> getIndex,
        Func<TModel, string> getHash)
        where TModel : class
    {
        foreach (var replacement in replacements)
        {
            result.Updated.Add(new ScriptingChangedElementInfo
            {
                Kind = kind,
                Id = getId?.Invoke(replacement.Value),
                Index = getIndex?.Invoke(replacement.Value),
                ContentHash = getHash(replacement.Value)
            });
        }
    }

    private void ApplyChanges()
    {
        ApplySnapshot snapshot = CaptureApplySnapshot();
        try
        {
            foreach (var replacement in taskForceReplacements)
                CopyTaskForce(replacement.Value, replacement.Key);
            foreach (var replacement in scriptReplacements)
                CopyScript(replacement.Value, replacement.Key);
            foreach (var replacement in teamTypeReplacements)
                CopyTeamType(replacement.Value, replacement.Key);
            foreach (var replacement in aiTriggerReplacements)
                CopyAITrigger(replacement.Value, replacement.Key);
            foreach (var replacement in localVariableReplacements)
                CopyLocalVariable(replacement.Value, replacement.Key);
            foreach (var replacement in triggerReplacements)
                CopyTrigger(replacement.Value, replacement.Key);
            foreach (var replacement in tagReplacements)
                CopyTag(replacement.Value, replacement.Key);

            map.TaskForces.AddRange(taskForceCreates.Select(pending => pending.Model));
            map.Scripts.AddRange(scriptCreates.Select(pending => pending.Model));
            map.TeamTypes.AddRange(teamTypeCreates.Select(pending => pending.Model));
            map.AITriggerTypes.AddRange(aiTriggerCreates.Select(pending => pending.Model));
            map.LocalVariables.AddRange(localVariableCreates.Select(pending => pending.Model));
            map.Triggers.AddRange(triggerCreates.Select(pending => pending.Model));
            map.Tags.AddRange(tagCreates.Select(pending => pending.Model));

            foreach (PendingDelete pendingDelete in deletes)
                RemoveElement(pendingDelete.Kind, pendingDelete.Model);

            map.LocalVariables.Sort((left, right) => left.Index.CompareTo(right.Index));
            RemoveDeletedIniSections(snapshot);
        }
        catch (Exception applyException)
        {
            try
            {
                RestoreApplySnapshot(snapshot);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException("The scripting change and its rollback both failed.", applyException, rollbackException);
            }

            throw;
        }

        try
        {
            map.NotifyScriptingChanged(GetAffectedModelTypes());
        }
        catch (Exception ex)
        {
            // The map change is already complete. A UI refresh failure must not make the caller
            // retry an operation that was successfully committed.
            Logger.Log("Failed to notify a scripting window after an MCP scripting change: " + ex.Message);
        }
    }

    private IReadOnlyCollection<Type> GetAffectedModelTypes()
    {
        var affectedModelTypes = new HashSet<Type>();

        if (taskForceCreates.Count > 0 || taskForceReplacements.Count > 0)
            affectedModelTypes.Add(typeof(TaskForce));
        if (scriptCreates.Count > 0 || scriptReplacements.Count > 0)
            affectedModelTypes.Add(typeof(Script));
        if (teamTypeCreates.Count > 0 || teamTypeReplacements.Count > 0)
            affectedModelTypes.Add(typeof(TeamType));
        if (aiTriggerCreates.Count > 0 || aiTriggerReplacements.Count > 0)
            affectedModelTypes.Add(typeof(AITriggerType));
        if (localVariableCreates.Count > 0 || localVariableReplacements.Count > 0)
            affectedModelTypes.Add(typeof(LocalVariable));
        if (triggerCreates.Count > 0 || triggerReplacements.Count > 0)
            affectedModelTypes.Add(typeof(Trigger));
        if (tagCreates.Count > 0 || tagReplacements.Count > 0)
            affectedModelTypes.Add(typeof(Tag));

        foreach (PendingDelete pendingDelete in deletes)
            affectedModelTypes.Add(pendingDelete.Model.GetType());

        return affectedModelTypes;
    }

    private void RemoveElement(ScriptingElementKind kind, object model)
    {
        switch (kind)
        {
            case ScriptingElementKind.TaskForce:
                map.TaskForces.Remove((TaskForce)model);
                break;
            case ScriptingElementKind.Script:
                map.Scripts.Remove((Script)model);
                break;
            case ScriptingElementKind.TeamType:
                map.TeamTypes.Remove((TeamType)model);
                break;
            case ScriptingElementKind.AITrigger:
                map.AITriggerTypes.Remove((AITriggerType)model);
                break;
            case ScriptingElementKind.LocalVariable:
                map.LocalVariables.Remove((LocalVariable)model);
                break;
            case ScriptingElementKind.Trigger:
                map.Triggers.Remove((Trigger)model);
                break;
            case ScriptingElementKind.Tag:
                map.Tags.Remove((Tag)model);
                break;
        }
    }

    private ApplySnapshot CaptureApplySnapshot()
    {
        return new ApplySnapshot
        {
            TaskForces = new List<TaskForce>(map.TaskForces),
            Scripts = new List<Script>(map.Scripts),
            TeamTypes = new List<TeamType>(map.TeamTypes),
            AITriggers = new List<AITriggerType>(map.AITriggerTypes),
            LocalVariables = new List<LocalVariable>(map.LocalVariables),
            Triggers = new List<Trigger>(map.Triggers),
            Tags = new List<Tag>(map.Tags),
            TaskForceValues = taskForceReplacements.Keys.ToDictionary(model => model, SnapshotTaskForce),
            ScriptValues = scriptReplacements.Keys.ToDictionary(model => model, SnapshotScript),
            TeamTypeValues = teamTypeReplacements.Keys.ToDictionary(model => model, SnapshotTeamType),
            AITriggerValues = aiTriggerReplacements.Keys.ToDictionary(model => model, SnapshotAITrigger),
            LocalVariableValues = localVariableReplacements.Keys.ToDictionary(model => model, SnapshotLocalVariable),
            TriggerValues = triggerReplacements.Keys.ToDictionary(model => model, SnapshotTrigger),
            TagValues = tagReplacements.Keys.ToDictionary(model => model, SnapshotTag),
            IniSections = CaptureDeletedIniSections()
        };
    }

    private List<PendingIniSectionRemoval> CaptureDeletedIniSections()
    {
        var sections = new List<PendingIniSectionRemoval>();
        if (map.LoadedINI == null)
            return sections;

        foreach (PendingDelete pendingDelete in deletes)
        {
            if (pendingDelete.Kind is not (ScriptingElementKind.TaskForce or ScriptingElementKind.Script or ScriptingElementKind.TeamType))
                continue;
            string id = GetElementId(pendingDelete.Kind, pendingDelete.Model);
            sections.Add(new PendingIniSectionRemoval(id, map.LoadedINI.GetSection(id)));
        }
        return sections;
    }

    private void RemoveDeletedIniSections(ApplySnapshot snapshot)
    {
        if (map.LoadedINI == null)
            return;
        foreach (PendingIniSectionRemoval section in snapshot.IniSections)
            map.LoadedINI.RemoveSection(section.Id);
    }

    private void RestoreApplySnapshot(ApplySnapshot snapshot)
    {
        foreach (var value in snapshot.TaskForceValues)
            CopyTaskForce(value.Value, value.Key);
        foreach (var value in snapshot.ScriptValues)
            CopyScript(value.Value, value.Key);
        foreach (var value in snapshot.TeamTypeValues)
            CopyTeamType(value.Value, value.Key);
        foreach (var value in snapshot.AITriggerValues)
            CopyAITrigger(value.Value, value.Key);
        foreach (var value in snapshot.LocalVariableValues)
            CopyLocalVariable(value.Value, value.Key);
        foreach (var value in snapshot.TriggerValues)
            CopyTrigger(value.Value, value.Key);
        foreach (var value in snapshot.TagValues)
            CopyTag(value.Value, value.Key);

        RestoreList(map.TaskForces, snapshot.TaskForces);
        RestoreList(map.Scripts, snapshot.Scripts);
        RestoreList(map.TeamTypes, snapshot.TeamTypes);
        RestoreList(map.AITriggerTypes, snapshot.AITriggers);
        RestoreList(map.LocalVariables, snapshot.LocalVariables);
        RestoreList(map.Triggers, snapshot.Triggers);
        RestoreList(map.Tags, snapshot.Tags);

        if (map.LoadedINI != null)
        {
            foreach (PendingIniSectionRemoval section in snapshot.IniSections)
            {
                if (section.Section != null && !map.LoadedINI.SectionExists(section.Id))
                    map.LoadedINI.AddSection(section.Section);
            }
        }
    }

    private static void RestoreList<T>(List<T> destination, List<T> snapshot)
    {
        destination.Clear();
        destination.AddRange(snapshot);
    }

    private void RefreshChangedHashes()
    {
        foreach (ScriptingChangedElementInfo updated in result.Updated)
        {
            object model = FindElementWithoutDiagnostic(updated.Kind, updated.Id, updated.Index, false);
            if (model != null)
                updated.ContentHash = ComputeHash(updated.Kind, model);
        }
        foreach (ScriptingCreatedElementInfo created in result.Created)
        {
            object model = FindElementWithoutDiagnostic(created.Kind, created.Id, created.Index, false);
            if (model != null)
                created.ContentHash = ComputeHash(created.Kind, model);
        }
    }

    private static void CopyTaskForce(TaskForce source, TaskForce destination)
    {
        destination.Name = source.Name;
        destination.Group = source.Group;
        Array.Clear(destination.TechnoTypes);
        for (int i = 0; i < source.TechnoTypes.Length; i++)
            destination.TechnoTypes[i] = source.TechnoTypes[i]?.Clone();
    }

    private static void CopyScript(Script source, Script destination)
    {
        destination.Name = source.Name;
        destination.EditorColor = source.EditorColor;
        destination.Actions.Clear();
        destination.Actions.AddRange(source.Actions.Select(action => action.Clone()));
    }

    private static void CopyTeamType(TeamType source, TeamType destination)
    {
        destination.Name = source.Name;
        destination.Group = source.Group;
        destination.HouseType = source.HouseType;
        destination.Script = source.Script;
        destination.TaskForce = source.TaskForce;
        destination.Tag = source.Tag;
        destination.Max = source.Max;
        destination.Priority = source.Priority;
        destination.Waypoint = source.Waypoint;
        destination.TransportWaypoint = source.TransportWaypoint;
        destination.MindControlDecision = source.MindControlDecision;
        destination.TechLevel = source.TechLevel;
        destination.VeteranLevel = source.VeteranLevel;
        destination.EditorColor = source.EditorColor;
        foreach (string flag in destination.EnabledTeamTypeFlags.ToList())
            destination.DisableFlag(flag);
        foreach (string flag in source.EnabledTeamTypeFlags)
            destination.EnableFlag(flag);
    }

    private static void CopyAITrigger(AITriggerType source, AITriggerType destination)
    {
        destination.Name = source.Name;
        destination.PrimaryTeam = source.PrimaryTeam;
        destination.OwnerName = source.OwnerName;
        destination.TechLevel = source.TechLevel;
        destination.ConditionType = source.ConditionType;
        destination.ConditionObject = source.ConditionObject;
        destination.Comparator = source.Comparator;
        destination.InitialWeight = source.InitialWeight;
        destination.MinimumWeight = source.MinimumWeight;
        destination.MaximumWeight = source.MaximumWeight;
        destination.EnabledInMultiplayer = source.EnabledInMultiplayer;
        destination.Unused = source.Unused;
        destination.Side = source.Side;
        destination.IsBaseDefense = source.IsBaseDefense;
        destination.SecondaryTeam = source.SecondaryTeam;
        destination.Easy = source.Easy;
        destination.Medium = source.Medium;
        destination.Hard = source.Hard;
        destination.Enabled = source.Enabled;
    }

    private static void CopyLocalVariable(LocalVariable source, LocalVariable destination)
    {
        destination.Name = source.Name;
        destination.InitialState = source.InitialState;
    }

    private static void CopyTrigger(Trigger source, Trigger destination)
    {
        destination.HouseType = source.HouseType;
        destination.LinkedTrigger = source.LinkedTrigger;
        destination.Name = source.Name;
        destination.Disabled = source.Disabled;
        destination.Easy = source.Easy;
        destination.Normal = source.Normal;
        destination.Hard = source.Hard;
        destination.EditorColor = source.EditorColor;
        destination.Conditions.Clear();
        destination.Conditions.AddRange(source.Conditions.Select(condition => condition.DoClone()));
        destination.Actions.Clear();
        destination.Actions.AddRange(source.Actions.Select(action => action.DoClone()));
    }

    private static void CopyTag(Tag source, Tag destination)
    {
        destination.Name = source.Name;
        destination.Repeating = source.Repeating;
        destination.Trigger = source.Trigger;
    }

    private static TaskForce SnapshotTaskForce(TaskForce source)
    {
        var snapshot = new TaskForce(source.ININame);
        CopyTaskForce(source, snapshot);
        return snapshot;
    }

    private static Script SnapshotScript(Script source)
    {
        var snapshot = new Script(source.ININame);
        CopyScript(source, snapshot);
        return snapshot;
    }

    private static TeamType SnapshotTeamType(TeamType source)
    {
        var snapshot = new TeamType(source.ININame);
        CopyTeamType(source, snapshot);
        return snapshot;
    }

    private static AITriggerType SnapshotAITrigger(AITriggerType source)
    {
        var snapshot = new AITriggerType(source.ININame);
        CopyAITrigger(source, snapshot);
        return snapshot;
    }

    private static LocalVariable SnapshotLocalVariable(LocalVariable source)
    {
        var snapshot = new LocalVariable(source.Index);
        CopyLocalVariable(source, snapshot);
        return snapshot;
    }

    private static Trigger SnapshotTrigger(Trigger source)
    {
        var snapshot = new Trigger(source.ID);
        CopyTrigger(source, snapshot);
        return snapshot;
    }

    private static Tag SnapshotTag(Tag source)
    {
        var snapshot = new Tag { ID = source.ID };
        CopyTag(source, snapshot);
        return snapshot;
    }

    private ScriptingElementIdentityInfo CreateIdentity(ScriptingElementKind kind, object model)
    {
        return new ScriptingElementIdentityInfo
        {
            Kind = kind,
            Id = GetElementId(kind, model),
            Index = GetElementIndex(kind, model),
            Name = GetElementName(kind, model),
            ContentHash = ComputeHash(kind, model)
        };
    }

    private object FindElementWithoutDiagnostic(ScriptingElementKind kind, string id, int? index, bool includeGlobal)
    {
        if (kind == ScriptingElementKind.LocalVariable)
            return index.HasValue ? map.LocalVariables.Find(variable => variable.Index == index.Value) : null;
        if (string.IsNullOrWhiteSpace(id))
            return null;

        object model = kind switch
        {
            ScriptingElementKind.TaskForce => map.TaskForces.Find(candidate => EqualsId(candidate.ININame, id)),
            ScriptingElementKind.Script => map.Scripts.Find(candidate => EqualsId(candidate.ININame, id)),
            ScriptingElementKind.TeamType => map.TeamTypes.Find(candidate => EqualsId(candidate.ININame, id)),
            ScriptingElementKind.AITrigger => map.AITriggerTypes.Find(candidate => EqualsId(candidate.ININame, id)),
            ScriptingElementKind.Trigger => map.Triggers.Find(candidate => EqualsId(candidate.ID, id)),
            ScriptingElementKind.Tag => map.Tags.Find(candidate => EqualsId(candidate.ID, id)),
            _ => null
        };
        if (model == null && includeGlobal)
        {
            model = kind switch
            {
                ScriptingElementKind.TaskForce => map.Rules.TaskForces.Find(candidate => EqualsId(candidate.ININame, id)),
                ScriptingElementKind.Script => map.Rules.Scripts.Find(candidate => EqualsId(candidate.ININame, id)),
                ScriptingElementKind.TeamType => map.Rules.TeamTypes.Find(candidate => EqualsId(candidate.ININame, id)),
                _ => null
            };
        }
        return model;
    }

    private static string GetElementId(ScriptingElementKind kind, object model)
    {
        return kind switch
        {
            ScriptingElementKind.TaskForce => ((TaskForce)model).ININame,
            ScriptingElementKind.Script => ((Script)model).ININame,
            ScriptingElementKind.TeamType => ((TeamType)model).ININame,
            ScriptingElementKind.AITrigger => ((AITriggerType)model).ININame,
            ScriptingElementKind.Trigger => ((Trigger)model).ID,
            ScriptingElementKind.Tag => ((Tag)model).ID,
            _ => null
        };
    }

    private static int? GetElementIndex(ScriptingElementKind kind, object model)
        => kind == ScriptingElementKind.LocalVariable ? ((LocalVariable)model).Index : null;

    private static string GetElementName(ScriptingElementKind kind, object model)
    {
        return kind switch
        {
            ScriptingElementKind.TaskForce => ((TaskForce)model).Name,
            ScriptingElementKind.Script => ((Script)model).Name,
            ScriptingElementKind.TeamType => ((TeamType)model).Name,
            ScriptingElementKind.AITrigger => ((AITriggerType)model).Name,
            ScriptingElementKind.LocalVariable => ((LocalVariable)model).Name,
            ScriptingElementKind.Trigger => ((Trigger)model).Name,
            ScriptingElementKind.Tag => ((Tag)model).Name,
            _ => null
        };
    }

    private string ComputeHash(ScriptingElementKind kind, object model)
    {
        return kind switch
        {
            ScriptingElementKind.TaskForce => ScriptingContentHasher.Compute((TaskForce)model),
            ScriptingElementKind.Script => ScriptingContentHasher.Compute((Script)model),
            ScriptingElementKind.TeamType => ScriptingContentHasher.Compute((TeamType)model),
            ScriptingElementKind.AITrigger => ScriptingContentHasher.Compute((AITriggerType)model),
            ScriptingElementKind.LocalVariable => ScriptingContentHasher.Compute((LocalVariable)model),
            ScriptingElementKind.Trigger => ScriptingContentHasher.Compute((Trigger)model, map.EditorConfig),
            ScriptingElementKind.Tag => ScriptingContentHasher.Compute((Tag)model),
            _ => throw new MapFacadeValidationException($"{kind} does not have an editable content hash.")
        };
    }

    private void RegisterTemporaryKey(
        string temporaryKey,
        ScriptingElementKind kind,
        object model,
        string id,
        int? index,
        string path)
    {
        if (temporaryKey == null)
            return;
        if (string.IsNullOrWhiteSpace(temporaryKey))
        {
            AddError("invalid_temporary_key", path + ".temporaryKey", "A supplied temporary key cannot be empty.");
            return;
        }
        if (temporaryKey != temporaryKey.Trim())
        {
            AddError("invalid_temporary_key", path + ".temporaryKey", "A temporary key cannot begin or end with whitespace.");
            return;
        }
        if (temporaryKey.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
        {
            AddError("invalid_temporary_key", path + ".temporaryKey", "A temporary key must be single-line text and cannot contain NUL characters.");
            return;
        }
        if (!temporaryElements.TryAdd(temporaryKey, new TemporaryElement(kind, model, id, index)))
            AddError("duplicate_temporary_key", path + ".temporaryKey", $"Temporary key '{temporaryKey}' is used more than once.");
    }

    private bool RegisterTouched(ScriptingElementKind kind, string id, int? index, string path)
    {
        string key = kind == ScriptingElementKind.LocalVariable
            ? $"{kind}:{index}"
            : $"{kind}:{id}";
        if (touchedElements.Add(key))
            return true;
        AddError("duplicate_operation", path, $"{kind} '{id ?? index?.ToString()}' is updated or deleted more than once in this change set.");
        return false;
    }

    private void ValidateExpectedHash(string expectedHash, string actualHash, string path)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            AddError("missing_content_hash", path, "expectedContentHash is required for updates, deletions, and preconditions.");
            return;
        }
        if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
        {
            AddError(
                "content_hash_mismatch",
                path,
                $"The element changed after it was read. Expected '{expectedHash}', but its current ContentHash is '{actualHash}'. " +
                "Read it again and rebuild the change set.");
        }
    }

    private static string ResolveEditorColor(string requestedColor, IEnumerable<string> supportedColors, string path)
    {
        if (requestedColor == null)
            return null;
        if (string.IsNullOrWhiteSpace(requestedColor))
            throw new ScriptingValidationException(path, "An editor color cannot be empty; use null for the default color.");
        string color = supportedColors.FirstOrDefault(candidate =>
            string.Equals(candidate, requestedColor.Trim(), StringComparison.OrdinalIgnoreCase));
        return color ?? throw new ScriptingValidationException(path, $"Editor color '{requestedColor}' is not supported.");
    }

    private static string RequireName(string value, string path)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ScriptingValidationException(path, "A non-empty name is required.");
        string name = value.Trim();
        if (name.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
            throw new ScriptingValidationException(path, "Names must be single-line text and cannot contain NUL characters.");
        return name;
    }

    private static string RequireCommaSafeName(string value, string path)
    {
        string name = RequireName(value, path);
        if (name.Contains(','))
            throw new ScriptingValidationException(path, "This name cannot contain commas because it is stored in a comma-separated map entry.");
        return name;
    }

    private static void RequireInput(object input, string path)
    {
        if (input == null)
            throw new ScriptingValidationException(path, "A value is required.");
    }

    private void TryBuild(string fallbackPath, Action action)
    {
        try
        {
            action();
        }
        catch (ScriptingValidationException ex)
        {
            AddError("validation_error", ex.Path ?? fallbackPath, ex.Message);
        }
    }

    private void AddError(string code, string path, string message)
    {
        result.Diagnostics.Add(new ScriptingDiagnosticInfo
        {
            Severity = "error",
            Code = code,
            Path = path,
            Message = message
        });
    }

    private bool HasErrors() => result.Diagnostics.Exists(diagnostic =>
        string.Equals(diagnostic.Severity, "error", StringComparison.OrdinalIgnoreCase));

    private bool HasPlannedChanges()
    {
        return taskForceCreates.Count + scriptCreates.Count + teamTypeCreates.Count + aiTriggerCreates.Count +
            localVariableCreates.Count + triggerCreates.Count + tagCreates.Count +
            taskForceReplacements.Count + scriptReplacements.Count + teamTypeReplacements.Count + aiTriggerReplacements.Count +
            localVariableReplacements.Count + triggerReplacements.Count + tagReplacements.Count + deletes.Count > 0;
    }

    private static bool EqualsId(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static void ForEach<T>(List<T> items, string path, Action<T, string> action)
    {
        if (items == null)
            return;
        for (int i = 0; i < items.Count; i++)
            action(items[i], path + $"[{i}]");
    }

    private static string GetExpectedHash(object operation)
    {
        return operation switch
        {
            TaskForceUpdate update => update.ExpectedContentHash,
            ScriptUpdate update => update.ExpectedContentHash,
            TeamTypeUpdate update => update.ExpectedContentHash,
            AITriggerUpdate update => update.ExpectedContentHash,
            TriggerUpdate update => update.ExpectedContentHash,
            TagUpdate update => update.ExpectedContentHash,
            _ => null
        };
    }

    private static string GetTemporaryKey(object operation)
    {
        return operation switch
        {
            TaskForceCreate create => create.TemporaryKey,
            ScriptCreate create => create.TemporaryKey,
            TeamTypeCreate create => create.TemporaryKey,
            AITriggerCreate create => create.TemporaryKey,
            LocalVariableCreate create => create.TemporaryKey,
            TriggerCreate create => create.TemporaryKey,
            TagCreate create => create.TemporaryKey,
            _ => null
        };
    }

    private sealed record TemporaryElement(ScriptingElementKind Kind, object Model, string Id, int? Index);
    private sealed record PendingCreate<TOperation, TModel>(TOperation Operation, TModel Model, string Path)
        where TOperation : class where TModel : class;
    private sealed record PendingUpdate<TOperation, TModel>(TOperation Operation, TModel Existing, string Path)
        where TOperation : class where TModel : class;
    private sealed record PendingDelete(ScriptingElementKind Kind, object Model, string Path);
    private sealed record PendingIniSectionRemoval(string Id, IniSection Section);

    private sealed class ApplySnapshot
    {
        public List<TaskForce> TaskForces { get; init; }
        public List<Script> Scripts { get; init; }
        public List<TeamType> TeamTypes { get; init; }
        public List<AITriggerType> AITriggers { get; init; }
        public List<LocalVariable> LocalVariables { get; init; }
        public List<Trigger> Triggers { get; init; }
        public List<Tag> Tags { get; init; }
        public Dictionary<TaskForce, TaskForce> TaskForceValues { get; init; }
        public Dictionary<Script, Script> ScriptValues { get; init; }
        public Dictionary<TeamType, TeamType> TeamTypeValues { get; init; }
        public Dictionary<AITriggerType, AITriggerType> AITriggerValues { get; init; }
        public Dictionary<LocalVariable, LocalVariable> LocalVariableValues { get; init; }
        public Dictionary<Trigger, Trigger> TriggerValues { get; init; }
        public Dictionary<Tag, Tag> TagValues { get; init; }
        public List<PendingIniSectionRemoval> IniSections { get; init; }
    }
}
