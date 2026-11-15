using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace tsoa.core.anomaly;

public class MapComponent_AnimaRitual : MapComponent
{
    public int flowerPowerEndTick = -1;
    public float flowerPowerMult = 1f;

    public int hungryGraveEndTick = -1;

    public MapComponent_AnimaRitual(Map map) : base(map)
    {
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref flowerPowerEndTick, "flowerPowerEndTick", -1);
        Scribe_Values.Look(ref flowerPowerMult, "flowerPowerMult", 1f);

        Scribe_Values.Look(ref hungryGraveEndTick, "hungryGraveEndTick", -1);

        base.ExposeData();
    }
}
