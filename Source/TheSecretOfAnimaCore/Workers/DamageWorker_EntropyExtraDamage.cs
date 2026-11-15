using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using System.ComponentModel;

namespace tsoa.core;

public class DamageWorker_EntropyExtraDamage : DamageWorker_AddInjury
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        Pawn pawn = dinfo.Instigator as Pawn;

        EntropyDamageExtension extension = dinfo.Weapon?.GetModExtension<EntropyDamageExtension>();
        
        if (pawn == null || victim is not Pawn || extension == null || pawn.GetPsylinkLevel() <= 0)
            return new();

        float entropyToSpend = 0;

        bool failedToSpend = false;

        float pawnCurrentEntropy = pawn.psychicEntropy.EntropyValue;
        float pawnMaxEntropy = pawn.psychicEntropy.MaxEntropy;
        float pawnEntropyAvailable = 0;
        float entropyCounted = 0;

        // Calculate the full cost first. If it cannot be paid, only baseline damage applies.
        float entropySpendPercent = extension.entropySpentPercent;
        float entropySpendFlat = extension.entropySpentFlat;
        if (entropySpendPercent != 0)
        {
            if (entropySpendPercent > 0)
            {
                // positive spend = remove heat
                // spend is a percent of current heat
                entropyToSpend = pawnCurrentEntropy * entropySpendPercent;
            }
        }
        else if (entropySpendFlat != 0)
        {
            if (entropySpendFlat > 0)
            {
                // positive spend = remove heat
                if (entropySpendFlat > pawnCurrentEntropy)
                {
                    failedToSpend = true;
                }
                else
                {
                    entropyToSpend = entropySpendFlat;
                }
            }
            else // implicit < 0
            {
                pawnEntropyAvailable = pawnMaxEntropy - pawnCurrentEntropy;
                if (pawnEntropyAvailable < 0)
                    pawnEntropyAvailable = 0;

                // negative spend = add heat
                if (-entropySpendFlat > pawnEntropyAvailable)
                {
                    failedToSpend = true;
                }
                else
                {
                    entropyToSpend = entropySpendFlat;
                }
            }
        }
        else
        {
            // Not an issue, entropySpent can be 0
        }

        float bonusDamage = 0;
        if (!failedToSpend)
        {
            float entropyCountPercent = extension.entropyCountedPercent;
            float entropyCountFlat = extension.entropyCountedFlat;
            if (entropyCountPercent > 0)
            {
                entropyCounted = pawnCurrentEntropy * entropyCountPercent;
            }
            else if (entropyCountFlat > 0)
            {
                entropyCounted = entropyCountFlat;
            }
            else
            {
                // would have already logged an error in EntropyDamageExtension.ResolveReferences, no need to error again
            }

            bonusDamage = entropyCounted * extension.damagePerEntropy;

            if (entropyToSpend != 0 && !pawn.psychicEntropy.TryAddEntropy(-entropyToSpend, null, false))
            {
                // If the tracker rejects the full cost, only baseline damage applies.
                bonusDamage = 0;
            }
        }

        // make sure a misconfigured damageDef doesn't create an infinite loop
        DamageDef newDamage = extension.damageDef.Worker is not DamageWorker_EntropyExtraDamage ? extension.damageDef : TSOA_DamageDefOf.TSOA_EntropicDischarge;

        DamageInfo newDinfo = new DamageInfo(dinfo);
        newDinfo.Def = newDamage;
        newDinfo.SetAmount(bonusDamage + extension.damageBaseline);
        newDinfo.SetIgnoreArmor(true);

        if (DebugSettings.godMode)
        {
            Log.Message($"[TSOA] DamageWorker_PsyExtraDamage: {pawn.LabelShort} consumed {entropyToSpend} heat (from {pawnCurrentEntropy}) to deal {bonusDamage} extra {newDamage.LabelCap} damage to {victim.LabelShort} with penetration: {dinfo.ArmorPenetrationInt}");
        }

        return newDamage.Worker.Apply(newDinfo, victim);
    }
}
