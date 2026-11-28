using System.Collections;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorMCP.Scripting;
using Rampastring.Tools;

namespace MapEditorMCP.MCPMutations;

internal enum ScriptingElementChangeType
{
    Created,
    Updated,
    Deleted
}

/// <summary>
/// One element changed by an MCP scripting change set, together with the state
/// needed to undo, redo, and review the change.
/// </summary>
internal sealed class ScriptingElementChange
{
    public ScriptingElementChange(ScriptingElementKind kind, ScriptingElementChangeType changeType, object model,
        object beforeSnapshot, object afterSnapshot, int originalListIndex, IniSection removedIniSection)
    {
        Kind = kind;
        ChangeType = changeType;
        Model = model ?? throw new ArgumentNullException(nameof(model));
        BeforeSnapshot = beforeSnapshot;
        AfterSnapshot = afterSnapshot;
        OriginalListIndex = originalListIndex;
        RemovedIniSection = removedIniSection;
    }

    public ScriptingElementKind Kind { get; }
    public ScriptingElementChangeType ChangeType { get; }

    /// <summary>
    /// The live model instance that belongs to (or belonged to) the map.
    /// </summary>
    public object Model { get; }

    /// <summary>
    /// Detached copy of the element before the change. Null for created elements.
    /// </summary>
    public object BeforeSnapshot { get; }

    /// <summary>
    /// Detached copy of the element after the change. Null for deleted elements.
    /// </summary>
    public object AfterSnapshot { get; }

    /// <summary>
    /// Index of a deleted element in its map list before it was deleted.
    /// </summary>
    public int OriginalListIndex { get; }

    /// <summary>
    /// INI section that was removed together with the element and must be restored with it.
    /// </summary>
    public IniSection RemovedIniSection { get; set; }

    public string BeforeHash { get; set; }
    public string AfterHash { get; set; }
}

/// <summary>
/// Makes an MCP scripting change set undoable and redoable through the editor's
/// regular undo system, including Ctrl+Z in the editor UI.
/// The change set itself is committed by <see cref="ScriptingChangePlanner"/>; this
/// mutation is recorded afterwards and only performs the inverse and re-application.
/// Undo and redo are refused when an element has been changed outside the undo system
/// since, so that a human's manual edits are never silently overwritten.
/// </summary>
internal sealed class ScriptingChangeMutation : Mutation, IGuardedMutation
{
    public ScriptingChangeMutation(IMutationTarget mutationTarget, IReadOnlyList<ScriptingElementChange> changes)
        : base(mutationTarget)
    {
        Changes = changes ?? throw new ArgumentNullException(nameof(changes));

        foreach (ScriptingElementChange change in Changes)
        {
            change.BeforeHash = change.BeforeSnapshot == null ? null : ScriptingModelOperations.ComputeHash(change.Kind, change.BeforeSnapshot, Map);
            change.AfterHash = change.AfterSnapshot == null ? null : ScriptingModelOperations.ComputeHash(change.Kind, change.AfterSnapshot, Map);
        }
    }

    public IReadOnlyList<ScriptingElementChange> Changes { get; }

    public int CreatedCount => Changes.Count(change => change.ChangeType == ScriptingElementChangeType.Created);
    public int UpdatedCount => Changes.Count(change => change.ChangeType == ScriptingElementChangeType.Updated);
    public int DeletedCount => Changes.Count(change => change.ChangeType == ScriptingElementChangeType.Deleted);

    public override string GetDisplayString()
    {
        return $"Scripting change: {CreatedCount} created, {UpdatedCount} updated, {DeletedCount} deleted";
    }

    public override void Perform()
    {
        foreach (ScriptingElementChange change in Changes)
        {
            switch (change.ChangeType)
            {
                case ScriptingElementChangeType.Updated:
                    ScriptingModelOperations.Copy(change.Kind, change.AfterSnapshot, change.Model);
                    break;
                case ScriptingElementChangeType.Created:
                    GetList(change.Kind).Add(change.Model);
                    RestoreIniSection(change);
                    break;
            }
        }

        foreach (ScriptingElementChange change in Changes.Where(change => change.ChangeType == ScriptingElementChangeType.Deleted))
        {
            GetList(change.Kind).Remove(change.Model);
            RemoveIniSection(change);
        }

        CompleteChange();
    }

    public override void Undo()
    {
        foreach (ScriptingElementChange change in Changes)
        {
            switch (change.ChangeType)
            {
                case ScriptingElementChangeType.Updated:
                    ScriptingModelOperations.Copy(change.Kind, change.BeforeSnapshot, change.Model);
                    break;
                case ScriptingElementChangeType.Created:
                    GetList(change.Kind).Remove(change.Model);
                    RemoveIniSection(change);
                    break;
            }
        }

        // Re-insert deleted elements in ascending original order so that each
        // one lands at its original position.
        foreach (ScriptingElementChange change in Changes
            .Where(change => change.ChangeType == ScriptingElementChangeType.Deleted)
            .OrderBy(change => change.OriginalListIndex))
        {
            IList list = GetList(change.Kind);
            list.Insert(Math.Clamp(change.OriginalListIndex, 0, list.Count), change.Model);
            RestoreIniSection(change);
        }

        CompleteChange();
    }

