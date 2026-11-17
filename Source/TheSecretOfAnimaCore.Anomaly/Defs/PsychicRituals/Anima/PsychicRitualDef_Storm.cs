using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_Storm : PsychicRitualDef_Unlocked
{
    public SimpleCurve successCurve;

    List<GameCondition> droughts;

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
        list.Add(new PsychicRitualToil_Storm(InvokerRole));

        return list;
    }

    public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
    {
        bool hasDrought = !droughts.NullOrEmpty();
        return outcomeDescription.Formatted((successCurve.Evaluate(qualityRange.min) * (hasDrought ? 0.5f : 1f)).ToStringPercent());
    }

    public override void InitializeCast(Map map)
    {
        base.InitializeCast(map);

        droughts = map.GameConditionManager.ActiveConditions.Where(gc => gc.def.defName == "Drought").ToList();
    }
}
