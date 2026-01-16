using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace tsoa.core
{
    public class CompAnimaSapTap : CompAnimaTreeLinkee
    {
        CompProperties_AnimaSapTap Props => (CompProperties_AnimaSapTap)props;

        public bool ShouldEmpty
        {
            get
            {
                if (!allowEmptying)
                    return false;

                if (ParentCrate.innerContainer == null || ParentCrate.innerContainer.Count == 0)
                    return false;

                Thing thing = ParentCrate.innerContainer[0];
                if (thing == null)
                    return false;

                return forceEmpty || thing.stackCount >= Props.workThreshhold;
            }
        }

        public Building_Crate ParentCrate => parent as Building_Crate;

        private bool harvesting;
        private bool allowEmptying;
        private bool forceEmpty;
        private int rareTicksSinceHarvest;

        public override void CompTickRare()
        {
            if (harvesting)
            {
                rareTicksSinceHarvest += 250;
                if (rareTicksSinceHarvest >= Props.drainTicks)
                {
                    rareTicksSinceHarvest = 0;
                    if (CompEssence.TryRemoveEssence(50))
                    {
                        TryAddSapToContainer();
                    }
                }
            }
        }

        private void TryAddSapToContainer()
        {
            Thing sap = ThingMaker.MakeThing(TSOA_DefOf.TSOA_AnimaSap);
            sap.stackCount = 1;
            ParentCrate.innerContainer.TryAdd(sap);
        }

        public void ToggleAllowEmpying()
        {
            allowEmptying = !allowEmptying;
        }

        public void ToggleForceEmpty()
        {
            if (!forceEmpty && ParentCrate.innerContainer != null && ParentCrate.innerContainer.Count != 0 && ParentCrate.innerContainer[0].stackCount > 0)
            {
                forceEmpty = true;
                return;
            }
            forceEmpty = false;
        }

        public override string CompInspectStringExtra()
        {
            return harvesting ? "TSOA_SapCurrentlyHarvesting".Translate() : "TSOA_SapNotCurrentlyHarvesting".Translate();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            yield return new Command_Action
            {
                defaultLabel = "TSOA_SapToggleEmptyingLabel".Translate(),
                defaultDesc = "TSOA_SapToggleEmptyingDescription".Translate(),
                // TODO need icon
                action = () =>
                {
                    ToggleAllowEmpying();
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "TSOA_SapForceEmptyLabel".Translate(),
                defaultDesc = "TSOA_SapForceEmptyDescription".Translate(),
                // TODO need icon
                action = () =>
                {
                    ToggleForceEmpty();
                }
            };

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Add 1 sap",
                    defaultDesc = "Adds 1 sap immediately.",
                    action = () =>
                    {
                        TryAddSapToContainer();
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Start or stop harvesting",
                    defaultDesc = "Starts or stops harvesting.",
                    action = () =>
                    {
                        harvesting = !harvesting;
                    }
                };
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref harvesting, "harvesting", false);
            Scribe_Values.Look(ref allowEmptying, "allowEmptying", true);
            Scribe_Values.Look(ref rareTicksSinceHarvest, "rareTicksSinceHarvest", 0);
            Scribe_Values.Look(ref forceEmpty, "forceEmpty", false);
        }
    }
}