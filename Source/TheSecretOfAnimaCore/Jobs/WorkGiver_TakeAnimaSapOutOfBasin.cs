using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using Verse;
using Verse.AI;

namespace tsoa.core
{
    public class WorkGiver_TakeAnimaSapOutOfBasin : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(TSOA_DefOf.TSOA_AnimaSapTap);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_AnimaSapBasin basin && basin.ShouldEmpty))
            {
                return false;
            }
            if (t.IsBurning())
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(TSOA_DefOf.TSOA_TakeAnimaSapOutOfBasinJob, t);
        }
    }
}
