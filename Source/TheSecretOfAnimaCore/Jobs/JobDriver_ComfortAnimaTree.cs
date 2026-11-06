using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace tsoa.core;

public class JobDriver_ComfortAnimaTree : JobDriver
{
    private const int ComfortTicks = 600;

    private Thing Tree => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!RCellFinder.TryFindGoodAdjacentSpotToTouch(pawn, Tree, out IntVec3 interactionCell))
        {
            return false;
        }

        job.SetTarget(TargetIndex.B, interactionCell);
        return pawn.Reserve(Tree, job, 1, -1, null, errorOnFailed) &&
            pawn.Reserve(interactionCell, job, 1, -1, null, errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        this.FailOn(() => !pawn.IsColonistPlayerControlled || pawn.GetPsylinkLevel() <= 0 ||
            Tree.TryGetComp<CompDyingAnimaTree>() == null);

        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

        yield return Toils_General.Wait(ComfortTicks, TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
            .WithProgressBarToilDelay(TargetIndex.A);

        yield return Toils_General.Do(() =>
        {
            Map map = pawn.Map;
            Thing tree = Tree;
            IntVec3 treeCell = tree.Position;
            Thing pearl = ThingMaker.MakeThing(TSOA_DefOf.TSOA_AnimaPearl);
            if (!GenPlace.TryPlaceThing(pearl, pawn.Position, map, ThingPlaceMode.Near,
                extraValidator: cell => cell != pawn.Position && cell.InHorDistOf(pawn.Position, 1.5f)))
            {
                pearl.Destroy();
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            SoundDefOf.PsychicSootheGlobal.PlayOneShot(new TargetInfo(treeCell, map));
            Hediff_AnimaAffinity.AddOrUpdateAffinityHediff(pawn, 0.1f);

            tree.DeSpawn(DestroyMode.WillReplace);
            tree.Destroy(DestroyMode.WillReplace);
            Plant silentTree = (Plant)GenSpawn.Spawn(TSOA_DefOf.TSOA_TreeAnimaSilent, treeCell, map);
            silentTree.Growth = 1f;

            Find.LetterStack.ReceiveLetter(
                "TSOA_SilentGroveComfortedLabel".Translate(pawn.Named("PAWN")),
                "TSOA_SilentGroveComfortedDesc".Translate(pawn.Named("PAWN")),
                LetterDefOf.NeutralEvent,
                new LookTargets(treeCell, map));
        });
    }
}
