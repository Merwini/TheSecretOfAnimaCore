using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI.Group;
using Verse;
using RimWorld;
using tsoa.core;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_AnimaAffinity : PsychicRitualToil
{
    public override void End(PsychicRitual psychicRitual, PsychicRitualGraph parent, bool success)
    {
        if (success && psychicRitual.def is PsychicRitualDef_Unlocked unlocked)
        {
            IEnumerable<Pawn> invokers = psychicRitual.assignments.AssignedPawns(unlocked.InvokerRole);
            foreach (Pawn invoker in invokers)
            {
                Hediff_AnimaAffinity.AddOrUpdateAffinityHediff(invoker, unlocked.invokerAffinity);
            }

            IEnumerable<Pawn> targets = psychicRitual.assignments.AssignedPawns(unlocked.TargetRole);
            foreach (Pawn target in targets)
            {
                Hediff_AnimaAffinity.AddOrUpdateAffinityHediff(target, unlocked.targetAffinity);
            }

            IEnumerable<Pawn> chanters = psychicRitual.assignments.AssignedPawns(unlocked.ChanterRole);
            foreach (Pawn chanter in chanters)
            {
                Hediff_AnimaAffinity.AddOrUpdateAffinityHediff(chanter, unlocked.chanterAffinity);
            }
        }

        base.End(psychicRitual, parent, success);
    }
}
