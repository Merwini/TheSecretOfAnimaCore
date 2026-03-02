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

    private CompAffectedByGroupedFacilities compABGF;
    public CompAffectedByGroupedFacilities CachedCompABGF
    {
        get
        {
            if (compABGF == null)
            {
                CompAffectedByGroupedFacilities comp = parent.GetComp<CompAffectedByGroupedFacilities>();
                if (comp != null)
                {
                    compABGF = comp;
                }
                else
                {
                    Log.Error($"CompSpecialMeditationFocus is applied to Thing of {parent.def.defName}, but Thing has no CompAffectedByGroupedFacilities");
                }
            }
            return compABGF;
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
        CompAffectedByGroupedFacilities comp = CachedCompABGF;
        if (comp == null)
            return originalProgress;

        foreach (Thing thing in comp.LinkedFacilities)
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

    public bool TryRemovePlantProgress(float amount)
    {
        if (CachedCompSpawnSubplant.progressToNextSubplant >= amount)
        {
            CachedCompSpawnSubplant.progressToNextSubplant -= amount;
            return true;
        }
        else if (TryConsumeGrass())
        {
            CachedCompSpawnSubplant.AddProgress(1 - amount);
            return true;
        }
        return false;
    }

    private bool TryConsumeGrass()
    {
        List<Thing> plants = CachedCompSpawnSubplant.SubplantsForReading;
        if (plants.NullOrEmpty())
            return false;

        Thing plant = plants.RandomElement();
        ConsumeGrass(plant);

        return true;
    }

    private void ConsumeGrass(Thing grass)
    {
        grass.Destroy(DestroyMode.Vanish);
        // TODO do an effect
    }
}
