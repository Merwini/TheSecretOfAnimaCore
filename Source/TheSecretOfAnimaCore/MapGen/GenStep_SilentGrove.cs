using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class GenStep_SilentGrove : GenStep_SilentAnimaTrees
{
    public override int SeedPart => 195673492;

    public override void Generate(Map map, GenStepParams parms)
    {
        // try to find a patch of ground with >0 fertility. Fallback to allow any ground.
        // spawn the first silent tree
        // reuse methods from base class to spawn the rest

        if (!TryFindInitialCell(map, out IntVec3 groveCenter))
        {
            Log.Error("GenStep_SilentGrove could not find a valid initial cell.");
            return;
        }

        Thing firstTreeThing = GenSpawn.Spawn(TSOA_DefOf.TSOA_TreeAnimaSilent, groveCenter, map);
        if (firstTreeThing is Plant firstTreePlant)
        {
            firstTreePlant.Growth = 1;
        }

        SpawnTreesAround(groveCenter, map);
    }

    private bool TryFindInitialCell(Map map, out IntVec3 result)
    {
        if (TryFindInitialCellWithFertility(map, out result))
        {
            return true;
        }

        return TryFindFallbackInitialCell(map, out result);
    }

    private bool TryFindInitialCellWithFertility(Map map, out IntVec3 result)
    {
        return CellFinderLoose.TryFindRandomNotEdgeCellWith(
            MinDistanceFromMapEdge,
            c => CanSpawnTreeAt(c, groveCenter, map, requireFertility: true),
            map,
            out result);
    }

    private bool TryFindFallbackInitialCell(Map map, out IntVec3 result)
    {
        return CellFinderLoose.TryFindRandomNotEdgeCellWith(
            MinDistanceFromMapEdge,
            c => CanSpawnTreeAt(c, groveCenter, map, requireFertility: false),
            map,
            out result);
    }

    public override bool CanSpawnTreeAt(IntVec3 c, IntVec3 animaTreePos, Map map, bool requireFertility = true)
    {
        if (!c.Standable(map))
            return false;

        if (c.Fogged(map))
            return false;

        if (c.Roofed(map))
            return false;

        if (!c.GetRoom(map).PsychologicallyOutdoors)
            return false;

        if (c.DistanceToEdge(map) < MinDistanceFromMapEdge)
            return false;

        if (c.GetTerrain(map).avoidWander)
            return false;

        if (requireFertility && c.GetFertility(map) <= 0f)
            return false;

        List<Thing> thingList = c.GetThingList(map);
        for (int i = 0; i < thingList.Count; i++)
        {
            Thing t = thingList[i];
            if (t.def == t.def == TSOA_DefOf.TSOA_TreeAnimaSilent)
                return false;
        }

        if (GenRadial.RadialDistinctThingsAround(c, map, MinDistanceBetweenSilentTrees, useCenter: false)
            .Any(t => t.def == TSOA_DefOf.TSOA_TreeAnimaSilent))
        {
            return false;
        }

        float dist = c.DistanceTo(center);
        if (dist < MinRadius || dist > MaxRadius + 1f)
            return false;

        return true;
    }
}
