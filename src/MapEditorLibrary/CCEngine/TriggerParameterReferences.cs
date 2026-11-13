using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.CCEngine;

public readonly record struct TriggerParameterReference(bool IsEvent, int EntryIndex, int ParameterIndex, string EntryName,
    TriggerParamType ParameterType, string Value, TriggerVariableAccess VariableAccess);

public static class TriggerParameterReferences
{
    public static IEnumerable<TriggerParameterReference> Enumerate(Trigger trigger, EditorConfig editorConfig)
    {
        for (int eventIndex = 0; eventIndex < trigger.Conditions.Count; eventIndex++)
        {
            var condition = trigger.Conditions[eventIndex];
            if (condition == null || !editorConfig.TriggerEventTypes.TryGetValue(condition.ConditionIndex, out var definition))
                continue;

            int parameterCount = Math.Min(condition.Parameters.Length, definition.Parameters.Length);
            for (int parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                var parameter = definition.Parameters[parameterIndex];
                if (parameter == null || !IsReference(parameter.TriggerParamType))
                    continue;

                yield return new TriggerParameterReference(true, eventIndex, parameterIndex, definition.Name,
                    parameter.TriggerParamType, condition.Parameters[parameterIndex],
                    parameter.TriggerParamType == TriggerParamType.LocalVariable ? TriggerVariableAccess.Read : TriggerVariableAccess.Reference);
            }
        }

        for (int actionIndex = 0; actionIndex < trigger.Actions.Count; actionIndex++)
        {
            var action = trigger.Actions[actionIndex];
            if (action == null || !editorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var definition))
                continue;

            int parameterCount = Math.Min(action.Parameters.Length, definition.Parameters.Length);
            for (int parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                var parameter = definition.Parameters[parameterIndex];
                if (parameter == null || !IsReference(parameter.TriggerParamType))
                    continue;

                yield return new TriggerParameterReference(false, actionIndex, parameterIndex, definition.Name,
                    parameter.TriggerParamType, action.Parameters[parameterIndex], parameter.VariableAccess);
            }
        }
    }

    private static bool IsReference(TriggerParamType type) =>
        type is TriggerParamType.Trigger or TriggerParamType.Tag or TriggerParamType.LocalVariable or TriggerParamType.TeamType;
}
