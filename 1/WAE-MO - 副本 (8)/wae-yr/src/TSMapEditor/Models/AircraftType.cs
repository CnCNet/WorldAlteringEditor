using TSMapEditor.Models.ArtConfig;

namespace TSMapEditor.Models
{
    public class AircraftType : TechnoType, IArtConfigContainer
    {
        public AircraftType(string iniName) : base(iniName)
        {
        }

        public AircraftArtConfig ArtConfig { get; private set; } = new AircraftArtConfig();
        public IArtConfig GetArtConfig() => ArtConfig;

        public override RTTIType WhatAmI() => RTTIType.AircraftType;
    }
}
