using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Versel


namespace tsoa.core;

public class PlaceWorker_AnimaTreeFacility : PlaceWorker_ShowGroupedFacilitiesConnections
{
    public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        IntVec3 c = center + IntVec3.South.RotatedBy(rot);
        IntVec3 c2 = center + IntVec3.North.RotatedBy(rot);
        if (c.Impassable(map) || c2.Impassable(map))
        {
            return "MustPlaceVentWithFreeSpaces".Translate();
        }
        return true;
    }
}
