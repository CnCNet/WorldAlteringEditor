using System.Collections.Generic;

namespace TSMapEditor.Mutations
{
    /// <summary>
    /// The Undo / Redo system.
    /// </summary>
    public class MutationManager
    {
        public List<Mutation> UndoList { get; } = new List<Mutation>();
        public List<Mutation> RedoList { get; } = new List<Mutation>();

        /// <summary>
        /// Performs a new mutation on the map.
        /// </summary>
        /// <param name="mutation">The mutation to perform.</param>
        public void PerformMutation(Mutation mutation)
        {
            mutation.Perform();
            RedoList.Clear();
            UndoList.Add(mutation);
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
        }

        public void ClearUndoAndRedoLists()
        {
            UndoList.Clear();
            RedoList.Clear();
        }
    }
}
