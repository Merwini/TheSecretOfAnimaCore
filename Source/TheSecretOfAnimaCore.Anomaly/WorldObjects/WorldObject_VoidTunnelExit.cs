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
        // Convert caravan into different world object, similar to a transport pod
        // Pre-calculate an ambush chance, and when it will be encountered
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        foreach (var f in base.GetFloatMenuOptions(caravan))
        {
            yield return f;
        }
        
        foreach (FloatMenuOption option in CaravanArrivalActionUtility.GetFloatMenuOptions(
            () => CaravanArrivalAction_EnterVoidTunnel.CanEnter(caravan, this),
            () => new CaravanArrivalAction_EnterVoidTunnel(this),
            "TSOA_VoidTunnelEnter".Translate(),
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

    private Map Map => tunnelExit?.Entrance?.Map;
    private string ColonyName => Map?.Parent?.Label;

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
		FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
		if (!floatMenuAcceptanceReport)
		{
			return floatMenuAcceptanceReport;
		}
        if (tunnelExit == null)
        {
            return false;
        }
		if (tunnelExit != null && tunnelExit.Tile != destinationTile)
		{
			return false;
		}
		return true;
	}

	public override void Arrived(Caravan caravan)
	{
		// TODO call method on void gate exit to send caravan through tunnel
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref tunnelExit, "tunnelExit");
	}

    public static FloatMenuAcceptanceReport CanEnter(Caravan caravan, WorldObject_VoidTunnelExit tunnelExit)
    {
        return tunnelExit != null && tunnelExit.Entrance != null;
    }

	public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_VoidTunnelExit tunnelExit)
	{
		return CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanEnter(caravan, tunnelExit), () => new CaravanArrivalAction_EnterVoidTunnel(tunnelExit), "TSOA_VoidGateEnterTunnelTo".Translate(tunnelExit.Entrance.Map.Parent.Label), caravan, tunnelExit.Tile, tunnelExit);
	}
}
