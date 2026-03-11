using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Grammar;

namespace tsoa.core;

public class SitePartWorker_SilentGrove : SitePartWorker
{
    public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
    {
        if (site.MainSitePartDef == def)
        {
            return null;
        }
        return base.GetPostProcessedThreatLabel(site, sitePart);
    }
}
