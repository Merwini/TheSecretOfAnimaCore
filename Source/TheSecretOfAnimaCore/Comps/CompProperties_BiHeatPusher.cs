using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class CompProperties_BiHeatPusher : CompProperties
{
    public FloatRange targetTemperatureRange;

    public float heatPerSecond = 10f;

    public bool worksOutdoors = false;

    public CompProperties_BiHeatPusher()
    {
        compClass = typeof(CompBiHeatPusher);
    }
}
