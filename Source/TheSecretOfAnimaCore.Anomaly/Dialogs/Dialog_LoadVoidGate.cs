using System.Collections.Generic;
using RimWorld;

namespace tsoa.core.anomaly;

public class Dialog_LoadVoidGate : Dialog_LoadTransporters
{
    private readonly Building_VoidGateEndpoint gate;

    public Dialog_LoadVoidGate(Building_VoidGateEndpoint gate) : base(gate.Map, new List<CompTransporter> { gate.compTransporter })
    {
        this.gate = gate;
    }

    public override bool OnCloseRequest()
    {
        // TODO rewrite to avoid base, so I can skip TransportersCaravanWillBeImmobile warning
        if (!base.OnCloseRequest())
            return false;

        // Accept starts the vanilla loading lord before closing the dialog.
        // Reject a partial expedition while leaving the dialog's selection intact.
        if (gate.Spawned && gate.compTransporter.LoadingInProgressOrReadyToLaunch)
            return gate.Notify_LoadingAccepted();

        return true;
    }
}
