// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Verse;
// using Verse.AI;
// using RimWorld;

// namespace tsoa.core.anomaly;

// public class JobGiver_KodamaFlee : ThinkNode_JobGiver
// {
//     private const int FleeDistance = 12;
//     private const int DistToDangerToFlee = 12;

//     public override Job TryGiveJob(Pawn pawn)
//     {
//         if (pawn?.Map == null || pawn.Downed)
//         {
//             return null;
//         }

//         List<IAttackTarget> targets = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
//         for (int i = 0; i < targets.Count; i++)
//         {
//             Thing thing = targets[i].Thing;
//             if (thing == null || thing.Destroyed || !thing.Spawned)
//             {
//                 continue;
//             }

//             if (!pawn.Position.InHorDistOf(thing.Position, DistToDangerToFlee))
//             {
//                 continue;
//             }

//             if (!thing.HostileTo(pawn))
//             {
//                 continue;
//             }

//             if (!FleeUtility.ShouldFleeFrom(thing, pawn, checkDistance: false, checkLOS: true))
//             {
//                 continue;
//             }

//             Job job = FleeUtility.FleeJob(pawn, thing, FleeDistance);
//             if (job != null)
//             {
//                 return job;
//             }
//         }

//         return FleeUtility.FleeLargeFireJob(pawn, 60, 10, FleeDistance);
//     }
// }
