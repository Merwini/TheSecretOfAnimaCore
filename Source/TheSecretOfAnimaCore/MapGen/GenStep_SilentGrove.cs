using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class GenStep_SilentGrove : GenStep_SpawnThingsAroundThing
{
    public override int SeedPart => 195673492;

    public override void Generate(Map map, GenStepParams parms)
    {
        // try to find a patch of ground with >0 fertility. Fallback to allow any ground.
        // spawn the first silent tree
        // reuse methods from base class to spawn the rest

        if (!TryFindInitialCell(map, out IntVec3 groveCenter))
        {
            Log.Error("GenStep_SilentGrove could not find a valid initial cell.");
            return;
        }

        List<Thing> thingsInCell = groveCenter.GetThingList(map);
        for (int i = thingsInCell.Count - 1; i >= 0; i--)
        {
            thingsInCell[i].Destroy();
        }

        Thing firstTreeThing = GenSpawn.Spawn(focusThing, groveCenter, map);
        if (firstTreeThing is Plant firstTreePlant)
        {
            firstTreePlant.Growth = 1;
        }

        SpawnThingsAround(groveCenter, map);
        SpawnApproachLetter(map, groveCenter);

        CellRect cellRect = CellRect.CenteredOn(groveCenter, 7, 7).ClipInsideMap(map);
        MapGenerator.SetVar("RectOfInterest", cellRect);
    }

    private bool TryFindInitialCell(Map map, out IntVec3 result)
    {
        if (TryFindInitialCellWithFertility(map, out result))
        {
            return true;
        }

        return TryFindFallbackInitialCell(map, out result);
    }

    private bool TryFindInitialCellWithFertility(Map map, out IntVec3 result)
    {
        return CellFinderLoose.TryFindRandomNotEdgeCellWith(
            MinDistanceFromMapEdge,
            c => CanPlaceGroveCenterAt(c, map, requireFertility: true),
            map,
            out result);
    }

    private bool TryFindFallbackInitialCell(Map map, out IntVec3 result)
    {
        return CellFinderLoose.TryFindRandomNotEdgeCellWith(
            MinDistanceFromMapEdge,
            c => CanPlaceGroveCenterAt(c, map, requireFertility: false),
            map,
            out result);
    }

    private bool CanPlaceGroveCenterAt(IntVec3 cell, Map map, bool requireFertility)
    {
        return CanSpawnThingAt(cell, map, requireFertility) &&
            map.reachability.CanReachMapEdge(cell,
                TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater));
    }

    private void SpawnApproachLetter(Map map, IntVec3 groveCenter)
    {
        string signalTag = "TSOA_GroveApproached-" + Find.UniqueIDsManager.GetNextSignalTagID();

        CellRect triggerRect = CellRect.CenteredOn(groveCenter, 12).ClipInsideMap(map);

        var letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter);

        letter.signalTag = signalTag;
        letter.letterDef = LetterDefOf.NeutralEvent;
        letter.letterLabelKey = "TSOA_SilentGroveApproachedLabel";
        letter.letterMessageKey = "TSOA_SilentGroveApproachedDesc";
        letter.lookTargets = new LookTargets(groveCenter, map);

        GenSpawn.Spawn(letter, groveCenter, map);

        var trigger = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger);

        trigger.signalTag = signalTag;
        trigger.Rect = triggerRect;
        trigger.destroyIfUnfogged = false;

        GenSpawn.Spawn(trigger, groveCenter, map);
    }
}
