using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_HungryGrave : PsychicRitualToil_AnimaAffinity
{
    private PsychicRitualRoleDef invokerRole;

    public PsychicRitualToil_HungryGrave()
    {
    }

    public PsychicRitualToil_HungryGrave(PsychicRitualRoleDef invokerRole)
    {
        this.invokerRole = invokerRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);
        Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
        PsychicRitualDef_HungryGrave def = (PsychicRitualDef_HungryGrave)psychicRitual.def;
        float durationHours = def.baseDurationHours * def.graveCurve.Evaluate(psychicRitual.PowerPercent);

        if (pawn != null)
        {
            ApplyOutcome(psychicRitual, invokerRole, durationHours);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, PsychicRitualRoleDef invokerRole, float durationHours)
    {
        psychicRitual.Map.GetComponent<MapComponent_AnimaRitual>().hungryGraveEndTick = Find.TickManager.TicksGame + Mathf.RoundToInt(durationHours * GenDate.TicksPerHour);

        Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), "TSOA_HungryGraveSuccess".Translate(psychicRitual.assignments.FirstAssignedPawn(invokerRole), durationHours.ToString("F1"), psychicRitual.def.Named("RITUAL")), LetterDefOf.NeutralEvent);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
    }
}
