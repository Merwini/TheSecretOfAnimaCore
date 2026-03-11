using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace tsoa.core
{
    public class PlaceWorker_ShowGroupedFacilitiesConnections : PlaceWorker
    {
        ThingDef cachedDef;
        List<Thing> potentialLinksCache;
        int lastCacheTick = -1;
        IntVec3 lastCachePosition;
        Map lastCacheMap;

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
                CompGroupedFacility.DrawLinesToPotentialThingsToLinkTo(def, center, rot, map, out List<Thing> potentialLinks);
                cachedDef = def;
                lastCachePosition = center;
                lastCacheMap = map;
                potentialLinksCache = potentialLinks;
                lastCacheTick = Find.TickManager.TicksGame;
            }
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            ThingDef thingDef = def as ThingDef;
            if (thingDef == null)
            {
                Log.Error($"PlaceWorker_ShowGroupedFacilitiesConnections only works on ThingDefs. defName: {def.defName}");
                return false;
            }

            CompProperties_GroupedFacility compProps = thingDef.GetCompProperties<CompProperties_GroupedFacility>();
            if (compProps == null)
                return true; // either has CompProperties_AffectedByGroupedFacilities, or someone put this on a non-GroupedFacility ThingDef. Either way no reason to error

            if (compProps.canPlaceWithoutLink)
                return true; // nothing more needs to be checked

            if (map != lastCacheMap || center != lastCachePosition || def != cachedDef || Find.TickManager.TicksGame - lastCacheTick > 60)
                return false; // need to see how bad this is. Should only be false for a frame until DrawGhost refreshes the cache. Not optimal but way easier than having this method also able to recache.

            if (potentialLinksCache.NullOrEmpty())
            {
                return "TSOA_FacilityMustBeLinked".Translate();
            }

            return true;
        }
    }
}
