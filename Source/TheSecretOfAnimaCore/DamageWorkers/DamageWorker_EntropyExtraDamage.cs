using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core
{
    public class DamageWorker_EntropyExtraDamage : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            if (!(victim is Pawn victimPawn))
            {
                return new DamageResult();
            }
            Pawn pawn = dinfo.Instigator as Pawn;
            EntropyExtraDamageExtension extension = dinfo.Weapon?.GetModExtension<EntropyExtraDamageExtension>();

            if (pawn == null || victim == null || extension == null || pawn.GetPsylinkLevel() <= 0)
                return new DamageResult();

            float originalHeat = pawn.psychicEntropy.EntropyValue;
            float heatCost = originalHeat * extension.heatConsumedPercent;

            if (heatCost == 0)
            {
                return new DamageResult();
            }

            float bonusDamage = heatCost * extension.damagePerHeatConsumed;

            pawn.psychicEntropy.TryAddEntropy(-heatCost, null);

            DamageInfo newDinfo = new DamageInfo(dinfo);
            newDinfo.Def = extension.damageDef;
            newDinfo.SetAmount(bonusDamage);

            if (Prefs.DevMode)
            {
                Log.Message($"[TSOA] DamageWorker_PsyExtraDamage: {pawn.LabelShort} consumed {heatCost} heat (from {originalHeat}) to deal {bonusDamage} extra {extension.damageDef.label} damage to {victim.LabelShort}.");
            }

            return base.Apply(newDinfo, victim);
        }
    }
}
