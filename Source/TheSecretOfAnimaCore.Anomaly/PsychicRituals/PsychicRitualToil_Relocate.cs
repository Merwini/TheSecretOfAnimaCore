using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_Relocate : PsychicRitualToil_AnimaAffinity
{
    private PsychicRitualRoleDef invokerRole;
    private IntVec3 targetCell;
    private Thing ritualFocus;

    public PsychicRitualToil_Relocate()
    {
    }

    public PsychicRitualToil_Relocate(PsychicRitualRoleDef invokerRole, IntVec3 targetCell, Thing ritualFocus)
    {
        this.invokerRole = invokerRole;
        this.targetCell = targetCell;
        this.ritualFocus = ritualFocus;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);

        float rand = Rand.Range(0f, 1f);
        float failureDegree = rand - psychicRitual.PowerPercent;
        if (failureDegree < 0f)
        {
            failureDegree = 0f;
        }
        int failureRadius = Mathf.RoundToInt(((PsychicRitualDef_Relocate)psychicRitual.def).relocateCurve.Evaluate(failureDegree));
        Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);

        psychicRitual.ReleaseAllPawnsAndBuildings();

        if (ritualFocus != null && !ritualFocus.Destroyed && ritualFocus.Spawned)
        {
            ApplyOutcome(psychicRitual, failureRadius, invoker);
        }
    }

        private void ApplyOutcome(PsychicRitual psychicRitual, int failureRadius, Pawn invoker)
        {
            Map map = ritualFocus.Map;
            IntVec3 destination;
            if (failureRadius <= 0 && PsychicRitualDef_Relocate.RelocateValidator(targetCell, map))
            {
                destination = targetCell;
            }
            else if (CellFinder.TryFindRandomCellNear(targetCell, map, failureRadius,
                cell => PsychicRitualDef_Relocate.RelocateValidator(cell, map),
                out IntVec3 result))
            {
                destination = result;
            }
            else
            {
                destination = ritualFocus.Position;
            }

        if (destination.IsValid && destination != ritualFocus.Position)
        {
            EffecterDefOf.Skip_Entry.Spawn(ritualFocus.Position, map);
            EffecterDefOf.Skip_Exit.Spawn(destination, map);
            SkipUtility.SkipTo(ritualFocus, destination, map);
            Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), (failureRadius == 0 ? "TSOA_RelocateSuccess" : "TSOA_RelocateFailure").Translate(invoker, psychicRitual.def.Named("RITUAL")), LetterDefOf.NeutralEvent);
        }
        else
        {
            Messages.Message("TSOA_RelocationNoMove".Translate(), MessageTypeDefOf.NegativeEvent);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
        Scribe_Values.Look(ref targetCell, "targetCell", IntVec3.Invalid);
        Scribe_References.Look(ref ritualFocus, "ritualFocus");
    }
}