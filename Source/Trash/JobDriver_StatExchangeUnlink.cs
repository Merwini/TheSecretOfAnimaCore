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
    public class JobDriver_StatExchangeUnlink : JobDriver
    {
        // Job targets:
        // A = target pawn to unlink from
        public Pawn TargetPawn => (Pawn)job.targetA.Thing;

        private HediffComp_StatExchanger MasterComp => pawn?.health?.hediffSet?.hediffs?
            .Select(h => h.TryGetComp<HediffComp_StatExchanger>())
            .FirstOrDefault(c => c != null && c.Props.isMaster);

        public int DurationTicks => MasterComp?.Props?.unlinkJobDuration ?? 300;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => MasterComp == null);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => TargetPawn == null || TargetPawn.Dead);

            Toil wait = Toils_General.Wait(DurationTicks);
            wait.handlingFacing = true;
            wait.WithProgressBarToilDelay(TargetIndex.None);

            wait.tickAction = () =>
            {
                if (TargetPawn != null && TargetPawn.Spawned)
                    pawn.rotationTracker.FaceCell(TargetPawn.Position);
            };

            yield return wait;

            Toil finalize = Toils_General.Do(() =>
            {
                if (MasterComp == null || TargetPawn == null)
                    return;

                HediffComp_StatExchanger targetComp = TargetPawn.health?.hediffSet?.hediffs?
                    .Select(h => h.TryGetComp<HediffComp_StatExchanger>())
                    .FirstOrDefault(c => c != null);

                if (targetComp == null)
                    return;

                if (!MasterComp.LinkedComps.Contains(targetComp))
                    return;

                MasterComp.UnlinkOtherPawn(targetComp);

                EffecterDef effDef = MasterComp.Props.unlinkCompleteEffecter;
                if (effDef != null && pawn.Map != null)
                {
                    Effecter eff = effDef.Spawn();
                    eff.scale = MasterComp.Props.unlinkCompleteEffecterScale;
                    eff.Trigger(pawn, pawn);
                    eff.Cleanup();
                }

                SoundDef snd = MasterComp.Props.unlinkCompleteSound;
                if (snd != null && pawn.Map != null)
                {
                    snd.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                }
            });
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return finalize;
        }
    }
}
