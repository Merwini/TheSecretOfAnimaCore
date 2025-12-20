using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class StatPart_Exchanger : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing)
                return;

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
                return;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                var comp = hediff.TryGetComp<HediffComp_StatExchanger>();
                if (comp == null)
                    continue;

                if (!comp.IsLinked)
                    continue;

                // In case anyone ever uses this for stats other than psychic sensitivity
                if (comp.AffectedStat != this.parentStat)
                    continue;

                val += comp.StatAdjustment;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing)
                return null;

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
                return null;

            float adjTotal = 0f;
            bool found = false;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                var comp = hediff.TryGetComp<HediffComp_StatExchanger>();
                if (comp == null || !comp.IsLinked)
                    continue;

                if (comp.AffectedStat != this.parentStat)
                    continue;

                adjTotal += comp.StatAdjustment;
                found = true;
            }

            if (!found)
                return null;

            return "TSOA_StatPartExchangerExplanation".Translate() + $"{adjTotal.ToString("0.##")}";
        }
    }
}
