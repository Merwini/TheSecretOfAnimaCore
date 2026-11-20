using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

public class Building_VoidGateEntrance : Building_VoidGateEndpoint, IThingGlower
{
    public enum GateMode { Closed, VoidDivePrepared, VoidDiveStarted, VoidTunnelPrepared, VoidTunnelOpen }

    private GateMode gateMode;
    private WorldObject tunnelExit;
    public WorldObject TunnelExit => tunnelExit;
    private List<Pawn> expeditionPawns = new();
    private float ritualQuality;

    public override bool IsGateOpen => gateMode != GateMode.Closed || tunnelExit != null || DestinationMap != null;
    protected override string LoadLabelKey => "TSOA_VoidGateLoadGate";
    public bool ShouldBeLitNow() => IsGateOpen;

    public override void Tick()
    {
        base.Tick();

        // TODO periodic entities emerge from gate if open, interval reduced by ritualQuality
        // this is to force players to do expeditions quickly, and also not send their entire colony out
    }

    public void PrepareVoidDive(float ritualQuality)
    {
        if (IsGateOpen)
            return;

        this.ritualQuality = ritualQuality;
        gateMode = GateMode.VoidDivePrepared;
        Messages.Message("TSOA_VoidGateDivePrepared".Translate(), this, MessageTypeDefOf.PositiveEvent);
    }

    public void PrepareVoidTunnel(float ritualQuality)
    {
        if (IsGateOpen)
            return;

        this.ritualQuality = ritualQuality;
        gateMode = GateMode.VoidTunnelPrepared;
        Messages.Message("TSOA_VoidGateTunnelPrepared".Translate(), this, MessageTypeDefOf.PositiveEvent);
    }

    protected override bool CanBeginLoading(out string blockingReason)
    {
        if (!base.CanBeginLoading(out blockingReason))
            return false;

        if (gateMode == GateMode.Closed)
        {
            blockingReason = "TSOA_VoidGateClosed".Translate();
        }
        // Can load if VoidDivePrepared
        else if (gateMode == GateMode.VoidDiveStarted)
        {
            blockingReason = "TSOA_VoidGateExpeditionAway".Translate();
        }
        else if (gateMode == GateMode.VoidTunnelPrepared && TunnelExit == null)
        {
            blockingReason = "TSOA_VoidGateTunnelExitNotSpawned".Translate();
        }

        return blockingReason == null;
    }

    protected override bool ValidatePawns(List<Pawn> pawns, out string invalidReason)
    {
        invalidReason = null;

        // IsColonistPlayerControlled is false while a pawn is loaded
        if (!pawns.Any(p => p.IsColonist && p.MentalStateDef == null && (p.HostFaction == null || p.IsSlave) && !p.Downed && !p.Dead))
        {
            invalidReason = "TSOA_VoidGateNeedsColonist".Translate();
        }

        return invalidReason == null;
    }

    protected override bool TransferContents(out string transferFailureReason)
    {
        transferFailureReason = "TSOA_VoidGateGenerationFailed".Translate();

        if (gateMode == GateMode.VoidDivePrepared)
        {
            // TODO separate method to generate maps, with more complex logic
            Map newMap = PocketMapUtility.GeneratePocketMap(new IntVec3(75, 1, 75), Anomaly_MiscDefOf.TSOA_VoidDive_Test, null, Map);
            PartnerGate = newMap.listerThings.AllThings.OfType<Building_VoidGateExit>().FirstOrDefault();
            
            // Safety check so we don't send pawns to a map that failed to generate
            if (PartnerGate == null || !PartnerGate.InteractionCell.Standable(newMap))
            {
                PocketMapUtility.DestroyPocketMap(newMap);
                newMap = null;
                PartnerGate = null;
                // TODO generate a new map
                return false;
            }
            PartnerGate.PartnerGate = this;
            expeditionPawns = new List<Pawn>(loadingPawns);
            gateMode = GateMode.VoidDiveStarted;
            return DropContentsNear(PartnerGate, out transferFailureReason);
        }

        return false;
    }

    protected override void PostTransferCompleted()
    {
        Current.Game.CurrentMap = DestinationMap;
        CameraJumper.TryJump(PartnerGate);
    }

    public IEnumerable<Pawn> RequiredReturnPawns()
    {
        if (DestinationMap == null)
            return Enumerable.Empty<Pawn>();

        return expeditionPawns.Where(p => p != null && !p.Dead && !p.Destroyed && p.MapHeld == DestinationMap)
            .Concat(DestinationMap.mapPawns.AllPawns.Where(p => p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony))
            .Distinct();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        if (DebugSettings.godMode && !IsGateOpen)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Prepare Void Dive (0% quality)",
                action = () => PrepareVoidDive(0f)
            };
            yield return new Command_Action
            {
                defaultLabel = "DEV: Prepare Void Dive (100% quality)",
                action = () => PrepareVoidDive(1f)
            };