    public string GetUndoBlockReason()
    {
        // Undo restores "before" content for updated elements, removes created ones,
        // and brings deleted ones back.
        var problems = new List<string>();
        var leavingElements = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var restoredElements = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var changedElements = new HashSet<object>(Changes.Select(change => change.Model), ReferenceEqualityComparer.Instance);

        foreach (ScriptingElementChange change in Changes)
        {
            switch (change.ChangeType)
            {
                case ScriptingElementChangeType.Updated:
                case ScriptingElementChangeType.Created:
                    CheckUnchangedSince(change, change.AfterHash, "after this change", problems);
                    if (change.ChangeType == ScriptingElementChangeType.Created)
                        leavingElements.Add(change.Model);
                    break;
                case ScriptingElementChangeType.Deleted:
                    CheckIdentityFree(change, problems);
                    restoredElements.Add(change.Model);
                    break;
            }
        }

        CheckNoOutsideReferences(leavingElements, changedElements, problems);
        CheckRestoredReferencesResolve(restoredElements, leavingElements, ScriptingElementChangeType.Created, problems);

        return FormatProblems("undone", problems);
    }

    public string GetRedoBlockReason()
    {
        // Redo re-applies "after" content for updated elements, re-adds created ones,
        // and deletes deleted ones again.
        var problems = new List<string>();
        var leavingElements = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var restoredElements = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var changedElements = new HashSet<object>(Changes.Select(change => change.Model), ReferenceEqualityComparer.Instance);

        foreach (ScriptingElementChange change in Changes)
        {
            switch (change.ChangeType)
            {
                case ScriptingElementChangeType.Updated:
                case ScriptingElementChangeType.Deleted:
                    CheckUnchangedSince(change, change.BeforeHash, "after this change was undone", problems);
                    if (change.ChangeType == ScriptingElementChangeType.Deleted)
                        leavingElements.Add(change.Model);
                    break;
                case ScriptingElementChangeType.Created:
                    CheckIdentityFree(change, problems);
                    restoredElements.Add(change.Model);
                    break;
            }
        }

        CheckNoOutsideReferences(leavingElements, changedElements, problems);
        CheckRestoredReferencesResolve(restoredElements, leavingElements, ScriptingElementChangeType.Deleted, problems);

        return FormatProblems("redone", problems);
    }

    /// <summary>
    /// Returns whether the element is currently in the map and matches the expected content.
    /// </summary>
    internal bool MatchesCurrentState(ScriptingElementChange change, string expectedHash)
    {
        if (!ContainsInstance(GetList(change.Kind), change.Model))
            return false;

        return ScriptingModelOperations.ComputeHash(change.Kind, change.Model, Map) == expectedHash;
    }

    private void CheckUnchangedSince(ScriptingElementChange change, string expectedHash, string when, List<string> problems)
    {
        string description = Describe(change);

        if (!ContainsInstance(GetList(change.Kind), change.Model))
        {
            problems.Add($"{description} was removed {when}.");
            return;
        }

        if (ScriptingModelOperations.ComputeHash(change.Kind, change.Model, Map) != expectedHash)
            problems.Add($"{description} was edited {when}.");
    }

    private void CheckIdentityFree(ScriptingElementChange change, List<string> problems)
    {
        if (ContainsInstance(GetList(change.Kind), change.Model))
        {
            problems.Add($"{Describe(change)} is already present in the map.");
            return;
        }

        if (change.Kind == ScriptingElementKind.LocalVariable)
        {
            int index = ((LocalVariable)change.Model).Index;
            if (Map.LocalVariables.Exists(variable => variable.Index == index))
                problems.Add($"Local variable index {index} has since been used by another local variable.");
            return;
        }

        string id = ScriptingChangePlanner.GetElementId(change.Kind, change.Model);
        if (ScriptingModelOperations.IsInternalIdInUse(Map, id))
            problems.Add($"ID {id} of {Describe(change)} has since been used by another element.");
    }

