using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class TunnelHiveSpawner_Eruption : TunnelHiveSpawner
{
    public IntVec3 defendPoint = IntVec3.Invalid;

    public override void Spawn(Map map, IntVec3 loc)
    {
        HashSet<Lord> existingLords = new HashSet<Lord>(map.lordManager.lords); // capture existing Lords
        base.Spawn(map, loc);

        // Let vanilla generate the insects, then find the Lord that isn't in the captured List
        foreach (Lord lord in map.lordManager.lords)
        {
            if (!existingLords.Contains(lord) && lord.faction == Faction.OfInsects
                && lord.LordJob is LordJob_AssaultColony)
            {
                lord.SetJob(new LordJob_DefendMinerals(defendPoint.IsValid ? defendPoint : loc));
                lord.GotoToil(lord.Graph.StartingToil);

                // Need to cancel existing assault job
                foreach (Pawn pawn in lord.ownedPawns)
                {
                    pawn.mindState.enemyTarget = null;
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref defendPoint, "defendPoint", IntVec3.Invalid);
    }
}
