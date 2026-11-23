using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI.CursorActions;

/// <summary>
/// Helper for mirrored placement of technos on the map.
/// </summary>
public class MirroredTechnoPlacement
{
    private readonly record struct Target(Point2D Origin, Point2D[] Foundation, MirrorTransform Transform, SubCell SubCell, bool CanPlace);

    public MirroredTechnoPlacement(Map map, MapMirroring mirroring, TechnoBase template, Point2D origin, bool overlap)
    {
        Point2D[] offsets = GetFoundation(template);
        Point2D doubledMirroringPoint = mirroring.GetDoubledMirroringPoint(map.Size);
        var foundationCenterOffset = new Point2D(offsets.Min(p => p.X) + offsets.Max(p => p.X),
            offsets.Min(p => p.Y) + offsets.Max(p => p.Y));
        Point2D doubledFoundationCenter = origin.ScaleBy(2) + foundationCenterOffset;

        var addedOrigins = new HashSet<Point2D>();
        var occupied = new HashSet<Point2D>();
        var targets = new List<Target>();

        foreach (MirrorTransform transform in mirroring.Transforms)
        {
            Point2D reflectedCenter = MapMirroring.TransformDoubledPosition(doubledFoundationCenter, doubledMirroringPoint, transform);
            Point2D doubledOrigin = reflectedCenter - foundationCenterOffset;

            // Keep the building's game-defined foundation. If an axis swaps
            // the dimensions, align its center to the nearest cell location.
            var targetOrigin = new Point2D((int)Math.Floor(doubledOrigin.X / 2.0), (int)Math.Floor(doubledOrigin.Y / 2.0));
            if (!addedOrigins.Add(targetOrigin))
                continue;

            Point2D[] foundation = offsets.Select(offset => targetOrigin + offset).ToArray();
            bool valid = map.GetTile(targetOrigin) != null && foundation.All(cell => map.GetTile(cell) != null)
                && map.CanPlaceObjectAt(template, targetOrigin, false, overlap)
                && (overlap || !foundation.Any(occupied.Contains));

            if (valid && !overlap)
                occupied.UnionWith(foundation);

            SubCell subCell = template is Infantry && valid ? map.GetTileOrFail(targetOrigin).GetFreeSubCellSpot() : SubCell.None;
            targets.Add(new Target(targetOrigin, foundation, transform, subCell, valid));
        }

        this.targets = targets;
    }

    private readonly IReadOnlyList<Target> targets;

    public IEnumerable<(Point2D Cell, bool CanPlace)> GetPreview() =>
        targets.SelectMany(target => target.Foundation.Select(cell => (Cell: cell, target.CanPlace)))
            .GroupBy(entry => entry.Cell)
            .Select(group => (group.Key, group.Any(entry => entry.CanPlace)));

    public void Perform(ICursorActionTarget actionTarget, TechnoBase template)
    {
        IMutationTarget mutationTarget = actionTarget.MutationTarget;
        var mutations = new List<IMutation>(targets.Count);

        foreach (Target target in targets.Where(target => target.CanPlace))
        {
            var techno = (TechnoBase)template.Clone();
            techno.Position = target.Origin;
            techno.Owner = mutationTarget.ObjectOwner;
            techno.Facing = target.Transform.ApplyToFacing(template.Facing);

            if (techno is Structure building)
                building.AIRepairable = building.ObjectType.Repairable && building.Owner.DefaultRepairableStructures;

            if (techno is Infantry infantry)
                infantry.SubCell = target.SubCell;

            mutations.Add(techno switch
            {
                Structure structure => new PlaceBuildingMutation(mutationTarget, structure),
                Unit unit => new PlaceVehicleMutation(mutationTarget, unit),
                Infantry placedInfantry => new PlaceInfantryMutation(mutationTarget, placedInfantry),
                Aircraft aircraft => new PlaceAircraftMutation(mutationTarget, aircraft),
                _ => throw new NotSupportedException($"Mirroring does not support {techno.WhatAmI()}.")
            });
        }

        if (mutations.Count > 0)
            actionTarget.MutationManager.PerformMutation(new MirroredPlacementMutation(mutationTarget, mutations, Mutation.EventIdNone));
    }

    private static Point2D[] GetFoundation(TechnoBase template)
    {
        if (template is not Structure building)
            return new[] { Point2D.Zero };

        var offsets = new List<Point2D>();
        building.ObjectType.ArtConfig.DoForFoundationCoordsOrOrigin(offsets.Add);
        return offsets.ToArray();
    }
}
