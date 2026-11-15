using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace tsoa.core;

public class DamageWorker_PsychicLeech : DamageWorker
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        Pawn instigatorPawn = dinfo.Instigator as Pawn;
        Pawn victimPawn = victim as Pawn;
        PsychicLeechExtension extension = dinfo.Weapon?.GetModExtension<PsychicLeechExtension>();

        if (instigatorPawn != null && victimPawn != null && extension != null)
        {
            float proposed = extension.sensitivityOffset;
            if (proposed == 0)
                return new();

            float actual = CalculateSensitivityOffset(victimPawn, proposed);

            ApplyOrUpdateHediff(instigatorPawn, actual);
            ApplyOrUpdateHediff(victimPawn, -actual);
        }

        return new();
    }

    public static float CalculateSensitivityOffset(Pawn victim, float proposed)
    {
        float available = victim.GetStatValueForPawn(StatDefOf.PsychicSensitivity, victim);
        if (available <= 0)
            return 0;

        if (available > proposed)
            return proposed;

        else
            return available;
    }

    public static void ApplyOrUpdateHediff(Pawn pawn, float amount)
    {
        Hediff_PsychicLeech hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(TSOAA_DefOf.TSOA_PsychicLeechHediff) as Hediff_PsychicLeech;
        if (hediff == null)
        {
            hediff = (Hediff_PsychicLeech)HediffMaker.MakeHediff(TSOAA_DefOf.TSOA_PsychicLeechHediff, pawn);
            pawn.health.AddHediff(hediff);
        }
        hediff.UpdateOffset(amount);
    }
}
