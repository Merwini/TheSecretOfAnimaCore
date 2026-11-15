using RimWorld;
using Verse;

namespace tsoa.core.anomaly;

public class JobGiver_DefendMinerals : JobGiver_HiveDefense
{
    public override IntVec3 GetFlagPosition(Pawn pawn)
    {
        return pawn.mindState.duty.focus.Cell;
    }

    public override bool ExtraTargetValidator(Pawn pawn, Thing target)
    {
        return target is Pawn
            && target.Position.InHorDistOf(GetFlagPosition(pawn), GetFlagRadius(pawn))
            && base.ExtraTargetValidator(pawn, target);
    }

    public override bool ShouldLoseTarget(Pawn pawn)
    {
        Thing target = pawn.mindState.enemyTarget;
        return target == null
            || !target.Position.InHorDistOf(GetFlagPosition(pawn), GetFlagRadius(pawn))
            || base.ShouldLoseTarget(pawn);
    }
}
