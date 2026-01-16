using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core
{
    public class HediffComp_AddsApparel : HediffComp
    {
        public HediffCompProperties_AddsApparel Props => (HediffCompProperties_AddsApparel)this.props;

        private List<Apparel> addedApparel = new List<Apparel>();

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (parent.pawn.apparel != null && !Props.apparelDefs.NullOrEmpty())
            {
                foreach (ThingDef def in Props.apparelDefs)
                {
                    AddApparel(def);
                }
            }
        }

        public override void CompPostPostRemoved()
        {
            RemoveApparels();
        }

        void AddApparel(ThingDef def)
        {
            Apparel apparel = ThingMaker.MakeThing(def) as Apparel;
            addedApparel.Add(apparel);
            parent.pawn.apparel.Wear(apparel, false);
            if (Props.lockApparel)
            {
                parent.pawn.apparel.Lock(apparel);
            }
        }

        void RemoveApparels()
        {
            foreach (Apparel apparel in addedApparel)
            {
                if (apparel != null && parent.pawn.apparel != null)
                {
                    parent.pawn.apparel.Remove(apparel);
                    apparel.Destroy();
                }
            }
        }
    }
}
