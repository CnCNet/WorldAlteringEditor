namespace TSMapEditor.Models
{
    public class FuzzySearchItem<T>(T item, int score)
    {
        public T Item { get; set; } = item;
        public int Score { get; set; } = score;
    };
}
