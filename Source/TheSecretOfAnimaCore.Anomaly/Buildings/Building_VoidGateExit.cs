using System.Collections.Generic;
using System.Linq;
using Verse;

namespace tsoa.core.anomaly;

public class Building_VoidGateExit : Building_VoidGateEndpoint
{
    protected override string LoadLabelKey => "TSOA_VoidGateLoadReturn";

    public Building_VoidGateExit()
    {
        gateMode = GateMode.VoidDiveStarted;
    }

    protected override bool CanBeginLoading(out string blockingReason)
    {
        if (!base.CanBeginLoading(out blockingReason))
            return false;
        return CanReturnToEntrance(out blockingReason);
    }

    private bool CanReturnToEntrance(out string blockingReason)
    {
        blockingReason = null;
        if (PartnerGate == null || !PartnerGate.Spawned || !PartnerGate.IsGateOpen || PartnerGate.DestinationMap != Map)
        {
            blockingReason = "TSOA_VoidGateEntranceMissing".Translate();
        }
        return blockingReason == null;
    }

    protected override bool ValidatePawns(List<Pawn> pawns, out string invalidReason)
    {
        invalidReason = null;

        Building_VoidGateEntrance partner = PartnerGate as Building_VoidGateEntrance;
        List<Pawn> missing = partner.RequiredReturnPawns().Where(p => !pawns.Contains(p)).ToList();
        if (missing.Count > 0)
            invalidReason = "TSOA_VoidGateReturnEveryone".Translate(string.Join(", ", missing.Select(p => p.LabelShortCap)));
        else if (pawns.Count == 0)
            invalidReason = "TSOA_VoidGateReturnNeedsPawn".Translate();

        return invalidReason == null;
    }

    protected override bool TransferContents(out string failureReason)
    {
        if (!CanReturnToEntrance(out failureReason) || !ValidatePawns(loadingPawns, out failureReason))
            return false;
            
        return DropContentsNear(PartnerGate, out failureReason);
    }

    protected override void PostTransferCompleted()
    {
        Building_VoidGateEntrance partner = PartnerGate as Building_VoidGateEntrance;
        // CompleteTransfer should have cleaned everything up, map should be safe to remove
        Current.Game.CurrentMap = partner.Map;
        CameraJumper.TryJump(partner);
        partner.CloseGate();
    }
}
