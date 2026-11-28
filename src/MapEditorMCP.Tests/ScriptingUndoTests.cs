using MapEditorLibrary;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using MapEditorLibrary.Mutations;
using MapEditorMCP.Infos;
using MapEditorMCP.MCPMutations;
using MapEditorMCP.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Server;

namespace MapEditorMCP.Tests;

[TestClass]
public sealed class ScriptingUndoTests
{
    [TestMethod]
    public void UndoAndRedoRestoreUpdatedCreatedAndDeletedElements()
    {
        var fixture = new Fixture();
        TaskForce updated = fixture.AddTaskForce("01000000", "Old name");
        TaskForce deleted = fixture.AddTaskForce("01000001", "Deleted");
        TaskForce kept = fixture.AddTaskForce("01000002", "Kept");

        long mutationId = fixture.ApplyAndRecord(
            Update(updated, taskForce => taskForce.Name = "New name"),
            Create(new TaskForce("01000003") { Name = "Created" }),
            Delete(deleted));

        Assert.AreEqual("New name", updated.Name);
        CollectionAssert.AreEqual(new[] { "01000000", "01000002", "01000003" }, fixture.TaskForceIds());

        fixture.MutationManager.Undo();

        Assert.AreEqual("Old name", updated.Name);
        CollectionAssert.AreEqual(new[] { "01000000", "01000001", "01000002" }, fixture.TaskForceIds());
        Assert.AreSame(deleted, fixture.Map.TaskForces[1]);
        Assert.AreSame(kept, fixture.Map.TaskForces[2]);
        Assert.AreEqual(1, fixture.MutationManager.RedoList.Count);

        fixture.MutationManager.Redo();

        Assert.AreEqual("New name", updated.Name);
        CollectionAssert.AreEqual(new[] { "01000000", "01000002", "01000003" }, fixture.TaskForceIds());
        Assert.AreEqual(mutationId, fixture.MutationManager.UndoList[^1].HistoryMetadata.MutationId);
    }

    [TestMethod]
    public void EditorUndoDiscardsEntryInsteadOfOverwritingManualEdit()
    {
        var fixture = new Fixture();
        TaskForce taskForce = fixture.AddTaskForce("01000000", "Before");
        fixture.ApplyAndRecord(Update(taskForce, model => model.Name = "Agent"));

        // The user edits the same TaskForce in the TaskForces window, which bypasses the undo history.
        taskForce.Name = "User";

        MutationDiscardedEventArgs discarded = null;
        fixture.MutationManager.MutationDiscarded += (_, e) => discarded = e;
        fixture.MutationManager.Undo();

        Assert.AreEqual("User", taskForce.Name);
        Assert.IsNotNull(discarded);
        StringAssert.Contains(discarded.Reason, "was edited after this change");
        Assert.AreEqual(0, fixture.MutationManager.UndoList.Count);
        Assert.AreEqual(0, fixture.MutationManager.RedoList.Count);
    }

    [TestMethod]
    public void McpUndoRefusesBlockedEntryAndKeepsHistory()
    {
        var fixture = new Fixture();
        TaskForce taskForce = fixture.AddTaskForce("01000000", "Before");
        long mutationId = fixture.ApplyAndRecord(Update(taskForce, model => model.Name = "Agent"));
        taskForce.Name = "User";

        MapMutationHistoryInfo history = fixture.Facade.GetMutationHistory(10);
        Assert.IsFalse(history.CanUndo);
        Assert.IsNotNull(history.UndoHistory[0].BlockReason);
        Assert.AreEqual(MapMutationKinds.Scripting, history.UndoHistory[0].Kind);

        Assert.ThrowsExactly<MapFacadeValidationException>(() =>
            fixture.Facade.UndoLatest(fixture.MutationManager.Revision, mutationId));

        Assert.AreEqual("User", taskForce.Name);
        Assert.AreEqual(1, fixture.MutationManager.UndoList.Count);
    }

    [TestMethod]
    public void UndoIsBlockedWhileCreatedElementIsReferencedByLaterManualWork()
    {
        var fixture = new Fixture();
        var created = new TaskForce("01000000") { Name = "Agent TaskForce" };
        fixture.ApplyAndRecord(Create(created));

        // The user then creates a TeamType that uses the agent's TaskForce.
        fixture.Map.TeamTypes.Add(new TeamType("01000001") { Name = "User team", TaskForce = created });

        var mutation = (ScriptingChangeMutation)fixture.MutationManager.UndoList[^1];
        string reason = mutation.GetUndoBlockReason();

        Assert.IsNotNull(reason);
        StringAssert.Contains(reason, "teamTypes[01000001].taskForce");
    }

    [TestMethod]
    public void RedoIsBlockedWhenCreatedIdWasReused()
    {
        var fixture = new Fixture();
        fixture.ApplyAndRecord(Create(new TaskForce("01000000") { Name = "Agent" }));
        fixture.MutationManager.Undo();

        // After the undo, the user creates a TaskForce that receives the now-free ID.
        fixture.AddTaskForce("01000000", "User");

        fixture.MutationManager.Redo();

        Assert.AreEqual(1, fixture.Map.TaskForces.Count);
        Assert.AreEqual("User", fixture.Map.TaskForces[0].Name);
        Assert.AreEqual(0, fixture.MutationManager.RedoList.Count);
    }

