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

        CellRect cellRect = CellRect.CenteredOn(groveCenter, 7, 7).ClipInsideMap(map);
        MapGenerator.SetVar("RectOfInterest", cellRect);
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
            c => CanSpawnTreeAt(c, map, requireFertility: true),
            map,
            out result);
    }

    private bool TryFindFallbackInitialCell(Map map, out IntVec3 result)
    {
        return CellFinderLoose.TryFindRandomNotEdgeCellWith(
            MinDistanceFromMapEdge,
            c => CanSpawnTreeAt(c, map, requireFertility: false),
            map,
            out result);
    }
}
