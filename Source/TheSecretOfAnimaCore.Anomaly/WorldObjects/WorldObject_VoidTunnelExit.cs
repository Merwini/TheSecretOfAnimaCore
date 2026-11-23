using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace tsoa.core.anomaly;

public class WorldObject_VoidTunnelExit : WorldObject
{
    private Building_VoidGateEntrance entrance;
    public Building_VoidGateEntrance Entrance { get => entrance; set => entrance = value; }

    public void SendCaravan(Caravan caravan)
    {
        if (caravan.Tile != Tile || !CaravanArrivalAction_EnterVoidTunnel.CanEnter(caravan, this))
            return;

        if (WorldObject_VoidTunnelCaravan.TryDepart(caravan.GetDirectlyHeldThings(), entrance, this,
            caravan.Faction, outgoing: false, caravanName: caravan.Name))
        {
            caravan.Destroy();
        }
        else
        {
            Messages.Message("TSOA_VoidGateTunnelDepartureFailed".Translate(), caravan, MessageTypeDefOf.RejectInput);
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        foreach (FloatMenuOption option in base.GetFloatMenuOptions(caravan))
            yield return option;

        foreach (FloatMenuOption option in CaravanArrivalActionUtility.GetFloatMenuOptions(
            () => CaravanArrivalAction_EnterVoidTunnel.CanEnter(caravan, this),
            () => new CaravanArrivalAction_EnterVoidTunnel(this),
            "TSOA_VoidGateEnterTunnelTo".Translate(entrance?.Map?.Parent?.Label ?? Label),
            caravan,
            Tile,
            this))
        {
            yield return option;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref entrance, "entrance");
    }
}

public class CaravanArrivalAction_EnterVoidTunnel : CaravanArrivalAction
{
    private WorldObject_VoidTunnelExit tunnelExit;

    private string ColonyName => tunnelExit?.Entrance?.Map?.Parent?.Label ?? tunnelExit?.Label ?? string.Empty;

    public override string Label => "TSOA_VoidGateTunnelTo".Translate(ColonyName);
    public override string ReportString => "TSOA_VoidGateTunnelReport".Translate(ColonyName);

    public CaravanArrivalAction_EnterVoidTunnel()
    {
    }

    public CaravanArrivalAction_EnterVoidTunnel(WorldObject_VoidTunnelExit tunnelExit)
    {
        this.tunnelExit = tunnelExit;
    }

    public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
    {
        FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
        if (!report)
            return report;

        if (tunnelExit == null || tunnelExit.Tile != destinationTile)
            return false;

        return CanEnter(caravan, tunnelExit);
    }

    public override void Arrived(Caravan caravan) => tunnelExit?.SendCaravan(caravan);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref tunnelExit, "tunnelExit");
    }

    public static FloatMenuAcceptanceReport CanEnter(Caravan caravan, WorldObject_VoidTunnelExit tunnelExit)
    {
        if (tunnelExit?.Entrance?.HasOpenTunnel(tunnelExit) != true)
            return FloatMenuAcceptanceReport.WithFailReason("TSOA_VoidGateEntranceMissing".Translate());

        if (caravan.ImmobilizedByMass)
            return FloatMenuAcceptanceReport.WithFailReason("CaravanImmobilizedByMass".Translate());

        return true;
    }
}
