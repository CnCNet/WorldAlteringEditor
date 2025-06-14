namespace TSMapEditor.Models
{
    public enum SpotlightType
    {
        /// <summary>
        /// No spotlight.
        /// </summary>
        None = 0,

        /// <summary>
        /// A spotlight that repeats its movement in reverse after completing an arc.
        /// </summary>
        Reciprocating = 1,

        /// <summary>
        /// A spotlight that moves in a circular loop.
        /// </summary>
        Loop = 2
    }
}
