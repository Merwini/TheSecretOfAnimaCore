using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace nuff.tsoa.core
{
    [StaticConstructorOnStartup]
    public class CompAffectedByGroupedFacilities : ThingComp
    {
        private List<Thing> linkedFacilities = new List<Thing>();

        public List<Thing> LinkedFacilities => linkedFacilities;

        public static Material InactiveFacilityLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

        private static readonly Dictionary<string, int> alreadyReturnedCount = new Dictionary<string, int>();

        private List<ThingDef> alreadyUsed = new List<ThingDef>();

        public List<Thing> LinkedFacilitiesListForReading => linkedFacilities;

        public CompProperties_AffectedByGroupedFacilities Props => (CompProperties_AffectedByGroupedFacilities)props;

        private IEnumerable<Thing> ThingsICanLinkTo
        {
            get
            {
                if (!parent.Spawned)
                {
                    yield break;
                }
                IEnumerable<Thing> enumerable = PotentialThingsToLinkTo(parent.def, parent.Position, parent.Rotation, parent.Map);
                foreach (Thing item in enumerable)
                {
                    if (CanLinkTo(item))
                    {
                        yield return item;
                    }
                }
            }
        }

        public bool CanLinkTo(Thing facility)
        {
            if (!facility.TryGetComp(out CompGroupedFacility comp))
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

        public static bool CanPotentiallyLinkTo_Static(Thing facility, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map myMap)
        {
            if (!CanPotentiallyLinkTo_Static(facility.def, facility.Position, facility.Rotation, myDef, myPos, myRot, myMap))
            {
                return false;
            }
            if (!IsPotentiallyValidFacilityForMe_Static(facility, myDef, myPos, myRot))
            {
                return false;
            }
            return true;
        }

        public bool CanPotentiallyLinkTo(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot)
        {
            if (!CanPotentiallyLinkTo_Static(facilityDef, facilityPos, facilityRot, parent.def, parent.Position, parent.Rotation, parent.Map))
            {
                return false;
            }
            if (!IsPotentiallyValidFacilityForMe(facilityDef, facilityPos, facilityRot))
            {
                return false;
            }

            CompProperties_GroupedFacility facilityProps = facilityDef.GetCompProperties<CompProperties_GroupedFacility>();
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

            bool betterCandidateExists = false;
            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                Thing linked = linkedFacilities[i];
                if (linked == null || linked.Destroyed)
                    continue;

                CompGroupedFacility compGrouped = linked.TryGetComp<CompGroupedFacility>();
                if (compGrouped == null)
                    continue;

                if (compGrouped.Props.categoryTag == tag)
                {
                    countInSameGroup++;

                    if (IsBetter(facilityDef, facilityPos, facilityRot, linked))
                    {
                        betterCandidateExists = true;
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
                        CompGroupedFacility facilityComp = facilityThing.TryGetComp<CompGroupedFacility>();
                        if (facilityComp != null)
                        {
                            facilityExistingLinks = facilityComp.LinkedThings.Count;
                        }
                    }
                }
            }

            if (betterCandidateExists)
            {
                return true;
            }

            FacilityLinkGroup relevantGroup = Props.GetLinkGroupForTag(tag);
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

        public static bool CanPotentiallyLinkTo_Static(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map myMap)
        {
            CompProperties_GroupedFacility compProperties = facilityDef.GetCompProperties<CompProperties_GroupedFacility>();
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

        public bool IsValidFacilityForMe(Thing facility)
        {
            if (!IsPotentiallyValidFacilityForMe_Static(facility, parent.def, parent.Position, parent.Rotation))
            {
                return false;
            }
            return true;
        }

        private bool IsPotentiallyValidFacilityForMe(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot)
        {
            if (!IsPotentiallyValidFacilityForMe_Static(facilityDef, facilityPos, facilityRot, parent.def, parent.Position, parent.Rotation, parent.Map))
            {
                return false;
            }
            if (facilityDef.GetCompProperties<CompProperties_GroupedFacility>().canLinkToMedBedsOnly && (!(parent is Building_Bed building_Bed) || !building_Bed.Medical))
            {
                return false;
            }
            return true;
        }

        private static bool IsPotentiallyValidFacilityForMe_Static(Thing facility, ThingDef myDef, IntVec3 myPos, Rot4 myRot)
        {
            return IsPotentiallyValidFacilityForMe_Static(facility.def, facility.Position, facility.Rotation, myDef, myPos, myRot, facility.Map);
        }

        private static bool IsPotentiallyValidFacilityForMe_Static(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            if (!facilityDef.GetCompProperties<CompProperties_GroupedFacility>().requiresLOS)
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
                            goto IL_007a;
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

        public void Notify_NewLink(Thing facility)
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
                potentiallySupplantedFacility.TryGetComp<CompGroupedFacility>().Notify_LinkRemoved(parent);
                linkedFacilities.Remove(potentiallySupplantedFacility);
            }
            linkedFacilities.Add(facility);
        }

        public void Notify_LinkRemoved(Thing thing)
        {
            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                if (linkedFacilities[i] == thing)
                {
                    linkedFacilities.RemoveAt(i);
                    return;
                }
            }
            Log.Error("Notify_LinkRemoved was called but there is no such link here.");
        }

        public void Notify_FacilityDespawned()
        {
            RelinkAll();
        }

        public void Notify_LOSBlockerSpawnedOrDespawned()
        {
            RelinkAll();
        }

        public void Notify_ThingChanged()
        {
            RelinkAll();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            LinkToNearbyFacilities();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            UnlinkAll();
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                if (IsFacilityActive(linkedFacilities[i]))
                {
                    GenDraw.DrawLineBetween(parent.TrueCenter(), linkedFacilities[i].TrueCenter());
                }
                else
                {
                    GenDraw.DrawLineBetween(parent.TrueCenter(), linkedFacilities[i].TrueCenter(), InactiveFacilityLineMat);
                }
            }
        }

        private bool IsBetter(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, Thing thanThisFacility)
        {
            CompProperties_GroupedFacility newProps = facilityDef.GetCompProperties<CompProperties_GroupedFacility>();
            CompProperties_GroupedFacility oldProps = thanThisFacility.def.GetCompProperties<CompProperties_GroupedFacility>();

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

        public static IEnumerable<Thing> PotentialThingsToLinkTo(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map myMap)
        {
            alreadyReturnedCount.Clear();

            CompProperties_AffectedByGroupedFacilities compProps = myDef.GetCompProperties<CompProperties_AffectedByGroupedFacilities>();
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
                if (!item.TryGetComp(out CompGroupedFacility comp) || !comp.CanLink() || !CanPotentiallyLinkTo_Static(item, myDef, myPos, myRot, myMap))
                {
                    continue;
                }

                string categoryTag = comp.Props.categoryTag;
                if (string.IsNullOrEmpty(categoryTag))
                    continue;

                FacilityLinkGroup relevantGroup = compProps.GetLinkGroupForTag(categoryTag);
                if (relevantGroup == null)
                    continue;

                if (!alreadyReturnedCount.TryGetValue(categoryTag, out int currentCount))
                {
                    alreadyReturnedCount[categoryTag] = 0;
                    currentCount = 0;
                }

                if (currentCount >= relevantGroup.maxLinks)
                    continue;

                alreadyReturnedCount[categoryTag] = currentCount + 1;
                yield return item;
            }
        }

        public static void DrawLinesToPotentialThingsToLinkTo(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            Vector3 a = GenThing.TrueCenter(myPos, myRot, myDef.size, myDef.Altitude);
            foreach (Thing item in PotentialThingsToLinkTo(myDef, myPos, myRot, map))
            {
                GenDraw.DrawLineBetween(a, item.TrueCenter());
            }
        }

        public static void DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo(float curX, ref float curY, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            int num = 0;
            foreach (Thing item in PotentialThingsToLinkTo(myDef, myPos, myRot, map))
            {
                num++;
                if (num == 1)
                {
                    DrawTextLine(ref curY, "FacilityPotentiallyLinkedTo".Translate() + ":");
                }
                DrawTextLine(ref curY, "  - " + item.LabelCap);
            }
            void DrawTextLine(ref float y, string text)
            {
                float lineHeight = Text.LineHeight;
                Widgets.Label(new Rect(curX, y, 999f, lineHeight), text);
                y += lineHeight;
            }
        }

        public void DrawRedLineToPotentiallySupplantedFacility(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot)
        {
            Thing potentiallySupplantedFacility = GetPotentiallySupplantedFacility(facilityDef, facilityPos, facilityRot);
            if (potentiallySupplantedFacility != null)
            {
                GenDraw.DrawLineBetween(parent.TrueCenter(), potentiallySupplantedFacility.TrueCenter(), InactiveFacilityLineMat);
            }
        }

        // TODO testing. I think IsBetter will also need a rewrite.
        private Thing GetPotentiallySupplantedFacility(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot)
        {
            CompProperties_GroupedFacility facilityProps = facilityDef.GetCompProperties<CompProperties_GroupedFacility>();
            if (facilityProps == null || string.IsNullOrEmpty(facilityProps.categoryTag))
                return null;

            FacilityLinkGroup relevantGroup = Props.GetLinkGroupForTag(facilityProps.categoryTag);
            if (relevantGroup == null)
                return null;

            string tag = facilityProps.categoryTag;

            Thing firstFound = null;
            int count = 0;

            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                Thing fac = linkedFacilities[i];

                CompGroupedFacility facComp = fac.TryGetComp<CompGroupedFacility>();
                if (facComp == null)
                    continue;

                if (facComp.Props.categoryTag == tag)
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

            CompProperties_GroupedFacility compProperties = facilityDef.GetCompProperties<CompProperties_GroupedFacility>();
            if (count + 1 <= relevantGroup.maxLinks)
                return null;

            Thing worst = firstFound;

            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                Thing fac = linkedFacilities[i];
                CompGroupedFacility facComp = fac.TryGetComp<CompGroupedFacility>();
                if (facComp == null || facComp.Props.categoryTag != tag)
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
                CompGroupedFacility CompGroupedFacility = linkedFacilities[i].TryGetComp<CompGroupedFacility>();
                if (CompGroupedFacility.StatOffsets != null)
                {
                    float statOffsetFromList = CompGroupedFacility.StatOffsets.GetStatOffsetFromList(stat);
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
            alreadyUsed.Clear();
            bool flag = false;
            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                bool flag2 = false;
                for (int j = 0; j < alreadyUsed.Count; j++)
                {
                    if (alreadyUsed[j] == linkedFacilities[i].def)
                    {
                        flag2 = true;
                        break;
                    }
                }
                if (flag2 || !IsFacilityActive(linkedFacilities[i]))
                {
                    continue;
                }
                CompGroupedFacility CompGroupedFacility = linkedFacilities[i].TryGetComp<CompGroupedFacility>();
                if (CompGroupedFacility.StatOffsets == null)
                {
                    continue;
                }
                float statOffsetFromList = CompGroupedFacility.StatOffsets.GetStatOffsetFromList(stat);
                if (statOffsetFromList == 0f)
                {
                    continue;
                }
                if (!flag)
                {
                    flag = true;
                    sb.AppendLine();
                    sb.AppendLine(whitespace + "StatsReport_Facilities".Translate() + ":");
                }
                int num = 0;
                for (int k = 0; k < linkedFacilities.Count; k++)
                {
                    if (IsFacilityActive(linkedFacilities[k]) && linkedFacilities[k].def == linkedFacilities[i].def)
                    {
                        num++;
                    }
                }
                statOffsetFromList *= (float)num;
                sb.Append(whitespace + "    ");
                if (num != 1)
                {
                    sb.Append(num + "x ");
                }
                sb.AppendLine(linkedFacilities[i].LabelCap + ": " + statOffsetFromList.ToStringByStyle(stat.toStringStyle, ToStringNumberSense.Offset));
                alreadyUsed.Add(linkedFacilities[i].def);
            }
        }

        private void RelinkAll()
        {
            LinkToNearbyFacilities();
        }

        public bool IsFacilityActive(Thing facility)
        {
            return facility.TryGetComp<CompGroupedFacility>().CanBeActive;
        }

        private void LinkToNearbyFacilities()
        {
            UnlinkAll();
            if (!parent.Spawned)
            {
                return;
            }
            foreach (Thing item in ThingsICanLinkTo)
            {
                if (item.TryGetComp(out CompGroupedFacility comp))
                {
                    linkedFacilities.Add(item);
                    comp.Notify_NewLink(parent);
                }
            }
        }

        private void UnlinkAll()
        {
            for (int i = 0; i < linkedFacilities.Count; i++)
            {
                linkedFacilities[i].TryGetComp<CompGroupedFacility>().Notify_LinkRemoved(parent);
            }
            linkedFacilities.Clear();
        }
    }
}