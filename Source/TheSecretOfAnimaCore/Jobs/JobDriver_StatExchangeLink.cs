using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace tsoa.core
{
    public class JobDriver_StatExchangeLink : JobDriver
    {
        public Pawn Master => (Pawn)job.targetA.Thing;
        public Pawn Other => (Pawn)job.targetB.Thing;

        private HediffComp_StatExchanger MasterComp => Master?.health?.hediffSet?.hediffs?
                .Select(h => h.TryGetComp<HediffComp_StatExchanger>())
                .FirstOrDefault(c => c != null && c.Props.isMaster);

        public int DurationTicks =>
            MasterComp?.Props?.linkJobDuration ?? 600;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Other, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            AddFinishAction((JobCondition _) =>
            {
                ReleaseOtherPawn();
            });

            yield return Toils_Goto.GotoThing(
                pawn == Master ? TargetIndex.B : TargetIndex.A,
                PathEndMode.Touch
            );

            Other.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, pawn), JobCondition.InterruptForced, resumeCurJobAfterwards: true);

            Toil wait = Toils_General.Wait(DurationTicks);
            wait.WithProgressBarToilDelay(
                pawn == Master ? TargetIndex.B : TargetIndex.A
            );
            wait.handlingFacing = true;

            wait.FailOn(() =>
                !Master.Spawned ||
                !Other.Spawned ||
                Master.Dead ||
                Other.Dead ||
                Master.Position.DistanceToSquared(Other.Position) > 2
            ); 

            yield return wait;

            Toil finalize = Toils_General.Do(() =>
            {
                MasterComp.LinkOtherPawn(Other);

                EffecterDef effDef = MasterComp.Props.linkCompleteEffecter;
                if (effDef != null)
                {
                    Effecter eff = effDef.Spawn();
                    eff.scale = MasterComp.Props.linkCompleteEffecterScale;
                    eff.Trigger(Master, Other);
                    eff.Cleanup();
                }

                SoundDef snd = MasterComp.Props.linkCompleteSound;
                if (snd != null)
                {
                    snd.PlayOneShot(new TargetInfo(Master.Position, Master.Map));
                }
            });
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return finalize;
        }

        void ReleaseOtherPawn()
        {
            if (Other?.jobs == null)
                return;

            if (Other.CurJobDef == JobDefOf.Wait)
            {
                Other.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
        }
    }
}
