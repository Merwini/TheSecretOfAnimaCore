using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tsoa.core
{
    public class CompAnimaTreePawnLink : ThingComp
    {
        public CompProperties_AnimaTreePawnLink Props => (CompProperties_AnimaTreePawnLink)this.props;

        public List<Pawn> linkedPawns = new List<Pawn>();

        public override void CompTickLong()
        {
            for (int i = linkedPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = linkedPawns[i];
                if (pawn == null || pawn.Destroyed || !pawn.Spawned)
                {
                    linkedPawns.RemoveAt(i);
                    continue;
                }
                Hediff_AnimaTreeLink hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TSOA_DefOf.TSOA_AnimaLinkHediff) as Hediff_AnimaTreeLink;
                if (hediff == null || hediff.AnimaTree != this.parent)
                {
                    linkedPawns.RemoveAt(i);
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref linkedPawns, "linkedPawns", LookMode.Reference);
        }
    }
}
