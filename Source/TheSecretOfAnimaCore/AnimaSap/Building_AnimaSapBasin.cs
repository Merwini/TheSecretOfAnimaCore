using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace nuff.tsoa.core
{
    [StaticConstructorOnStartup]
    public class Building_AnimaSapBasin : Building, IThingHolder
    {
        public ThingOwner innerContainer;

        public bool harvesting;
        public FloatRange harvestRange = new FloatRange(0.5f, 0.95f);
        public bool allowEmptying = true;
        public bool emptyNow;
        public int ticksSinceHarvest; 

        public const int drainTicks = 1250;
        public const int essencePerSap = 50;
        public const int maximumSap = 50;
        public const int workThreshhold = 25;

        private Thing linkedTree;
        public virtual Thing LinkedTree
        {
            get
            {
                if (linkedTree == null)
                {
                    CompGroupedFacility compFac = this.TryGetComp<CompGroupedFacility>();
                    if (compFac == null)
                    {
                        Log.Error($"{this.Label} unable to get CompGroupedFacility");
                        return null;
                    }

                    Thing firstTree = compFac.LinkedThings.FirstOrDefault(t => t.HasComp<CompAnimaTreeEssence>());
                    if (firstTree == null)
                    {
                        Log.Error($"{this.Label} linked to Thing without CompAnimaTreeEssence");
                        return null;
                    }
                    linkedTree = firstTree;
                }
                return linkedTree;
            }
        }

        CompAnimaTreeEssence compEssence;

        public virtual CompAnimaTreeEssence CompEssence
        {
            get
            {
                if (compEssence == null)
                {
                    CompAnimaTreeEssence comp = LinkedTree.TryGetComp<CompAnimaTreeEssence>();
                    if (comp == null)
                    {
                        Log.Error($"{this.Label} linked to Tree without CompAnimaTreeEssence"); // I don't think this could happen without LinkedTree erroring first, just want to cover bases
                        return null;
                    }
                    compEssence = comp;
                }

                return compEssence;
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

        private bool ShouldHarvest
        {
            get
            {
                if (CompEssence == null)
                    return false;

                if (StorageFull)
                    return false;

                float percent = TreeEssencePercent;

                if (!harvesting)
                {
                    return percent >= harvestRange.max;
                }
                else
                {
                    return percent > harvestRange.min;
                }
            }
        }

        public int CurrentSapCount => (innerContainer.Count > 0 && innerContainer[0] != null) ? innerContainer[0].stackCount : 0;

        public bool StorageFull => CurrentSapCount >= maximumSap;

        public float TreeEssencePercent => (float)CompEssence.StoredEssence / CompEssence.Props.maximumEssence;

        public Building_AnimaSapBasin()
        {
            innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false, LookMode.Deep);
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren) { }

        public override void TickRare()
        {
            base.TickRare();

            harvesting = ShouldHarvest;

            if (!harvesting)
                return;

            ticksSinceHarvest += 250;

            if (ticksSinceHarvest >= drainTicks)
            {
                if (CompEssence != null && CompEssence.TryRemoveEssence(essencePerSap))
                {
                    ticksSinceHarvest = 0;
                    TryAddSap(); 
                    if (StorageFull)
                    {
                        harvesting = false;
                    }
                }
                else
                {
                    harvesting = false; //tree must be empty
                }
            }
        }

        private void TryAddSap()
        {
            Thing sap = ThingMaker.MakeThing(TSOA_DefOf.TSOA_AnimaSap);
            sap.stackCount = 1;
            innerContainer.TryAddOrTransfer(sap);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            yield return new Command_Action
            {
                defaultLabel = "TSOA_SetTapRangeLabel".Translate(),
                defaultDesc = "TSOA_SetTapRangeDescription".Translate(),
                // TODO icon. Anima tree?
                action = () =>
                {
                    Find.WindowStack.Add(new Dialog_SapTapThresholds(this));
                }
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

            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEV: Add 1 sap",
                    action = () => TryAddSap()
                };
            }
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(base.GetInspectString());

            sb.AppendLine(harvesting ? "TSOA_SapCurrentlyHarvesting".Translate() : "TSOA_SapNotCurrentlyHarvesting".Translate());
            sb.AppendLine(allowEmptying ? "TSOA_SapEmptyingAllowed".Translate() : "TSOA_SapEmptyingDisallowed".Translate());
            sb.AppendLine("TSOA_SapThresholdsInspect".Translate((harvestRange.min * 100f).ToString("F0"), (harvestRange.max * 100f).ToString("F0")));

            if (innerContainer.Count > 0)
                sb.AppendLine("TSOA_StoredSap".Translate(innerContainer[0].stackCount, maximumSap));

            return sb.ToString().Trim();
        }

        private void ToggleEmptyNow()
        {
            if (!emptyNow && CurrentSapCount > 0)
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

            Scribe_Values.Look(ref harvesting, "harvesting", false);
            Scribe_Values.Look(ref harvestRange, "harvestRange", new FloatRange(0.5f, 0.95f));
            Scribe_Values.Look(ref allowEmptying, "allowEmptying", true);
            Scribe_Values.Look(ref emptyNow, "emptyNow", false);
            Scribe_Values.Look(ref ticksSinceHarvest, "rareTicksSinceHarvest", 0);
        }
    }
}
