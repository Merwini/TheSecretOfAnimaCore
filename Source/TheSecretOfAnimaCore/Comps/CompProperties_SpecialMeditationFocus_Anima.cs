using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core
{
    public class CompProperties_SpecialMeditationFocus_Anima : CompProperties
    {
        public float meditationTickProgress = 6.666667E-05f; // vanilla default
        public MeditationFocusDef requiredFocusType;

        public CompProperties_SpecialMeditationFocus_Anima()
        {
            this.compClass = typeof(CompSpecialMeditationFocus_Anima);
        }
    }
}
