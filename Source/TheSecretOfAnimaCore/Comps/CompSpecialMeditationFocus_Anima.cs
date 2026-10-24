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

    private CompAffectedByFacilities_Grouped cachedFacilityComp;
    public CompAffectedByFacilities_Grouped CachedCompABFG => cachedFacilityComp;

    private CompSpawnSubplant cachedSubplantComp;
    public CompSpawnSubplant CachedSubplantComp => cachedSubplantComp;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        cachedFacilityComp = parent.TryGetComp<CompAffectedByFacilities_Grouped>();
        if (cachedFacilityComp == null)
        {
            Log.Error($"[TSOA] CompSpecialMeditationFocus_Anima was unable to find CompAffectedByFacilities_Grouped on {parent.Label}");
        }

        cachedSubplantComp = parent.TryGetComp<CompSpawnSubplant>();
        if (cachedFacilityComp == null)
        {
            Log.Error($"[TSOA] CompSpecialMeditationFocus_Anima was unable to find CompSpawnSubplant on {parent.Label}");
        }
    }

    public virtual void DoMeditationTick(Pawn pawn) => ApplyProgress(Props.meditationTickProgress);
    public void AddExternalProgress(float progress) => ApplyProgress(progress);

    private void ApplyProgress(float progressToAdd)
    {
        progressToAdd = ApplyAnimaBasinAdjustment(progressToAdd);

        var subplant = CachedSubplantComp;
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