    private void CheckNoOutsideReferences(HashSet<object> leavingElements, HashSet<object> changedElements, List<string> problems)
    {
        if (leavingElements.Count == 0)
            return;

        var referenceService = new ScriptingReferenceService(Map);
        foreach (object element in leavingElements)
        {
            ScriptingElementChange change = Changes.First(candidate => ReferenceEquals(candidate.Model, element));

            // Local variables are referenced by index from trigger parameters; the reference
            // service resolves those, so they are covered by the same check.
            List<string> referencePaths = referenceService.GetIncomingReferencePaths(element, changedElements);
            if (referencePaths.Count > 0)
            {
                problems.Add($"{Describe(change)} would be removed, but it is still referenced by: " +
                    string.Join(", ", referencePaths.Take(5)) + (referencePaths.Count > 5 ? ", ..." : string.Empty) + ".");
            }
        }
    }

    private void CheckRestoredReferencesResolve(HashSet<object> restoredElements, HashSet<object> leavingElements,
        ScriptingElementChangeType leavingChangeType, List<string> problems)
    {
        foreach (object element in restoredElements)
        {
            ScriptingElementChange change = Changes.First(candidate => ReferenceEquals(candidate.Model, element));

            foreach ((string path, object target) in ScriptingModelOperations.GetDirectReferences(change.Kind, element))
            {
                if (target == null || restoredElements.Contains(target))
                    continue;

                bool presentAfterOperation = ScriptingModelOperations.IsPresentInMapOrRules(Map, target) && !leavingElements.Contains(target);
                if (!presentAfterOperation)
                    problems.Add($"{Describe(change)} refers through {path} to an element that no longer exists in the map.");
            }
        }
    }

    private static string FormatProblems(string operation, List<string> problems)
    {
        if (problems.Count == 0)
            return null;

        return $"The scripting change cannot be safely {operation} because the map changed outside the undo history: " +
            string.Join(" ", problems);
    }

    private void RemoveIniSection(ScriptingElementChange change)
    {
        if (!ScriptingModelOperations.HasOwnIniSection(change.Kind) || Map.LoadedINI == null)
            return;

        string id = ScriptingChangePlanner.GetElementId(change.Kind, change.Model);
        IniSection section = Map.LoadedINI.GetSection(id);
        if (section != null)
        {
            change.RemovedIniSection = section;
            Map.LoadedINI.RemoveSection(id);
        }
    }

    private void RestoreIniSection(ScriptingElementChange change)
    {
        if (change.RemovedIniSection == null || Map.LoadedINI == null)
            return;

        if (!Map.LoadedINI.SectionExists(change.RemovedIniSection.SectionName))
            Map.LoadedINI.AddSection(change.RemovedIniSection);

        change.RemovedIniSection = null;
    }

    private void CompleteChange()
    {
        Map.LocalVariables.Sort((left, right) => left.Index.CompareTo(right.Index));

        try
        {
            Map.NotifyScriptingChanged(Changes.Select(change => change.Model.GetType()).Distinct().ToList());
        }
        catch (Exception ex)
        {
            // The data change is complete; a UI refresh failure must not corrupt the history.
            Logger.Log("Failed to notify a scripting window after undoing or redoing an MCP scripting change: " + ex.Message);
        }
    }

    private IList GetList(ScriptingElementKind kind) => ScriptingModelOperations.GetLocalList(Map, kind);

    private static bool ContainsInstance(IList list, object model)
    {
        foreach (object item in list)
        {
            if (ReferenceEquals(item, model))
                return true;
        }

        return false;
    }

    internal static string Describe(ScriptingElementChange change)
    {
        object model = change.AfterSnapshot ?? change.BeforeSnapshot ?? change.Model;
        string name = ScriptingChangePlanner.GetElementName(change.Kind, model);
        string identity = change.Kind == ScriptingElementKind.LocalVariable
            ? $"index {((LocalVariable)change.Model).Index}"
            : ScriptingChangePlanner.GetElementId(change.Kind, change.Model);

        return string.IsNullOrWhiteSpace(name) ? $"{change.Kind} {identity}" : $"{change.Kind} {identity} (\"{name}\")";
    }
}

/// <summary>
/// Kind-dispatched helpers for scripting models.
/// </summary>
internal static class ScriptingModelOperations
{
    public static IList GetLocalList(Map map, ScriptingElementKind kind)
    {
        return kind switch
        {
            ScriptingElementKind.TaskForce => map.TaskForces,
            ScriptingElementKind.Script => map.Scripts,
            ScriptingElementKind.TeamType => map.TeamTypes,
            ScriptingElementKind.AITrigger => map.AITriggerTypes,
            ScriptingElementKind.LocalVariable => map.LocalVariables,
            ScriptingElementKind.Trigger => map.Triggers,
            ScriptingElementKind.Tag => map.Tags,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not an editable scripting element kind.")
        };
    }

    /// <summary>
    /// TaskForces, Scripts, and TeamTypes are stored in their own INI sections named after their ID.
    /// </summary>
    public static bool HasOwnIniSection(ScriptingElementKind kind)
        => kind is ScriptingElementKind.TaskForce or ScriptingElementKind.Script or ScriptingElementKind.TeamType;

