using RimWorld.Planet;
using Verse;

namespace tsoa.core.anomaly;

public class WorldObject_VoidTunnelExit : WorldObject
{
    private Building_VoidGateEntrance entrance;
    public Building_VoidGateEntrance Entrance { get => entrance; set => entrance = value; }

    // TODO gizmo to send caravans back to Entrance

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref entrance, "entrance");
    }
}
