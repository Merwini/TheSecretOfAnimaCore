using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace tsoa.core;

public class JobDriver_StatExchangeLink : JobDriver
{
    private HediffWithComps masterHediff;
    private HediffWithComps otherHediff;
    private int otherWaitJobId = -1;

    public Pawn Master => (Pawn)job.targetA.Thing;
    public Pawn Other => (Pawn)job.targetB.Thing;

    private HediffComp_StatExchanger MasterComp => masterHediff?.TryGetComp<HediffComp_StatExchanger>();
    private HediffComp_StatExchanger OtherComp => otherHediff?.TryGetComp<HediffComp_StatExchanger>();

    private bool CachedHediffsStillPresent =>
        masterHediff != null && otherHediff != null &&
        Master?.health?.hediffSet?.hediffs?.Contains(masterHediff) == true &&
        Other?.health?.hediffSet?.hediffs?.Contains(otherHediff) == true;

    public int DurationTicks =>
        MasterComp?.Props?.linkJobDuration ?? 600;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        masterHediff = Master?.health?.hediffSet?.hediffs?
            .OfType<HediffWithComps>()
            .FirstOrDefault(h => h.TryGetComp<HediffComp_StatExchanger>()?.IsMaster == true);

        HediffComp_StatExchanger masterComp = MasterComp;
        if (masterComp == null)
            return false;

        otherHediff = Other?.health?.hediffSet?.hediffs?
            .OfType<HediffWithComps>()
            .FirstOrDefault(h => masterComp.Props.targetHediffs?.Contains(h.def) == true &&
                h.TryGetComp<HediffComp_StatExchanger>() != null);

        return CachedHediffsStillPresent && pawn.Reserve(Other, job, 1, -1, null, errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
        this.FailOn(() => !CachedHediffsStillPresent);
        AddFinishAction((JobCondition _) =>
        {
            ReleaseOtherPawn();
        });

        yield return Toils_Goto.GotoThing(
            pawn == Master ? TargetIndex.B : TargetIndex.A,
            PathEndMode.Touch
        );

        // SetupToils also runs on load; keep the saved wait job instead of restarting it.
        if (otherWaitJobId < 0 && Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            Job otherWaitJob = JobMaker.MakeJob(JobDefOf.Wait, pawn);
            otherWaitJobId = otherWaitJob.loadID;
            Other.jobs.StartJob(otherWaitJob, JobCondition.InterruptForced, resumeCurJobAfterwards: true);
        }

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
            HediffComp_StatExchanger masterComp = MasterComp;
            HediffComp_StatExchanger otherComp = OtherComp;
            if (!CachedHediffsStillPresent || masterComp == null || otherComp == null ||
                masterComp.LinkedComps.Contains(otherComp) || otherComp.LinkedComps.Contains(masterComp) ||
                masterComp.LinkedComps.Count() >= masterComp.Props.maxLinks ||
                otherComp.LinkedComps.Count() >= otherComp.Props.maxLinks)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            masterComp.FormLink(otherComp);

            EffecterDef effDef = masterComp.Props.linkCompleteEffecter;
            if (effDef != null)
            {
                Effecter eff = effDef.Spawn();
                eff.scale = masterComp.Props.linkCompleteEffecterScale;
                eff.Trigger(Master, Other);
                eff.Cleanup();
            }

            SoundDef snd = masterComp.Props.linkCompleteSound;
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
        int waitJobId = otherWaitJobId;
        otherWaitJobId = -1;

        if (waitJobId >= 0 && Other?.jobs != null && Other.CurJob?.loadID == waitJobId)
        {
            Other.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref otherWaitJobId, "otherWaitJobId", -1);
        Scribe_References.Look(ref masterHediff, "masterHediff");
        Scribe_References.Look(ref otherHediff, "otherHediff");
    }

}
