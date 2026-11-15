using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tsoa.core;
using Verse;
using RimWorld;

namespace tsoa.core.anomaly;

public class CompBiHeatPusherRitualized : CompBiHeatPusher
{
    private MapComponent_AnimaRitual mapComp;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        mapComp = parent.Map.GetComponent<MapComponent_AnimaRitual>();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        mapComp = null;
        base.PostDeSpawn(map, mode);
    }

    protected override float HeatPerSecond => base.HeatPerSecond * (mapComp?.flowerPowerMult ?? 1f);

    public override bool ShouldPushHeatNow(out float temperature)
    {
        if (mapComp == null || mapComp.flowerPowerEndTick <= Find.TickManager.TicksGame)
        {
            temperature = 0;
            return false;
        }

        return base.ShouldPushHeatNow(out temperature);
    }

    public override string CompInspectStringExtra()
    {
        int ticksLeft = (mapComp?.flowerPowerEndTick ?? -1) - Find.TickManager.TicksGame;
        if (ticksLeft <= 0)
        {
            return "TSOA_FlowerPowerInactive".Translate();
        }

        return base.CompInspectStringExtra() + "\n" + "TSOA_FlowerPowerDuration".Translate(ticksLeft.ToStringTicksToDays());
    }
}
