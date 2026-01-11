using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class CompProperties_AnimaTreeEssence : CompProperties
    {
        public int maximumEssence = 1000;
        public int refillRate = 1;
        public int autoRefillTicks = 6000; // 10 per day

        public CompProperties_AnimaTreeEssence()
        {
            compClass = typeof(CompAnimaTreeEssence);
        }
    }
}
