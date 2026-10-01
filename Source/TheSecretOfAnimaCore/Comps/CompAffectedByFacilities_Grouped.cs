using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class CompAffectedByFacilities_Grouped : CompAffectedByFacilities
{
    public CompProperties_AffectedByFacilities_Grouped Props_Grouped => (CompProperties_AffectedByFacilities_Grouped)props;

    private static readonly Dictionary<string, int> alreadyReturnedCount_ByTag = new Dictionary<string, int>();


    // Use base ThingsICanLinkTo

    // Has Harmony detour
    public bool CanLinkTo_Grouped(Thing facility)
    {
        if (!facility.TryGetComp(out CompFacility_Grouped comp))
        {
            return false;
        }
        if (!comp.CanLink())
        {
            return false;
        }
        if (!CanPotentiallyLinkTo(facility.def, facility.Position, facility.Rotation))
        {
            return false;
        }
        if (!IsValidFacilityForMe(facility))
        {
            return false;
        }
        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            if (linkedFacilities[i] == facility)
            {
                return false;
            }
        }
        return true;
    }

    // Use base CanPotentiallyLinkTo_Static

    // Has Harmony detour
    public bool CanPotentiallyLinkTo_Grouped(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot)
    {
        if (!CanPotentiallyLinkTo_Static(facilityDef, facilityPos, facilityRot, parent.def, parent.Position, parent.Rotation, parent.Map))
        {
            return false;
        }
        if (!IsPotentiallyValidFacilityForMe(facilityDef, facilityPos, facilityRot))
        {
            return false;
        }

        CompProperties_Facility_Grouped facilityProps = facilityDef.GetCompProperties<CompProperties_Facility_Grouped>();
        if (facilityProps == null)
        {
            return false;
        }

        string tag = facilityProps.categoryTag;
        if (tag.NullOrEmpty())
        {
            return false;
        }

        int countInSameGroup = 0;

        bool closerThanExisting = false;
        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            Thing linked = linkedFacilities[i];
            if (linked == null || linked.Destroyed)
                continue;

            CompFacility_Grouped compGrouped = linked.TryGetComp<CompFacility_Grouped>();
            if (compGrouped == null)
                continue;

            if (compGrouped.Props_Grouped.categoryTag == tag)
            {
                countInSameGroup++;

                if (IsBetter(facilityDef, facilityPos, facilityRot, linked))
                {
                    closerThanExisting = true;
                    break;
                }
            }
        }

        int facilityExistingLinks = 0;

        if (facilityProps.maxAffected > 0)
        {
            if (facilityPos.InBounds(parent.Map))
            {
                Thing facilityThing = facilityPos.GetThingList(parent.Map).FirstOrDefault(t => t.def == facilityDef);

                if (facilityThing != null)
                {
                    CompFacility_Grouped facilityComp = facilityThing.TryGetComp<CompFacility_Grouped>();
                    if (facilityComp != null)
                    {
                        facilityExistingLinks = facilityComp.LinkedBuildings.Count;
                    }
                }
            }
        }

        if (closerThanExisting)
        {
            return true;
        }

        FacilityLinkGroup relevantGroup = Props_Grouped.GetLinkGroupForTag(tag);
        if (relevantGroup == null)
        {
            return false;
        }

        if (countInSameGroup + 1 > relevantGroup.maxLinks)
        {
            return false;
        }

        if (facilityProps.maxAffected > 0 && facilityExistingLinks + 1 > facilityProps.maxAffected)
        {
            return false;
        }

        return true;
    }

    // Has Harmony detour
    public static bool CanPotentiallyLinkTo_Static_Grouped(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map myMap)
    {
        CompProperties_Facility_Grouped compProperties = facilityDef.GetCompProperties<CompProperties_Facility_Grouped>();
        if (compProperties == null)
        {
            return false;
        }

        if (compProperties.mustBePlacedAdjacent)
        {
            CellRect rect = GenAdj.OccupiedRect(myPos, myRot, myDef.size);
            CellRect rect2 = GenAdj.OccupiedRect(facilityPos, facilityRot, facilityDef.size);
            if (!GenAdj.AdjacentTo8WayOrInside(rect, rect2))
            {
                return false;
            }
        }
        if (compProperties.mustBePlacedFacingThingLinear)
        {
            if (ContainmentUtility.IsLinearBuildingBlocked(facilityDef, facilityPos, facilityRot, myMap))
            {
                return false;
            }
            CellRect cellRect = GenAdj.OccupiedRect(myPos, myRot, myDef.size);
            foreach (IntVec3 inhibitorAffectedCell in ContainmentUtility.GetInhibitorAffectedCells(facilityDef, facilityPos, facilityRot, myMap))
            {
                if (cellRect.Cells.Contains(inhibitorAffectedCell))
                {
                    return true;
                }
            }
            return false;
        }
        if (compProperties.mustBePlacedAdjacentCardinalToBedHead || compProperties.mustBePlacedAdjacentCardinalToAndFacingBedHead)
        {
            if (!myDef.IsBed)
            {
                return false;
            }
            CellRect other = GenAdj.OccupiedRect(facilityPos, facilityRot, facilityDef.size);
            bool flag = false;
            int sleepingSlotsCount = BedUtility.GetSleepingSlotsCount(myDef.size);
            for (int i = 0; i < sleepingSlotsCount; i++)
            {
                IntVec3 sleepingSlotPos = BedUtility.GetSleepingSlotPos(i, myPos, myRot, myDef.size);
                if (!sleepingSlotPos.IsAdjacentToCardinalOrInside(other))
                {
                    continue;
                }
                if (compProperties.mustBePlacedAdjacentCardinalToAndFacingBedHead)
                {
                    if (other.MovedBy(facilityRot.FacingCell).Contains(sleepingSlotPos))
                    {
                        flag = true;
                    }
                }
                else
                {
                    flag = true;
                }
            }
            if (!flag)
            {
                return false;
            }
        }
        if (!compProperties.mustBePlacedAdjacent && !compProperties.mustBePlacedAdjacentCardinalToBedHead && !compProperties.mustBePlacedAdjacentCardinalToAndFacingBedHead)
        {
            Vector3 a = GenThing.TrueCenter(myPos, myRot, myDef.size, myDef.Altitude);
            Vector3 b = GenThing.TrueCenter(facilityPos, facilityRot, facilityDef.size, facilityDef.Altitude);
            float num = Vector3.Distance(a, b);
            if (num > compProperties.maxDistance || (compProperties.minDistance > 0f && num < compProperties.minDistance))
            {
                return false;
            }
        }
        return true;
    }

    // Use base IsValidFacilityForMe

    // Has Harmony detour
    internal bool IsPotentiallyValidFacilityForMe_Grouped(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot)
    {
        if (!IsPotentiallyValidFacilityForMe_Static(facilityDef, facilityPos, facilityRot, parent.def, parent.Position, parent.Rotation, parent.Map))
        {
            return false;
        }
        if (facilityDef.GetCompProperties<CompProperties_Facility_Grouped>().canLinkToMedBedsOnly && (!(parent is Building_Bed building_Bed) || !building_Bed.Medical))
        {
            return false;
        }
        return true;
    }

    // Use base IsPotentiallyValidFacilityForMe_Static(Thing facility, ThingDef myDef, IntVec3 myPos, Rot4 myRot)

    // Has Harmony detour
    internal static bool IsPotentiallyValidFacilityForMe_Static_Grouped(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
    {
        CompProperties_Facility_Grouped compProperties = facilityDef.GetCompProperties<CompProperties_Facility_Grouped>();
        if (compProperties == null)
        {
            return false;
        }

        if (!compProperties.requiresLOS)
        {
            return true;
        }
        CellRect startRect = GenAdj.OccupiedRect(myPos, myRot, myDef.size);
        CellRect endRect = GenAdj.OccupiedRect(facilityPos, facilityRot, facilityDef.size);
        bool flag = false;
        for (int i = startRect.minZ; i <= startRect.maxZ; i++)
        {
            for (int j = startRect.minX; j <= startRect.maxX; j++)
            {
                for (int k = endRect.minZ; k <= endRect.maxZ; k++)
                {
                    int num = endRect.minX;
                    while (num <= endRect.maxX)
                    {
                        IntVec3 start = new IntVec3(j, 0, i);
                        IntVec3 end = new IntVec3(num, 0, k);
                        if (!GenSight.LineOfSight(start, end, map, startRect, endRect))
                        {
                            num++;
                            continue;
                        }
                        goto IL_007a; // this is copy-pasted from ILSpy output, I don't even know what this should be as source code
                    }
                }
            }
            continue;
            IL_007a:
            flag = true;
            break;
        }
        if (!flag)
        {
            return false;
        }
        return true;
    }

    // Has Harmony detour
    public void Notify_NewLink_Grouped(Thing facility)
    {
        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            if (linkedFacilities[i] == facility)
            {
                Log.Error("Notify_NewLink was called but the link is already here.");
                return;
            }
        }
        Thing potentiallySupplantedFacility = GetPotentiallySupplantedFacility(facility.def, facility.Position, facility.Rotation);
        if (potentiallySupplantedFacility != null)
        {
            potentiallySupplantedFacility.TryGetComp<CompFacility_Grouped>().Notify_LinkRemoved(parent);
            linkedFacilities.Remove(potentiallySupplantedFacility);
        }
        linkedFacilities.Add(facility);
    }

    // Use base Notify_LinkRemoved

    // Use base Notify_FacilityDespawned

    // Use base Notify_LOSBlockerSpawnedOrDespawned

    // Use base Notify_ThingChanged

    // Use base PostSpawnSetup

    // Use base PostDeSpawn

    // Use base PostDrawExtraSelectionOverlays

    // Has Harmony detour
    // Note: "Closer" is more accurate than "Better"
    internal bool IsBetter_Grouped(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, Thing thanThisFacility)
    {
        CompProperties_Facility_Grouped newProps = facilityDef.GetCompProperties<CompProperties_Facility_Grouped>();
        CompProperties_Facility_Grouped oldProps = thanThisFacility.def.GetCompProperties<CompProperties_Facility_Grouped>();

        if (newProps == null || oldProps == null || newProps.categoryTag != oldProps.categoryTag)
        {
            Log.Error("Comparing two facilities in different category tags.");
            return false;
        }

        Vector3 b = GenThing.TrueCenter(facilityPos, facilityRot, facilityDef.size, facilityDef.Altitude);
        Vector3 a = parent.TrueCenter();
        float num = Vector3.Distance(a, b);
        float num2 = Vector3.Distance(a, thanThisFacility.TrueCenter());

        if (num != num2)
        {
            return num < num2;
        }

        if (facilityPos.x != thanThisFacility.Position.x)
        {
            return facilityPos.x < thanThisFacility.Position.x;
        }

        return facilityPos.z < thanThisFacility.Position.z;
    }

    // Has Harmony detour
    public static IEnumerable<Thing> PotentialThingsToLinkTo_Grouped(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map myMap)
    {
        alreadyReturnedCount_ByTag.Clear();

        CompProperties_AffectedByFacilities_Grouped compProps = myDef.GetCompProperties<CompProperties_AffectedByFacilities_Grouped>();
        if (compProps?.linkGroups == null)
            yield break;

        List<ThingDef> candidateDefs = compProps.linkableFacilities;
        if (candidateDefs == null || candidateDefs.Count == 0)
            yield break;

        IEnumerable<Thing> candidates = Enumerable.Empty<Thing>();
        for (int i = 0; i < candidateDefs.Count; i++)
        {
            ThingDef def = candidateDefs[i];
            List<Thing> thingsOfDef = myMap.listerThings.ThingsOfDef(def);

            if (!thingsOfDef.NullOrEmpty())
                candidates = candidates.Concat(thingsOfDef);
        }

        Vector3 myTrueCenter = GenThing.TrueCenter(myPos, myRot, myDef.size, myDef.Altitude);
        IOrderedEnumerable<Thing> orderedEnumerable = from x in candidates
            orderby Vector3.Distance(myTrueCenter, x.TrueCenter()), x.Position.x, x.Position.z
            select x;

        foreach (Thing item in orderedEnumerable)
        {
            if (!item.TryGetComp(out CompFacility_Grouped comp) || !comp.CanLink() || !CanPotentiallyLinkTo_Static(item, myDef, myPos, myRot, myMap))
            {
                continue;
            }

            // Skip Facilities that are already linked to a different Thing
            if (comp.Props_Grouped.maxAffected > 0 && comp.LinkedBuildings.Count >= comp.Props_Grouped.maxAffected)
            {
                continue;
            }

            string categoryTag = comp.Props_Grouped.categoryTag;
            if (string.IsNullOrEmpty(categoryTag))
                continue;

            FacilityLinkGroup relevantGroup = compProps.GetLinkGroupForTag(categoryTag);
            if (relevantGroup == null)
                continue;

            if (!alreadyReturnedCount_ByTag.TryGetValue(categoryTag, out int currentCount))
            {
                alreadyReturnedCount_ByTag[categoryTag] = 0;
                currentCount = 0;
            }

            if (currentCount >= relevantGroup.maxLinks)
                continue;

            alreadyReturnedCount_ByTag[categoryTag] = currentCount + 1;
            yield return item;
        }
    }

    // Use base DrawLinesToPoentialThingsToLinkTo

    // Use base DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo

    // Use base DrawRedLineToPotentiallySupplantedFacility

    // Has Harmony detour
    // TODO testing
    internal Thing GetPotentiallySupplantedFacility_Grouped(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot)
    {
        CompProperties_Facility_Grouped facilityProps = facilityDef.GetCompProperties<CompProperties_Facility_Grouped>();
        if (facilityProps == null || string.IsNullOrEmpty(facilityProps.categoryTag))
            return null;

        FacilityLinkGroup relevantGroup = Props_Grouped.GetLinkGroupForTag(facilityProps.categoryTag);
        if (relevantGroup == null)
            return null;

        string tag = facilityProps.categoryTag;

        Thing firstFound = null;
        int count = 0;

        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            Thing fac = linkedFacilities[i];

            CompFacility_Grouped facComp = fac.TryGetComp<CompFacility_Grouped>();
            if (facComp == null)
                continue;

            if (facComp.Props_Grouped.categoryTag == tag)
            {
                if (firstFound == null)
                    firstFound = fac;

                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        CompProperties_Facility_Grouped compProperties = facilityDef.GetCompProperties<CompProperties_Facility_Grouped>();
        if (count + 1 <= relevantGroup.maxLinks)
            return null;

        Thing worst = firstFound;

        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            Thing fac = linkedFacilities[i];
            CompFacility_Grouped facComp = fac.TryGetComp<CompFacility_Grouped>();
            if (facComp == null || facComp.Props_Grouped.categoryTag != tag)
                continue;

            if (IsBetter(worst.def, worst.Position, worst.Rotation, fac))
            {
                worst = fac;
            }
        }

        return worst;
    }

    public override float GetStatOffset(StatDef stat)
    {
        float num = 0f;
        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            CompFacility_Grouped compFacility_Grouped = linkedFacilities[i].TryGetComp<CompFacility_Grouped>();
            if (compFacility_Grouped.StatOffsets != null)
            {
                float statOffsetFromList = compFacility_Grouped.StatOffsets.GetStatOffsetFromList(stat);
                if (statOffsetFromList != 0f && IsFacilityActive(linkedFacilities[i]))
                {
                    num += statOffsetFromList;
                }
            }
        }
        return num;
    }

    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
    {
        bool headerWritten = false;
        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            Thing facility = linkedFacilities[i];
            if (!IsFacilityActive(facility))
            {
                continue;
            }
            CompFacility_Grouped compFacility_Grouped = facility.TryGetComp<CompFacility_Grouped>();
            if (compFacility_Grouped.StatOffsets == null)
            {
                continue;
            }
            float statOffsetFromList = compFacility_Grouped.StatOffsets.GetStatOffsetFromList(stat);
            if (statOffsetFromList == 0f)
            {
                continue;
            }
            if (!headerWritten)
            {
                headerWritten = true;
                sb.AppendLine();
                sb.AppendLine(whitespace + "StatsReport_Facilities".Translate() + ":");
            }
            sb.Append(whitespace + "    ");
            sb.AppendLine(facility.LabelCap + ": " + statOffsetFromList.ToStringByStyle(stat.toStringStyle, ToStringNumberSense.Offset));
        }
    }

    // Use base RelinkAll

    // Has Harmony detour
    public bool IsFacilityActive_Grouped(Thing facility)
    {
        return facility.TryGetComp<CompFacility_Grouped>().CanBeActive;
    }

    // Has Harmony detour
    internal void LinkToNearbyFacilities_Grouped()
    {
        UnlinkAll();
        if (!parent.Spawned)
        {
            return;
        }
        foreach (Thing item in ThingsICanLinkTo)
        {
            if (item.TryGetComp(out CompFacility_Grouped comp))
            {
                linkedFacilities.Add(item);
                comp.Notify_NewLink(parent);
            }
        }
    }

    // Has Harmony detour
    internal void UnlinkAll_Grouped()
    {
        for (int i = 0; i < linkedFacilities.Count; i++)
        {
            linkedFacilities[i].TryGetComp<CompFacility_Grouped>().Notify_LinkRemoved(parent);
        }
        linkedFacilities.Clear();
    }
}
