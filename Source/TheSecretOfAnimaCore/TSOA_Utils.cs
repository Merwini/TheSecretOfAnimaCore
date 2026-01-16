using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core
{
    public class TSOA_Utils
    {
        public static float GetPsyScalingFactor(float psyScaling, float sensitivity)
        {
            float val = psyScaling * (sensitivity - 1);
            //return psyScaling * (1 - sensitivity);
            return val;
        }

        public static float GetPsyScaledValue(float initialVal, float psyScaling, float sensitivity)
        {
            float val = initialVal * (1 + GetPsyScalingFactor(psyScaling, sensitivity));
            return val;
            //return initialVal * GetPsyScalingFactor(psyScaling, sensitivity);
        }
    }
}
