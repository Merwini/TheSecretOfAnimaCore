using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace tsoa.core;

public class PlaceWorker_ShowGroupedFacilitiesConnections : PlaceWorker
{
    public override void DrawPlaceMouseAttachments(float curX, ref float curY, BuildableDef bdef, IntVec3 center, Rot4 rot)
    {
        if (bdef is ThingDef thingDef)
        {
            Map map = Find.CurrentMap;
            if (thingDef.HasComp(typeof(CompAffectedByFacilities_Grouped)))
            {
                CompAffectedByFacilities_Grouped.DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo(curX, ref curY, thingDef, center, rot, map);
            }
            else
            {
                CompFacility_Grouped.DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo(curX, ref curY, thingDef, center, rot, map);
            }
        }
    }

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        Map map = Find.CurrentMap;

        if (def.HasComp(typeof(CompAffectedByFacilities_Grouped)))
        {
            CompAffectedByFacilities_Grouped.DrawLinesToPotentialThingsToLinkTo(def, center, rot, map);
        }
        else
        {
            CompFacility_Grouped.DrawLinesToPotentialThingsToLinkTo_Grouped(def, center, rot, map, out _);
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

        CompProperties_Facility_Grouped compProps = thingDef.GetCompProperties<CompProperties_Facility_Grouped>();
        if (compProps == null)
            return true; // either has CompProperties_AffectedByFacilities_Grouped, or someone put this on a non-GroupedFacility ThingDef. Either way no reason to error

        if (compProps.canPlaceWithoutLink)
            return true; // nothing more needs to be checked

        if (!CompFacility_Grouped.PotentialThingsToLinkTo_Grouped(thingDef, center, rot, map).Any())
        {
            return "TSOA_FacilityMustBeLinked".Translate();
        }

        return true;
    }
}
