using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace nuff.tsoa.core
{
    class StatPart_PsyScaling : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing)
                return;

            Thing thing = req.Thing;

            float scaling = thing.def.statBases.GetStatValueFromList(TSOA_DefOf.TSOA_PsyScaling, 0);
            if (scaling <= 0f)
                return;

            Pawn pawn = null;
            if (thing.ParentHolder is Pawn_EquipmentTracker eq)
                pawn = eq.pawn;
            else if (thing.ParentHolder is Pawn_ApparelTracker ap)
                pawn = ap.pawn;

            if (pawn == null)
                return;

            float sensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            if (sensitivity <= 1f) // Won't scale values down
                return;

            val = TSOA_Utils.GetPsyScaledValue(val, scaling, sensitivity);
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing)
                return null;

            Thing thing = req.Thing;

            float scaling = thing.def.statBases.GetStatValueFromList(TSOA_DefOf.TSOA_PsyScaling, 0);
            if (scaling <= 0f)
                return null;

            Pawn pawn = null;
            if (thing.ParentHolder is Pawn_EquipmentTracker eq)
                pawn = eq.pawn;
            else if (thing.ParentHolder is Pawn_ApparelTracker ap)
                pawn = ap.pawn;

            if (pawn == null)
                return null;

            float sensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float bonus = TSOA_Utils.GetPsyScalingFactor(scaling, sensitivity);
            if (bonus <= 0f)
                return null;   // show no scaling if no bonus applied

            float totalMult = 1f + bonus;

            return "TSOA_PsyScaling_StatPartExplanation".Translate(
                sensitivity.ToStringPercent(),
                scaling.ToStringPercent(),
                totalMult.ToStringPercent()
            );
        }
    }
}
