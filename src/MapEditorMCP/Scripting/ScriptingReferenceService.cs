using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MapEditorMCP.Scripting;

/// <summary>
/// Builds a fresh, read-only reference graph from the current map scripting state.
/// </summary>
internal sealed class ScriptingReferenceService
{
    public ScriptingReferenceService(Map map)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
    }

    private readonly Map map;

    public ScriptingReferencesResult GetReferences(ScriptingElementReference elementReference)
    {
        object element = ResolveElement(elementReference);
        List<ReferenceEdge> edges = BuildReferenceGraph();

        return new ScriptingReferencesResult
        {
            Element = CreateIdentity(element),
            IncomingReferences = edges
                .Where(edge => ReferenceEquals(edge.Target, element))
                .OrderBy(edge => edge.Path, StringComparer.Ordinal)
                .ThenBy(edge => GetIdentitySortKey(edge.Source), StringComparer.Ordinal)
                .Select(CreateReferenceInfo)
                .ToList(),
            OutgoingReferences = edges
                .Where(edge => ReferenceEquals(edge.Source, element))
                .OrderBy(edge => edge.Path, StringComparer.Ordinal)
                .ThenBy(edge => GetIdentitySortKey(edge.Target), StringComparer.Ordinal)
                .Select(CreateReferenceInfo)
                .ToList()
        };
    }

    internal List<string> GetIncomingReferencePaths(ScriptingElementKind kind, string id, int? index)
    {
        object element = ResolveElement(new ScriptingElementReference
        {
            Kind = kind,
            Id = id,
            Index = index
        });

        return BuildReferenceGraph()
            .Where(edge => ReferenceEquals(edge.Target, element))
            .Select(edge => edge.Path)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    internal ScriptingElementIdentityInfo GetElementIdentity(ScriptingElementReference elementReference)
        => CreateIdentity(ResolveElement(elementReference));

    private List<ReferenceEdge> BuildReferenceGraph()
    {
        var edges = new List<ReferenceEdge>();

        foreach (TeamType teamType in EnumerateLocalAndGlobalTeamTypes())
        {
            AddEdge(edges, teamType, teamType.TaskForce, $"teamTypes[{teamType.ININame}].taskForce");
            AddEdge(edges, teamType, teamType.Script, $"teamTypes[{teamType.ININame}].script");
            AddEdge(edges, teamType, teamType.Tag, $"teamTypes[{teamType.ININame}].tag");
        }

        foreach (AITriggerType aiTrigger in map.AITriggerTypes)
        {
            AddEdge(edges, aiTrigger, aiTrigger.PrimaryTeam, $"aiTriggers[{aiTrigger.ININame}].primaryTeam");
            AddEdge(edges, aiTrigger, aiTrigger.SecondaryTeam, $"aiTriggers[{aiTrigger.ININame}].secondaryTeam");
        }

        foreach (Tag tag in map.Tags)
            AddEdge(edges, tag, tag.Trigger, $"tags[{tag.ID}].trigger");

        foreach (Trigger trigger in map.Triggers)
        {
            AddEdge(edges, trigger, trigger.LinkedTrigger, $"triggers[{trigger.ID}].linkedTrigger");
            AddTriggerParameterEdges(edges, trigger);
        }

        foreach (Script script in EnumerateLocalAndGlobalScripts())
            AddScriptParameterEdges(edges, script);

        foreach (TechnoBase techno in EnumerateMapTechnos())
        {
            if (techno.AttachedTag != null)
                AddEdge(edges, techno, techno.AttachedTag, $"mapTechnos[{techno.ObjectId.ToString(CultureInfo.InvariantCulture)}].attachedTag");
        }

        foreach (CellTag cellTag in map.CellTags)
        {
            if (cellTag.Tag != null)
                AddEdge(edges, cellTag, cellTag.Tag, $"cellTags[{FormatCellTagId(cellTag)}].tag");
        }

        return edges;
    }

    private void AddScriptParameterEdges(List<ReferenceEdge> edges, Script script)
    {
        for (int actionIndex = 0; actionIndex < script.Actions.Count; actionIndex++)
        {
            ScriptActionEntry action = script.Actions[actionIndex];
            if (action == null || !map.EditorConfig.ScriptActions.TryGetValue(action.Action, out var actionDefinition))
                continue;

            object target = ResolveConfiguredParameterTarget(
                actionDefinition.ParamType,
                action.Argument.ToString(CultureInfo.InvariantCulture),
                isIntegerScriptArgument: true);

            AddEdge(
                edges,
                script,
                target,
                $"scripts[{script.ININame}].actions[{actionIndex.ToString(CultureInfo.InvariantCulture)}].argument");
        }
    }

    private void AddTriggerParameterEdges(List<ReferenceEdge> edges, Trigger trigger)
    {
        for (int eventIndex = 0; eventIndex < trigger.Conditions.Count; eventIndex++)
        {
            TriggerCondition condition = trigger.Conditions[eventIndex];
            if (condition == null ||
                !map.EditorConfig.TriggerEventTypes.TryGetValue(condition.ConditionIndex, out var eventDefinition))
            {
                continue;
            }

            int parameterCount = Math.Min(condition.Parameters.Length, eventDefinition.Parameters.Length);
            for (int parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                if (eventDefinition.Parameters[parameterIndex] == null)
                    continue;

                object target = ResolveConfiguredParameterTarget(
                    eventDefinition.Parameters[parameterIndex].TriggerParamType,
                    condition.Parameters[parameterIndex],
                    isIntegerScriptArgument: false);
                string parameterPath = $"triggers[{trigger.ID}].events[{eventIndex.ToString(CultureInfo.InvariantCulture)}]." +
                    $"parameters[{parameterIndex.ToString(CultureInfo.InvariantCulture)}]";

                AddEdge(edges, trigger, target, parameterPath);
            }
        }

        for (int actionIndex = 0; actionIndex < trigger.Actions.Count; actionIndex++)
        {
            TriggerAction action = trigger.Actions[actionIndex];
            if (action == null ||
                !map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var actionDefinition))
            {
                continue;
            }

            int parameterCount = Math.Min(action.Parameters.Length, actionDefinition.Parameters.Length);
            for (int parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                if (actionDefinition.Parameters[parameterIndex] == null)
                    continue;

                object target = ResolveConfiguredParameterTarget(
                    actionDefinition.Parameters[parameterIndex].TriggerParamType,
                    action.Parameters[parameterIndex],
                    isIntegerScriptArgument: false);
                string parameterPath = $"triggers[{trigger.ID}].actions[{actionIndex.ToString(CultureInfo.InvariantCulture)}]." +
                    $"parameters[{parameterIndex.ToString(CultureInfo.InvariantCulture)}]";

                AddEdge(edges, trigger, target, parameterPath);
            }
        }
    }

    private object ResolveConfiguredParameterTarget(
        TriggerParamType parameterType,
        string rawValue,
        bool isIntegerScriptArgument)
    {
        switch (parameterType)
        {
            case TriggerParamType.TeamType:
                return FindTeamType(rawValue, isIntegerScriptArgument);

            case TriggerParamType.LocalVariable:
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int variableIndex))
                    return map.LocalVariables.Find(variable => variable.Index == variableIndex);

                return null;

            case TriggerParamType.Trigger:
                return map.Triggers.Find(trigger => IdMatches(trigger.ID, rawValue, isIntegerScriptArgument));

            case TriggerParamType.Tag:
                return map.Tags.Find(tag => IdMatches(tag.ID, rawValue, isIntegerScriptArgument));

            default:
                return null;
        }
    }

    private object ResolveElement(ScriptingElementReference elementReference)
    {
        if (elementReference == null)
            throw new MapFacadeValidationException("A scripting element reference is required.");

        if (!string.IsNullOrWhiteSpace(elementReference.TemporaryKey))
            throw new MapFacadeValidationException("TemporaryKey references are only valid inside a scripting change set and cannot be inspected.");

        bool hasId = !string.IsNullOrWhiteSpace(elementReference.Id);
        bool hasIndex = elementReference.Index.HasValue;

        if (hasId == hasIndex)
        {
            throw new MapFacadeValidationException(
                "An existing scripting element reference must specify exactly one permanent Id or local-variable Index.");
        }

        object element = elementReference.Kind switch
        {
            ScriptingElementKind.TaskForce when hasId => FindTaskForce(elementReference.Id),
            ScriptingElementKind.Script when hasId => FindScript(elementReference.Id),
            ScriptingElementKind.TeamType when hasId => FindTeamType(elementReference.Id, false),
            ScriptingElementKind.AITrigger when hasId => map.AITriggerTypes.Find(item => IdMatches(item.ININame, elementReference.Id, false)),
            ScriptingElementKind.LocalVariable when hasIndex => map.LocalVariables.Find(item => item.Index == elementReference.Index.Value),
            ScriptingElementKind.Trigger when hasId => map.Triggers.Find(item => IdMatches(item.ID, elementReference.Id, false)),
            ScriptingElementKind.Tag when hasId => map.Tags.Find(item => IdMatches(item.ID, elementReference.Id, false)),
            ScriptingElementKind.MapTechno when hasId => FindMapTechno(elementReference.Id),
            ScriptingElementKind.CellTag when hasId => FindCellTag(elementReference.Id),
            _ => null
        };

        if (element == null)
        {
            string identifier = hasId
                ? $"ID '{elementReference.Id}'"
                : $"index {elementReference.Index.Value.ToString(CultureInfo.InvariantCulture)}";

            throw new MapFacadeValidationException(
                $"{elementReference.Kind} with {identifier} does not exist in the current map context.");
        }

        return element;
    }

    private TaskForce FindTaskForce(string id)
    {
        return map.TaskForces.Find(item => IdMatches(item.ININame, id, false)) ??
            map.Rules.TaskForces.Find(item => IdMatches(item.ININame, id, false));
    }

    private Script FindScript(string id)
    {
        return map.Scripts.Find(item => IdMatches(item.ININame, id, false)) ??
            map.Rules.Scripts.Find(item => IdMatches(item.ININame, id, false));
    }

    private TeamType FindTeamType(string id, bool allowNumericEquivalent)
    {
        return map.TeamTypes.Find(item => IdMatches(item.ININame, id, allowNumericEquivalent)) ??
            map.Rules.TeamTypes.Find(item => IdMatches(item.ININame, id, allowNumericEquivalent));
    }

    private TechnoBase FindMapTechno(string id)
    {
        if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int objectId))
            return null;

        return EnumerateMapTechnos().FirstOrDefault(techno => techno.ObjectId == objectId);
    }

    private CellTag FindCellTag(string id)
    {
        return map.CellTags.Find(cellTag => FormatCellTagId(cellTag) == id);
    }

    private IEnumerable<Script> EnumerateLocalAndGlobalScripts()
    {
        return map.Scripts.Concat(map.Rules.Scripts).Distinct();
    }

    private IEnumerable<TeamType> EnumerateLocalAndGlobalTeamTypes()
    {
        return map.TeamTypes.Concat(map.Rules.TeamTypes).Distinct();
    }

    private IEnumerable<TechnoBase> EnumerateMapTechnos()
    {
        return map.Structures.Cast<TechnoBase>()
            .Concat(map.Units)
            .Concat(map.Infantry)
            .Concat(map.Aircraft)
            .Where(techno => techno.ObjectId > 0);
    }

    private static bool IdMatches(string candidateId, string rawValue, bool allowNumericEquivalent)
    {
        if (string.Equals(candidateId, rawValue, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!allowNumericEquivalent ||
            !int.TryParse(candidateId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int candidateNumber) ||
            !int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawNumber))
        {
            return false;
        }

        return candidateNumber == rawNumber;
    }

    private static void AddEdge(List<ReferenceEdge> edges, object source, object target, string path)
    {
        if (source != null && target != null)
            edges.Add(new ReferenceEdge(source, target, path));
    }

    private ScriptingReferenceInfo CreateReferenceInfo(ReferenceEdge edge)
    {
        return new ScriptingReferenceInfo
        {
            Source = CreateIdentity(edge.Source),
            Target = CreateIdentity(edge.Target),
            Path = edge.Path
        };
    }

    private ScriptingElementIdentityInfo CreateIdentity(object element)
    {
        return element switch
        {
            TaskForce taskForce => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.TaskForce,
                Id = taskForce.ININame,
                Name = taskForce.Name,
                ContentHash = ScriptingContentHasher.Compute(taskForce)
            },
            Script script => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.Script,
                Id = script.ININame,
                Name = script.Name,
                ContentHash = ScriptingContentHasher.Compute(script)
            },
            TeamType teamType => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.TeamType,
                Id = teamType.ININame,
                Name = teamType.Name,
                ContentHash = ScriptingContentHasher.Compute(teamType)
            },
            AITriggerType aiTrigger => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.AITrigger,
                Id = aiTrigger.ININame,
                Name = aiTrigger.Name,
                ContentHash = ScriptingContentHasher.Compute(aiTrigger)
            },
            LocalVariable variable => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.LocalVariable,
                Index = variable.Index,
                Name = variable.Name,
                ContentHash = ScriptingContentHasher.Compute(variable)
            },
            Trigger trigger => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.Trigger,
                Id = trigger.ID,
                Name = trigger.Name,
                ContentHash = ScriptingContentHasher.Compute(trigger, map.EditorConfig)
            },
            Tag tag => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.Tag,
                Id = tag.ID,
                Name = tag.Name,
                ContentHash = ScriptingContentHasher.Compute(tag)
            },
            TechnoBase techno => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.MapTechno,
                Id = techno.ObjectId.ToString(CultureInfo.InvariantCulture),
                Name = techno.GetObjectType()?.ININame,
                ContentHash = ComputeMapTechnoReferenceHash(techno)
            },
            CellTag cellTag => new ScriptingElementIdentityInfo
            {
                Kind = ScriptingElementKind.CellTag,
                Id = FormatCellTagId(cellTag),
                Name = cellTag.Tag?.Name,
                ContentHash = ScriptingContentHasher.Compute(cellTag)
            },
            _ => throw new InvalidOperationException($"Unsupported scripting reference graph element type {element?.GetType().FullName ?? "<null>"}.")
        };
    }

    private ScriptingElementIdentityInfo CreateIdentityForSort(object element)
    {
        return CreateIdentity(element);
    }

    private string GetIdentitySortKey(object element)
    {
        ScriptingElementIdentityInfo identity = CreateIdentityForSort(element);
        return $"{(int)identity.Kind:D2}|{identity.Id}|{identity.Index?.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string FormatCellTagId(CellTag cellTag)
    {
        return $"{cellTag.Position.X.ToString(CultureInfo.InvariantCulture)},{cellTag.Position.Y.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string ComputeMapTechnoReferenceHash(TechnoBase techno)
    {
        using var stream = new MemoryStream();
        WriteHashString(stream, "WAE-MapTechno-Reference-v1");
        WriteHashString(stream, techno.ObjectId.ToString(CultureInfo.InvariantCulture));
        WriteHashString(stream, techno.GetObjectType()?.ININame);
        WriteHashString(stream, techno.Owner?.ININame);
        WriteHashString(stream, techno.Position.X.ToString(CultureInfo.InvariantCulture));
        WriteHashString(stream, techno.Position.Y.ToString(CultureInfo.InvariantCulture));
        WriteHashString(stream, techno.AttachedTag?.ID);
        return "sha256:" + Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void WriteHashString(Stream stream, string value)
    {
        if (value == null)
        {
            stream.WriteByte(0);
            return;
        }

        stream.WriteByte(1);
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] length = BitConverter.GetBytes(bytes.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(length);

        stream.Write(length, 0, length.Length);
        stream.Write(bytes, 0, bytes.Length);
    }

    private sealed class ReferenceEdge
    {
        public ReferenceEdge(object source, object target, string path)
        {
            Source = source;
            Target = target;
            Path = path;
        }

        public object Source { get; }
        public object Target { get; }
        public string Path { get; }
    }
}
