using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core;

public class Hediff_CairnEffect : HediffWithComps
{
    private List<StatModifier> statOffsets = new();
    private List<StatModifier> statFactors = new();
    private HashSet<Building_AnimusCairn> appliedCairns = new();

    private HediffStage curStage;
    public override HediffStage CurStage
    {
        get
        {
            if (curStage == null)
            {
                curStage = new HediffStage();
                curStage.statOffsets = statOffsets;
                curStage.statFactors = statFactors;
            }
            return curStage;
        }
    }

    public bool HasStats => !statOffsets.NullOrEmpty() || !statFactors.NullOrEmpty();

    public void AddStatModifiersFromCairn(Building_AnimusCairn cairn)
    {
        if (appliedCairns.Add(cairn))
        {
            CairnEffectExtension extension = cairn.EffectExtension;

            List<StatModifier> offsets = extension.statOffsets;
            if (!offsets.NullOrEmpty())
            {
                foreach (var offset in offsets)
                {
                    StatModifier mod = CurStage.statOffsets.FirstOrDefault(x => x.stat == offset.stat);
                    if (mod != null)
                    {
                        mod.value += offset.value;
                    }
                    else
                    {
                        StatModifier newMod = new StatModifier() { stat = offset.stat, value = offset.value };
                        CurStage.statOffsets.Add(newMod);
                    }
                }
            }

            List<StatModifier> factors = extension.statFactors;
            if (!factors.NullOrEmpty())
            {
                foreach (var factor in factors)
                {
                    StatModifier mod = CurStage.statFactors.FirstOrDefault(x => x.stat == factor.stat);
                    if (mod != null)
                    {
                        mod.value *= factor.value;
                    }
                    else
                    {
                        StatModifier newMod = new StatModifier() { stat = factor.stat, value = factor.value };
                        CurStage.statFactors.Add(newMod);
                    }
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Collections.Look(ref appliedCairns, "appliedCairns", LookMode.Reference);
        appliedCairns ??= new();

        List<StatDef> statOffsetDefs = null;
        List<float> statOffsetValues = null;
        List<StatDef> statFactorDefs = null;
        List<float> statFactorValues = null;

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            statOffsetDefs = new List<StatDef>();
            statOffsetValues = new List<float>();
            statFactorDefs = new List<StatDef>();
            statFactorValues = new List<float>();

            if (!statOffsets.NullOrEmpty())
            {
                foreach (StatModifier statModifier in statOffsets)
                {
                    statOffsetDefs.Add(statModifier.stat);
                    statOffsetValues.Add(statModifier.value);
                }
            }
            if (!statFactors.NullOrEmpty())
            {
                foreach (StatModifier statModifier in statFactors)
                {
                    statFactorDefs.Add(statModifier.stat);
                    statFactorValues.Add(statModifier.value);
                }
            }
        }

        Scribe_Collections.Look(ref statOffsetDefs, "statOffsetDefs", LookMode.Def);
        Scribe_Collections.Look(ref statOffsetValues, "statOffsetValues", LookMode.Value);
        Scribe_Collections.Look(ref statFactorDefs, "statFactorDefs", LookMode.Def);
        Scribe_Collections.Look(ref statFactorValues, "statFactorValues", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (!statOffsetDefs.NullOrEmpty())
            {
                if (statOffsets == null)
                {
                    statOffsets = new List<StatModifier>();
                }

                for (int i = 0; i < statOffsetDefs.Count; i++)
                {
                    StatModifier sm = new StatModifier();
                    sm.stat = statOffsetDefs[i];
                    sm.value = statOffsetValues[i];
                    statOffsets.Add(sm);
                }
            }
            if (!statFactorDefs.NullOrEmpty())
            {
                if (statFactors == null)
                {
                    statFactors = new List<StatModifier>();
                }

                for (int i = 0; i < statFactorDefs.Count; i++)
                {
                    StatModifier sm = new StatModifier();
                    sm.stat = statFactorDefs[i];
                    sm.value = statFactorValues[i];
                    statFactors.Add(sm);
                }
            }

            curStage = null;
        }
    }
}
