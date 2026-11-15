using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

public class Hediff_RegrowingPart : HediffWithComps
{
    private const float randomVariance = 0.2f;

    private int totalTicks = -1;
    private int ticksRemaining = -1;

    public override bool ShouldRemove => false;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        TryRemoveMissingFrom(pawn, Part);

        int baseTicks = TicksToRegrow(Part);
        totalTicks = Rand.RangeInclusive(Mathf.RoundToInt(baseTicks * (1f - randomVariance)),Mathf.RoundToInt(baseTicks * (1f + randomVariance)));
        ticksRemaining = totalTicks;
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (ticksRemaining > 0)
        {
            ticksRemaining = Math.Max(0, ticksRemaining - delta);
        }

        Severity = 1f - ((float)ticksRemaining / totalTicks);
        if (ticksRemaining == 0)
        {
            FinishRegrow();
        }
    }

    private void FinishRegrow()
    {
        BodyPartRecord part = Part;

        if (part.parts != null && part.parts.Count > 0)
        {
            foreach (BodyPartRecord child in part.parts)
            {
                if (child == null) continue;
                    
                // Skip regrowing child part if they were somehow restored or replaced by other means
                if (!pawn.health.hediffSet.PartIsMissing(child))
                    continue;

                if (pawn.health.hediffSet.HasDirectlyAddedPartFor(child))
                    continue;

                if (TryRemoveMissingFrom(pawn, child))
                {
                    Hediff_RegrowingPart regrow = (Hediff_RegrowingPart)HediffMaker.MakeHediff(def, pawn, child);
                    pawn.health.AddHediff(regrow);
                }
            }
        }

        pawn.health.RemoveHediff(this);
    }

    public override string LabelInBrackets
    {
        get
        {
            return $"{CurStage.label}: {Severity.ToStringPercent()}";
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref totalTicks, "totalTicks", -1);
        Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", -1);
    }

    private int TicksToRegrow(BodyPartRecord part)
    {
        BodyPartDef  def = part.def;
        int days;

        // TODO refine. Maybe expose to xml?
        // Even essential organs included, for mod compatibility or use with Deathless
        switch (def.defName)
        {
            case "Brain":
            case "Heart":
            case "Lung":
            case "Kidney":
            case "Liver":
            case "Stomach":
                days = 15;
                break;

            default:
                days = 5;
                break;
        }

        return days * GenDate.TicksPerDay;
    }

    public static bool TryRemoveMissingFrom(Pawn pawn, BodyPartRecord part)
    {
        Hediff_MissingPart missing = pawn.health.hediffSet.hediffs
            .OfType<Hediff_MissingPart>()
            .FirstOrDefault(mp => mp.Part == part);

        if (missing != null)
        {
            pawn.health.RemoveHediff(missing);
            return true;
        }

        return false;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (Prefs.DevMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEBUG: Finish Regrowth",
                action = () =>
                {
                    ticksRemaining = 0;
                    FinishRegrow();
                }
            };
        }
    }
}

