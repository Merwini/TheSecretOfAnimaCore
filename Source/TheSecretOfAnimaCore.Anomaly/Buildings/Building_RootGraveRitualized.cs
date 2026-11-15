using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using tsoa.core;

namespace tsoa.core.anomaly;

public class Building_RootGraveRitualized : Building_RootGrave
{
    private const float hungryGraveConsumeMult = 10f;
    private MapComponent_AnimaRitual mapComp;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        mapComp = map.GetComponent<MapComponent_AnimaRitual>();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        mapComp = null;
        base.DeSpawn(mode);
    }

    public override float ConsumeRate
    {
        get
        {
            if (mapComp == null || mapComp.hungryGraveEndTick <= Find.TickManager.TicksGame)
            {
                return 1;
            }

            return hungryGraveConsumeMult;
        }
    }
}
