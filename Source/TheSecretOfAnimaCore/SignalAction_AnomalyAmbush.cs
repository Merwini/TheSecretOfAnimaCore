using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace tsoa.core;

public class SignalAction_AnomalyAmbush : SignalAction_Ambush
{
    private List<Faction> AnomalyFactions
    {
        get
        {
            List<Faction> result = new List<Faction>();

            Faction entities = Faction.OfEntities;
            if (entities != null)
                result.Add(entities);

            Faction horaxCult = Faction.OfHoraxCult;
            if (horaxCult != null)
                result.Add(horaxCult);

            // TODO I haven't actually made this mod yet, make sure I end up using this packageId and defName
            if (ModLister.GetActiveModWithIdentifier("tsoa.factions") != null)
            {
                List<Faction> voidDisciples = Find.FactionManager.AllFactions.Where(f => f.def.defName == "TSOA_VoidDisciples").ToList();
                if (!voidDisciples.NullOrEmpty())
                {
                    voidDisciples.Shuffle(); // in case of multiple
                    result.Add(voidDisciples[0]);
                }
            }

            return result;
        }
    }

    public override void DoAction(SignalArgs args)
    {
        if (!ModsConfig.AnomalyActive)
            return;

        if (points <= 0f)
            return;

        List<Pawn> list = new List<Pawn>();
        foreach (Pawn item in GenerateAnomalyAmbushPawns())
        {
            IntVec3 result;
            if (spawnPawnsOnEdge)
            {
                if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.Standable(base.Map) && !x.Fogged(base.Map) && base.Map.reachability.CanReachColony(x), base.Map, CellFinder.EdgeRoadChance_Ignore, out result))
                {
                    Find.WorldPawns.PassToWorld(item);
                    break;
                }
            }
            else if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(spawnAround, spawnNear, base.Map, out result))
            {
                Find.WorldPawns.PassToWorld(item);
                break;
            }
            if (useDropPods)
            {
                DropPodUtility.DropThingsNear(result, base.Map, Gen.YieldSingle(item));
            }
            else
            {
                GenSpawn.Spawn(item, result, base.Map);
                if (!spawnPawnsOnEdge)
                {
                    for (int num = 0; num < 10; num++)
                    {
                        FleckMaker.ThrowAirPuffUp(item.DrawPos, base.Map);
                    }
                }
            }
            list.Add(item);
        }
        if (!list.Any())
        {
            return;
        }
        if (ambushType == SignalActionAmbushType.Manhunters)
        {
            for (int num2 = 0; num2 < list.Count; num2++)
            {
                list[num2].health.AddHediff(HediffDefOf.Scaria);
                list[num2].mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
            }
        }
        else
        {
            Faction faction = list[0].Faction;
            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), base.Map, list);
        }
        if (!spawnPawnsOnEdge && !useDropPods)
        {
            for (int num3 = 0; num3 < list.Count; num3++)
            {
                list[num3].jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 120));
                list[num3].Rotation = Rot4.Random;
            }
        }
        Find.LetterStack.ReceiveLetter("LetterLabelAmbushInExistingMap".Translate(), "LetterAmbushInExistingMap".Translate(Faction.OfPlayer.def.pawnsPlural).CapitalizeFirst(), LetterDefOf.ThreatBig, list);
    }

    private IEnumerable<Entity> GenerateAnomalyAmbushPawns()
    {
        List<Faction> possibleFactions = AnomalyFactions;
        if (possibleFactions.Count == 0)
        {
            Log.Warning("SignalAction_AnomalyAmbush.GenerateAnomalyAmbushPawns failed to find any usable factions");
            return Enumerable.Empty<Pawn>();
        }

        possibleFactions.Shuffle();
        Faction chosenFaction = possibleFactions[0]; // I probably don't need to null check this, do I?

        PawnGroupKindDef groupKindDef = null;
        if (chosenFaction == Faction.OfEntities)
        {
            groupKindDef = chosenFaction.def.pawnGroupMakers.RandomElement().kindDef;
        }
        else
        {
            groupKindDef = PawnGroupKindDefOf.Combat;
        }

        return PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = groupKindDef,
            tile = base.Map.Tile,
            faction = chosenFaction,
            points = Mathf.Max(points, chosenFaction.def.MinPointsToGeneratePawnGroup(groupKindDef))
        });
    }
}
