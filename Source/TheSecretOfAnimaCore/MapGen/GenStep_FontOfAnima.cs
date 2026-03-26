using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace tsoa.core;

public class GenStep_FontOfAnima : GenStep
{
    public override int SeedPart => 621764206;

    public override void Generate(Map map, GenStepParams parms)
    {
        IntVec3 mapCenter = map.Center;

        // TODO stress test and see if this can actually fail, make fallback if so
        DropCellFinder.TryFindDropSpotNear(mapCenter, map, out IntVec3 fontCell, false, false, false, new IntVec2(7, 7));

        GenSpawn.Spawn(TSOA_DefOf.TSOA_FontOfAnima, fontCell, map);
    }
}
