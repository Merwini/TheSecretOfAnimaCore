using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_Fever : PsychicRitualDef_Unlocked
{
    public SimpleCurve successCurve;

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, parent);
        list.Add(new PsychicRitualToil_Fever(InvokerRole, TargetRole));
        list.Add(new PsychicRitualToil_TargetCleanup(InvokerRole, TargetRole));
        return list;
    }

    public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
    {
        return outcomeDescription.Formatted(successCurve.Evaluate(qualityRange.min).ToStringPercent());
    }
}