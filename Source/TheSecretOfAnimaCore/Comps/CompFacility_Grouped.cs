using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class CompFacility_Grouped : CompFacility
{
    public List<Thing> LinkedThings => linkedBuildings;

    public CompProperties_Facility_Grouped Props_Grouped => (CompProperties_Facility_Grouped)props;

    private List<StatModifier> qualityStatOffsets = new List<StatModifier>();

    public virtual List<StatModifier> QualityStatOffsets => qualityStatOffsets;

    // Only used if the facility uses statOffsetsPerQuality, so minimal safety checking needed 
    private CompQuality compQuality;
    public CompQuality CompQualityCached
    {
        get
        {
            if (compQuality == null)
            {
                compQuality = parent.TryGetComp<CompQuality>();
                if (compQuality == null)
                {
                    Log.Error("Trying to get CompQuality on a Thing that has none while calculating stat offsets for statOffsetsPerQuality");
                }
            }
            return compQuality;
        }
    }

    // No Harmony detour, only called by my PlaceWorker
    public static void DrawLinesToPotentialThingsToLinkTo_Grouped(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map, out List<Thing> potentialLinks)
    {
        potentialLinks = new List<Thing>();
        CompProperties_GroupedFacility compProperties = myDef.GetCompProperties<CompProperties_GroupedFacility>();
        if (compProperties?.linkableThingDefs == null)
            return;

        int max = compProperties.maxAffected > 0 ? compProperties.maxAffected : int.MaxValue;

        Vector3 myCenter = GenThing.TrueCenter(myPos, myRot, myDef.size, myDef.Altitude);

        for (int i = 0; i < compProperties.linkableThingDefs.Count; i++)
        {
            foreach (Thing item in map.listerThings.ThingsOfDef(compProperties.linkableThingDefs[i]))
            {
                CompAffectedByGroupedFacilities compAffectee = item.TryGetComp<CompAffectedByGroupedFacilities>();

                if (compAffectee != null &&
                    compAffectee.CanPotentiallyLinkTo(myDef, myPos, myRot))
                {
                    potentialLinks.Add(item);
                }
            }
        }

        if (potentialLinks.Count == 0)
            return;

        potentialLinks.Sort((a, b) =>
            Vector3.Distance(myCenter, a.TrueCenter())
            .CompareTo(Vector3.Distance(myCenter, b.TrueCenter())));

        int drawn = 0;

        foreach (Thing candidate in potentialLinks)
        {
            if (drawn >= max)
                break;

            Vector3 targetCenter = candidate.TrueCenter();

            GenDraw.DrawLineBetween(myCenter, targetCenter);

            CompAffectedByGroupedFacilities compAffectee = candidate.TryGetComp<CompAffectedByGroupedFacilities>();

            compAffectee?.DrawRedLineToPotentiallySupplantedFacility(myDef, myPos, myRot);

            drawn++;
        }
    }

    // Has Harmony detour
    public static void DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo_Grouped(float curX, ref float curY, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
    {
        CompProperties_Facility_Grouped compProperties = myDef.GetCompProperties<CompProperties_Facility_Grouped>();
        int num = 0;
        for (int i = 0; i < compProperties.linkableThingDefs.Count; i++)
        {
            foreach (Thing item in map.listerThings.ThingsOfDef(compProperties.linkableThingDefs[i]))
            {
                CompAffectedByFacilities_Grouped compAffectedByFacilities = item.TryGetComp<CompAffectedByFacilities_Grouped>();
                if (compAffectedByFacilities != null && compAffectedByFacilities.CanPotentiallyLinkTo(myDef, myPos, myRot))
                {
                    num++;
                    if (num == 1)
                    {
                        DrawTextLine(ref curY, "FacilityPotentiallyLinkedTo".Translate() + ":");
                    }
                    DrawTextLine(ref curY, "  - " + item.LabelCap);
                }
            }
        }
        if (num == 0)
        {
            DrawTextLine(ref curY, "FacilityNoPotentialLinks".Translate());
        }
        void DrawTextLine(ref float y, string text)
        {
            float lineHeight = Text.LineHeight;
            Widgets.Label(new Rect(curX, y, 999f, lineHeight), text);
            y += lineHeight;
        }
    }

    // Use vanilla CompTick()

    // Use vanilla CanLink()

    // Use vanilla Notify_NewLink

    // Use vanilla Notify_LinkRemoved

    // Use vanilla Notify_LinkRemoved

    // Use vanilla Notify_ThingChanged

    // Use vanilla PostSpawnSetup

    // Use vanilla PostMapInit
    
    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        thingsToNotify.Clear();
        for (int i = 0; i < linkedBuildings.Count; i++)
        {
            thingsToNotify.Add(linkedBuildings[i]);
        }
        UnlinkAll();
        foreach (Thing item in thingsToNotify)
        {
            item.TryGetComp<CompAffectedByFacilities_Grouped>().Notify_FacilityDespawned();
        }
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        for (int i = 0; i < linkedBuildings.Count; i++)
        {
            if (linkedBuildings[i].TryGetComp<CompAffectedByFacilities_Grouped>().IsFacilityActive(parent))
            {
                GenDraw.DrawLineBetween(parent.TrueCenter(), linkedBuildings[i].TrueCenter());
            }
            else
            {
                GenDraw.DrawLineBetween(parent.TrueCenter(), linkedBuildings[i].TrueCenter(), CompAffectedByFacilities_Grouped.InactiveFacilityLineMat);
            }
        }
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (StatOffsets != null)
        {
            bool flag = AmIActiveForAnyone();
            for (int i = 0; i < StatOffsets.Count; i++)
            {
                StatDef stat = StatOffsets[i].stat;
                stringBuilder.Append(stat.OffsetLabelCap);
                stringBuilder.Append(": ");
                stringBuilder.Append(StatOffsets[i].ValueToStringAsOffset);
                if (!flag)
                {
                    stringBuilder.Append(" (");
                    stringBuilder.Append("InactiveFacility".Translate());
                    stringBuilder.Append(")");
                }
                if (i < StatOffsets.Count - 1)
                {
                    stringBuilder.AppendLine();
                }
            }
            //stringBuilder.Append("\n");
        }
        CompProperties_Facility_Grouped compProperties_Facility_Grouped = Props_Grouped;
        if (compProperties_Facility_Grouped.showMaxSimultaneous)
        {
            string categoryTag = Props_Grouped.categoryTag;
            foreach (Thing linkedThing in linkedBuildings)
            {
                if (linkedThing == null || linkedThing.Destroyed)
                    continue;

                CompAffectedByFacilities_Grouped compAffected = linkedThing.TryGetComp<CompAffectedByFacilities_Grouped>();
                if (compAffected == null)
                    continue;

                FacilityLinkGroup group = compAffected.Props_Grouped.GetLinkGroupForTag(categoryTag);
                if (group == null)
                    continue;

                int count = 0;
                foreach (Thing facility in compAffected.LinkedFacilities)
                {
                    if (facility == null || facility.Destroyed)
                        continue;

                    CompFacility_Grouped compFacility = facility.TryGetComp<CompFacility_Grouped>();
                    if (compFacility != null && compFacility.Props_Grouped.categoryTag == categoryTag)
                    {
                        count++;
                    }
                }

                stringBuilder.AppendInNewLine($"{linkedThing.LabelCap} {group.label} {"TSOA_Linked".Translate()}: {count}/{group.maxLinks}");
            }
        }
        if (compProperties_Facility_Grouped.mustBePlacedFacingThingLinear && parent.Spawned && ContainmentUtility.IsLinearBuildingBlocked(parent.def, parent.Position, parent.Rotation, parent.Map))
        {
            stringBuilder.AppendInNewLine("FacilityFrontBlocked".Translate());
        }
        return stringBuilder.ToString().TrimEndNewlines();
    }

    // Use vanilla RelinkAll

    // Has Harmony detour
    internal void LinkToNearbyBuildings_Grouped()
    {
        UnlinkAll();

        if (Props_Grouped.linkableThingDefs == null)
            return;

        List<Thing> potentiallyAffected = new List<Thing>(); 

        foreach (ThingDef affectedDef in Props_Grouped.linkableThingDefs)
        {
            potentiallyAffected.AddRange(parent.Map.listerThings.ThingsOfDef(affectedDef));
        }

        potentiallyAffected = potentiallyAffected.Where(t =>
            {
                CompAffectedByGroupedFacilities comp = t.TryGetComp<CompAffectedByGroupedFacilities>();
                return comp != null && comp.CanLinkTo(parent);
            }).ToList();

        Vector3 center = parent.TrueCenter();

        potentiallyAffected.Sort((a, b) =>
            Vector3.Distance(center, a.TrueCenter())
            .CompareTo(Vector3.Distance(center, b.TrueCenter())));

        int linkLimit = Props_Grouped.maxAffected > 0 ? Props_Grouped.maxAffected : int.MaxValue;

        foreach (Thing target in potentiallyAffected.Take(linkLimit))
        {
            CompAffectedByGroupedFacilities comp = target.TryGetComp<CompAffectedByGroupedFacilities>();
            linkedBuildings.Add(target);
            comp.Notify_NewLink(parent);

            OnLinkAdded?.Invoke(this, target);
        }
    }

    // Has Harmony detour
    private bool AmIActiveForAnyone_Grouped()
    {
        for (int i = 0; i < linkedBuildings.Count; i++)
        {
            if (linkedBuildings[i].TryGetComp<CompAffectedByGroupedFacilities>().IsFacilityActive(parent))
            {
                return true;
            }
        }
        return false;
    }

    private void UnlinkAll()

    public bool IsLinked(Thing thing)
    {
        return LinkedThings.Contains(thing);
    }
}