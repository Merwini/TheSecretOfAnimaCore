using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace nuff.tsoa.core
{
    public class JobDriver_TakeAnimaSapOutOfBasin : JobDriver
    {
        private const TargetIndex TapInd = TargetIndex.A;
        private const TargetIndex SapInd = TargetIndex.B;
        private const TargetIndex StoreCellInd = TargetIndex.C;

        private const int Duration = 600;

        protected Building_AnimaSapBasin Tap => (Building_AnimaSapBasin)job.GetTarget(TapInd).Thing;

        protected Thing Sap => job.GetTarget(SapInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Tap, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TapInd);
            this.FailOnBurningImmobile(TapInd);

            yield return Toils_Goto.GotoThing(TapInd, PathEndMode.Touch);

            yield return Toils_General.Wait(Duration)
                .FailOnDestroyedNullOrForbidden(TapInd)
                .FailOnCannotTouch(TapInd, PathEndMode.Touch)
                .FailOn(() => Tap.innerContainer.Count == 0)
                .WithProgressBarToilDelay(TapInd);

            Toil extract = ToilMaker.MakeToil("TakeSapFromBasin");
            extract.initAction = delegate
            {
                if (Tap.innerContainer.Count == 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing sap = Tap.innerContainer[0];
                Tap.innerContainer.Remove(sap);

                GenPlace.TryPlaceThing(sap, pawn.Position, Map, ThingPlaceMode.Near);

                Tap.DirtyMapMesh(Map);
                Tap.emptyNow = false;
                Tap.UpdateDesignation();

                StoragePriority prio = StoreUtility.CurrentStoragePriorityOf(sap);
                IntVec3 bestCell;

                if (StoreUtility.TryFindBestBetterStoreCellFor(sap, pawn, Map, prio, pawn.Faction, out bestCell))
                {
                    job.SetTarget(StoreCellInd, bestCell);
                    job.SetTarget(SapInd, sap);
                    job.count = sap.stackCount;
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            extract.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return extract;

            yield return Toils_Reserve.Reserve(SapInd);
            yield return Toils_Reserve.Reserve(StoreCellInd);

            yield return Toils_Goto.GotoThing(SapInd, PathEndMode.ClosestTouch);

            yield return Toils_Haul.StartCarryThing(SapInd);

            Toil carry = Toils_Haul.CarryHauledThingToCell(StoreCellInd);
            yield return carry;

            yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carry, storageMode: true);
        }
    }
}
