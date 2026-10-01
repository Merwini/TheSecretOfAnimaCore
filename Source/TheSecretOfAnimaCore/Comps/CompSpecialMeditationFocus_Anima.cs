using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class CompSpecialMeditationFocus_Anima : ThingComp
{
    public CompProperties_SpecialMeditationFocus_Anima Props => (CompProperties_SpecialMeditationFocus_Anima)props;

    private CompAffectedByFacilities_Grouped compABFG;
    public CompAffectedByFacilities_Grouped CachedCompABFG
    {
        get
        {
            if (compABFG == null)
            {
                CompAffectedByFacilities_Grouped comp = parent.GetComp<CompAffectedByFacilities_Grouped>();
                if (comp != null)
                {
                    compABFG = comp;
                }
                else
                {
                    Log.Error($"CompSpecialMeditationFocus is applied to Thing of {parent.def.defName}, but Thing has no CompAffectedByFacilities_Grouped");
                }
            }
            return compABFG;
        }
    }

    private CompSpawnSubplant compSpawnSubplant;
    public CompSpawnSubplant CachedCompSpawnSubplant
    {
        get
        {
            if (compSpawnSubplant == null)
            {
                CompSpawnSubplant comp = parent.GetComp<CompSpawnSubplant>();
                if (comp != null)
                {
                    compSpawnSubplant = comp;
                }
                else
                {
                    Log.Error($"CompSpecialMeditationFocus is applied to Thing of {parent.def.defName}, but Thing has no CompSpawnSubplant");
                }
            }
            return compSpawnSubplant;
        }
    }

    public virtual void DoMeditationTick(Pawn pawn) => ApplyProgress(Props.meditationTickProgress);
    public void AddExternalProgress(float progress) => ApplyProgress(progress);

    private void ApplyProgress(float progressToAdd)
    {
        progressToAdd = ApplyAnimaBasinAdjustment(progressToAdd);

        var subplant = CachedCompSpawnSubplant;
        if (subplant != null)
        {
            subplant.AddProgress(progressToAdd);
        }
    }

    public float ApplyAnimaBasinAdjustment(float originalProgress)
    {
        float adjustedProgress = originalProgress;
        CompAffectedByFacilities_Grouped comp = CachedCompABFG;
        if (comp == null)
            return originalProgress;

        foreach (Thing thing in comp.LinkedFacilitiesListForReading)
        {
            if (thing is Building_AnimaSapBasin basin && basin.IsHarvesting)
            {
                float progressToRemove = Mathf.Min(adjustedProgress, originalProgress * basin.harvestPercent);
                adjustedProgress -= progressToRemove;
                basin.AddProgress(progressToRemove);
            }
        }

        return adjustedProgress;
    }
}
