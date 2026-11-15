using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class LordJob_DefendMinerals : LordJob
{
    public const float DefendRadius = 10f;

    private IntVec3 defendPoint = IntVec3.Invalid;

    public LordJob_DefendMinerals()
    {
    }

    public LordJob_DefendMinerals(IntVec3 defendPoint)
    {
        this.defendPoint = defendPoint;
    }

    public override bool AddFleeToil => false;

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new StateGraph();
        graph.AddToil(new LordToil_DefendMinerals(defendPoint));
        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref defendPoint, "defendPoint", IntVec3.Invalid);
    }
}

public class LordToil_DefendMinerals : LordToil_DefendPoint
{
    public LordToil_DefendMinerals()
    {
    }

    public LordToil_DefendMinerals(IntVec3 point)
        : base(point, defendRadius: LordJob_DefendMinerals.DefendRadius, wanderRadius: 5f)
    {
    }

    public override void UpdateAllDuties()
    {
        base.UpdateAllDuties();
        foreach (Pawn pawn in lord.ownedPawns)
        {
            if (pawn?.mindState?.duty != null)
            {
                pawn.mindState.duty.def = TSOAR_DefOf.TSOA_DefendMinerals;
                pawn.mindState.duty.radius = LordJob_DefendMinerals.DefendRadius;
            }
        }
    }
}
