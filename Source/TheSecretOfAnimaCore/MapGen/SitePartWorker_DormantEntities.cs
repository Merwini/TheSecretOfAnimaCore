using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld;
using Verse;

namespace tsoa.core;

public class SitePartWorker_DormantEntities : SitePartWorker
{
    // TODO, and MAKE SURE IT IS ANOMALY GATED
    public override bool IsAvailable()
    {
        if (base.IsAvailable())
        {
            return ModsConfig.AnomalyActive && Faction.OfEntities != null;
        }
        return false;
    }


}
