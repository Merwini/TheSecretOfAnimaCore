using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_Eruption : PsychicRitualToil_AnimaAffinity
{
    private const int MaxVeinAttempts = 100;
    private const float InfestationSearchRadius = 2f;

    private PsychicRitualRoleDef invokerRole;
    private ThingDef chosenOreDef;

    public PsychicRitualToil_Eruption()
    {
    }

    public PsychicRitualToil_Eruption(PsychicRitualRoleDef invokerRole, ThingDef chosenOreDef)
    {
        this.invokerRole = invokerRole;
        this.chosenOreDef = chosenOreDef;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);

        Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);

        bool success = chosenOreDef == null
            ? Rand.Chance(((PsychicRitualDef_Eruption)psychicRitual.def).successCurveAnyOre.Evaluate(psychicRitual.PowerPercent))
            : Rand.Chance(((PsychicRitualDef_Eruption)psychicRitual.def).successCurve.Evaluate(psychicRitual.PowerPercent));

        psychicRitual.ReleaseAllPawnsAndBuildings();

        if (invoker != null)
        {
            ApplyOutcome(psychicRitual, invoker, success);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker, bool success)
    {
        if (chosenOreDef == null)
        {
            List<ThingDef> potentialOres = DefDatabase<ThingDef>.AllDefs.Where(td => td.building != null && td.building.isResourceRock).ToList();
            chosenOreDef = potentialOres.RandomElementByWeight(td => td.building.mineableScatterCommonality);
        }

        int oreAmount = chosenOreDef.building.mineableScatterLumpSizeRange.RandomInRange;
        Map map = psychicRitual.Map;

        if (!TryChooseVein(map, oreAmount, out IntVec3 chosenCell, out List<IntVec3> cells))
        {
            Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label),
                "TSOA_EruptionNoOre".Translate(invoker, psychicRitual.def.Named("RITUAL")), LetterDefOf.NeutralEvent);
            return;
        }

        List<Thing> spawnedOres = SpawnOreVein(map, cells);
        SpawnFilthAround(spawnedOres);

        LetterDef textLetterDef;
        TaggedString text;

        if (!success && SpawnInfestation(map, chosenCell, cells))
        {
            textLetterDef = LetterDefOf.NegativeEvent;
            text = "TSOA_EruptionFailure".Translate(invoker, psychicRitual.def.Named("RITUAL"));
        }
        else // if infestation fails, player can have that success as a freebie
        {
            textLetterDef = LetterDefOf.PositiveEvent;
            text = "TSOA_EruptionSuccess".Translate(invoker, psychicRitual.def.Named("RITUAL"));
        }

        Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), text, textLetterDef, lookTargets: new LookTargets(chosenCell, map));
    }

    private bool SpawnInfestation(Map map, IntVec3 defendPoint, List<IntVec3> oreCells)
    {
        HashSet<IntVec3> occupiedCells = new HashSet<IntVec3>(oreCells);
        HashSet<IntVec3> candidates = new HashSet<IntVec3>();
        foreach (IntVec3 oreCell in oreCells)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(oreCell, InfestationSearchRadius, false))
            {
                if (!occupiedCells.Contains(cell) && IsValid(cell, map) && cell.Standable(map)
                    && cell.GetEdifice(map) == null
                    && cell.GetFirstThing(map, ThingDefOf.Hive) == null
                    && !cell.GetThingList(map).Any(thing => thing is TunnelHiveSpawner))
                {
                    candidates.Add(cell);
                }
            }
        }

        if (!candidates.TryRandomElement(out IntVec3 spawnCell))
            return false;

        TunnelHiveSpawner_Eruption tunnelHiveSpawner = (TunnelHiveSpawner_Eruption)ThingMaker.MakeThing(Anomaly_ThingDefOf.TSOA_EruptionTunnel);
        tunnelHiveSpawner.defendPoint = defendPoint;
        tunnelHiveSpawner.spawnHive = false;
        tunnelHiveSpawner.insectsPoints = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(map) * Rand.Range(0.3f, 0.6f), 200f, 1000f);
        tunnelHiveSpawner.spawnedByInfestationThingComp = true;
        GenSpawn.Spawn(tunnelHiveSpawner, spawnCell, map, WipeMode.FullRefund);
        return true;
    }

    private List<Thing> SpawnOreVein(Map map, List<IntVec3> cells)
    {
        List<Thing> spawnedOres = new List<Thing>();
        foreach (IntVec3 cell in cells)
        {
            foreach (Plant plant in cell.GetThingList(map).OfType<Plant>().ToList())
            {
                plant.Destroy(DestroyMode.Vanish);
            }

            spawnedOres.Add(GenSpawn.Spawn(chosenOreDef, cell, map));
        }
        return spawnedOres;
    }

    private void SpawnFilthAround(List<Thing> spawnedOres)
    {
        foreach (Thing ore in spawnedOres)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 adjacentCell = ore.Position + GenAdj.CardinalDirections[i];
                if (adjacentCell.InBounds(ore.Map) && Rand.Chance(0.5f))
                {
                    FilthMaker.TryMakeFilth(adjacentCell, ore.Map, ThingDefOf.Filth_RubbleRock);
                }
            }
        }
    }

    private bool TryChooseVein(Map map, int oreAmount, out IntVec3 chosenCell, out List<IntVec3> cells)
    {
        chosenCell = IntVec3.Invalid;
        cells = null;
        if (oreAmount <= 0)
            return false;

        List<IntVec3> candidates = map.AllCells.Where(c => IsValid(c, map)).ToList();
        candidates.Shuffle();
        foreach (IntVec3 center in candidates.Take(MaxVeinAttempts))
        {
            List<IntVec3> proposed = GridShapeMaker.IrregularLump(center, map, oreAmount, c => IsValid(c, map));
            if (proposed.Count != oreAmount)
                continue;

            chosenCell = center;
            cells = proposed;
            return true;
        }

        return false;
    }

    private bool IsValid(IntVec3 c, Map map)
    {
        if (!c.InBounds(map))
            return false;

        TerrainDef terrain = c.GetTerrain(map);
        if (terrain == null || terrain.passability == Traversability.Impassable)
            return false;

        // Plants will be cleared when the vein is spawned; other obstructions still block it.
        foreach (Thing thing in c.GetThingList(map))
        {
            if (thing is not Plant && thing.def.passability != Traversability.Standable)
                return false;
        }

        if (c.Fogged(map))
            return false;

        if (c.Roofed(map))
            return false;

        if (c.GetRoom(map)?.PsychologicallyOutdoors != true)
            return false;

        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
        Scribe_Defs.Look(ref chosenOreDef, "chosenOreDef");
    }
}
