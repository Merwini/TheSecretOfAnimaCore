using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace nuff.tsoa.core
{
    public class CompProperties_AnimaTreePawnLink : CompProperties
    {
        public CompProperties_AnimaTreePawnLink()
        {
            compClass = typeof(CompAnimaTreePawnLink);
        }

        public float psychicSensitivityPerSubplant = 0.05f;
    }
}
