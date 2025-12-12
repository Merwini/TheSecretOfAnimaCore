using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class EntropyExtraDamageExtension : DefModExtension
    {
        public float heatConsumedPercent = 0.1f;

        public float damagePerHeatConsumed = 4f;

        public DamageDef damageDef;
    }
}
