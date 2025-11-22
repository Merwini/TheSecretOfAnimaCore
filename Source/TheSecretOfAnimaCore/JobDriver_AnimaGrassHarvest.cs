using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;

namespace nuff.tsoa.core
{

    public class JobDriver_AnimaGrassHarvest : JobDriver
    {
        private const int HarvestPauseTicks = 120;

        private Thing Tree => TargetA.Thing;

        private Thing targetGrass;

        private CompSpawnSubplant Comp => Tree?.TryGetComp<CompSpawnSubplant>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            //return pawn.Reserve(Tree, job, 1, -1, null, errorOnFailed);
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            // anima tree is TargetA
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.mindState.duty == null);
            this.FailOn(() => Comp == null);

            // pick next grass
            Toil pickNextGrass = new Toil();
            pickNextGrass.initAction = () =>
            {
                targetGrass = FindRandomGrass();

                if (targetGrass == null)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
            };
            pickNextGrass.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return pickNextGrass;

            // move to the grass
            Toil gotoGrass = new Toil();
            gotoGrass.initAction = () =>
            {
                if (targetGrass == null || !targetGrass.Spawned)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.pather.StartPath(targetGrass.Position, PathEndMode.Touch);
            };

            gotoGrass.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            gotoGrass.FailOn(() => targetGrass == null || targetGrass.Destroyed);
            yield return gotoGrass;

            // pause and "harvest" the grass
            Toil harvestPause = new Toil();
            harvestPause.defaultCompleteMode = ToilCompleteMode.Delay;
            harvestPause.defaultDuration = HarvestPauseTicks;

            harvestPause.tickAction = () =>
            {
                Pawn actor = harvestPause.actor;

                if (targetGrass == null || !targetGrass.Spawned)
                {
                    ReadyForNextToil();
                    return;
                }

                actor.rotationTracker.FaceTarget(targetGrass);

                if (actor.IsHashIntervalTick(30))
                {
                    FleckMaker.ThrowMicroSparks(targetGrass.TrueCenter(), actor.Map);
                    FleckMaker.ThrowDustPuff(targetGrass.TrueCenter(), actor.Map, 1.2f);
                }
            };

            yield return harvestPause;

            // loop back to the start
            yield return Toils_Jump.Jump(pickNextGrass);
        }

        private Thing FindRandomGrass()
        {
            if (Comp == null) return null;

            List<Thing> grasses = Comp.SubplantsForReading;
            if (grasses == null || grasses.Count == 0) return null;

            return grasses.RandomElement();
        }
    }
}
