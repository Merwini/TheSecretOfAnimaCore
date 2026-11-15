using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_Regrowth : PsychicRitualDef_Unlocked
{
    public SimpleCurve successCurve;

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
        list.Add(new PsychicRitualToil_Regrowth(InvokerRole, TargetRole));
        return list;
    }

    public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
    {
        foreach (string item in base.BlockingIssues(assignments, map))
        {
            yield return item;
        }
        Pawn target = assignments.FirstAssignedPawn(TargetRole);
            
        if (!HasPermanentInjuries(target))
        {
            yield return "TSOA_RegrowthRitualBlocker".Translate();
        }
    }

    public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
    {
        return outcomeDescription.Formatted(successCurve.Evaluate(qualityRange.min).ToStringPercent());
    }

    public bool HasPermanentInjuries(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null)
            return false;

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is Hediff_MissingPart mp)
            {
                // Skip regrowing eyes for Blindsight pawns
                if (ModsConfig.IdeologyActive && pawn.Ideo != null && pawn.Ideo.HasMeme(TSOAR_DefOf.Blindsight) && mp.Part.def == BodyPartDefOf.Eye)
                    continue;

                return true;
            }

            if (hediff is Hediff_Injury injury && injury.IsPermanent())
                return true;
        }

        return false;
    }
}