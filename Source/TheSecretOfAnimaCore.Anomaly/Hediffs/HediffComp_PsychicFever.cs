using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

public class HediffComp_PsychicFever : HediffComp
{
    private int ticksRemaining = -1;

    public HediffCompProperties_PsychicFever Props => (HediffCompProperties_PsychicFever)props;

    public override bool CompShouldRemove => ticksRemaining <= 0;

    private float severityPerTick;

    public override void CompPostMake()
    {
        base.CompPostMake();
        
        float days = Props.durationDays > 0f ? Props.durationDays : (Props.durationDaysRange.TrueMin > 0f ? Props.durationDaysRange.RandomInRange : 2f);
        ticksRemaining = Mathf.Max(1, Mathf.RoundToInt(days * GenDate.TicksPerDay));

        int ticksToLethal = (int)(Props.hoursToLethal * GenDate.TicksPerHour);
        severityPerTick = 1f / ticksToLethal;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        ticksRemaining--;
        if (ticksRemaining <= 0)
        {
            Pawn.health.RemoveHediff(parent);
            return;
        }

        severityAdjustment += severityPerTick;

        base.CompPostTick(ref severityAdjustment);
    }

    public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
    {
        base.CompTended(quality, maxQuality, batchPosition);

        parent.Severity = Props.severityAfterTend;
    }

    public override string CompTipStringExtra
    {
        get
        {
            if (ticksRemaining < 0)
                return null;

            return "TSOA_PsychicFever_TimeRemaining".Translate(ticksRemaining.ToStringTicksToPeriod(allowSeconds: false, shortForm: true));
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", -1);
        Scribe_Values.Look(ref severityPerTick, "severityPerTick", 1f/15000f);
    }

}