    public static object Snapshot(ScriptingElementKind kind, object model)
    {
        return kind switch
        {
            ScriptingElementKind.TaskForce => ScriptingChangePlanner.SnapshotTaskForce((TaskForce)model),
            ScriptingElementKind.Script => ScriptingChangePlanner.SnapshotScript((Script)model),
            ScriptingElementKind.TeamType => ScriptingChangePlanner.SnapshotTeamType((TeamType)model),
            ScriptingElementKind.AITrigger => ScriptingChangePlanner.SnapshotAITrigger((AITriggerType)model),
            ScriptingElementKind.LocalVariable => ScriptingChangePlanner.SnapshotLocalVariable((LocalVariable)model),
            ScriptingElementKind.Trigger => ScriptingChangePlanner.SnapshotTrigger((Trigger)model),
            ScriptingElementKind.Tag => ScriptingChangePlanner.SnapshotTag((Tag)model),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not an editable scripting element kind.")
        };
    }

    public static void Copy(ScriptingElementKind kind, object source, object destination)
    {
        switch (kind)
        {
            case ScriptingElementKind.TaskForce:
                ScriptingChangePlanner.CopyTaskForce((TaskForce)source, (TaskForce)destination);
                break;
            case ScriptingElementKind.Script:
                ScriptingChangePlanner.CopyScript((Script)source, (Script)destination);
                break;
            case ScriptingElementKind.TeamType:
                ScriptingChangePlanner.CopyTeamType((TeamType)source, (TeamType)destination);
                break;
            case ScriptingElementKind.AITrigger:
                ScriptingChangePlanner.CopyAITrigger((AITriggerType)source, (AITriggerType)destination);
                break;
            case ScriptingElementKind.LocalVariable:
                ScriptingChangePlanner.CopyLocalVariable((LocalVariable)source, (LocalVariable)destination);
                break;
            case ScriptingElementKind.Trigger:
                ScriptingChangePlanner.CopyTrigger((Trigger)source, (Trigger)destination);
                break;
            case ScriptingElementKind.Tag:
                ScriptingChangePlanner.CopyTag((Tag)source, (Tag)destination);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not an editable scripting element kind.");
        }
    }

    public static string ComputeHash(ScriptingElementKind kind, object model, Map map)
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
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not an editable scripting element kind.")
        };
    }

    public static List<KeyValuePair<string, string>> DescribeFields(ScriptingElementKind kind, object model, Map map)
        => ScriptingContentHasher.DescribeFields(() => ComputeHash(kind, model, map));

    public static bool IsInternalIdInUse(Map map, string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return map.TaskForces.Exists(taskForce => taskForce.ININame == id) ||
               map.Scripts.Exists(script => script.ININame == id) ||
               map.TeamTypes.Exists(teamType => teamType.ININame == id) ||
               map.Triggers.Exists(trigger => trigger.ID == id) ||
               map.Tags.Exists(tag => tag.ID == id) ||
               map.AITriggerTypes.Exists(aiTrigger => aiTrigger.ININame == id);
    }

    /// <summary>
    /// Returns the direct object references held by a scripting element. Parameter-encoded
    /// references (trigger and script action parameters) are stored by ID and resolved at use.
    /// </summary>
    public static IEnumerable<(string Path, object Target)> GetDirectReferences(ScriptingElementKind kind, object model)
    {
        switch (kind)
        {
            case ScriptingElementKind.TeamType:
                var teamType = (TeamType)model;
                yield return ("taskForce", teamType.TaskForce);
                yield return ("script", teamType.Script);
                yield return ("tag", teamType.Tag);
                break;
            case ScriptingElementKind.AITrigger:
                var aiTrigger = (AITriggerType)model;
                yield return ("primaryTeam", aiTrigger.PrimaryTeam);
                yield return ("secondaryTeam", aiTrigger.SecondaryTeam);
                break;
            case ScriptingElementKind.Tag:
                yield return ("trigger", ((Tag)model).Trigger);
                break;
            case ScriptingElementKind.Trigger:
                yield return ("linkedTrigger", ((Trigger)model).LinkedTrigger);
                break;
        }
    }

    public static bool IsPresentInMapOrRules(Map map, object target)
    {
        return target switch
        {
            TaskForce taskForce => map.TaskForces.Contains(taskForce) || map.Rules.TaskForces.Contains(taskForce),
            Script script => map.Scripts.Contains(script) || map.Rules.Scripts.Contains(script),
            TeamType teamType => map.TeamTypes.Contains(teamType) || map.Rules.TeamTypes.Contains(teamType),
            AITriggerType aiTrigger => map.AITriggerTypes.Contains(aiTrigger),
            Trigger trigger => map.Triggers.Contains(trigger),
            Tag tag => map.Tags.Contains(tag),
            _ => true
        };
    }
}
