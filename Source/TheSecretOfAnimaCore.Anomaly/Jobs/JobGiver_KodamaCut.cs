// using RimWorld;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Verse;
// using Verse.AI;

// namespace tsoa.core.anomaly;

// public class JobGiver_KodamaCut : ThinkNode_JobGiver
// {
//     private static readonly WorkGiver_PlantsCut DesignatedWorker = new WorkGiver_PlantsCut();
//     private static readonly WorkGiver_GrowerHarvest HarvestGrowZoneWorker = new WorkGiver_GrowerHarvest(); // should I make this a separate ThinkNode?

//     public override Job TryGiveJob(Pawn pawn)
//     {
//         Job job = TryGiveDesignatedJob(pawn);
        
//         if (job == null)
//         {
//             job = TryGiveHarvestJob(pawn);
//         }

//         return job;
//     }

//     private Job TryGiveDesignatedJob(Pawn pawn)
//     {
//         IEnumerable<Thing> cuttablePlants = DesignatedWorker.PotentialWorkThingsGlobal(pawn);
//         if (cuttablePlants.Count() == 0)
//             return null;

//         Predicate<Thing> validator = thing =>
//         {
//             return !thing.IsForbidden(pawn) && DesignatedWorker.HasJobOnThing(pawn, thing);
//         };

//         Thing target = GenClosest.ClosestThing_Global_Reachable(
//             pawn.Position,
//             pawn.Map,
//             cuttablePlants,
//             DesignatedWorker.PathEndMode,
//             TraverseParms.For(pawn, DesignatedWorker.MaxPathDanger(pawn)),
//             validator: validator
//         );

//         return DesignatedWorker.JobOnThing(pawn, target);
//     }

//     // Cobbled together from JobGiver_Work.TryIssueJobPackage and WorkGiver_GrowerHarvest.TryGiveJob
//     private Job TryGiveHarvestJob(Pawn pawn)
//     {
//         IEnumerable<IntVec3> potentialCells = HarvestGrowZoneWorker.PotentialWorkCellsGlobal(pawn);

//         if (potentialCells.Count() == 0)
//         {
//             return null;
//         }

//         IntVec3 bestCell = IntVec3.Invalid;
//         float bestDistSq = 99999f;
//         float bestPriority = float.MinValue;

//         bool prioritized = HarvestGrowZoneWorker.Prioritized;
//         bool allowUnreachable = HarvestGrowZoneWorker.AllowUnreachable;
//         PathEndMode pathEndMode = HarvestGrowZoneWorker.PathEndMode;
//         Danger maxDanger = HarvestGrowZoneWorker.MaxPathDanger(pawn);

//         foreach (IntVec3 cell in HarvestGrowZoneWorker.PotentialWorkCellsGlobal(pawn))
//         {
//             bool useThisCell = false;
//             float distSq = (cell - pawn.Position).LengthHorizontalSquared;
//             float priority = 0f;

//             if (prioritized)
//             {
//                 if (!cell.IsForbidden(pawn) && HarvestGrowZoneWorker.HasJobOnCell(pawn, cell, false))
//                 {
//                     if (!allowUnreachable && !pawn.CanReach(cell, pathEndMode, maxDanger))
//                     {
//                         continue;
//                     }

//                     priority = HarvestGrowZoneWorker.GetPriority(pawn, cell);

//                     if (priority > bestPriority || priority == bestPriority && distSq < bestDistSq)
//                     {
//                         useThisCell = true;
//                     }
//                 }
//             }
//             else if (distSq < bestDistSq && !cell.IsForbidden(pawn) && HarvestGrowZoneWorker.HasJobOnCell(pawn, cell))
//             {
//                 if (!allowUnreachable && !pawn.CanReach(cell, pathEndMode, maxDanger))
//                 {
//                     continue;
//                 }

//                 useThisCell = true;
//             }

//             if (useThisCell)
//             {
//                 bestCell = cell;
//                 bestDistSq = distSq;
//                 bestPriority = priority;
//             }
//         }

//         if (!bestCell.IsValid)
//         {
//             return null;
//         }

//         return HarvestGrowZoneWorker.JobOnCell(pawn, bestCell, false);

//     }
// }
