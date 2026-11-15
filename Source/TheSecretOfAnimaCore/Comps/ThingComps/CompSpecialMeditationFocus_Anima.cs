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
    private const int ResetPawnProgressAfterTicks = 120;
    public CompProperties_SpecialMeditationFocus_Anima Props => (CompProperties_SpecialMeditationFocus_Anima)props;

    private Dictionary<int, MeditationTracker> trackerDict = new();
    private Dictionary<int, List<Building_AnimusCairn>> cairnEffectsDict = new();

    private CompAffectedByFacilities_Grouped cachedFacilityComp;
    public CompAffectedByFacilities_Grouped CachedFacilityComp => cachedFacilityComp;

    private CompSpawnSubplant cachedSubplantComp;
    public CompSpawnSubplant CachedSubplantComp => cachedSubplantComp;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        cachedFacilityComp = parent.TryGetComp<CompAffectedByFacilities_Grouped>();
        cachedSubplantComp = parent.TryGetComp<CompSpawnSubplant>();

        CleanTrackerDictionary();
        BuildCairnEffectsDictionary();
    }

    public override void CompTickLong()
    {
        CleanTrackerDictionary();
        BuildCairnEffectsDictionary();
        base.CompTickLong();
    }

    public virtual void DoMeditationTick(Pawn pawn)
    {
        ApplyProgress(Props.meditationTickProgress);
        if (pawn == null)
            return;

        int meditatedTicks = UpdateTrackerForPawn(pawn);

        if (cairnEffectsDict.ContainsKey(meditatedTicks))
        {
            Hediff_CairnEffect hediff = ApplyOrReapplyHediffToPawn(pawn);
            if (ApplyCairnEffectsForTick(meditatedTicks, hediff))
            {
                // TODO some sort of visual/auditory effect
            }
        }
    }

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
        CompAffectedByFacilities_Grouped comp = CachedFacilityComp;
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

    private int UpdateTrackerForPawn(Pawn pawn)
    {
        int id = pawn.thingIDNumber;
        int now = Find.TickManager.TicksGame;
        if (!trackerDict.TryGetValue(id, out var tr))
        {
            tr = new MeditationTracker
            {
                accumulatedTicks = 0,
                lastTickSeen = now
            };
            trackerDict[id] = tr;
        }
        else
        {
            if (now - tr.lastTickSeen > ResetPawnProgressAfterTicks)
            {
                tr.accumulatedTicks = 0;
            }
        }

        tr.lastTickSeen = now;
        tr.accumulatedTicks++;

        return tr.accumulatedTicks;
    }

    private void CleanTrackerDictionary()
    {
        trackerDict ??= new();

        int now = Find.TickManager.TicksGame;

        if (trackerDict.Count == 0)
            return;

        List<int> cleaningList = new List<int>();
        foreach (var kvp in trackerDict)
        {
            var tracker = kvp.Value;
            if (now - tracker.lastTickSeen > ResetPawnProgressAfterTicks)
            {
                cleaningList.Add(kvp.Key);
            }
        }

        foreach (int item in cleaningList)
        {
            trackerDict.Remove(item);
        }
    }

    private void BuildCairnEffectsDictionary()
    {
        cairnEffectsDict ??= new();
        cairnEffectsDict.Clear();

        List<Thing> linkedFacilities = CachedFacilityComp.LinkedFacilitiesListForReading;
        if (linkedFacilities.NullOrEmpty())
            return;

        List<Building_AnimusCairn> cairns = linkedFacilities.OfType<Building_AnimusCairn>().ToList();
        if (cairns.NullOrEmpty())
            return;

        foreach (var cairn in cairns)
        {
            int ticks = cairn.EffectTicks;
            if (cairnEffectsDict.TryGetValue(ticks, out var list))
            {
                list.Add(cairn);
            }
            else
            {
                cairnEffectsDict.Add(ticks, new() {cairn});
            }
        }
    }

    public Hediff_CairnEffect ApplyOrReapplyHediffToPawn(Pawn pawn)
    {
        Hediff_CairnEffect hediff = pawn.health.hediffSet.GetFirstHediff<Hediff_CairnEffect>();
        if (hediff == null)
        {
            hediff = (Hediff_CairnEffect)HediffMaker.MakeHediff(TSOA_HediffDefOf.TSOA_CairnHediff, pawn);
            pawn.health.AddHediff(hediff);
        }
        else
        {
            // resetting the hediff timer does allow one pawn to maintain buffs from one tree by meditating at any tree, but I'm fine with this edge case
            HediffComp_Disappears disappears = hediff.GetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ResetElapsedTicks();
            }
            else
            {
                Log.Error($"[TSOA] Cairn Effect hediff is missing HediffComp_Disappears. Some other mod must have messed with it");
            }
        }

        return hediff;
    }

    public bool ApplyCairnEffectsForTick(int ticks, Hediff_CairnEffect hediff)
    {
        bool appliedAny = false;
        if (cairnEffectsDict.TryGetValue(ticks, out var cairns))
        {
            appliedAny = true;
            foreach (var cairn in cairns)
            {
                hediff.AddStatModifiersFromCairn(cairn);
            }
        }

        return appliedAny;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Collections.Look(ref trackerDict, "trackerDict", LookMode.Value, LookMode.Deep);
    }

    private class MeditationTracker : IExposable
    {
        public int accumulatedTicks;
        public int lastTickSeen;

        public void ExposeData()
        {
            Scribe_Values.Look(ref accumulatedTicks, "accumulatedTicks", 0);
            Scribe_Values.Look(ref lastTickSeen, "lastTickSeen", 0);
        }
    }
}
