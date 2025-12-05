using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace nuff.tsoa.core
{
    public class CompAnimaRootGrave : ThingComp
    {
        private const int checkInterval = 250;
        private const int consumeTicks = 60000;
        private const int defaultEssence = 100;

        private int ticksWithCorpse;
        private Corpse cachedCorpse;

        private Thing linkedTree;
        public Thing LinkedTree
        {
            get
            {
                if (linkedTree == null)
                {
                    CompGroupedFacility compFac = parent.TryGetComp<CompGroupedFacility>();
                    if (compFac == null)
                    {
                        Log.Error("CompAnimaRootGrave unable to get CompGroupedFacility");
                        return null;
                    }

                    Thing firstTree = compFac.LinkedThings.FirstOrDefault(t => t.HasComp<CompAnimaTreeEssence>());
                    if (firstTree == null)
                    {
                        Log.Error("CompAnimaRootGrave linked to Thing without CompAnimaTreeEssence");
                        return null;
                    }
                    linkedTree = firstTree;
                }

                return linkedTree;
            }
        }

        CompAnimaTreeEssence compEssence;

        CompAnimaTreeEssence CompEssence
        {
            get
            {
                if (compEssence == null)
                {
                    CompAnimaTreeEssence comp = LinkedTree.TryGetComp<CompAnimaTreeEssence>();
                    if (comp == null)
                    {
                        Log.Error("CompAnimaRootGrave linked to Tree without CompAnimaTreeEssence"); // I don't think this could happen without LinkedTree erroring first, just want to cover bases
                        return null;
                    }
                }

                return compEssence;
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            Corpse currentCorpse = GetCorpse();

            if (currentCorpse == null)
            {
                return;
            }

            if (currentCorpse != cachedCorpse)
            {
                cachedCorpse = currentCorpse;
                ResetTimer();
                return;
            }

            ticksWithCorpse += checkInterval;

            if (ticksWithCorpse >= consumeTicks && CompEssence != null)
            {
                AddEssence();
                DestroyCorpse();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksWithCorpse, "ticksWithCorpse", 0);
            Scribe_References.Look(ref cachedCorpse, "cachedCorpse");
        }

        public void ResetTimer()
        {
            ticksWithCorpse = 0;
        }

        public void DestroyCorpse()
        {
            Corpse corpse = GetCorpse();

            if (parent is Building_Grave grave)
            {
                if (grave.innerContainer.Contains(corpse))
                {
                    grave.innerContainer.Remove(corpse);
                }
            }

            corpse.Destroy(DestroyMode.Vanish);

            FleckMaker.ThrowLightningGlow(parent.TrueCenter(), parent.Map, 1.5f);
        }

        public int CalculateEssence()
        {
            Corpse corpse = GetCorpse();
            Pawn pawn = corpse.InnerPawn;
            int essence = (int)(defaultEssence * pawn.GetStatValue(StatDefOf.PsychicSensitivity));

            return essence;
        }

        public void AddEssence()
        {
            int essenceToAdd = CalculateEssence();
            CompEssence.AddEssence(essenceToAdd);
        }

        private Corpse GetCorpse()
        {
            if (parent is Building_Grave grave && !grave.innerContainer.NullOrEmpty())
            {
                return grave.innerContainer[0] as Corpse;
            }

            return null;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();

            Corpse corpse = GetCorpse();
            if (corpse != null)
            {
                if (ticksWithCorpse > 0)
                {
                    int ticksLeft = consumeTicks - ticksWithCorpse;
                    if (ticksLeft > 0)
                    {
                        float hours = ticksLeft / 2500f;

                        sb.AppendLine("TSOA_CorpseConsumptionIn".Translate(hours.ToString("F1")));
                    }
                }

                int essence = CalculateEssence();
                sb.Append("TSOA_EssenceYield".Translate(essence));
            }

            return sb.ToString().TrimEnd();
        }
    }
}
