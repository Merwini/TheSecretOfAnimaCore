using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core
{
    public class TheSecretOfAnimaSettings : ModSettings
    {
        public static float essenceMultiplier = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref essenceMultiplier, "essenceMultiplier", 1f);

            base.ExposeData();
        }
    }
}
