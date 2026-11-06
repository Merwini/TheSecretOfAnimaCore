using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;

namespace tsoa.core;

public class CompProperties_DyingAnimaTree : CompProperties
{
    public CompProperties_DyingAnimaTree()
    {
        compClass = typeof(CompDyingAnimaTree);
    }
}

public class CompDyingAnimaTree : ThingComp
{
    private static FloatMenuOption DisabledOption(string reason)
    {
        return new FloatMenuOption("TSOA_ComfortAnimaTree".Translate() + ": " + reason, null);
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        foreach (var option in base.CompFloatMenuOptions(selPawn))
        {
            yield return option;
        }
        
        if (selPawn.GetPsylinkLevel() <= 0)
        {
            yield return DisabledOption("TSOA_ComfortAnimaTreeRequiresPsylink".Translate());
            yield break;
        }

        if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly) ||
            !RCellFinder.TryFindGoodAdjacentSpotToTouch(selPawn, parent, out _))
        {
            yield return DisabledOption("NoPath".Translate());
            yield break;
        }

        if (!selPawn.CanReserve(parent))
        {
            yield return DisabledOption("Reserved".Translate());
            yield break;
        }

        if (parent.IsForbidden(selPawn))
        {
            yield return DisabledOption("ForbiddenLower".Translate());
            yield break;
        }

        yield return new FloatMenuOption("TSOA_ComfortAnimaTree".Translate(), () =>
        {
            Job job = JobMaker.MakeJob(TSOA_DefOf.TSOA_ComfortAnimaTree, parent);
            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        });
    }
}
