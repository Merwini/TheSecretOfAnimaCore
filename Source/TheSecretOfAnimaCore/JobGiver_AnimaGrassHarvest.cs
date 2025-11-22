using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;

namespace nuff.tsoa.core
{
    public class JobGiver_AnimaGrassHarvest : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState.duty;
            if (duty == null)
                return null;

            if (!pawn.CanReserveAndReach(duty.focusSecond, PathEndMode.Touch, Danger.Deadly))
                return null;

            Job job = JobMaker.MakeJob(TSOA_DefOf.TSOA_HarvestAnimaGrassJob, duty.focusSecond, duty.focus);

            return job;
        }
    }
}
