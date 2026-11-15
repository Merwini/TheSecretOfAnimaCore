using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace tsoa.core;

public class EvolvingImplantDef : Def
{
    public ThingDef thingDef; // not currently used

    public StatDef evolvingStat;

    public List<float> lowerThresholds = new List<float>();

    public List<HediffDef> tiers = new List<HediffDef>();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (evolvingStat == null)
        {
            yield return "evolvingStat is not defined.";
        }

        if (tiers.NullOrEmpty())
        {
            yield return "tiers list null or empty";
        }

        if (lowerThresholds.NullOrEmpty())
        {
            yield return "lowerThresholds list null or empty";
        }

        if (!lowerThresholds.NullOrEmpty() && !tiers.NullOrEmpty())
        {
             if (lowerThresholds.Count != tiers.Count)
            {
                yield return "The number of thresholds must match the number of tiers.";
            }

            if (!lowerThresholds.NullOrEmpty() && lowerThresholds[0] != 0)
            {
                yield return "Lowest threshold should always be 0";
            }

            for (int i = 1; i < lowerThresholds.Count; i++)
            {
                if (lowerThresholds[i] <= lowerThresholds[i - 1])
                {
                    yield return "Lower thresholds must be in ascending order.";
                }
            }
        }
    }
}