using RimWorld;
using RimWorld.Planet;
using Verse;

namespace tsoa.core.anomaly;

public class Building_VoidGate : Building, IThingGlower
{
    private WorldObject tunnelExit;
    private Map diveMap;

    public bool IsGateOpen => tunnelExit != null || diveMap != null;

    public bool ShouldBeLitNow() => false;

    public void StartVoidDive()
    {
        Messages.Message("void dive", MessageTypeDefOf.PositiveEvent);
    }

}