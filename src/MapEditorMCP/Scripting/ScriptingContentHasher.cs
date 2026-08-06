using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MapEditorMCP.Scripting;

/// <summary>
/// Computes deterministic content hashes for scripting models.
/// The hashes deliberately do not use object identity or <see cref="object.GetHashCode"/>.
/// </summary>
internal static class ScriptingContentHasher
{
    private const string HashFormatVersion = "WAE-Scripting-Content-v1";
    private const string HashPrefix = "sha256:";

    public static string Compute(TaskForce taskForce)
    {
        ArgumentNullException.ThrowIfNull(taskForce);

        return Hash(nameof(TaskForce), writer =>
        {
            writer.WriteString("id", taskForce.ININame);
            writer.WriteString("name", taskForce.Name);
            writer.WriteInt32("group", taskForce.Group);

            TaskForceTechnoEntry[] entries = taskForce.TechnoTypes;
            writer.WriteInt32("memberSlotCount", entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                writer.WriteInt32("memberIndex", i);
                WriteTaskForceEntry(writer, entries[i]);
            }
        });
    }

    public static string Compute(TaskForceTechnoEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return Hash(nameof(TaskForceTechnoEntry), writer => WriteTaskForceEntry(writer, entry));
    }

    public static string Compute(Script script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return Hash(nameof(Script), writer =>
        {
            writer.WriteString("id", script.ININame);
            writer.WriteString("name", script.Name);
            writer.WriteString("editorColor", script.EditorColor);
            writer.WriteInt32("actionCount", script.Actions.Count);

            for (int i = 0; i < script.Actions.Count; i++)
            {
                writer.WriteInt32("actionIndex", i);
                WriteScriptAction(writer, script.Actions[i]);
            }
        });
    }

    public static string Compute(ScriptActionEntry action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Hash(nameof(ScriptActionEntry), writer => WriteScriptAction(writer, action));
    }

    public static string Compute(TeamType teamType)
    {
        ArgumentNullException.ThrowIfNull(teamType);

        return Hash(nameof(TeamType), writer =>
        {
            writer.WriteString("id", teamType.ININame);
            writer.WriteString("name", teamType.Name);
            writer.WriteInt32("group", teamType.Group);
            writer.WriteString("houseTypeId", teamType.HouseType?.ININame);
            writer.WriteString("scriptId", teamType.Script?.ININame);
            writer.WriteString("taskForceId", teamType.TaskForce?.ININame);
            writer.WriteString("tagId", teamType.Tag?.ID);
            writer.WriteInt32("max", teamType.Max);
            writer.WriteInt32("priority", teamType.Priority);
            writer.WriteString("waypoint", teamType.Waypoint);
            writer.WriteString("transportWaypoint", teamType.TransportWaypoint);
            writer.WriteNullableInt32("mindControlDecision", teamType.MindControlDecision);
            writer.WriteInt32("techLevel", teamType.TechLevel);
            writer.WriteInt32("veteranLevel", teamType.VeteranLevel);
            writer.WriteString("editorColor", teamType.EditorColor);
            writer.WriteBoolean("isGlobal", teamType.IsGlobalTeamType);

            List<string> sortedFlags = teamType.EnabledTeamTypeFlags.OrderBy(flag => flag, StringComparer.Ordinal).ToList();

            writer.WriteInt32("enabledFlagCount", sortedFlags.Count);
            foreach (string flag in sortedFlags)
                writer.WriteString("enabledFlag", flag);
        });
    }

    public static string Compute(AITriggerType aiTrigger)
    {
        ArgumentNullException.ThrowIfNull(aiTrigger);

        return Hash(nameof(AITriggerType), writer =>
        {
            writer.WriteString("id", aiTrigger.ININame);
            writer.WriteString("name", aiTrigger.Name);
            writer.WriteString("primaryTeamId", aiTrigger.PrimaryTeam?.ININame);
            writer.WriteString("ownerName", aiTrigger.OwnerName);
            writer.WriteInt32("techLevel", aiTrigger.TechLevel);
            writer.WriteInt32("conditionType", (int)aiTrigger.ConditionType);
            writer.WriteString("conditionObjectId", aiTrigger.ConditionObject?.ININame);
            writer.WriteInt32("comparatorOperator", (int)aiTrigger.Comparator.ComparatorOperator);
            writer.WriteInt32("comparatorQuantity", aiTrigger.Comparator.Quantity);
            writer.WriteDouble("initialWeight", NormalizeAITriggerWeight(aiTrigger.InitialWeight));
            writer.WriteDouble("minimumWeight", NormalizeAITriggerWeight(aiTrigger.MinimumWeight));
            writer.WriteDouble("maximumWeight", NormalizeAITriggerWeight(aiTrigger.MaximumWeight));
            writer.WriteBoolean("enabledInMultiplayer", true);
            writer.WriteBoolean("unused", false);
            writer.WriteInt32("side", aiTrigger.Side);
            writer.WriteBoolean("isBaseDefense", false);
            writer.WriteString("secondaryTeamId", aiTrigger.SecondaryTeam?.ININame);
            writer.WriteBoolean("easy", aiTrigger.Easy);
            writer.WriteBoolean("medium", aiTrigger.Medium);
            writer.WriteBoolean("hard", aiTrigger.Hard);
            writer.WriteBoolean("enabled", aiTrigger.Enabled);
        });
    }

