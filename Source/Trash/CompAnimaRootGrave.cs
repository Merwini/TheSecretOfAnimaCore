//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;
//using static HarmonyLib.Code;

//namespace tsoa.core
//{
//    public class CompAnimaRootGrave : CompAnimaTreeLinkee
//    {
//        private const int checkInterval = 250;
//        private const int consumeTicks = 60000;
//        private const int defaultEssence = 100;

//        private int ticksWithCorpse;
//        private Corpse cachedCorpse;

//        public override void CompTickRare()
//        {
//            base.CompTickRare();

//            Corpse currentCorpse = GetCorpse();

//            if (currentCorpse == null)
//            {
//                return;
//            }
//            if (currentCorpse != cachedCorpse)
//            {
//                cachedCorpse = currentCorpse;
//                ResetTimer();
//                return;
//            }

//            ticksWithCorpse += checkInterval;
//            if (ticksWithCorpse >= consumeTicks && CompEssence != null)
//            {
//                AddEssence();
//                DestroyCorpse();
//            }
//        }

//        public override void PostExposeData()
//        {
//            base.PostExposeData();
//            Scribe_Values.Look(ref ticksWithCorpse, "ticksWithCorpse", 0);
//            Scribe_References.Look(ref cachedCorpse, "cachedCorpse");
//        }

//        public void ResetTimer()
//        {
//            ticksWithCorpse = 0;
//        }

//        public void DestroyCorpse()
//        {
//            Corpse corpse = GetCorpse();

//            if (parent is Building_Grave grave)
//            {
//                if (grave.innerContainer.Contains(corpse))
//                {
//                    grave.innerContainer.Remove(corpse);
//                    grave.Map.mapDrawer.MapMeshDirty(grave.Position, MapMeshFlagDefOf.Things);
//                }
//            }

//            corpse.Destroy(DestroyMode.Vanish);
//            cachedCorpse = null;
            

//        }

//        public int CalculateEssence()
//        {
//            Corpse corpse = GetCorpse();
//            Pawn pawn = corpse.InnerPawn;
//            int essence = (int)(defaultEssence * pawn.GetStatValue(StatDefOf.PsychicSensitivity));

//            return essence;
//        }

//        public void AddEssence()
//        {
//            int essenceToAdd = CalculateEssence();
//            CompEssence.AddEssence(essenceToAdd);
//        }

//        private Corpse GetCorpse()
//        {
//            if (parent is Building_Grave grave && !grave.innerContainer.NullOrEmpty())
//            {
//                return grave.innerContainer[0] as Corpse;
//            }

//            return null;
//        }

//        public override string CompInspectStringExtra()
//        {
//            if (CompEssence == null)
//            {
//                return "TSOA_NotLinked".Translate();
//            }

//            StringBuilder sb = new StringBuilder();

//            Corpse corpse = GetCorpse();
//            if (corpse != null)
//            {
//                if (ticksWithCorpse > 0)
//                {
//                    int ticksLeft = consumeTicks - ticksWithCorpse;
//                    if (ticksLeft > 0)
//                    {
//                        float hours = ticksLeft / 2500f;

//                        sb.AppendLine("TSOA_CorpseConsumptionIn".Translate(hours.ToString("F1")));
//                    }
//                }

//                int essence = CalculateEssence();
//                sb.Append("TSOA_EssenceYield".Translate(essence));
//            }

//            return sb.ToString().TrimEnd();
//        }

//        public override IEnumerable<Gizmo> CompGetGizmosExtra()
//        {
//            foreach (var g in base.CompGetGizmosExtra())
//                yield return g;

//            if (DebugSettings.godMode)
//            {
//                Corpse corpse = GetCorpse();
//                if (corpse == null)
//                    yield break;

//                yield return new Command_Action
//                {
//                    defaultLabel = "DEV: Set timer to 1",
//                    defaultDesc = "Sets countdown until corpse is consumed to 1 tick.",
//                    action = () =>
//                    {
//                        ticksWithCorpse = 59999;
//                    }
//                };
//            }
//        }
//    }
//}
