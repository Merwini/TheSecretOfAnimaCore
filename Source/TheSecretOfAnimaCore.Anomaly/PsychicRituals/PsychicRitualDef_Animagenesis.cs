using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;
using tsoa.core;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_Animagenesis : PsychicRitualDef_Unlocked
{
    public SimpleCurve successCurve;
    public float minimumAffinity = 0.2f;

    public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
    {
        foreach (string item in base.BlockingIssues(assignments, map))
        {
            yield return item;
        }
        Pawn target = assignments.FirstAssignedPawn(TargetRole);
        Hediff_AnimaAffinity hediff = target?.health?.hediffSet?.GetFirstHediff<Hediff_AnimaAffinity>();


        if (hediff == null || hediff.CurrentAffinity < minimumAffinity)
        {
            yield return "TSOA_AnimagenesisRitualBlocker".Translate(minimumAffinity * 100);
        }
    }

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, parent);
        list.Add(new PsychicRitualToil_Animagenesis(InvokerRole, TargetRole));
        //list.Add(new PsychicRitualToil_TargetCleanup(InvokerRole, TargetRole));
        return list;
    }

    public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
    {
        return outcomeDescription.Formatted().ToString();
    }
}
