using MapEditorLibrary.Models.ArtConfig;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.Models;

public class InfantryType : TechnoType, IArtConfigContainer
{
    public InfantryType(string iniName) : base(iniName)
    {
    }

    public InfantryArtConfig ArtConfig { get; } = new InfantryArtConfig();
    public IArtConfig GetArtConfig() => ArtConfig;

    public override RTTIType WhatAmI() => RTTIType.InfantryType;
}
