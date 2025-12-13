//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;
//using RimWorld;
//using Steamworks;

//namespace nuff.tsoa.core
//{
//    public class HediffComp_SetCore : HediffComp
//    {
//        public HediffCompProperties_SetCore Props => (HediffCompProperties_SetCore)this.props;

//        public List<Hediff> currentSet = new List<Hediff>();

//        public List<HediffComp_SetPiece> currentComps = new List<HediffComp_SetPiece>();

//        public override void CompPostTick(ref float severityAdjustment)
//        {
//            base.CompPostTick(ref severityAdjustment);

//            if (parent.pawn.IsHashIntervalTick(900))
//            {
//                RebaseSetPieceList();

//                // Remove this hediff if all set pieces are gone
//                if (currentSet.Count <= 1 && currentSet.Contains(parent))
//                {
//                    parent.pawn.health.RemoveHediff(parent);
//                }
//            }
//        }

//        public override void CompPostPostAdd(DamageInfo? dinfo)
//        {
//            RebaseSetPieceList();
//        }

//        internal void RebaseSetPieceList()
//        {
//            currentSet.Clear();
//            currentComps.Clear();
//            foreach (var hediff in parent.pawn.health.hediffSet.hediffs)
//            {
//                var comp = hediff.TryGetComp<HediffComp_SetPiece>();
//                if (comp != null && comp.Props.setName == Props.setName)
//                {
//                    currentSet.Add(hediff);
//                    currentComps.Add(comp);
//                }
//            }
//        }

//        public void RemoveSelfReferenceFromSetPieces()
//        {
//            foreach (var comp in currentComps)
//            {
//                if ((comp.parent != this.parent) && comp.SetCoreComp == this)
//                {
//                    comp.ClearCore();
//                }
//            }
//        }
//    }
//}
