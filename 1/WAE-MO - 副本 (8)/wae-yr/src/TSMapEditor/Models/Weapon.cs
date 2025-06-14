namespace TSMapEditor.Models
{
    public class Weapon : INIDefineable, INIDefined
    {
        public Weapon(string iniName)
        {
            ININame = iniName;
        }

        public string ININame { get; }

        public int Index { get; set; }

        public double Range { get; set; }
    }
}
