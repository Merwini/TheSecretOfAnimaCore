using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core
{
    public class CairnEffectExtension : DefModExtension
    {
        public bool isOffset;
        public bool isFactor;

        public StatDef statDef;
        public float value;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
                yield return error;

            if ((!isOffset && !isFactor) || (isOffset && isFactor))
            {
                yield return "Config error in CairnEffectExtension. Must either be a stat offset or a stat factor";
            }
        }
    }
}