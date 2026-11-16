using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_VoidDive : PsychicRitualDef_Unlocked
{
    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, parent);
        list.Add(new PsychicRitualToil_VoidDive());
        //list.Add(new PsychicRitualToil_TargetCleanup(InvokerRole, TargetRole)); // will need this just for VoidTunnel with sacrifice, leaving as a reminder
        return list;
    }

    public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
    {
        foreach (string item in base.BlockingIssues(assignments, map))
        {
            yield return item;
        }
        if (assignments.Target.Thing is Building_VoidGate gate && gate.IsGateOpen)
        {
            yield return "TSOA_RitualBlockedGateOpen".Translate();
        }
    }

    // TODO do I need an OutcomeDescription if there is no success/failure?
}