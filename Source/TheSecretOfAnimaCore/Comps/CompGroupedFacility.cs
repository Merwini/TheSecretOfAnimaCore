using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RimWorld.Planet.WorldGenStep_Roads;

namespace nuff.tsoa.core
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

        public List<Thing> LinkedBuildings => linkedThings;

        public virtual List<StatModifier> StatOffsets => Props.statOffsets;

        public CompProperties_GroupedFacility Props => (CompProperties_GroupedFacility)props;

        protected virtual string MaxConnectedString => "FacilityMaxSimultaneousConnections".Translate();

        public event Action<CompGroupedFacility, Thing> OnLinkAdded;

        public event Action<CompGroupedFacility, Thing> OnLinkRemoved;

        public static void DrawLinesToPotentialThingsToLinkTo(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            CompProperties_GroupedFacility compProperties = myDef.GetCompProperties<CompProperties_GroupedFacility>();
            if (compProperties?.linkableThings == null)
            {
                return;
            }
            Vector3 a = GenThing.TrueCenter(myPos, myRot, myDef.size, myDef.Altitude);
            for (int i = 0; i < compProperties.linkableThings.Count; i++)
            {
                foreach (Thing item in map.listerThings.ThingsOfDef(compProperties.linkableThings[i]))
                {
                    CompAffectedByGroupedFacilities compAffectedByFacilities = item.TryGetComp<CompAffectedByGroupedFacilities>();
                    if (compAffectedByFacilities != null && compAffectedByFacilities.CanPotentiallyLinkTo(myDef, myPos, myRot))
                    {
                        GenDraw.DrawLineBetween(a, item.TrueCenter());
                        compAffectedByFacilities.DrawRedLineToPotentiallySupplantedFacility(myDef, myPos, myRot);
                    }
                }
            }
        }

        public static void DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo(float curX, ref float curY, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            CompProperties_GroupedFacility compProperties = myDef.GetCompProperties<CompProperties_GroupedFacility>();
            int num = 0;
            for (int i = 0; i < compProperties.linkableThings.Count; i++)
            {
                foreach (Thing item in map.listerThings.ThingsOfDef(compProperties.linkableThings[i]))
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
            if (compProperties_GroupedFacility.linkableThings == null)
            {
                return;
            }
            for (int i = 0; i < compProperties_GroupedFacility.linkableThings.Count; i++)
            {
                foreach (Thing item in parent.Map.listerThings.ThingsOfDef(compProperties_GroupedFacility.linkableThings[i]))
                {
                    CompAffectedByGroupedFacilities compAffectedByFacilities = item.TryGetComp<CompAffectedByGroupedFacilities>();
                    if (compAffectedByFacilities != null && compAffectedByFacilities.CanLinkTo(parent))
                    {
                        linkedThings.Add(item);
                        compAffectedByFacilities.Notify_NewLink(parent);
                        this.OnLinkAdded?.Invoke(this, compAffectedByFacilities.parent);
                    }
                }
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
