using MapEditorLibrary.Models;
using System.Globalization;

namespace MapEditorMCP.Scripting;

internal sealed class ScriptingIdAllocator
{
    public ScriptingIdAllocator(Map map)
    {
        this.map = map;
    }

    private readonly Map map;
    private readonly HashSet<string> reservedInternalIds = new HashSet<string>();
    private readonly HashSet<int> reservedLocalVariableIndices = new HashSet<int>();

    public string AllocateInternalId()
    {
        int candidate = 1_000_000;

        while (true)
        {
            string candidateString = "0" + candidate.ToString(CultureInfo.InvariantCulture);
            if (!InternalIdExists(candidateString) && reservedInternalIds.Add(candidateString))
                return candidateString;

            candidate++;
        }
    }

    public int AllocateLocalVariableIndex()
    {
        int candidate = 0;
        while (map.LocalVariables.Exists(variable => variable.Index == candidate) ||
               reservedLocalVariableIndices.Contains(candidate))
        {
            candidate++;
        }

        reservedLocalVariableIndices.Add(candidate);
        return candidate;
    }

    public void ReserveLocalVariableIndex(int index)
    {
        reservedLocalVariableIndices.Add(index);
    }

    private bool InternalIdExists(string id)
    {
        return map.TaskForces.Exists(taskForce => taskForce.ININame == id) ||
               map.Scripts.Exists(script => script.ININame == id) ||
               map.TeamTypes.Exists(teamType => teamType.ININame == id) ||
               map.Triggers.Exists(trigger => trigger.ID == id) ||
               map.Tags.Exists(tag => tag.ID == id) ||
               map.AITriggerTypes.Exists(aiTrigger => aiTrigger.ININame == id) ||
               map.LoadedINI?.SectionExists(id) == true;
    }
}