    [TestMethod]
    public void UndoThroughUndoesSeveralMcpEntries()
    {
        var fixture = new Fixture();
        TaskForce taskForce = fixture.AddTaskForce("01000000", "v0");
        long first = fixture.ApplyAndRecord(Update(taskForce, model => model.Name = "v1"));
        fixture.ApplyAndRecord(Update(taskForce, model => model.Name = "v2"));
        fixture.ApplyAndRecord(Update(taskForce, model => model.Name = "v3"));

        MapMutationBatchUndoResult result = fixture.Facade.UndoThrough(fixture.MutationManager.Revision, first);

        Assert.AreEqual("v0", taskForce.Name);
        Assert.AreEqual(3, result.UndoneMutations.Count);
        Assert.AreEqual(first, result.UndoneMutations[^1].MutationId);
        Assert.IsTrue(result.CanRedo);
    }

    [TestMethod]
    public void UndoThroughNeverCrossesManualEdits()
    {
        var fixture = new Fixture();
        TaskForce taskForce = fixture.AddTaskForce("01000000", "v0");
        long first = fixture.ApplyAndRecord(Update(taskForce, model => model.Name = "v1"));
        fixture.MutationManager.PerformMutation(new ManualMutation(fixture.Target));
        fixture.ApplyAndRecord(Update(taskForce, model => model.Name = "v2"));

        var exception = Assert.ThrowsExactly<MapFacadeValidationException>(() =>
            fixture.Facade.UndoThrough(fixture.MutationManager.Revision, first));

        StringAssert.Contains(exception.Message, "manual edit");
        Assert.AreEqual("v2", taskForce.Name);
        Assert.AreEqual(3, fixture.MutationManager.UndoList.Count);
    }

    [TestMethod]
    public void ReviewActionReportsFieldLevelDiff()
    {
        var fixture = new Fixture();
        TaskForce taskForce = fixture.AddTaskForce("01000000", "Old name");
        taskForce.Group = 1;
        long mutationId = fixture.ApplyAndRecord(Update(taskForce, model =>
        {
            model.Name = "New name";
            model.Group = 4;
        }));

        MapActionReviewInfo review = fixture.Facade.ReviewAction(mutationId, 200);

        Assert.AreEqual("Undo", review.HistoryStack);
        Assert.AreEqual(0, review.PositionFromTop);
        Assert.AreEqual(1, review.ScriptingChanges.Count);

        ScriptingElementChangeReviewInfo change = review.ScriptingChanges[0];
        Assert.AreEqual("Updated", change.ChangeType);
        Assert.IsTrue(change.MatchesExpectedCurrentState);
        Assert.AreEqual(2, change.FieldChanges.Count);
        Assert.AreEqual("name", change.FieldChanges[0].Field);
        Assert.AreEqual("Old name", change.FieldChanges[0].Before);
        Assert.AreEqual("New name", change.FieldChanges[0].After);
        Assert.AreEqual("group", change.FieldChanges[1].Field);

        fixture.MutationManager.Undo();
        review = fixture.Facade.ReviewAction(mutationId, 200);
        Assert.AreEqual("Redo", review.HistoryStack);
        Assert.IsTrue(review.ScriptingChanges[0].MatchesExpectedCurrentState);
    }

    [TestMethod]
    public void DescribedFieldsAreScopedPerEntry()
    {
        var taskForce = new TaskForce("01000000") { Name = "Armor" };
        taskForce.TechnoTypes[0] = new TaskForceTechnoEntry { Count = 3 };

        var fields = ScriptingContentHasher.DescribeFields(() => ScriptingContentHasher.Compute(taskForce))
            .ToDictionary(field => field.Key, field => field.Value);

        Assert.AreEqual("Armor", fields["name"]);
        Assert.AreEqual("3", fields["members[0].count"]);
        Assert.AreEqual("false", fields["members[1].hasMember"]);
        Assert.IsFalse(fields.ContainsKey("format"));
    }

