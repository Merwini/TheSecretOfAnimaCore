using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace tsoa.core;

public class JobDriver_TakeAnimaAmberOutOfFont : JobDriver
{
    private const TargetIndex FontInd = TargetIndex.A;
    private const TargetIndex AmberInd = TargetIndex.B;
    private const TargetIndex StoreCellInd = TargetIndex.C;

    private const int Duration = 600;

    protected Building_AnimaFont Font => (Building_AnimaFont)job.GetTarget(FontInd).Thing;

    protected Thing Amber => job.GetTarget(AmberInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Font, job, 1, -1, null, errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(FontInd);
        this.FailOnBurningImmobile(FontInd);

        yield return Toils_Goto.GotoThing(FontInd, PathEndMode.Touch);

        yield return Toils_General.Wait(Duration)
            .FailOnDestroyedNullOrForbidden(FontInd)
            .FailOnCannotTouch(FontInd, PathEndMode.Touch)
            .WithProgressBarToilDelay(FontInd);

        Toil extract = ToilMaker.MakeToil("TakeAmberFromFont");
        extract.initAction = delegate
        {
            if (Font.AmberAmount == 0)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            // Check amber amount of font, spawn it
            Thing amber = GenSpawn.Spawn(TSOA_DefOf.TSOA_AnimaAmber, pawn.Position, Map);
            // TODO account for multiple stacks. Maybe just dump them all on the ground?
            amber.stackCount = Font.AmberAmount;
            Font.AmberAmount = 0;

            Font.ToggleEmptyNow();
            Font.DirtyMapMesh(Map);

            StoragePriority prio = StoreUtility.CurrentStoragePriorityOf(amber);
            IntVec3 bestCell;

            if (StoreUtility.TryFindBestBetterStoreCellFor(amber, pawn, Map, prio, pawn.Faction, out bestCell))
            {
                job.SetTarget(StoreCellInd, bestCell);
                job.SetTarget(AmberInd, amber);
                job.count = amber.stackCount;
            }
            else
            {
                EndJobWith(JobCondition.Incompletable);
            }
        };
        extract.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return extract;

        yield return Toils_Reserve.Reserve(AmberInd);
        yield return Toils_Reserve.Reserve(StoreCellInd);

        yield return Toils_Goto.GotoThing(AmberInd, PathEndMode.ClosestTouch);

        yield return Toils_Haul.StartCarryThing(AmberInd);

        Toil carry = Toils_Haul.CarryHauledThingToCell(StoreCellInd);
        yield return carry;

        yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carry, storageMode: true);
    }
}
