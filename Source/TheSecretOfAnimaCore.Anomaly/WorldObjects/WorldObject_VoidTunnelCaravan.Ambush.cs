using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public partial class WorldObject_VoidTunnelCaravan
{
    private Map legacyAmbushMap;
    private List<Pawn> legacyAmbushParty;
    private bool ambushQueued;

    private void QueueAmbush()
    {
        if (ambushQueued)
            return;

        ambushQueued = true;
        LongEventHandler.QueueLongEvent(BeginAmbush, "GeneratingMapForNewEncounter", false, null);
    }

    private void BeginAmbush()
    {
        ambushQueued = false;
        if (!Spawned)
            return;

        ambushAt = NoAmbush;
        List<Pawn> party = LivingPassengers.ToList();
        Pawn leader = party.FirstOrDefault(pawn => CaravanUtility.IsOwner(pawn, Faction));
        PlanetTile siteTile = FindAmbushTile();
        if (leader == null || !siteTile.Valid)
            return;

        WorldObject_VoidTunnelAmbush site = CreateAmbushSite(siteTile);
        try
        {
            Map map = MapGenerator.GenerateMap(new IntVec3(75, 1, 75), site, site.MapGeneratorDef);
            if (map == null || !map.Center.Standable(map))
            {
                site.Destroy();
                return;
            }

            SpawnAmbushers(map, party);
            if (!Building_VoidGateEndpoint.TryDropNear(Contents, leader, map, map.Center, unloadInventory: false))
            {
                site.Destroy();
                return;
            }

            PauseAtAmbush(site, party);

            // Loose cargo stays with the suspended caravan during the fight.
            foreach (Pawn pawn in party)
                if (pawn != leader)
                    Building_VoidGateEndpoint.TryDropNear(Contents, pawn, map, map.Center, unloadInventory: false);

            CameraJumper.TryJump(new TargetInfo(map.Center, map));
            Find.LetterStack.ReceiveLetter("TSOA_VoidGateCaravanAmbushLetter".Translate(),
                "TSOA_VoidGateCaravanAmbushText".Translate(Label), LetterDefOf.ThreatBig, new LookTargets(map.Center, map));
        }
        catch (Exception ex)
        {
            Log.Error($"[TSOA] Void tunnel ambush failed: {ex}");
            if (Spawned && !site.Destroyed)
                site.Destroy();
        }
    }

    private void SpawnAmbushers(Map map, List<Pawn> party)
    {
        Faction entities = Faction.OfEntities;
        int count = Mathf.Clamp((party.Count(pawn => CaravanUtility.IsOwner(pawn, Faction)) + 1) / 2, 1, 6);
        IntVec3 enemySpot = map.Center + new IntVec3(18, 0, 0);
        List<Pawn> enemies = new(count);
        for (int i = 0; i < count; i++)
        {
            Pawn enemy = PawnGenerator.GeneratePawn(PawnKindDefOf.Fingerspike, entities);
            IntVec3 cell = CellFinder.RandomSpawnCellForPawnNear(enemySpot, map, 5);
            GenSpawn.Spawn(enemy, cell, map);
            enemies.Add(enemy);
        }
        LordMaker.MakeNewLord(entities, new LordJob_AssaultColony(entities, canKidnap: false, canTimeoutOrFlee: false,
            canSteal: false), map, enemies);
    }

    private PlanetTile FindAmbushTile()
    {
        // DrawPos can be over water or an occupied tile. Use the nearest tile that can host the map.
        PlanetLayer layer = Tile.Layer;
        Vector3 direction = DrawPos.normalized;
        PlanetTile closest = PlanetTile.Invalid;
        float closestAlignment = float.NegativeInfinity;
        for (int i = 0; i < layer.TilesCount; i++)
        {
            float alignment = Vector3.Dot(direction, layer.GetTileCenter(i).normalized);
            if (alignment <= closestAlignment)
                continue;

            PlanetTile candidate = new(i, layer);
            if (Find.World.Impassable(candidate) || Find.WorldObjects.AnyWorldObjectAt(candidate))
                continue;

            closest = candidate;
            closestAlignment = alignment;
        }
        return closest;
    }

    private WorldObject_VoidTunnelAmbush CreateAmbushSite(PlanetTile tile)
    {
        WorldObject_VoidTunnelAmbush site = (WorldObject_VoidTunnelAmbush)WorldObjectMaker.MakeWorldObject(Anomaly_MiscDefOf.TSOA_VoidTunnelAmbushSite);
        site.Tile = tile;
        site.SetFaction(Faction);
        Find.WorldObjects.Add(site);
        return site;
    }

    private void PauseAtAmbush(WorldObject_VoidTunnelAmbush site, List<Pawn> party, Map legacyMap = null)
    {
        // Keep the original world object saved in the site so its route and progress carry over.
        Find.WorldObjects.Remove(this);
        site.TakeOver(this, party, legacyMap);
    }

    private void MigrateLegacyAmbush()
    {
        PlanetTile siteTile = FindAmbushTile();
        WorldObject_VoidTunnelAmbush site = CreateAmbushSite(siteTile.Valid ? siteTile : Tile);
        List<Pawn> party = legacyAmbushParty ?? LivingPassengers.ToList();
        PauseAtAmbush(site, party, legacyAmbushMap);
        legacyAmbushMap = null;
        legacyAmbushParty = null;
    }

    internal bool HasPassenger(Pawn pawn) => Contents.Contains(pawn);

    internal bool TryBoardAmbushPawn(Pawn pawn, Map map)
    {
        // A spawned pawn can have the map as its holder, so despawn it before checking containers.
        if (pawn.Spawned)
        {
            IntVec3 cell = pawn.Position;
            Rot4 rotation = pawn.Rotation;
            pawn.jobs?.StopAll();
            pawn.DeSpawn();
            if (!Contents.TryAdd(pawn))
            {
                GenSpawn.Spawn(pawn, cell, map, rotation);
                return false;
            }
        }
        else if (pawn.holdingOwner?.TryTransferToContainer(pawn, Contents) != true)
        {
            return false;
        }

        RetainWorldPawn(pawn);
        return true;
    }

    internal void ResumeFromAmbush()
    {
        lastMassCheckTick = -MassCheckInterval;
        Find.WorldObjects.Add(this);
    }
}
