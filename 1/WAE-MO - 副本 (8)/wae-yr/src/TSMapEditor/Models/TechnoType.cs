using System;

namespace TSMapEditor.Models
{
    public abstract class TechnoType : GameObjectType
    {
        public TechnoType(string iniName) : base(iniName)
        {
        }

        public string Image { get; set; }
        public string Owner { get; set; }
        public bool NoShadow { get; set; }

        public int Cost { get; set; }

        public Weapon Primary { get; set; }
        public Weapon Secondary { get; set; }
        public Weapon ElitePrimary { get; set; }
        public Weapon EliteSecondary { get; set; }
        public int WeaponCount { get; set; }

        public double GuardRange { get; set; }

        public bool GapGenerator { get; set; }
        public int GapRadiusInCells { get; set; }

        public double GetWeaponRange()
        {
            double primaryRange = Primary?.Range ?? 0.0;
            double secondaryRange = Secondary?.Range ?? 0.0;

            return Math.Max(primaryRange, secondaryRange);
        }
    }
}
