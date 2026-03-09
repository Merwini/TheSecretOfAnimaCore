using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class GenStep_SilentAnimaTrees : GenStep
{
    private const int MinPerTree = 4;
    private const int MaxPerTree = 6;

    private const float MinRadius = 2.5f;
    private const float MaxRadius = 6.5f;

    private const int MaxAttemptsPerTree = 80;
    private const int MinDistanceBetweenTrees = 2;
    private const int MinDistanceFromMapEdge = 10;

    public override int SeedPart => 184672391;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (map.Biome.isExtremeBiome)
            return;

        List<Thing> animaTrees = map.listerThings.ThingsOfDef(ThingDefOf.Plant_TreeAnima);
        if (animaTrees.NullOrEmpty())
            return;

        for (int i = 0; i < animaTrees.Count; i++)
        {
            Thing animaTree = animaTrees[i];
            if (animaTree == null || animaTree.Destroyed)
                continue;

            SpawnTreesAround(animaTree.Position, map);
        }
    }

    public void SpawnTreesAround(IntVec3 center, Map map)
    {
        int targetCount = Rand.RangeInclusive(MinPerTree, MaxPerTree);
        int spawned = 0;
        int attempts = 0;

        while (spawned < targetCount && attempts < MaxAttemptsPerTree)
        {
            attempts++;

            IntVec3 cell = CellInAnnulus(center, MinRadius, MaxRadius);

            if (!CanSpawnTreeAt(cell, center, map))
                continue;

            Thing thing = GenSpawn.Spawn(TSOA_DefOf.TSOA_TreeAnimaSilent, cell, map);
            if (thing is Plant plant)
            {
                plant.Growth = 1;
            }

            spawned++;
        }
    }

    // TODO is picking random cells wrong? Should I make a list of all possible cells, shuffle it, and then go down the list?
    public IntVec3 CellInAnnulus(IntVec3 center, float minRadius, float maxRadius)
    {
        float angle = Rand.Range(0f, 360f);
        float radius = Rand.Range(minRadius, maxRadius);

        Vector3 offset = Vector3Utility.HorizontalVectorFromAngle(angle) * radius;
        return center + offset.ToIntVec3();
    }

    public bool CanSpawnTreeAt(IntVec3 c, IntVec3 animaTreePos, Map map)
    {
        if (!c.InBounds(map))
            return false;

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

        if (c.GetFertility(map) <= 0f)
            return false;

        // probably just wasted calculations, since we're centering on the anima tree either way
        //if (!map.reachability.CanReachFactionBase(c, map.ParentFaction))
        //    return false;

        List<Thing> things = c.GetThingList(map);
        for (int i = 0; i < things.Count; i++)
        {
            Thing t = things[i];
            // TODO maybe add more things for it not to replace
            if (t.def == TSOA_DefOf.TSOA_TreeAnimaSilent || t.def == ThingDefOf.Plant_TreeAnima)
                return false;
        }

        if (GenRadial.RadialDistinctThingsAround(c, map, MinDistanceBetweenTrees, useCenter: false)
            .Any(t => t.def == TSOA_DefOf.TSOA_TreeAnimaSilent))
        {
            return false;
        }

        float dist = c.DistanceTo(animaTreePos);
        if (dist < MinRadius || dist > MaxRadius + 1f)
            return false;

        return true;
    }
}
