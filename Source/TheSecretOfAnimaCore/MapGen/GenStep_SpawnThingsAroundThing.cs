using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace tsoa.core;

public class GenStep_SpawnThingsAroundThing : GenStep
{
    protected const int MaxAttemptsPerThing = 80;
    protected const int MinDistanceFromMapEdge = 10;

    public ThingDef focusThing;
    public ThingDef spawnThing;
    public int spawnMin = 4;
    public int spawnMax = 6;
    public float minRadius = 2.5f;
    public float maxRadius = 6.5f;
    public int minDistanceBetweenThings = 2;

    public override int SeedPart => 184672391;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (map.Biome.isExtremeBiome)
            return;

        List<Thing> focuses = map.listerThings.ThingsOfDef(focusThing);
        if (focuses.NullOrEmpty())
            return;

        for (int i = 0; i < focuses.Count; i++)
        {
            Thing focus = focuses[i];
            if (focus == null || focus.Destroyed)
                continue;

            SpawnThingsAround(focus.Position, map);
        }
    }

    public void SpawnThingsAround(IntVec3 center, Map map)
    {
        int targetCount = Rand.RangeInclusive(spawnMin, spawnMax);
        int spawned = 0;
        int attempts = 0;

        while (spawned < targetCount && attempts < MaxAttemptsPerThing)
        {
            attempts++;

            IntVec3 cell = CellInAnnulus(center, minRadius, maxRadius);

            if (!CanSpawnThingAt(cell, map))
                continue;

            List<Thing> thingsInCell = cell.GetThingList(map);
            for (int i = thingsInCell.Count - 1; i >= 0; i--)
            {
                thingsInCell[i].Destroy();
            }

            Thing thing = GenSpawn.Spawn(spawnThing, cell, map);
            if (thing is Plant plant)
            {
                plant.Growth = 1;
            }

            spawned++;
            attempts = 0;
        }
    }

    public IntVec3 CellInAnnulus(IntVec3 center, float minRadius, float maxRadius)
    {
        float angle = Rand.Range(0f, 360f);
        float radius = Rand.Range(minRadius, maxRadius);

        Vector3 offset = Vector3Utility.HorizontalVectorFromAngle(angle) * radius;
        return center + offset.ToIntVec3();
    }

    public virtual bool CanSpawnThingAt(IntVec3 c, Map map, bool requireFertility = true)
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

        if (requireFertility && c.GetFertility(map) <= 0f)
            return false;

        List<Thing> things = c.GetThingList(map);
        for (int i = 0; i < things.Count; i++)
        {
            Thing t = things[i];
            // TODO maybe add more things for it not to replace
            if (t.def == spawnThing || t.def == focusThing)
                return false;
        }

        if (GenRadial.RadialDistinctThingsAround(c, map, minDistanceBetweenThings, useCenter: false)
            .Any(t => t.def == spawnThing))
        {
            return false;
        }

        return true;
    }
}
