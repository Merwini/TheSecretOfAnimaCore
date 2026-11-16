using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI.Group;
using UnityEngine;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_FlowerPower : PsychicRitualToil_AnimaAffinity
{
    private PsychicRitualRoleDef invokerRole;

    public PsychicRitualToil_FlowerPower()
    {
    }

    public PsychicRitualToil_FlowerPower(PsychicRitualRoleDef invokerRole)
    {
        this.invokerRole = invokerRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);
        Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
        float outcomeStrength = ((PsychicRitualDef_FlowerPower)psychicRitual.def).flowerCurve.Evaluate(psychicRitual.PowerPercent);
        float durationHours = ((PsychicRitualDef_FlowerPower)psychicRitual.def).durationHours;

        if (pawn != null)
        {
            ApplyOutcome(psychicRitual, invokerRole, outcomeStrength, durationHours);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, PsychicRitualRoleDef invokerRole, float outcomeStrength, float durationHours)
    {
        MapComponent_AnimaRitual mapComp = psychicRitual.Map.GetComponent<MapComponent_AnimaRitual>();
        mapComp.flowerPowerEndTick = Find.TickManager.TicksGame + Mathf.RoundToInt(durationHours * GenDate.TicksPerHour);
        mapComp.flowerPowerMult = outcomeStrength;
        float durationDays = durationHours * GenDate.TicksPerHour / GenDate.TicksPerDay;

        Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), "TSOA_FlowerPowerSuccess".Translate(psychicRitual.assignments.FirstAssignedPawn(invokerRole), durationDays.ToString("F1"), outcomeStrength.ToString("P1"), psychicRitual.def.Named("RITUAL")), LetterDefOf.NeutralEvent);
    }

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref invokerRole, "invokerRole");

        base.ExposeData();
    }
}
