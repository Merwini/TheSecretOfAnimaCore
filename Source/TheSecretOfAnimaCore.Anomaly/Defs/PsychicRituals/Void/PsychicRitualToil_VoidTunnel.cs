using RimWorld;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_VoidTunnel : PsychicRitualToil
{
    PsychicRitualRoleDef invokerRole;

    public PsychicRitualToil_VoidTunnel()
    {
    }

    public PsychicRitualToil_VoidTunnel(PsychicRitualRoleDef invokerRole)
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
        gate.PrepareVoidTunnel(ritualQuality);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
    }
}