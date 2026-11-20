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
    // state flags
    private bool loadingAccepted;
    private bool transferQueued;
    private bool transferFailed;
    private bool transferStarted;

    protected List<Pawn> loadingPawns = new();

    public CompTransporter compTransporter;

    protected bool TransferQueued => transferQueued;
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

        if (!this.IsHashIntervalTick(60) || transferQueued || transferFailed || !loadingAccepted)
            return;

        if (!compTransporter.LoadingInProgressOrReadyToLaunch)
        {
            ResetLoadingFlags();
            return;
        }

        if (!compTransporter.AnythingLeftToLoad && compTransporter.innerContainer.Any)
        {
            transferQueued = true;
            LongEventHandler.QueueLongEvent(CompleteTransfer, "GeneratingMap", false, null);
        }
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

        if (compTransporter.LoadingInProgressOrReadyToLaunch && !transferQueued && !transferStarted)
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
        List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(compTransporter.leftToLoad);
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
            if (!Spawned || !loadingAccepted || compTransporter.AnythingLeftToLoad)
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
            transferStarted = true;
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
    {
        dropFailureReason = "TSOA_VoidGateTransferFailed".Translate();
        if (destinationGate == null || !destinationGate.Spawned || destinationGate.Destroyed)
            return false;

        Map map = destinationGate.Map;
        IntVec3 exitCell = destinationGate.InteractionCell;
        foreach (Thing thing in compTransporter.innerContainer.ToList())
        {
            bool ValidCell(IntVec3 c) => c.InBounds(map) && c.Standable(map)
                && (thing is not Pawn || c.GetFirstPawn(map) == null)
                && map.reachability.CanReach(exitCell, c, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly);

            IntVec3 cell = CellFinder.StandableCellNear(exitCell, map, 12f, ValidCell);
            if (!cell.IsValid)
                return false;

            if (thing is Pawn pawn)
            {
                pawn.jobs?.StopAll();
            }

            if (!compTransporter.innerContainer.TryDrop(thing, cell, map, ThingPlaceMode.Near, out Thing placed, nearPlaceValidator: ValidCell))
                return false;

            if (placed is Pawn arrived && arrived.inventory != null)
            {
                arrived.inventory.UnloadEverything = map.IsPlayerHome;
            }

            placed?.SetForbidden(false, false);
        }

        dropFailureReason = null;
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
