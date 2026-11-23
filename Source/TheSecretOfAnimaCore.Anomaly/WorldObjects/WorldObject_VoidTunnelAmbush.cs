using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

public class WorldObject_VoidTunnelAmbush : MapParent
{
    private WorldObject_VoidTunnelCaravan caravan;
    private List<Pawn> party;
    private Map legacyAmbushMap;

    private Map EncounterMap => legacyAmbushMap ?? Map;

    internal void TakeOver(WorldObject_VoidTunnelCaravan trip, List<Pawn> passengers, Map legacyMap)
    {
        caravan = trip;
        party = passengers;
        legacyAmbushMap = legacyMap;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        Map map = EncounterMap;
        if (caravan == null || map == null)
            yield break;

        // Older encounters are still registered under their original pocket map parent.
        if (legacyAmbushMap != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "TSOA_VoidGateCaravanViewAmbush".Translate(),
                icon = def.ExpandingIconTexture,
                action = () => CameraJumper.TryJump(new TargetInfo(map.Center, map))
            };
        }

        Command_Action reform = new()
        {
            defaultLabel = "TSOA_VoidGateCaravanResume".Translate(),
            defaultDesc = "TSOA_VoidGateCaravanResumeDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan"),
            action = ReformCaravan
        };
        string blockingReason = ReformBlockReason(map);
        if (blockingReason != null)
            reform.Disable(blockingReason);
        yield return reform;
    }

    private string ReformBlockReason(Map map)
    {
        if (GenHostility.AnyHostileActiveThreatToPlayer(map))
            return "TSOA_VoidGateCaravanAmbushHostiles".Translate();

        if (!party.Any(pawn => IsPresentOwner(pawn, map) && !pawn.Downed && !pawn.InMentalState))
            return "TSOA_VoidGateNeedsColonist".Translate();

        return null;
    }

    private bool IsPresentOwner(Pawn pawn, Map map) => pawn != null && !pawn.Destroyed && !pawn.Dead
        && (pawn.MapHeld == map || caravan.HasPassenger(pawn)) && CaravanUtility.IsOwner(pawn, Faction);

    private void ReformCaravan()
    {
        Map map = EncounterMap;
        if (caravan == null || map == null || ReformBlockReason(map) != null)
            return;

        // Pick up carried pawns before despawning their carriers.
        foreach (Pawn pawn in party.Where(pawn => pawn != null && !pawn.Destroyed && !pawn.Dead && pawn.MapHeld == map).OrderBy(pawn => pawn.Spawned))
        {
            if (caravan.TryBoardAmbushPawn(pawn, map))
                continue;

            Messages.Message("TSOA_VoidGateCaravanReformFailed".Translate(pawn.LabelShortCap), this, MessageTypeDefOf.RejectInput);
            return;
        }

        WorldObject_VoidTunnelCaravan trip = caravan;
        trip.ResumeFromAmbush();
        caravan = null;
        Destroy();
        CameraJumper.TryJumpAndSelect(trip);
        Find.WorldCameraDriver.JumpTo(trip.DrawPos);
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        Map map = EncounterMap;
        if (caravan == null || map == null || !party.Any(pawn => IsPresentOwner(pawn, map)))
            Destroy();
    }

    public override void Destroy()
    {
        Map map = EncounterMap;
        if (map != null && Find.Maps.Contains(map))
        {
            if (legacyAmbushMap != null)
                PocketMapUtility.DestroyPocketMap(map);
            else
                Current.Game.DeinitAndRemoveMap(map, notifyPlayer: false);
        }

        caravan?.Destroy();
        caravan = null;
        base.Destroy();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref caravan, "caravan");
        Scribe_Collections.Look(ref party, "party", LookMode.Reference);
        Scribe_References.Look(ref legacyAmbushMap, "legacyAmbushMap");
    }
}
