using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.AIMutations;

public sealed class DeleteMapObjectsMutation : Mutation
{
    public DeleteMapObjectsMutation(IMutationTarget mutationTarget, List<GameObject> objects) : base(mutationTarget)
    {
        this.objects = objects ?? throw new ArgumentNullException(nameof(objects));
        if (objects.Count == 0)
            throw new ArgumentException("At least one map object must be provided.", nameof(objects));
    }

    private readonly List<GameObject> objects;

    public override string GetDisplayString()
    {
        return $"Delete {objects.Count} map object(s)";
    }

    public override void Perform()
    {
        foreach (var mapObject in objects)
        {
            RemoveObject(mapObject);
            MutationTarget.AddRefreshPoint(mapObject.Position);
        }
    }

    public override void Undo()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            RestoreObject(objects[i]);
            MutationTarget.AddRefreshPoint(objects[i].Position);
        }
    }

    private void RemoveObject(GameObject mapObject)
    {
        switch (mapObject)
        {
            case Structure structure:
                Map.RemoveBuilding(structure);
                break;
            case Unit unit:
                Map.RemoveUnit(unit);
                break;
            case Infantry infantry:
                Map.RemoveInfantry(infantry);
                break;
            case Aircraft aircraft:
                Map.RemoveAircraft(aircraft);
                break;
            case TerrainObject terrainObject:
                Map.RemoveTerrainObject(terrainObject);
                break;
            default:
                throw new InvalidOperationException($"Cannot delete map object of type {mapObject.WhatAmI()}.");
        }
    }

    private void RestoreObject(GameObject mapObject)
    {
        switch (mapObject)
        {
            case Structure structure:
                Map.PlaceBuilding(structure);
                break;
            case Unit unit:
                Map.PlaceUnit(unit);
                break;
            case Infantry infantry:
                Map.PlaceInfantry(infantry);
                break;
            case Aircraft aircraft:
                Map.PlaceAircraft(aircraft);
                break;
            case TerrainObject terrainObject:
                Map.AddTerrainObject(terrainObject);
                break;
            default:
                throw new InvalidOperationException($"Cannot restore map object of type {mapObject.WhatAmI()}.");
        }
    }
}
