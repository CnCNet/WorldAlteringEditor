using System;
using System.Collections.Generic;

namespace TSMapEditor.Mutations
{
    /// <summary>
    /// The Undo / Redo system.
    /// </summary>
    public class MutationManager
    {
        public const string MCPMutationOrigin = "MCP";

        public List<IMutation> UndoList { get; } = new List<IMutation>();
        public List<IMutation> RedoList { get; } = new List<IMutation>();

        public int Revision { get; private set; }

        private long nextMutationId = 1;

        /// <summary>
        /// Performs a new mutation on the map.
        /// </summary>
        /// <param name="mutation">The mutation to perform.</param>
        public void PerformMutation(IMutation mutation)
        {
            mutation.Perform();
            RedoList.Clear();
            UndoList.Add(mutation);
            Revision++;
        }

        /// <summary>
        /// Performs a new MCP mutation on the map and records information about its source.
        /// </summary>
        public void PerformMutation(Mutation mutation, string origin, string toolName, MutationAffectedCells affectedCells)
        {
            PerformMutation(mutation);

            mutation.SetHistoryMetadata(new MutationHistoryMetadata(nextMutationId, Revision,
                string.IsNullOrWhiteSpace(origin) ? MCPMutationOrigin : origin,
                string.IsNullOrWhiteSpace(toolName) ? mutation.GetType().Name : toolName,
                affectedCells ?? throw new ArgumentNullException(nameof(affectedCells))));

            nextMutationId++;
        }

        public bool CanUndo() => UndoList.Count > 0;

        /// <summary>
        /// Undoes the last mutation performed to the map, or the last chain of mutations
        /// if multiple mutations have the same <see cref="Mutation.EventID"/>.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo())
                return;

            int lastMutationEventId = UndoList[UndoList.Count - 1].EventID;

            if (lastMutationEventId < 0)
            {
                UndoOne();
                return;
            }

            while (CanUndo() && UndoList[UndoList.Count - 1].EventID == lastMutationEventId)
            {
                UndoOne();
            }
        }

        /// <summary>
        /// Returns the latest mutation performed to the map.
        /// </summary>
        public IMutation GetLatestMutation() => UndoList.Count > 0 ? UndoList[^1] : null;

        /// <summary>
        /// Undoes the last mutation performed to the map.
        /// </summary>
        public void UndoOne()
        {
            if (!CanUndo())
                return;

            int lastUndoIndex = UndoList.Count - 1;
            UndoList[lastUndoIndex].Undo();
            RedoList.Add(UndoList[lastUndoIndex]);
            UndoList.RemoveAt(lastUndoIndex);
            Revision++;
        }

        public bool CanRedo() => RedoList.Count > 0;

        /// <summary>
        /// Redoes the last un-done mutation on the map.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo())
                return;

            int lastRedoIndex = RedoList.Count - 1;
            RedoList[lastRedoIndex].Perform();
            UndoList.Add(RedoList[lastRedoIndex]);
            RedoList.RemoveAt(lastRedoIndex);
            Revision++;
        }

        public void ClearUndoAndRedoLists()
        {
            UndoList.Clear();
            RedoList.Clear();
        }
    }
}
