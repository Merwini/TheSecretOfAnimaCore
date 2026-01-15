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
    public class Building_AnimaSapBasin : Building, IThingHolder
    {
        public ThingOwner innerContainer;

        public bool harvestingToggled = true;
        public float progress = 0f;
        public float harvestPercent = 0.1f;
        public bool IsHarvesting => harvestingToggled && !IsFull;
        public bool IsFull => CurrentSap >= MaximumSap;
        public int CurrentSap => (!innerContainer.NullOrEmpty() && innerContainer[0] != null) ? innerContainer[0].stackCount : 0;
        public int MaximumSap => TSOA_DefOf.TSOA_AnimaSap.stackLimit;

        public bool allowEmptying = true;
        public bool emptyNow;

        public const int drainTicks = 1250;
        public const int workThreshhold = 25;

        private Thing linkedTree;
        public virtual Thing LinkedTree
        {
            get
            {
                if (linkedTree != null && (linkedTree.Destroyed || !linkedTree.Spawned))
                {
                    linkedTree = null;
                }

                if (linkedTree == null)
                {

                    CompGroupedFacility compFac = this.TryGetComp<CompGroupedFacility>();
                    if (compFac == null)
                    {
                        return null;
                    }

                    linkedTree = compFac.LinkedThings.FirstOrDefault(t => t?.TryGetComp<CompSpawnSubplant>() != null);
                }

                return linkedTree;
            }
        }

        public bool ShouldEmpty
        {
            get
            {
                if (!allowEmptying)
                    return false;

                if (innerContainer.Count == 0)
                    return false;

                Thing t = innerContainer[0];
                if (t == null)
                    return false;

                return emptyNow || t.stackCount >= workThreshhold;
            }
        }

        public Building_AnimaSapBasin()
        {
            innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false, LookMode.Deep);
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren) { }

        public override void TickRare()
        {
            base.TickRare();

            TickInterval(250);
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            TryConsumeProgress();
        }

        public void AddProgress(float progressToAdd)
        {
            progress += progressToAdd;
            TryConsumeProgress();
        }

        public void TryConsumeProgress()
        {
            if (IsFull)
                return;

            while (progress >= 1f && !IsFull)
            {
                TryAddSap();
                progress -= 1;
            }
        }

        private void TryAddSap()
        {
            bool addingFirst = CurrentSap == 0;
            Thing sap = ThingMaker.MakeThing(TSOA_DefOf.TSOA_AnimaSap);
            sap.stackCount = 1;
            innerContainer.TryAddOrTransfer(sap);
            if (addingFirst)
            {
                this.DirtyMapMesh(Map);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            // TODO dialog to set harvestPercent

            //yield return new Command_Action
            //{
            //    defaultLabel = "TSOA_SetTapRangeLabel".Translate(),
            //    defaultDesc = "TSOA_SetTapRangeDescription".Translate(),
            //    // TODO icon. Anima tree?
            //    action = () =>
            //    {
            //        Find.WindowStack.Add(new Dialog_SapTapThresholds(this));
            //    }
            //};
            yield return new Command_Toggle
            {
                defaultLabel = "TSOA_SapHarvestToggleLabel".Translate(),
                defaultDesc = "TSOA_SapHarvestToggleDesc".Translate(),
                isActive = () => harvestingToggled,
                toggleAction = () => harvestingToggled = !harvestingToggled
            };

            yield return new Command_Action()
            {
                defaultLabel = "TSOA_SapToggleEmptyingLabel".Translate(),
                defaultDesc = "TSOA_SapToggleEmptyingDescription".Translate(),
                // TODO icon. Sap texture with red cancel symbol overlaying it. Maybe separate gizmos for toggle on vs toggle off?
                action = () => allowEmptying = !allowEmptying
            };

            yield return new Command_Action()
            {
                defaultLabel = "TSOA_SapEmptyNowLabel".Translate(),
                defaultDesc = "TSOA_SapEmptyNowDescription".Translate(),
                // TODO icon. Sap texture with blue arrow pointing up overlaying it
                action = () => ToggleEmptyNow()
            };

            if (DebugSettings.godMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEV: Add 1 sap",
                    action = () => TryAddSap()
                };

                yield return new Command_Action()
                {
                    defaultLabel = "DEV: Fill progress",
                    action = () => progress = 1f
                };
            }
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(base.GetInspectString());

            if (innerContainer.Count > 0)
                sb.AppendLine("TSOA_StoredSap".Translate(CurrentSap, MaximumSap));

            if (LinkedTree == null)
            {
                sb.AppendLine("TSOA_NotLinked".Translate());
                return sb.ToString();
            }

            sb.AppendLine(harvestingToggled ? "TSOA_SapCurrentlyHarvesting".Translate() : "TSOA_SapNotCurrentlyHarvesting".Translate());
            sb.AppendLine(allowEmptying ? "TSOA_SapEmptyingAllowed".Translate() : "TSOA_SapEmptyingDisallowed".Translate());
            sb.AppendLine("TSOA_SapHarvestProgress".Translate((progress * 100).ToString("F2")));

            return sb.ToString().Trim();
        }

        private void ToggleEmptyNow()
        {
            if (!emptyNow && CurrentSap > 0)
            {
                emptyNow = true;
                allowEmptying = true;
            }
            else
            {
                emptyNow = false;
            }
            UpdateDesignation();
        }

        public void UpdateDesignation()
        {
            if (!Spawned) return;

            Designation designation = Map.designationManager.DesignationOn(this, TSOA_DefOf.TSOA_EmptyNowSapBasin);

            if (emptyNow)
            {
                if (designation == null)
                    Map.designationManager.AddDesignation(new Designation(this, TSOA_DefOf.TSOA_EmptyNowSapBasin));
            }
            else
            {
                if (designation != null)
                    designation.Delete();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

            Scribe_Values.Look(ref progress, "progress", 0);
            Scribe_Values.Look(ref harvestPercent, "harvestPercent", 0);
            Scribe_Values.Look(ref harvestingToggled, "harvestingToggled", false);
            Scribe_Values.Look(ref allowEmptying, "allowEmptying", true);
            Scribe_Values.Look(ref emptyNow, "emptyNow", false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            UpdateDesignation();
        }
    }
}