    public static string Compute(LocalVariable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        return Hash(nameof(LocalVariable), writer =>
        {
            writer.WriteInt32("index", variable.Index);
            writer.WriteString("name", variable.Name);
            writer.WriteInt32("initialState", variable.InitialState);
        });
    }

    public static string Compute(GlobalVariable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        return Hash(nameof(GlobalVariable), writer =>
        {
            writer.WriteInt32("index", variable.Index);
            writer.WriteString("name", variable.Name);
        });
    }

    public static string Compute(Trigger trigger, EditorConfig editorConfig)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(editorConfig);

        return Hash(nameof(Trigger), writer =>
        {
            writer.WriteString("id", trigger.ID);
            writer.WriteString("houseTypeId", trigger.HouseType?.ININame);
            writer.WriteString("linkedTriggerId", trigger.LinkedTrigger?.ID);
            writer.WriteString("name", trigger.Name);
            writer.WriteBoolean("disabled", trigger.Disabled);
            writer.WriteBoolean("easy", trigger.Easy);
            writer.WriteBoolean("normal", trigger.Normal);
            writer.WriteBoolean("hard", trigger.Hard);
            writer.WriteString("editorColor", trigger.EditorColor);

            writer.WriteInt32("eventCount", trigger.Conditions.Count);
            for (int i = 0; i < trigger.Conditions.Count; i++)
            {
                writer.WriteInt32("eventIndex", i);
                WritePersistedTriggerCondition(writer, trigger.Conditions[i], editorConfig);
            }

            writer.WriteInt32("actionCount", trigger.Actions.Count);
            for (int i = 0; i < trigger.Actions.Count; i++)
            {
                writer.WriteInt32("actionIndex", i);
                WritePersistedTriggerAction(writer, trigger.Actions[i], editorConfig);
            }
        });
    }

    public static string Compute(TriggerCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return Hash(nameof(TriggerCondition), writer => WriteTriggerCondition(writer, condition));
    }

    public static string Compute(TriggerAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Hash(nameof(TriggerAction), writer => WriteTriggerAction(writer, action));
    }

    public static string Compute(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return Hash(nameof(Tag), writer =>
        {
            writer.WriteString("id", tag.ID);
            writer.WriteInt32("repeating", tag.Repeating);
            writer.WriteString("name", tag.Name);
            writer.WriteString("triggerId", tag.Trigger?.ID);
        });
    }

    public static string Compute(CellTag cellTag)
    {
        ArgumentNullException.ThrowIfNull(cellTag);

        return Hash(nameof(CellTag), writer =>
        {
            writer.WriteInt32("x", cellTag.Position.X);
            writer.WriteInt32("y", cellTag.Position.Y);
            writer.WriteString("tagId", cellTag.Tag?.ID);
        });
    }

    internal static double NormalizeAITriggerWeight(double weight)
    {
        string serializedWeight = weight.ToString("0.######", CultureInfo.InvariantCulture);
        return double.Parse(serializedWeight, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static void WriteTaskForceEntry(CanonicalWriter writer, TaskForceTechnoEntry entry)
    {
        writer.WriteBoolean("hasMember", entry != null);
        if (entry == null)
            return;

        writer.WriteString("technoTypeId", entry.TechnoType?.ININame);
        writer.WriteInt32("count", entry.Count);
    }

    private static void WriteScriptAction(CanonicalWriter writer, ScriptActionEntry action)
    {
        writer.WriteBoolean("hasAction", action != null);
        if (action == null)
            return;

        writer.WriteInt32("actionId", action.Action);
        writer.WriteInt32("rawArgument", action.Argument);
    }

    private static void WriteTriggerCondition(CanonicalWriter writer, TriggerCondition condition)
    {
        writer.WriteBoolean("hasEvent", condition != null);
        if (condition == null)
            return;

        writer.WriteInt32("eventId", condition.ConditionIndex);
        WriteRawParameters(writer, condition.Parameters);
    }

    private static void WriteTriggerAction(CanonicalWriter writer, TriggerAction action)
    {
        writer.WriteBoolean("hasAction", action != null);
        if (action == null)
            return;

        writer.WriteInt32("actionId", action.ActionIndex);
        WriteRawParameters(writer, action.Parameters);
    }

    private static void WritePersistedTriggerCondition(
        CanonicalWriter writer,
        TriggerCondition condition,
        EditorConfig editorConfig)
    {
        writer.WriteBoolean("hasEvent", condition != null);
        if (condition == null)
            return;

        writer.WriteInt32("eventId", condition.ConditionIndex);
        if (!editorConfig.TriggerEventTypes.TryGetValue(condition.ConditionIndex, out TriggerEventType definition))
        {
            WriteRawParameters(writer, condition.Parameters);
            return;
        }

        int persistedParameterCount = TriggerCondition.DEF_PARAM_COUNT + definition.AdditionalParams;
        writer.WriteInt32("parameterCount", persistedParameterCount);
        for (int i = 0; i < persistedParameterCount; i++)
        {
            writer.WriteInt32("parameterIndex", i);
            writer.WriteString("rawParameter", condition.ParamToString(i));
        }
    }

    private static void WritePersistedTriggerAction(
        CanonicalWriter writer,
        TriggerAction action,
        EditorConfig editorConfig)
    {
        writer.WriteBoolean("hasAction", action != null);
        if (action == null)
            return;

        writer.WriteInt32("actionId", action.ActionIndex);
        writer.WriteInt32("parameterCount", TriggerAction.PARAM_COUNT);
        for (int i = 0; i < TriggerAction.PARAM_COUNT; i++)
        {
            writer.WriteInt32("parameterIndex", i);
            writer.WriteString("rawParameter", GetPersistedTriggerActionParameter(action, i, editorConfig));
        }
    }

    private static string GetPersistedTriggerActionParameter(
        TriggerAction action,
        int parameterIndex,
        EditorConfig editorConfig)
    {
        int specialIndex = TriggerAction.PARAM_COUNT - 1;
        if (parameterIndex != specialIndex ||
            !editorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out TriggerActionType definition) ||
            definition.Parameters[specialIndex].TriggerParamType != TriggerParamType.Unused)
        {
            return action.ParamToString(parameterIndex);
        }

        return "A";
    }

    private static void WriteRawParameters(CanonicalWriter writer, string[] parameters)
    {
        writer.WriteInt32("parameterCount", parameters.Length);

        for (int i = 0; i < parameters.Length; i++)
        {
            writer.WriteInt32("parameterIndex", i);
            writer.WriteString("rawParameter", parameters[i]);
        }
    }

    private static string Hash(string modelType, Action<CanonicalWriter> writeContent)
    {
        using var stream = new MemoryStream();
        var writer = new CanonicalWriter(stream);
        writer.WriteString("format", HashFormatVersion);
        writer.WriteString("modelType", modelType);
        writeContent(writer);

        byte[] hash = SHA256.HashData(stream.ToArray());
        return HashPrefix + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class CanonicalWriter
    {
        public CanonicalWriter(Stream stream)
        {
            this.stream = stream;
        }

        private readonly Stream stream;

        public void WriteBoolean(string name, bool value)
        {
            WriteName(name);
            stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteInt32(string name, int value)
        {
            WriteName(name);
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            stream.Write(buffer);
        }

        public void WriteNullableInt32(string name, int? value)
        {
            WriteName(name);
            stream.WriteByte(value.HasValue ? (byte)1 : (byte)0);
            if (!value.HasValue)
                return;

            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(buffer, value.Value);
            stream.Write(buffer);
        }

        public void WriteDouble(string name, double value)
        {
            WriteName(name);
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(buffer, BitConverter.DoubleToInt64Bits(value));
            stream.Write(buffer);
        }

        public void WriteString(string name, string value)
        {
            WriteName(name);
            WriteRawString(value);
        }

        private void WriteName(string name)
        {
            WriteRawString(name);
        }

        private void WriteRawString(string value)
        {
            if (value == null)
            {
                stream.WriteByte(0);
                return;
            }

            stream.WriteByte(1);
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            Span<byte> lengthBuffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, bytes.Length);
            stream.Write(lengthBuffer);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
