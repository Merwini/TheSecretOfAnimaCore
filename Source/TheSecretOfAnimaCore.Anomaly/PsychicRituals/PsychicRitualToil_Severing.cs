using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_Severing : PsychicRitualToil_AnimaAffinity
{
    private PsychicRitualRoleDef invokerRole;
    private FloatRange failureDurationDays;

    public PsychicRitualToil_Severing()
    {
    }

    public PsychicRitualToil_Severing(PsychicRitualRoleDef invokerRole, FloatRange failureDurationDays)
    {
        this.invokerRole = invokerRole;
        this.failureDurationDays = failureDurationDays;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);
        Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
        bool positiveOutcome = Rand.Chance(psychicRitual.PowerPercent);
        psychicRitual.ReleaseAllPawnsAndBuildings();
        if (pawn != null)
        {
            ApplyOutcome(psychicRitual, pawn, positiveOutcome);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker, bool positiveOutcome)
    {
        float severityPerDay = Anomaly_HediffDefOf.TSOA_PsychicSevering.CompProps<HediffCompProperties_SeverityPerDay>().severityPerDay;

        foreach (Pawn humanLike in invoker.Map.mapPawns.AllHumanlikeSpawned)
        {
            Hediff hediff = HediffMaker.MakeHediff(Anomaly_HediffDefOf.TSOA_PsychicSevering, humanLike);
            if (positiveOutcome)
            {
                hediff.Severity = 2 * -severityPerDay;
            }
            else
            {
                hediff.Severity = Rand.Range(failureDurationDays.min * -severityPerDay, failureDurationDays.max * -severityPerDay);
            }
            Hediff existing = humanLike.health.hediffSet.GetFirstHediffOfDef(Anomaly_HediffDefOf.TSOA_PsychicSevering);
            if (existing != null)
            {
                humanLike.health.RemoveHediff(existing);
            }
            humanLike.health.AddHediff(hediff);
        }
        Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), (positiveOutcome ? "TSOA_SeveringSuccess" : "TSOA_SeveringFailure").Translate(invoker, (positiveOutcome ? 2 : failureDurationDays.ToString()), psychicRitual.def.Named("RITUAL")), LetterDefOf.NeutralEvent);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
        Scribe_Values.Look(ref failureDurationDays, "failureDurationDays");
    }
}
