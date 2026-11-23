using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace tsoa.core.anomaly;

public abstract class Building_VoidGateEndpoint : Building
{
    public enum GateMode { Closed, VoidDivePrepared, VoidDiveStarted, VoidTunnelPrepared, VoidTunnelOpen }

    // state flags
    private bool loadingAccepted;
    private bool transferQueued;
    private bool transferFailed;
    private bool transferStarted;

    protected GateMode gateMode;
    protected List<Pawn> loadingPawns = new();

    public CompTransporter compTransporter;

    protected bool TransferQueued => transferQueued;
    protected virtual bool CanTransferLoadedContents => gateMode == GateMode.VoidDivePrepared;
    public virtual bool IsGateOpen => false;

    private Building_VoidGateEndpoint partnerGate;
    public virtual Building_VoidGateEndpoint PartnerGate { get => partnerGate; set => partnerGate = value; }

    public Map DestinationMap => partnerGate?.Map;

    protected abstract string LoadLabelKey { get; }

    protected virtual bool CanBeginLoading(out string blockingReason)
    {
        blockingReason = null;
        if (transferQueued || compTransporter.LoadingInProgressOrReadyToLaunch)
        {
            blockingReason = "TSOA_VoidGateAlreadyLoading".Translate();
            return false;
        }
        return true;
    }
    protected abstract bool ValidatePawns(List<Pawn> pawns, out string invalidReason);
    protected abstract bool TransferContents(out string failureReason);
    protected virtual void PostTransferCompleted() { }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        compTransporter = GetComp<CompTransporter>();
    }

    public override void Tick()
    {
        base.Tick();

        if (!this.IsHashIntervalTick(60) || transferQueued || transferFailed)
            return;

        if (!compTransporter.LoadingInProgressOrReadyToLaunch)
        {
            if (loadingAccepted)
            {
                ResetLoadingFlags();
            }
            return;
        }

        if (!CanTransferLoadedContents)
            return;

        // Pick up loads that started before the gate recorded the selected pawns.
        if (!loadingAccepted && !Notify_LoadingAccepted())
            return;

        if (compTransporter.AnythingLeftToLoad || !compTransporter.innerContainer.Any)
            return;

        transferQueued = true;
        LongEventHandler.QueueLongEvent(CompleteTransfer, "GeneratingMap", false, null);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        bool canLoad = CanBeginLoading(out string blockingReason);
        Command_Action load = new()
        {
            defaultLabel = LoadLabelKey.Translate(),
            defaultDesc = "TSOA_VoidGateLoadDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter"),
            action = () => Find.WindowStack.Add(new Dialog_LoadVoidGate(this))
        };

        if (!canLoad)
        {
            load.Disable(blockingReason);
        }
        yield return load;

        if (compTransporter.LoadingInProgressOrReadyToLaunch && !transferQueued
            && (!transferStarted || gateMode == GateMode.VoidTunnelOpen))
        {
            yield return new Command_Action
            {
                defaultLabel = "CommandCancelLoad".Translate(),
                defaultDesc = "TSOA_VoidGateCancelLoadDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                action = CancelLoading
            };
        }
        if (transferFailed) // Doesn't retry the transfer directly. Lets Tick() get past the early return, and use the normal transfer logic. 
        {
            yield return new Command_Action
            {
                defaultLabel = "TSOA_VoidGateRetryTransfer".Translate(),
                defaultDesc = "TSOA_VoidGateRetryTransferDesc".Translate(),
                action = () => transferFailed = false
            };
        }
    }

    public bool Notify_LoadingAccepted()
    {
        // Instant loading fills the container without making a to-load list.
        List<Pawn> pawns = compTransporter.leftToLoad != null
            ? TransferableUtility.GetPawnsFromTransferables(compTransporter.leftToLoad)
            : new List<Pawn>();
        pawns.AddRange(compTransporter.innerContainer.OfType<Pawn>());
        if (!ValidatePawns(pawns, out string reason))
        {
            CancelLoading();
            Messages.Message(reason, this, MessageTypeDefOf.RejectInput, false);
            return false;
        }

        loadingPawns = pawns.Distinct().ToList();
        loadingAccepted = true;
        transferFailed = false;
        return true;
    }

    public void CancelLoading()
    {
        if (transferQueued)
            return;

        compTransporter.CancelLoad();
        ResetLoadingFlags();
    }

    private void ResetLoadingFlags()
    {
        loadingAccepted = false;
        transferFailed = false;
        transferStarted = false;
        loadingPawns.Clear();
    }

    private void CompleteTransfer()
    {
        try
        {
            if (!Spawned || !loadingAccepted || !CanTransferLoadedContents || compTransporter.AnythingLeftToLoad || !compTransporter.innerContainer.Any)
                return;

            if (loadingPawns.Any(p => p != null && !p.Dead && !p.Destroyed && !compTransporter.innerContainer.Contains(p) && (DestinationMap == null || p.MapHeld != DestinationMap)))
            {
                FailTransfer("TSOA_VoidGateExpeditionNotLoaded".Translate());
                return;
            }

            if (!transferStarted && !ValidatePawns(loadingPawns, out string pawnReason))
            {
                FailTransfer(pawnReason);
                return;
            }

            compTransporter.TryRemoveLord(Map);
            // Dive transfers can make partial progress. A failed tunnel departure leaves the load together.
            transferStarted = gateMode != GateMode.VoidTunnelOpen;
            if (!TransferContents(out string transferFailureReason))
            {
                FailTransfer(transferFailureReason);
                return;
            }

            compTransporter.CleanUpLoadingVars(Map);
            ResetLoadingFlags();
            PostTransferCompleted();
        }
        catch (Exception ex)
        {
            Log.Error($"[TSOA] Void gate transfer failed: {ex}");
            FailTransfer("TSOA_VoidGateTransferFailed".Translate());
        }
        finally
        {
            transferQueued = false;
        }
    }

    // While transferFailed == true, the gate will early return on Tick(), waiting for player to change something
    private void FailTransfer(string transferFailureReason)
    {
        transferFailed = true;
        Messages.Message(transferFailureReason, this, MessageTypeDefOf.RejectInput, false);
    }

    protected bool DropContentsNear(Building destinationGate, out string dropFailureReason)
        => DropContentsNear(compTransporter.innerContainer, destinationGate, out dropFailureReason);

    internal static bool DropContentsNear(ThingOwner contents, Building destinationGate, out string dropFailureReason)
    {
        dropFailureReason = "TSOA_VoidGateTransferFailed".Translate();
        if (destinationGate == null || !destinationGate.Spawned || destinationGate.Destroyed)
            return false;

        Map map = destinationGate.Map;
        IntVec3 exitCell = destinationGate.InteractionCell;
        // Leave the pawns aboard until there is room for the loose cargo as well.
        foreach (Thing thing in contents.OrderBy(thing => thing is Pawn).ToList())
        {
            if (!TryDropNear(contents, thing, map, exitCell, unloadInventory: map.IsPlayerHome))
                return false;
        }

        dropFailureReason = null;
        return true;
    }

    internal static bool TryDropNear(ThingOwner contents, Thing thing, Map map, IntVec3 exitCell, bool unloadInventory)
    {
        bool ValidCell(IntVec3 cell) => cell.InBounds(map) && cell.Standable(map)
            && (thing is not Pawn || cell.GetFirstPawn(map) == null)
            && map.reachability.CanReach(exitCell, cell, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly);

        IntVec3 cell = CellFinder.StandableCellNear(exitCell, map, 12f, ValidCell);
        if (!cell.IsValid)
            return false;

        if (thing is Pawn pawn)
            pawn.jobs?.StopAll();

        if (!contents.TryDrop(thing, cell, map, ThingPlaceMode.Near, out Thing placed, nearPlaceValidator: ValidCell))
            return false;

        if (placed is Pawn arrived && arrived.inventory != null)
            arrived.inventory.UnloadEverything = unloadInventory;

        placed?.SetForbidden(false, false);
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref loadingAccepted, "voidLoadingAccepted");
        Scribe_Values.Look(ref transferStarted, "voidTransferStarted");
        Scribe_Values.Look(ref transferFailed, "voidTransferFailed");
        Scribe_Collections.Look(ref loadingPawns, "voidLoadingPawns", LookMode.Reference);

        Scribe_References.Look(ref partnerGate, "partnerGate");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            loadingPawns ??= new();
    }
}
