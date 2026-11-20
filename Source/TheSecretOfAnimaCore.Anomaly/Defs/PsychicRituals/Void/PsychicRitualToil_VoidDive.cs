using RimWorld;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_VoidDive : PsychicRitualToil
{
    PsychicRitualRoleDef invokerRole;

    public PsychicRitualToil_VoidDive()
    {
    }

    public PsychicRitualToil_VoidDive(PsychicRitualRoleDef invokerRole)
    {
        this.invokerRole = invokerRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);

        
        // not much to do here
        if (psychicRitual.assignments.Target.Thing is Building_VoidGateEntrance gate)
        {
            ApplyOutcome(gate, psychicRitual.PowerPercent);
        }
    }

    private void ApplyOutcome(Building_VoidGateEntrance gate, float ritualQuality)
    {
        gate.PrepareVoidDive(ritualQuality);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
    }
}