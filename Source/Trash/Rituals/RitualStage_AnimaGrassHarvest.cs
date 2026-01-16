using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace tsoa.core
{
    public class RitualStage_AnimaGrassHarvest : RitualStage
    {
        public override TargetInfo GetSecondFocus(LordJob_Ritual ritual)
        {
            return ritual.selectedTarget;
        }
    }
}