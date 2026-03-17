using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace tsoa.core;

public class JobDriver_PlantAnimaPearl : JobDriver
{
    private const int PlantTicks = 600;

    private Thing Pearl => job.GetTarget(TargetIndex.A).Thing;
    private IntVec3 PlantCell => job.GetTarget(TargetIndex.B).Cell;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Pearl, job, 1, -1, null, errorOnFailed) && pawn.Reserve(PlantCell, job, 1, -1, null, errorOnFailed); ;
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnForbidden(TargetIndex.A);
        this.FailOn(() => !PlantCell.InBounds(pawn.Map));
        this.FailOn(() => !PlantCell.Standable(pawn.Map));
        // Is this wasted CPU cycles? Already validate fertility during targeting. I guess it stops bugs if something spawns/is built in the target cell after the job starts
        this.FailOn(() => pawn.Map.fertilityGrid.FertilityAt(PlantCell) <= ThingDefOf.Plant_TreeAnima.plant.fertilityMin);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);

        Toil goToCell = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);
        yield return goToCell;

        Toil plant = ToilMaker.MakeToil("PlantAnimaPearl");
        plant.initAction = delegate
        {
            pawn.pather.StopDead();
        };
        plant.defaultCompleteMode = ToilCompleteMode.Delay;
        plant.defaultDuration = PlantTicks;
        plant.WithProgressBarToilDelay(TargetIndex.B);
        plant.PlaySustainerOrSound(() => SoundDefOf.Replant_Complete);
        yield return plant;

        Toil finish = ToilMaker.MakeToil("FinishPlantAnimaPearl");
        finish.initAction = delegate
        {
            Thing pearl = Pearl;
            Map map = pawn.Map;

            if (pearl != null && !pearl.Destroyed)
            {
                pearl.Destroy();
            }

            GenSpawn.Spawn(ThingDefOf.Plant_TreeAnima, PlantCell, map);
        };
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }
}
