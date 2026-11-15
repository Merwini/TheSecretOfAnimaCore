using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace tsoa.core;

class HediffComp_Evolving : HediffComp
{
    public HediffCompProperties_Evolving Props => (HediffCompProperties_Evolving)this.props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (parent.pawn.IsHashIntervalTick(600))
        {
            Pawn pawn = parent.pawn;

            EvolvingImplantDef evolvingImplantDef = Props.evolvingImplantDef;

            float stat = pawn.GetStatValue(evolvingImplantDef.evolvingStat);

            HediffDef target = null;

            // Iterate through thresholds until we hit one that is higher than the current stat
            for (int i = 0; i < evolvingImplantDef.lowerThresholds.Count; i++)
            {
                if (stat >= evolvingImplantDef.lowerThresholds[i])
                {
                    target = evolvingImplantDef.tiers[i];
                }
                else
                {
                    break;
                }
            }

            // Don't change if already has the target hediff
            if (target == null || target == parent.def)
                return;

            BodyPartRecord part = parent.Part;

            // Get injuries on the body part
            List<Hediff_Injury> injuries = part == null ? new List<Hediff_Injury>() : pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>()
                    .Where(injury => injury.Part == part).ToList();

            parent.pawn.health.RemoveHediff(parent);

            Hediff newHediff = HediffMaker.MakeHediff(target, pawn, part);
            pawn.health.AddHediff(newHediff);

            // Re-add injuries after hediff replacement
            foreach (Hediff_Injury injury in injuries)
            {
                if (!pawn.health.hediffSet.hediffs.Contains(injury))
                {
                    pawn.health.AddHediff(injury, part);
                }
            }
        }
    }
}
