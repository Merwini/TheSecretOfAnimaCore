using Verse;
using Verse.AI;
using RimWorld;

namespace tsoa.core.anomaly;

public class PsychicRitualRoleDef_VoidGate : PsychicRitualRoleDef
{
    public override bool CanReach(Pawn pawn, TargetInfo target)
    {
        return pawn.CanReachNonLocal(target, PathEndMode.InteractionCell, Danger.Deadly);
    }
}