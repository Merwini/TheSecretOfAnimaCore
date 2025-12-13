using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace nuff.tsoa.core
{
    public class Recipe_InstallSetPiece : Recipe_InstallArtificialBodyPart
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!(thing is Pawn pawn))
            {
                return false;
            }

            List<Hediff> existingHediffs = pawn.health.hediffSet.hediffs;

            // Would only be null if recipe is misconfigured
            HediffCompProperties_SetPiece newSetPieceComp = recipe.addsHediff.comps.FirstOrDefault(c => c is HediffCompProperties_SetPiece) as HediffCompProperties_SetPiece;
            string newSetName = newSetPieceComp.setName;

            foreach (Hediff hediff in existingHediffs)
            {
                if (!(hediff is Hediff_Implant))
                    continue;

                if (hediff.TryGetComp<HediffComp_SetCore>() != null)
                    continue;

                HediffComp_SetPiece setPieceComp = hediff.TryGetComp<HediffComp_SetPiece>();
                if (setPieceComp == null)
                    continue;

                if (setPieceComp.Props.setName == newSetName)
                {
                    if (hediff.Part == part)
                        return false; // already applied to this part

                    if (part == null && hediff.Part == null)
                        return false; // trying to add second whole-body (Core was skipped earlier) hediff from same set
                }

            }

            return base.AvailableOnNow(thing, part);
        }
    }
}
