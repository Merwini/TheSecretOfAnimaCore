using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class Hediff_AnimaAffinity : Hediff
{
    private float currentAffinity = 0;
    public float CurrentAffinity
    {
        get
        {
            return currentAffinity;
        }
    }

    private HediffStage curStage;
    public override HediffStage CurStage
    {
        get
        {
            if (curStage == null)
            {
                curStage = new HediffStage();
                curStage.statOffsets = new List<StatModifier>()
                {
                    new StatModifier()
                    {
                        stat = StatDefOf.PsychicSensitivity,
                        value = currentAffinity
                    }
                };
            }
            return curStage;
        }
    }

    // I decided AddAffinity is better than a setter
    public void AddAffinity(float amount)
    {
        currentAffinity += amount;
        curStage = null;
    }

    public static void AddOrUpdateAffinityHediff(Pawn pawn, float amount)
    {
        Hediff_AnimaAffinity hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(TSOA_DefOf.TSOA_AnimaAffinityHediff) as Hediff_AnimaAffinity;
        if (hediff == null)
        {
            hediff = (Hediff_AnimaAffinity)HediffMaker.MakeHediff(TSOA_DefOf.TSOA_AnimaAffinityHediff, pawn);
            pawn.health.AddHediff(hediff);
        }
        hediff.AddAffinity(amount);
    }

    public override string LabelInBrackets => currentAffinity.ToStringPercent();

    public override void ExposeData()
    {
        Scribe_Values.Look(ref currentAffinity, "currentAffinity", 0);

        base.ExposeData();
    }
}
