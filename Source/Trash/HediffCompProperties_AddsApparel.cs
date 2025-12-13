using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace nuff.tsoa.core
{
    public class HediffCompProperties_AddsApparel : HediffCompProperties
    {
        public List<ThingDef> apparelDefs;

        public bool lockApparel = true;

        public HediffCompProperties_AddsApparel()
        {
            this.compClass = typeof(HediffComp_AddsApparel);
        }
    }
}