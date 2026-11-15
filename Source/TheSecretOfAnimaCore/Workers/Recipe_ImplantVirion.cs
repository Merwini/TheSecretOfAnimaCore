using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class Recipe_ImplantVirion : Recipe_InstallImplant
{
    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (billDoer != null)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        }

        VirionExtension extension = bill.recipe.GetModExtension<VirionExtension>();
        if (extension == null)
        {
            Log.Error("Recipe_ImplantVirion used recipe without VirionExtension");
            return;
        }

        Hediff_Virion hediff = HediffMaker.MakeHediff(recipe.addsHediff, pawn, part) as Hediff_Virion;

        hediff.virionRecipeDef = bill.recipe;

        pawn.health.AddHediff(hediff, part);
    }
}
