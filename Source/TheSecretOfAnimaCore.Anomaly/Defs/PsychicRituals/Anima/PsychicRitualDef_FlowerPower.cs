using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_FlowerPower : PsychicRitualDef_Unlocked
{
    public SimpleCurve flowerCurve;
    public float durationHours;

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
        list.Add(new PsychicRitualToil_FlowerPower(InvokerRole));
        return list;
    }

    //public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
    //{
    //    // TODO change this
    //    return outcomeDescription.Formatted(qualityRange.min.ToStringPercent());
    //}
}
