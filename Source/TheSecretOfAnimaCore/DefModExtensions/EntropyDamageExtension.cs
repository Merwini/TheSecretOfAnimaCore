using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tsoa.core;
using Verse;

namespace tsoa.core;

public class EntropyDamageExtension : DefModExtension
{
    public float entropyCountedPercent = 0;
    public float entropySpentPercent = 0;

    public float entropyCountedFlat = 0;
    public float entropySpentFlat = 0;

    public float damagePerEntropy = 0;

    public DamageDef damageDef;

    public float damageBaseline = 0;

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);
        
        if ((entropyCountedPercent != 0 && entropyCountedFlat != 0)
            || (entropySpentPercent != 0 && entropySpentFlat != 0))
        {
            Log.Error($"[TSOA] EntropyDamageExtension has both percent and flat values configured on {parentDef.defName}. It should be one or the other.");
        }

        if ((entropyCountedPercent != 0 && entropySpentFlat != 0)
            || (entropySpentPercent != 0 && entropyCountedFlat != 0))
        {
            Log.Error($"[TSOA] EntropyDamageExtension has a mix of percent and flat values configured on {parentDef.defName}. It should be one or the other.");
        }

        if (entropyCountedPercent <= 0 && entropyCountedFlat <= 0)
        {
            Log.Error($"[TSOA] EntropyDamageExtension has 0 or less entropyCounted on {parentDef.defName} and will only ever apply damageBaseline, if there is one.");
        }

        if (entropySpentPercent < 0)
        {
            Log.Error($"[TSOA] entropySpentPercent is negative on {parentDef.defName}. The system does not currently support negative entropySpentPercent. Defaulting to 0");
            entropySpentPercent = 0;
        }

        if (entropySpentPercent > 1)
        {
            Log.Error($"[TSOA] entropySpentPercent is greater than 100% on {parentDef.defName}. The system does not currently support this. Defaulting to 0");
            entropySpentPercent = 0;
        }

        if (damageDef == null)
        {
            damageDef = TSOAA_DefOf.TSOA_EntropicDischarge;
            Log.Error($"[TSOA] EntropyDamageExtension on {parentDef.defName} had no damageDef defined. Defaulted to EntropicDischarge.");
        }
    }
}
