namespace TSMapEditor.Models
{
    public class Aircraft : Foot<AircraftType>
    {
        public Aircraft(AircraftType objectType) : base(objectType)
        {
        }

        public AircraftType AircraftType => ObjectType;

        // [Aircraft]
        // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

        public override RTTIType WhatAmI() => RTTIType.Aircraft;

        public override bool Remapable() => ObjectType.ArtConfig.Remapable;
    }
}
