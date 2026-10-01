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
    public CompProperties_Facility_Grouped Props_Grouped => (CompProperties_Facility_Grouped)props;

    private List<StatModifier> qualityStatOffsets = new List<StatModifier>();

    public override List<StatModifier> StatOffsets => qualityStatOffsets.Count > 0 ? qualityStatOffsets : base.StatOffsets;

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

    // Shared use by DrawLinesToPotentialThingsToLinkTo_Grouped and PlaceWorker
    public static IEnumerable<Thing> PotentialThingsToLinkTo_Grouped(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
    {
        CompProperties_Facility_Grouped compProperties = myDef.GetCompProperties<CompProperties_Facility_Grouped>();
        if (map == null || compProperties?.linkableThingDefs == null)
            yield break;

        for (int i = 0; i < compProperties.linkableThingDefs.Count; i++)
        {
            foreach (Thing item in map.listerThings.ThingsOfDef(compProperties.linkableThingDefs[i]))
            {
                CompAffectedByFacilities_Grouped compAffectee = item.TryGetComp<CompAffectedByFacilities_Grouped>();

                if (compAffectee != null &&
                    compAffectee.CanPotentiallyLinkTo(myDef, myPos, myRot))
                {
                    yield return item;
                }
            }
        }
    }

    // No Harmony detour, only called by my PlaceWorker
    public static void DrawLinesToPotentialThingsToLinkTo_Grouped(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map, out List<Thing> potentialLinks)
    {
        potentialLinks = PotentialThingsToLinkTo_Grouped(myDef, myPos, myRot, map).ToList();
        if (potentialLinks.Count == 0)
            return;

        CompProperties_Facility_Grouped compProperties = myDef.GetCompProperties<CompProperties_Facility_Grouped>();
        int max = compProperties.maxAffected > 0 ? compProperties.maxAffected : int.MaxValue;
        Vector3 myCenter = GenThing.TrueCenter(myPos, myRot, myDef.size, myDef.Altitude);

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

            CompAffectedByFacilities_Grouped compAffectee = candidate.TryGetComp<CompAffectedByFacilities_Grouped>();

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

    // Use base CompTick()

    // Use base CanLink()

    // Use base Notify_NewLink

    // Use base Notify_LinkRemoved

    // Use base Notify_LinkRemoved

    // Use base Notify_ThingChanged

    // Use base PostSpawnSetup

    // Use base PostMapInit
    
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
                foreach (Thing facility in compAffected.LinkedFacilitiesListForReading)
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

    // Use base RelinkAll

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
                CompAffectedByFacilities_Grouped comp = t.TryGetComp<CompAffectedByFacilities_Grouped>();
                return comp != null && comp.CanLinkTo(parent);
            }).ToList();

        Vector3 center = parent.TrueCenter();

        potentiallyAffected.Sort((a, b) =>
            Vector3.Distance(center, a.TrueCenter())
            .CompareTo(Vector3.Distance(center, b.TrueCenter())));

        int linkLimit = Props_Grouped.maxAffected > 0 ? Props_Grouped.maxAffected : int.MaxValue;

        foreach (Thing target in potentiallyAffected.Take(linkLimit))
        {
            CompAffectedByFacilities_Grouped comp = target.TryGetComp<CompAffectedByFacilities_Grouped>();
            comp.Notify_NewLink(parent);
            Notify_NewLink(target);
        }
    }

    // Has Harmony detour
    internal bool AmIActiveForAnyone_Grouped()
    {
        for (int i = 0; i < linkedBuildings.Count; i++)
        {
            if (linkedBuildings[i].TryGetComp<CompAffectedByFacilities_Grouped>().IsFacilityActive(parent))
            {
                return true;
            }
        }
        return false;
    }

    // Has Harmony detour
    internal void UnlinkAll_Grouped()
    {
        List<Thing> thingsToNotify = linkedBuildings.ToList();
        for (int i = 0; i < thingsToNotify.Count; i++)
        {
            thingsToNotify[i].TryGetComp<CompAffectedByFacilities_Grouped>().Notify_LinkRemoved(parent);
            Notify_LinkRemoved(thingsToNotify[i]);
        }
    }

    public bool IsLinked(Thing thing)
    {
        return linkedBuildings.Contains(thing);
    }

    public virtual void PostQualitySet()
    {
        SetStatOffsets();
    }

    private void SetStatOffsets()
    {
        qualityStatOffsets.Clear();

        Dictionary<StatDef, Dictionary<QualityCategory, float>> statOffsetsPerQuality = Props_Grouped.statOffsetsPerQuality;
        if (statOffsetsPerQuality != null)
        {
            CompQuality qualityComp = CompQualityCached;
            if (qualityComp == null)
                return;

            foreach (KeyValuePair<StatDef, Dictionary<QualityCategory, float>> item in statOffsetsPerQuality)
            {
                float offset = Props.statOffsets?.GetStatOffsetFromList(item.Key) ?? 0f;
                if (item.Value != null && item.Value.TryGetValue(qualityComp.Quality, out float qualityOffset))
                    offset = qualityOffset;

                qualityStatOffsets.Add(new StatModifier
                {
                    stat = item.Key,
                    value = offset
                });
            }
            return;
        }

        List<StatModifier> statModifiers = Props.statOffsets;
        if (statModifiers != null)
        {
            qualityStatOffsets = statModifiers.ToList();
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            SetStatOffsets();
        }
    }
}
