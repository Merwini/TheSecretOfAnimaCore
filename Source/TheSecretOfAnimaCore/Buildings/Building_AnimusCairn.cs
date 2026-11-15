using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core;

public class Building_AnimusCairn : Building
{
    private CairnEffectExtension effectExtension;
    public CairnEffectExtension EffectExtension
    {
        get
        {
            if (effectExtension == null)
            {
                CairnEffectExtension extension = def.GetModExtension<CairnEffectExtension>();
                if (extension == null)
                {
                    Log.Error($"[TSOA] Building_AnimusCairn of def {def.defName} from {def.modContentPack} has no CairnEffectExtension");
                    effectExtension = new CairnEffectExtension();
                }
                else
                {
                    effectExtension = extension;
                }
            }
            return effectExtension;
        }
    }

    public int EffectTicks => EffectExtension.ticksToApplyEffect;

    public void ApplyCairnEffect(HediffStage stage)
    {
        CairnEffectExtension extension = EffectExtension;

        if (!extension.statOffsets.NullOrEmpty())
        {
            foreach (StatModifier modifier in extension.statOffsets)
            {
                AddOrUpdateModifier(ref stage.statOffsets, modifier);
            }
        }

        if (!extension.statFactors.NullOrEmpty())
        {
            foreach (StatModifier modifier in extension.statFactors)
            {
                AddOrUpdateModifier(ref stage.statFactors, modifier);
            }
        }
    }

    void AddOrUpdateModifier(ref List<StatModifier> list, StatModifier mod)
    {
        if (list == null)
        {
            list = new List<StatModifier>();
        }

        var existing = list.FirstOrDefault(sm => sm.stat == mod.stat);
        if (existing != null)
        {
            existing.value += mod.value;
        }
        else
        {
            list.Add(new StatModifier {stat = mod.stat, value = mod.value });
        }
    }
}
