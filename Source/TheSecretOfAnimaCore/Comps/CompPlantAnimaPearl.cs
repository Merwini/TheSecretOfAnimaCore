using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;

namespace tsoa.core;

public class CompPlantAnimaPearl : ThingComp
{
    public CompProperties_PlantAnimaPearl Props => (CompProperties_PlantAnimaPearl)props;

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
        {
            yield return option;
        }

        if (selPawn == null || !selPawn.IsColonistPlayerControlled)
            yield break;

        if (!selPawn.CanReserve(parent))
        {
            yield return new FloatMenuOption("TSOA_CannotPlant".Translate() + ": " + "Reserved".Translate(), null);
            yield break;
        }

        yield return new FloatMenuOption("TSOA_PlantAnimaPearl".Translate(), delegate
        {
            Find.Targeter.BeginTargeting(
                new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetPawns = false,
                    canTargetBuildings = false,
                    canTargetItems = false,
                    validator = t =>
                    {
                        IntVec3 cell = t.Cell;
                        return cell.InBounds(selPawn.Map)
                            && cell.Standable(selPawn.Map)
                            && selPawn.Map.fertilityGrid.FertilityAt(cell) > 0.08f;
                    }
                },
                delegate (LocalTargetInfo target)
                {
                    Job job = JobMaker.MakeJob(TSOA_DefOf.TSOA_PlantAnimaPearl, parent, target.Cell);
                    job.count = 1;
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        });
    }
}
