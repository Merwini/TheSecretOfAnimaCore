using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static UnityEngine.GridBrushBase;

namespace tsoa.core;

public class JobDriver_AddAnimaSapToFont : JobDriver
{
    private const TargetIndex FontInd = TargetIndex.A;
    private const TargetIndex SapInd = TargetIndex.B;

    private const int Duration = 600;

    protected Building_AnimaFont Font => (Building_AnimaFont)job.GetTarget(FontInd).Thing;
    protected Thing Sap => job.GetTarget(SapInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Font, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Sap, job, 1, job.count, null, errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(FontInd);
        this.FailOnBurningImmobile(FontInd);

        yield return Toils_Goto.GotoThing(SapInd, PathEndMode.Touch)
            .FailOnDestroyedNullOrForbidden(SapInd);

        yield return Toils_Haul.StartCarryThing(SapInd);

        yield return Toils_Goto.GotoThing(FontInd, PathEndMode.InteractionCell); // TODO check if PathEndMode.InteractionCell is used with GoToThing

        yield return Toils_General.Wait(Duration)
            .FailOnDestroyedNullOrForbidden(FontInd)
            .FailOnCannotTouch(FontInd, PathEndMode.Touch)
            .WithProgressBarToilDelay(FontInd);

        Toil addSapToil = ToilMaker.MakeToil("AddSapToFont");
        addSapToil.initAction = () =>
        {
            Font.AddSap(Sap);
        };
        addSapToil.defaultCompleteMode = ToilCompleteMode.Instant;

        yield return addSapToil;
    }
}
