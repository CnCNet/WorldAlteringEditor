namespace TSMapEditor.Models
{
    public abstract class AbstractObject : INIDefineable
    {
        public abstract RTTIType WhatAmI();

        public bool IsTechno()
        {
            RTTIType rtti = WhatAmI();

            return rtti == RTTIType.Aircraft || 
                   rtti == RTTIType.Infantry || 
                   rtti == RTTIType.Unit || 
                   rtti == RTTIType.Building;
        }

        public bool IsTechnoType()
        {
            RTTIType rtti = WhatAmI();

            return rtti == RTTIType.AircraftType ||
                   rtti == RTTIType.InfantryType ||
                   rtti == RTTIType.UnitType ||
                   rtti == RTTIType.BuildingType;
        }

        public bool IsFoot()
        {
            RTTIType rtti = WhatAmI();

            return rtti == RTTIType.Aircraft ||
                   rtti == RTTIType.Infantry ||
                   rtti == RTTIType.Unit;
        }

        public virtual AbstractObject Clone()
        {
            return (AbstractObject)MemberwiseClone();
        }
    }
}
