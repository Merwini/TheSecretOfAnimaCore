//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;
//using RimWorld;

//namespace tsoa.core
//{
//    public class HediffComp_SetPiece : HediffComp
//    {
//        public HediffCompProperties_SetPiece Props => (HediffCompProperties_SetPiece)this.props;

//        public bool HediffIsSetCore => parent.TryGetComp<HediffComp_SetCore>() != null;

//        private HediffComp_SetCore setCoreComp;

//        private bool evolving = false;

//        public HediffComp_SetCore SetCoreComp
//        {
//            get
//            {
//                if (setCoreComp == null)
//                {
//                    var allHediffs = parent.pawn.health.hediffSet.hediffs;

//                    foreach (var hediff in allHediffs)
//                    {
//                        var comp = hediff.TryGetComp<HediffComp_SetCore>();
//                        if (comp != null && comp.Props.setName == Props.setName)
//                        {
//                            setCoreComp = comp;
//                            return setCoreComp;
//                        }
//                    }

//                    var newCore = HediffMaker.MakeHediff(Props.coreHediff, parent.pawn, parent.pawn.RaceProps.body.corePart);
//                    parent.pawn.health.AddHediff(newCore);

//                    setCoreComp = newCore.TryGetComp<HediffComp_SetCore>();
//                }
//                return setCoreComp;
//            }
//        }

//        public override void CompPostPostAdd(DamageInfo? dinfo)
//        {
//            base.CompPostPostAdd(dinfo);

//            if (SetCoreComp != null && !SetCoreComp.currentSet.Contains(parent))
//            {
//                SetCoreComp.RebaseSetPieceList();
//            }
//        }

//        public override void CompPostPostRemoved()
//        {
//            base.CompPostPostRemoved();

//            // Can skip rebase if evolving, since the comp on the new hediff will call it
//            if (SetCoreComp != null && SetCoreComp.currentSet.Contains(parent) && !evolving)
//            {
//                SetCoreComp.RebaseSetPieceList();
//            }
//        }

//        public override void CompPostTick(ref float severityAdjustment)
//        {
//            base.CompPostTick(ref severityAdjustment);

//            if (parent.pawn.IsHashIntervalTick(900))
//            {
//                CheckIfEvolveDevolve();
//            }
//        }

//        void CheckIfEvolveDevolve()
//        {
//            int numberOfPieces = SetCoreComp.currentSet.Count;
//            Log.Message($"{parent.Label} got set count: {numberOfPieces}. Lower threshold: {Props.lowerThreshold.ToString()}. Upper threshold: {Props.upperThreshold}.");

//            if (numberOfPieces < Props.lowerThreshold && Props.hediffBelow != null)
//            {
//                Log.Message($"devolving to: {Props.hediffAbove.defName}");
//                EvolveHediffTo(Props.hediffBelow);
//            }
//            else if (numberOfPieces > Props.upperThreshold && Props.hediffAbove != null)
//            {
//                Log.Message($"evolving to: {Props.hediffAbove.defName}");
//                EvolveHediffTo(Props.hediffAbove);
//            }
//        }

//        void EvolveHediffTo(HediffDef def)
//        {
//            Pawn pawn = parent.pawn;
//            BodyPartRecord part = parent.Part;
//            evolving = true;

//            ResetSetPiecesIfThisIsCore() todochangethistoupdatethereferences,noreasontomakethemdoitthemselves

//            pawn.health.RemoveHediff(parent);

//            float maxHp = part.def.GetMaxHealth(pawn);
//            float currentHp = pawn.health.hediffSet.GetPartHealth(part);
//            float partHpPercent = currentHp / maxHp;

//            dfgjksnflgjksnfkldjgnsklj

//            // Pawn already has the new hediff on the part somehow
//            if (pawn.health.hediffSet.HasHediff(def, part))
//                return;

//            Hediff newHediff = HediffMaker.MakeHediff(def, pawn, part);
//            parent.pawn.health.AddHediff(newHediff);
//        }

//        void ResetSetPiecesIfThisIsCore()
//        {
//            var coreComp = parent.TryGetComp<HediffComp_SetCore>();
//            if (coreComp != null)
//            {
//                coreComp.RemoveSelfReferenceFromSetPieces();
//            }
//        }

//        public void ClearCore()
//        {
//            setCoreComp = null;
//        }
//    }
//}
