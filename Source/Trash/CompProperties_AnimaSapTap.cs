using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core
{
    public class CompProperties_AnimaSapTap : CompProperties
    {
        public int drainTicks = 1250;
        public int essencePerSap = 50;
        public int maximumSap = 50;
        public int workThreshhold = 25;

        public CompProperties_AnimaSapTap()
        {
            compClass = typeof(CompAnimaSapTap);
        }
    }
}