    [TestMethod]
    public void CountFieldsAfterAListAreNotScopedToItsLastEntry()
    {
        var trigger = new Trigger("01000000") { Name = "Trigger" };
        trigger.Conditions.Add(new TriggerCondition { ConditionIndex = 13 });
        trigger.Actions.Add(new TriggerAction { ActionIndex = 54 });

        var fields = ScriptingContentHasher.DescribeFields(() => ScriptingContentHasher.Compute(trigger, new EditorConfig()))
            .Select(field => field.Key)
            .ToList();

        CollectionAssert.Contains(fields, "actionCount");
        CollectionAssert.Contains(fields, "actions[0].parameterCount");
        CollectionAssert.Contains(fields, "actions[0].parameter[1].rawParameter");
        Assert.IsFalse(fields.Any(field => field.EndsWith(".actionCount", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void DescribingFieldsDoesNotChangeTheHash()
    {
        var taskForce = new TaskForce("01000000") { Name = "Armor" };
        string expected = ScriptingContentHasher.Compute(taskForce);

        ScriptingContentHasher.DescribeFields(() => ScriptingContentHasher.Compute(taskForce));

        Assert.AreEqual(expected, ScriptingContentHasher.Compute(taskForce));
    }

    [TestMethod]
    public void NewToolsAreRegisteredWithSchemas()
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithTools<MapTools>().WithTools<EditorTools>();

        using ServiceProvider provider = services.BuildServiceProvider();
        var tools = provider.GetServices<McpServerTool>().ToDictionary(tool => tool.ProtocolTool.Name);

        foreach (string name in new[] { "get_camera", "move_camera", "save_map", "review_action", "undo_through", "get_mutation_history" })
            Assert.IsTrue(tools.ContainsKey(name), $"Tool {name} is not registered.");

        string moveCameraSchema = tools["move_camera"].ProtocolTool.InputSchema.GetRawText();
        StringAssert.Contains(moveCameraSchema, "mutationId");
        Assert.IsTrue(tools["save_map"].ProtocolTool.Annotations.DestructiveHint);
        Assert.IsTrue(tools["review_action"].ProtocolTool.Annotations.ReadOnlyHint);
    }

    private static Func<Fixture, ScriptingElementChange> Update(TaskForce taskForce, Action<TaskForce> edit) => fixture =>
    {
        object before = ScriptingModelOperations.Snapshot(ScriptingElementKind.TaskForce, taskForce);
        edit(taskForce);
        object after = ScriptingModelOperations.Snapshot(ScriptingElementKind.TaskForce, taskForce);
        return new ScriptingElementChange(ScriptingElementKind.TaskForce, ScriptingElementChangeType.Updated, taskForce, before, after, -1, null);
    };

    private static Func<Fixture, ScriptingElementChange> Create(TaskForce taskForce) => fixture =>
    {
        fixture.Map.TaskForces.Add(taskForce);
        object after = ScriptingModelOperations.Snapshot(ScriptingElementKind.TaskForce, taskForce);
        return new ScriptingElementChange(ScriptingElementKind.TaskForce, ScriptingElementChangeType.Created, taskForce, null, after, -1, null);
    };

    private static Func<Fixture, ScriptingElementChange> Delete(TaskForce taskForce) => fixture =>
    {
        int index = fixture.Map.TaskForces.IndexOf(taskForce);
        object before = ScriptingModelOperations.Snapshot(ScriptingElementKind.TaskForce, taskForce);
        fixture.Map.TaskForces.Remove(taskForce);
        return new ScriptingElementChange(ScriptingElementKind.TaskForce, ScriptingElementChangeType.Deleted, taskForce, before, null, index, null);
    };

    private sealed class Fixture
    {
        public Fixture()
        {
            Map = new Map();
            typeof(Map).GetProperty(nameof(Map.Rules))!.SetValue(Map, new Rules());
            Target = new TestMutationTarget(Map);
            MutationManager = new MutationManager();
            Facade = new MapFacade(Map, MutationManager, Target);
        }

        public Map Map { get; }
        public TestMutationTarget Target { get; }
        public MutationManager MutationManager { get; }
        public MapFacade Facade { get; }

        public TaskForce AddTaskForce(string id, string name)
        {
            var taskForce = new TaskForce(id) { Name = name };
            Map.TaskForces.Add(taskForce);
            return taskForce;
        }

        public string[] TaskForceIds() => Map.TaskForces.Select(taskForce => taskForce.ININame).ToArray();

        /// <summary>
        /// Applies changes the way <see cref="ScriptingChangePlanner"/> does (updates and creates
        /// first, deletes last) and records them like <see cref="ScriptingFacade"/>.
        /// </summary>
        public long ApplyAndRecord(params Func<Fixture, ScriptingElementChange>[] steps)
        {
            var changes = steps.Select(step => step(this)).ToList();
            var mutation = new ScriptingChangeMutation(Target, changes);
            MutationManager.RecordPerformedMutation(mutation, MutationManager.MCPMutationOrigin, "apply_scripting_changes",
                new MutationAffectedCells(true, 0, null, null, null, null));
            return mutation.HistoryMetadata.MutationId;
        }
    }

    private sealed class ManualMutation : Mutation
    {
        public ManualMutation(IMutationTarget target) : base(target)
        {
        }

        public override string GetDisplayString() => "Manual edit";
        public override void Perform() { }
        public override void Undo() { }
    }

    private sealed class TestMutationTarget : IMutationTarget
    {
        public TestMutationTarget(Map map) => Map = map;

        public Map Map { get; }
        public ITheater TheaterGraphicsData => null;
        public void AddRefreshPoint(Point2D point, int size = 10) { }
        public void InvalidateMap() { }
        public House ObjectOwner => null;
        public BrushSize BrushSize => new BrushSize(1, 1);
        public Randomizer Randomizer { get; } = new Randomizer();
        public bool AutoLATEnabled => false;
        public LightingPreviewMode LightingPreviewState => LightingPreviewMode.NoLighting;
        public bool LightDisabledLightSources => false;
        public bool OnlyPaintOnClearGround => false;
    }
}
