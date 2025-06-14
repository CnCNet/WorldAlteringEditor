namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    /// <summary>
    /// Defines what kind of comparison to use when comparing the height of a cell
    /// to the height of another cell.
    /// </summary>
    public enum HeightComparisonType
    {
        /// <summary>
        /// The height of the other cell is irrelevant for the resulting ramp type.
        /// </summary>
        Irrelevant,

        /// <summary>
        /// The other cell must be higher by 1 level.
        /// </summary>
        Higher,

        /// <summary>
        /// The other cell must be higher by 2 or more levels.
        /// </summary>
        MuchHigher,

        /// <summary>
        /// The other cell must be higher (by 1 level) or equal.
        /// </summary>
        HigherOrEqual,

        /// <summary>
        /// The other cell must be lower by 1 level.
        /// </summary>
        Lower,

        /// <summary>
        /// The other cell must be lower by 2 or more levels.
        /// </summary>
        MuchLower,

        /// <summary>
        /// The other cell must be lower or equal.
        /// </summary>
        LowerOrEqual,

        /// <summary>
        /// The other cell must be equal.
        /// </summary>
        Equal
    }
}
