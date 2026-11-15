using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_Fever : PsychicRitualToil_AnimaAffinity
{
    PsychicRitualRoleDef invokerRole;
    PsychicRitualRoleDef targetRole;

    public PsychicRitualToil_Fever()
    {
    }

    public PsychicRitualToil_Fever(PsychicRitualRoleDef invokerRole, PsychicRitualRoleDef targetRole)
    {
        this.invokerRole = invokerRole;
        this.targetRole = targetRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);

        bool success = Rand.Chance(((PsychicRitualDef_Fever)psychicRitual.def).successCurve.Evaluate(psychicRitual.PowerPercent));

        Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
        IEnumerable<Pawn> targets = psychicRitual.assignments.AssignedPawns(targetRole).Where(p => p != null && !p.Dead && p.Spawned);

        if (invoker != null && !targets.EnumerableNullOrEmpty())
        {
            ApplyOutcome(psychicRitual, success, invoker, targets);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, bool success, Pawn invoker, IEnumerable<Pawn> targets)
    {
        foreach (Pawn target in targets)
        {
            Hediff hediff;
            if (success)
            {
                hediff = HediffMaker.MakeHediff(Anomaly_HediffDefOf.TSOA_PsychicFever_Success, target);
            }
            else
            {
                hediff = HediffMaker.MakeHediff(Anomaly_HediffDefOf.TSOA_PsychicFever_Failure, target);
            }
            Hediff existing = target.health.hediffSet.GetFirstHediffOfDef(Anomaly_HediffDefOf.TSOA_PsychicFever_Success);
            Hediff existing2 = target.health.hediffSet.GetFirstHediffOfDef(Anomaly_HediffDefOf.TSOA_PsychicFever_Failure);
            if (existing != null)
            {
                target.health.RemoveHediff(existing);
            }
            if (existing2 != null)
            {
                target.health.RemoveHediff(existing2);
            }
            target.health.AddHediff(hediff);
        }

        string durationDays;
        if (success)
        {
            durationDays = Anomaly_HediffDefOf.TSOA_PsychicFever_Success.CompProps<HediffCompProperties_PsychicFever>().durationDays.ToString("F1");
        }
        else
        {
            FloatRange failureDurationDays = Anomaly_HediffDefOf.TSOA_PsychicFever_Failure.CompProps<HediffCompProperties_PsychicFever>().durationDaysRange;
            durationDays = failureDurationDays.ToString();
        }

        Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), (success ? "TSOA_FeverSuccess" : "TSOA_FeverFailure").Translate(invoker, durationDays, psychicRitual.def.Named("RITUAL")), LetterDefOf.NeutralEvent);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
        Scribe_Defs.Look(ref targetRole, "targetRole");
    }
}