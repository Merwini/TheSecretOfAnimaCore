using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class CompProperties_PsyShield : CompProperties
    {
        public float heatPerDamage = 0.5f;

        public float resetHeatPercent = 0.8f;

        public int resetDelayTicks = 120;

        public float minDrawSize = 1.2f;

        public float maxDrawSize = 1.55f;

        public CompProperties_PsyShield()
        {
            this.compClass = typeof(CompPsyShield);
        }
    }
}
