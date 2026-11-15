// using RimWorld;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Verse;
// using Verse.AI;

// namespace tsoa.core.anomaly;

// public class JobGiver_KodamaClean : ThinkNode_JobGiver
// {
//     private static readonly WorkGiver_CleanFilth Worker = new WorkGiver_CleanFilth();

//     public override Job TryGiveJob(Pawn pawn)
//     {
//         Map map = pawn.Map;
//         IEnumerable<Thing> filth = map.listerFilthInHomeArea.FilthInHomeArea;

//         if (filth.Count() == 0)
//             return null;

//         Predicate<Thing> validator = thing =>
//         {
//             return Worker.HasJobOnThing(pawn,thing);
//         };

//         Thing target = GenClosest.ClosestThing_Global_Reachable(
//             pawn.Position,
//             map,
//             filth,
//             Worker.PathEndMode,
//             TraverseParms.For(pawn, Worker.MaxPathDanger(pawn)),
//             validator: validator
//         );

//         if (target == null)
//             return null;

//         return Worker.JobOnThing(pawn, target);
//     }
// }