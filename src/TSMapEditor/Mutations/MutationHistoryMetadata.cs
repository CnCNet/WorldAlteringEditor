using System;

namespace TSMapEditor.Mutations
{
    public class MutationAffectedCells
    {
        public MutationAffectedCells(bool isKnown, int? count, int? minX, int? minY, int? maxX, int? maxY)
        {
            IsKnown = isKnown;
            Count = count;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public bool IsKnown { get; }
        public int? Count { get; }
        public int? MinX { get; }
        public int? MinY { get; }
        public int? MaxX { get; }
        public int? MaxY { get; }

    }

    public class MutationHistoryMetadata
    {
        public MutationHistoryMetadata(long mutationId, int createdRevision, string origin, string toolName, MutationAffectedCells affectedCells)
        {
            MutationId = mutationId;
            CreatedRevision = createdRevision;
            Origin = origin;
            ToolName = toolName;
            AffectedCells = affectedCells ?? throw new ArgumentNullException(nameof(affectedCells));
        }

        public long MutationId { get; }
        public int CreatedRevision { get; }
        public string Origin { get; }
        public string ToolName { get; }
        public MutationAffectedCells AffectedCells { get; }
    }
}
