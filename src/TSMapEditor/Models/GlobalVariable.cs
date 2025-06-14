namespace TSMapEditor.Models
{
    public class GlobalVariable
    {
        public GlobalVariable(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public int Index { get; }
        public string Name { get; }
    }
}
