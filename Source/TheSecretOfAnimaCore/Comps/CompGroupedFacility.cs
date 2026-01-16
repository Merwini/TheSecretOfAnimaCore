using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core
{
    public class CompGroupedFacility : ThingComp
    {
        private List<Thing> linkedThings = new List<Thing>();

        private const int UpdateRateIntervalTicks = 120;

        private HashSet<Thing> thingsToNotify = new HashSet<Thing>();

        public virtual bool CanBeActive
        {
            get
            {
                CompPowerTrader compPowerTrader = parent.TryGetComp<CompPowerTrader>();
                if (compPowerTrader != null && !compPowerTrader.PowerOn)
                {
                    return false;
                }
                return true;
            }
        }

        public List<Thing> LinkedThings => linkedThings;

        public virtual List<StatModifier> StatOffsets => Props.statOffsets;

        public CompProperties_GroupedFacility Props => (CompProperties_GroupedFacility)props;

        protected virtual string MaxConnectedString => "FacilityMaxSimultaneousConnections".Translate();

        public event Action<CompGroupedFacility, Thing> OnLinkAdded;

        public event Action<CompGroupedFacility, Thing> OnLinkRemoved;

        public static void DrawLinesToPotentialThingsToLinkTo(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {

            CompProperties_GroupedFacility compProperties = myDef.GetCompProperties<CompProperties_GroupedFacility>();
            if (compProperties?.linkableThingDefs == null)
                return;

            int max = compProperties.maxAffected > 0 ? compProperties.maxAffected : int.MaxValue;

            Vector3 myCenter = GenThing.TrueCenter(myPos, myRot, myDef.size, myDef.Altitude);

            List<Thing> potentialThings = new List<Thing>();

            for (int i = 0; i < compProperties.linkableThingDefs.Count; i++)
            {
                foreach (Thing item in map.listerThings.ThingsOfDef(compProperties.linkableThingDefs[i]))
                {
                    CompAffectedByGroupedFacilities compAffectee = item.TryGetComp<CompAffectedByGroupedFacilities>();

                    if (compAffectee != null &&
                        compAffectee.CanPotentiallyLinkTo(myDef, myPos, myRot))
                    {
                        potentialThings.Add(item);
                    }
                }
            }

            if (potentialThings.Count == 0)
                return;

            potentialThings.Sort((a, b) =>
                Vector3.Distance(myCenter, a.TrueCenter())
                .CompareTo(Vector3.Distance(myCenter, b.TrueCenter())));

            int drawn = 0;

            foreach (Thing candidate in potentialThings)
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

        public static void DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo(float curX, ref float curY, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            CompProperties_GroupedFacility compProperties = myDef.GetCompProperties<CompProperties_GroupedFacility>();
            int num = 0;
            for (int i = 0; i < compProperties.linkableThingDefs.Count; i++)
            {
                foreach (Thing item in map.listerThings.ThingsOfDef(compProperties.linkableThingDefs[i]))
                {
                    CompAffectedByGroupedFacilities compAffectedByFacilities = item.TryGetComp<CompAffectedByGroupedFacilities>();
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

        public override void CompTick()
        {
            base.CompTick();
            if (Props.mustBePlacedFacingThingLinear && parent.Spawned && parent.IsHashIntervalTick(120))
            {
                bool flag = ContainmentUtility.IsLinearBuildingBlocked(parent.def, parent.Position, parent.Rotation, parent.Map);
                if ((linkedThings.Any() && flag) || (linkedThings.Empty() && !flag))
                {
                    RelinkAll();
                }
            }
        }

        public virtual bool CanLink()
        {
            return true;
        }

        public void Notify_NewLink(Thing thing)
        {
            for (int i = 0; i < linkedThings.Count; i++)
            {
                if (linkedThings[i] == thing)
                {
                    Log.Error("Notify_NewLink was called but the link is already here.");
                    return;
                }
            }
            linkedThings.Add(thing);
            this.OnLinkAdded?.Invoke(this, thing);
        }

        public void Notify_LinkRemoved(Thing thing)
        {
            for (int i = 0; i < linkedThings.Count; i++)
            {
                if (linkedThings[i] == thing)
                {
                    linkedThings.RemoveAt(i);
                    this.OnLinkRemoved?.Invoke(this, thing);
                    return;
                }
            }
            Log.Error("Notify_LinkRemoved was called but there is no such link here.");
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
            LinkToNearbyBuildings();
        }

        public override void PostMapInit()
        {
            RelinkAll();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            thingsToNotify.Clear();
            for (int i = 0; i < linkedThings.Count; i++)
            {
                thingsToNotify.Add(linkedThings[i]);
            }
            UnlinkAll();
            foreach (Thing item in thingsToNotify)
            {
                item.TryGetComp<CompAffectedByGroupedFacilities>().Notify_FacilityDespawned();
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            for (int i = 0; i < linkedThings.Count; i++)
            {
                if (linkedThings[i].TryGetComp<CompAffectedByGroupedFacilities>().IsFacilityActive(parent))
                {
                    GenDraw.DrawLineBetween(parent.TrueCenter(), linkedThings[i].TrueCenter());
                }
                else
                {
                    GenDraw.DrawLineBetween(parent.TrueCenter(), linkedThings[i].TrueCenter(), CompAffectedByGroupedFacilities.InactiveFacilityLineMat);
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
                stringBuilder.Append("\n");
            }
            CompProperties_GroupedFacility compProperties_GroupedFacility = Props;
            if (compProperties_GroupedFacility.showMaxSimultaneous)
            {
                string categoryTag = Props.categoryTag;
                foreach (Thing linkedThing in linkedThings)
                {
                    if (linkedThing == null || linkedThing.Destroyed)
                        continue;

                    CompAffectedByGroupedFacilities compAffected = linkedThing.TryGetComp<CompAffectedByGroupedFacilities>();
                    if (compAffected == null)
                        continue;

                    FacilityLinkGroup group = compAffected.Props.GetLinkGroupForTag(categoryTag);
                    if (group == null)
                        continue;

                    int count = 0;
                    foreach (Thing facility in compAffected.LinkedFacilities)
                    {
                        if (facility == null || facility.Destroyed)
                            continue;

                        CompGroupedFacility compFacility = facility.TryGetComp<CompGroupedFacility>();
                        if (compFacility != null && compFacility.Props.categoryTag == categoryTag)
                        {
                            count++;
                        }
                    }

                    stringBuilder.AppendInNewLine($"{linkedThing.LabelCap} {group.label} {"TSOA_Linked".Translate()}: {count}/{group.maxLinks}");
                }
            }
            if (compProperties_GroupedFacility.mustBePlacedFacingThingLinear && parent.Spawned && ContainmentUtility.IsLinearBuildingBlocked(parent.def, parent.Position, parent.Rotation, parent.Map))
            {
                stringBuilder.AppendInNewLine("FacilityFrontBlocked".Translate());
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        private void RelinkAll()
        {
            LinkToNearbyBuildings();
        }

        private void LinkToNearbyBuildings()
        {
            UnlinkAll();

            CompProperties_GroupedFacility compProperties_GroupedFacility = Props;

            if (compProperties_GroupedFacility.linkableThingDefs == null)
                return;

            List<Thing> potentiallyAffected = new List<Thing>(); 

            foreach (ThingDef affectedDef in Props.linkableThingDefs)
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

            int linkLimit = Props.maxAffected > 0 ? Props.maxAffected : int.MaxValue;

            foreach (Thing target in potentiallyAffected.Take(linkLimit))
            {
                CompAffectedByGroupedFacilities comp = target.TryGetComp<CompAffectedByGroupedFacilities>();
                linkedThings.Add(target);
                comp.Notify_NewLink(parent);

                OnLinkAdded?.Invoke(this, target);
            }
        }

        private bool AmIActiveForAnyone()
        {
            for (int i = 0; i < linkedThings.Count; i++)
            {
                if (linkedThings[i].TryGetComp<CompAffectedByGroupedFacilities>().IsFacilityActive(parent))
                {
                    return true;
                }
            }
            return false;
        }

        private void UnlinkAll()
        {
            for (int i = 0; i < linkedThings.Count; i++)
            {
                linkedThings[i].TryGetComp<CompAffectedByGroupedFacilities>().Notify_LinkRemoved(parent);
                this.OnLinkRemoved?.Invoke(this, linkedThings[i]);
            }
            linkedThings.Clear();
        }
    }
}