            yield return new Command_Action
            {
                defaultLabel = "DEV: Prepare Void Tunnel (0% quality)",
                action = () => PrepareVoidTunnel(0f)
            };
            yield return new Command_Action
            {
                defaultLabel = "DEV: Prepare Void Tunnel (100% quality)",
                action = () => PrepareVoidTunnel(1f)
            };
            // TODO prepare void tunnel
        }

        if (gateMode == GateMode.VoidTunnelPrepared && TunnelExit == null)
        {
            yield return new Command_Action
            {
                defaultLabel = "TSOA_VoidGateSelectTunnelExit".Translate(),
                defaultDesc = "TSOA_VoidGateSelectTunnelExitDesc".Translate(),
                icon = def.uiIcon,
                action = BeginTunnelExitTargeting
            };
        }

        if (IsGateOpen)
        {
            Command_Action close = new()
            {
                defaultLabel = "TSOA_VoidGateClose".Translate(),
                defaultDesc = "TSOA_VoidGateCloseDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                action = RequestClose
            };

            if (TransferQueued)
            {
                close.Disable("TSOA_VoidGateAlreadyLoading".Translate());
            }
            yield return close;
        }
    }

    private void BeginTunnelExitTargeting()
    {
        if (!Spawned || gateMode != GateMode.VoidTunnelPrepared || TunnelExit != null)
            return;

        CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(Map.Tile)));
        Find.WorldSelector.ClearSelection();
        Find.WorldTargeter.BeginTargeting(
            TryPlaceTunnelExit,
            canTargetTiles: true,
            mouseAttachment: def.uiIcon,
            closeWorldTabWhenFinished: true,
            onUpdate: () =>
            {
                if (!Spawned || gateMode != GateMode.VoidTunnelPrepared || TunnelExit != null)
                {
                    Find.WorldTargeter.StopTargeting();
                }
            },
            extraLabelGetter: target => CanPlaceTunnelExit(target.Tile, out string reason) ? "TSOA_VoidGateChooseTunnelExit".Translate() : reason);
    }

    private static bool CanPlaceTunnelExit(PlanetTile tile, out string failureReason)
    {
        failureReason = null;

        if (!tile.Valid)
        {
            failureReason = "TSOA_VoidGateTunnelInvalidTile".Translate();
            return false;
        }
        if (Find.World.Impassable(tile))
        {
            failureReason = "TSOA_VoidGateTunnelImpassable".Translate();
            return false;
        }

        List<PlanetTile> neighbors = new();
        Find.WorldGrid.GetTileNeighbors(tile, neighbors);
        if (!neighbors.Any(neighbor => !Find.World.Impassable(neighbor)))
        {
            failureReason = "TSOA_VoidGateTunnelNoPassableNeighbor".Translate();
            return false;
        }
        if (Find.WorldObjects.AnyWorldObjectAt(tile) || neighbors.Any(neighbor => Find.WorldObjects.AnyWorldObjectAt(neighbor)))
        {
            failureReason = "TSOA_VoidGateTunnelWorldObjectNearby".Translate();
            return false;
        }
        return true;
    }

    private bool TryPlaceTunnelExit(GlobalTargetInfo target)
    {
        // Check in case something happened to the Entrance while targetting
        if (!Spawned || gateMode != GateMode.VoidTunnelPrepared || TunnelExit != null)
            return true;

        if (!CanPlaceTunnelExit(target.Tile, out string reason))
        {
            Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
            return false;
        }

        WorldObject_VoidTunnelExit exit = (WorldObject_VoidTunnelExit)WorldObjectMaker.MakeWorldObject(Anomaly_MiscDefOf.TSOA_VoidTunnelExit);
        exit.Tile = target.Tile;
        exit.SetFaction(Faction.OfPlayer);
        exit.Entrance = this;
        Find.WorldObjects.Add(exit);
        tunnelExit = exit;
        gateMode = GateMode.VoidTunnelOpen;
        Messages.Message("TSOA_VoidGateTunnelOpened".Translate(), exit, MessageTypeDefOf.PositiveEvent);
        return true;
    }

    private void RequestClose()
    {
        int pawnCount = DestinationMap?.mapPawns.AllPawns.Count ?? 0;
        if (pawnCount > 0)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("TSOA_VoidGateCloseWarning".Translate(pawnCount), CloseGate, destructive: true));
        }
        // TODO warning about losing void tunnel caravans
        else
        {
            CloseGate();
        }
    }

    public void CloseGate()
    {
        CancelLoading();
        Map oldMap = DestinationMap;
        PartnerGate?.CancelLoading();
        PartnerGate = null;
        expeditionPawns.Clear();
        gateMode = GateMode.Closed;
        if (tunnelExit != null && !tunnelExit.Destroyed)
            tunnelExit.Destroy();
        tunnelExit = null;
        ritualQuality = 0f;
        if (oldMap != null)
        {
            if (Find.CurrentMap == oldMap && Spawned)
            {
                Current.Game.CurrentMap = Map;
            }
            PocketMapUtility.DestroyPocketMap(oldMap);
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        CloseGate();
        base.DeSpawn(mode);
    }

    public override string GetInspectString()
    {
        string status = gateMode switch
        {
            GateMode.VoidTunnelPrepared => "TSOA_VoidGateTunnelExitNotSpawned",
            GateMode.VoidTunnelOpen => "TSOA_VoidGateTunnelReady",
            _ => !IsGateOpen ? "TSOA_VoidGateClosed" : DestinationMap == null ? "TSOA_VoidGateDiveReady" : "TSOA_VoidGateExpeditionAway"
        };
        return base.GetInspectString() + "\n" + status.Translate();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_References.Look(ref tunnelExit, "tunnelExit");
        Scribe_Collections.Look(ref expeditionPawns, "expeditionPawns", LookMode.Reference);
        Scribe_Values.Look(ref gateMode, "gateMode");
        Scribe_Values.Look(ref ritualQuality, "ritualQuality");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            expeditionPawns ??= new();
        }
    }
}
