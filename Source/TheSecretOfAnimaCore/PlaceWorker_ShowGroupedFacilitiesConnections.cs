using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace nuff.tsoa.core
{
    public class PlaceWorker_ShowGroupedFacilitiesConnections : PlaceWorker
    {
        public override void DrawPlaceMouseAttachments(float curX, ref float curY, BuildableDef bdef, IntVec3 center, Rot4 rot)
        {
            if (bdef is ThingDef thingDef)
            {
                Map map = Find.CurrentMap;
                if (thingDef.HasComp(typeof(CompAffectedByGroupedFacilities)))
                {
                    CompAffectedByGroupedFacilities.DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo(curX, ref curY, thingDef, center, rot, map);
                }
                else
                {
                    CompGroupedFacility.DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo(curX, ref curY, thingDef, center, rot, map);
                }
            }
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map map = Find.CurrentMap;

            if (def.HasComp(typeof(CompAffectedByGroupedFacilities)))
            {
                CompAffectedByGroupedFacilities.DrawLinesToPotentialThingsToLinkTo(def, center, rot, map);
            }
            else
            {
                CompGroupedFacility.DrawLinesToPotentialThingsToLinkTo(def, center, rot, map);
            }
        }
    }
}
