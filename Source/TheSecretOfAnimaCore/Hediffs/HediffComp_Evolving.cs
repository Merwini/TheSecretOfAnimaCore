using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core
{
    class HediffComp_Evolving : HediffComp
    {
        public HediffCompProperties_Evolving Props => (HediffCompProperties_Evolving)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (parent.pawn.IsHashIntervalTick(600))
            {
                Pawn pawn = parent.pawn;

                // Should I check that Props.evolvingStat was defined properly?

                float stat = pawn.GetStatValue(Props.evolvingStat);

                HediffDef target = null;

                if (stat > Props.upperThreshold && Props.hediffAbove != null)
                {
                    target = Props.hediffAbove;
                }
                else if (stat < Props.lowerThreshold && Props.hediffBelow != null)
                {
                    target = Props.hediffBelow;
                }
                else
                {
                    return;
                }

                // Don't re-add if already has the target hediff
                if (pawn.health.hediffSet.HasHediff(target, parent.Part))
                    return;

                // Need to cache these since we remove before we add
                if (target != null)
                {
                    BodyPartRecord part = parent.Part;

                    parent.pawn.health.RemoveHediff(parent);

                    Hediff newHediff = HediffMaker.MakeHediff(target, pawn, part);
                    pawn.health.AddHediff(newHediff);
                }
            }
        }
    }
}
