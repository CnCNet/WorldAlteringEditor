using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.AIMutations;

public sealed class TechnoPropertiesSnapshot
{
    public static TechnoPropertiesSnapshot Capture(TechnoBase techno)
    {
        var snapshot = new TechnoPropertiesSnapshot
        {
            RTTI = techno.WhatAmI(),
            Owner = techno.Owner,
            Health = techno.HP,
            Facing = techno.Facing,
            AttachedTag = techno.AttachedTag
        };

        switch (techno)
        {
            case Unit unit:
                snapshot.CaptureFoot(unit);
                break;
            case Infantry infantry:
                snapshot.CaptureFoot(infantry);
                break;
            case Aircraft aircraft:
                snapshot.CaptureFoot(aircraft);
                break;
            case Structure structure:
                snapshot.AISellable = structure.AISellable;
                snapshot.AIRebuildable = structure.AIRebuildable;
                snapshot.Powered = structure.Powered;
                snapshot.AIRepairable = structure.AIRepairable;
                snapshot.Nominal = structure.Nominal;
                snapshot.Spotlight = structure.Spotlight;
                snapshot.Upgrades = structure.Upgrades.ToArray();
                break;
            default:
                throw new ArgumentException($"Unsupported techno type {techno.WhatAmI()}.", nameof(techno));
        }

        return snapshot;
    }

    public RTTIType RTTI { get; private set; }
    public House Owner { get; set; }
    public int Health { get; set; }
    public byte Facing { get; set; }
    public Tag AttachedTag { get; set; }

    public string Mission { get; set; }
    public int Veterancy { get; set; }
    public int Group { get; set; }
    public bool OnBridge { get; set; }
    public bool AutocreateNoRecruitable { get; set; }
    public bool AutocreateYesRecruitable { get; set; }

    public bool AISellable { get; set; }
    public bool AIRebuildable { get; set; }
    public bool Powered { get; set; }
    public bool AIRepairable { get; set; }
    public bool Nominal { get; set; }
    public SpotlightType Spotlight { get; set; }
    public BuildingType[] Upgrades { get; set; }

    public void ApplyTo(TechnoBase techno)
    {
        if (techno.WhatAmI() != RTTI)
            throw new InvalidOperationException($"Cannot apply {RTTI} properties to {techno.WhatAmI()}.");

        techno.Owner = Owner;
        techno.HP = Health;
        techno.Facing = Facing;
        techno.AttachedTag = AttachedTag;

        switch (techno)
        {
            case Unit unit:
                ApplyFoot(unit);
                break;
            case Infantry infantry:
                ApplyFoot(infantry);
                break;
            case Aircraft aircraft:
                ApplyFoot(aircraft);
                break;
            case Structure structure:
                structure.AISellable = AISellable;
                structure.AIRebuildable = AIRebuildable;
                structure.Powered = Powered;
                structure.AIRepairable = AIRepairable;
                structure.Nominal = Nominal;
                structure.Spotlight = Spotlight;
                Array.Copy(Upgrades, structure.Upgrades, structure.Upgrades.Length);
                structure.UpdatePowerUpAnims();
                break;
        }
    }

    public bool HasSameValuesAs(TechnoPropertiesSnapshot other)
    {
        if (other == null || RTTI != other.RTTI || Owner != other.Owner || Health != other.Health || Facing != other.Facing || AttachedTag != other.AttachedTag)
            return false;

        if (RTTI == RTTIType.Building)
        {
            return AISellable == other.AISellable && AIRebuildable == other.AIRebuildable && Powered == other.Powered &&
                AIRepairable == other.AIRepairable && Nominal == other.Nominal && Spotlight == other.Spotlight && Upgrades.SequenceEqual(other.Upgrades);
        }

        return Mission == other.Mission && Veterancy == other.Veterancy && Group == other.Group && OnBridge == other.OnBridge &&
            AutocreateNoRecruitable == other.AutocreateNoRecruitable && AutocreateYesRecruitable == other.AutocreateYesRecruitable;
    }

    private void CaptureFoot<T>(Foot<T> foot) where T : TechnoType
    {
        Mission = foot.Mission;
        Veterancy = foot.Veterancy;
        Group = foot.Group;
        OnBridge = foot.High;
        AutocreateNoRecruitable = foot.AutocreateNoRecruitable;
        AutocreateYesRecruitable = foot.AutocreateYesRecruitable;
    }

    private void ApplyFoot<T>(Foot<T> foot) where T : TechnoType
    {
        foot.Mission = Mission;
        foot.Veterancy = Veterancy;
        foot.Group = Group;
        foot.High = OnBridge;
        foot.AutocreateNoRecruitable = AutocreateNoRecruitable;
        foot.AutocreateYesRecruitable = AutocreateYesRecruitable;
    }
}

public sealed class TechnoPropertyChange
{
    public TechnoPropertyChange(TechnoBase techno, TechnoPropertiesSnapshot oldProperties, TechnoPropertiesSnapshot newProperties)
    {
        Techno = techno;
        OldProperties = oldProperties;
        NewProperties = newProperties;
    }

    public TechnoBase Techno { get; }
    public TechnoPropertiesSnapshot OldProperties { get; }
    public TechnoPropertiesSnapshot NewProperties { get; }
}

public sealed class ModifyTechnosMutation : Mutation
{
    public ModifyTechnosMutation(IMutationTarget mutationTarget, List<TechnoPropertyChange> changes) : base(mutationTarget)
    {
        this.changes = changes ?? throw new ArgumentNullException(nameof(changes));
        if (changes.Count == 0)
            throw new ArgumentException("At least one techno property change must be provided.", nameof(changes));
    }

    private readonly List<TechnoPropertyChange> changes;

    public override string GetDisplayString()
    {
        return $"Modify properties of {changes.Count} techno object(s)";
    }

    public override void Perform()
    {
        foreach (var change in changes)
            ApplyProperties(change.Techno, change.NewProperties);
    }

    public override void Undo()
    {
        for (int i = changes.Count - 1; i >= 0; i--)
            ApplyProperties(changes[i].Techno, changes[i].OldProperties);
    }

    private void ApplyProperties(TechnoBase techno, TechnoPropertiesSnapshot properties)
    {
        bool refreshLighting = techno is Structure structure && structure.Powered != properties.Powered && structure.ObjectType.LightIntensity != 0.0;
        properties.ApplyTo(techno);
        MutationTarget.AddRefreshPoint(techno.Position);

        if (refreshLighting)
            Map.RefreshCellLighting(MutationTarget.LightingPreviewState, MutationTarget.LightDisabledLightSources, ((Structure)techno).LitTiles);
    }
}
