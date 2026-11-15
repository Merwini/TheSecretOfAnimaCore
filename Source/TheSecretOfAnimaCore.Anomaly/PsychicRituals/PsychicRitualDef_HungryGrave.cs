using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_HungryGrave : PsychicRitualDef_Unlocked
{
    public SimpleCurve graveCurve;
    public float baseDurationHours;

    public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
    {
        foreach (string item in base.BlockingIssues(assignments, map))
        {
            yield return item;
        }
        
        if (map.listerThings.ThingsOfDef(ThingDef.Named("TSOA_AnimaRootGrave")).Count == 0)
        {
            yield return "TSOA_HungryGraveRitualBlocker".Translate();
        }
    }

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
        list.Add(new PsychicRitualToil_HungryGrave(InvokerRole));
        return list;
    }
}
