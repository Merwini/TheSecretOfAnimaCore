using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace tsoa.core
{
    // I think I may not actually need this at all, since harvesters will be moving around
    public class RitualPosition_AnimaGrassHarvesters : RitualPosition
    {
        public override PawnStagePosition GetCell(IntVec3 spot, Pawn p, LordJob_Ritual ritual)
        {
            if (SpectatorCellFinder.TryFindCircleSpectatorCellFor(p, CellRect.CenteredOn(spot, 0), 2f, 3f, p.Map, out var cell))
            {
                return new PawnStagePosition(cell, null, Rot4.FromAngleFlat((spot - cell).AngleFlat), highlight);
            }
            CompSpawnSubplant comp = ritual.selectedTarget.Thing?.TryGetComp<CompSpawnSubplant>();
            if (comp != null && TryFindLinkSpot(p, out var spot2, ritual.selectedTarget.Thing as ThingWithComps))
            {
                Rot4 orientation = Rot4.FromAngleFlat((spot - spot2.Cell).AngleFlat);
                return new PawnStagePosition(spot2.Cell, null, orientation, highlight);
            }
            return new PawnStagePosition(IntVec3.Invalid, null, Rot4.Invalid, highlight);
        }

        // modified from CompPsylinkable
        public bool TryFindLinkSpot(Pawn pawn, out LocalTargetInfo spot, ThingWithComps parent)
        {
            spot = MeditationUtility.FindMeditationSpot(pawn).spot;
            if (CanUseSpot(pawn, spot, parent))
            {
                return true;
            }
            int num = GenRadial.NumCellsInRadius(2.9f);
            int num2 = GenRadial.NumCellsInRadius(3.9f);
            for (int i = num; i < num2; i++)
            {
                IntVec3 intVec = parent.Position + GenRadial.RadialPattern[i];
                if (CanUseSpot(pawn, intVec, parent))
                {
                    spot = intVec;
                    return true;
                }
            }
            spot = IntVec3.Zero;
            return false;
        }

        // modified from CompPsylinkable
        private bool CanUseSpot(Pawn pawn, LocalTargetInfo spot, ThingWithComps parent)
        {
            IntVec3 cell = spot.Cell;
            if (cell.DistanceTo(parent.Position) > 3.9f)
            {
                return false;
            }
            if (!cell.Standable(parent.Map))
            {
                return false;
            }
            if (!GenSight.LineOfSight(cell, parent.Position, parent.Map))
            {
                return false;
            }
            if (!pawn.CanReach(spot, PathEndMode.OnCell, Danger.Deadly))
            {
                return false;
            }
            return true;
        }
    }
}
