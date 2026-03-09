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

    // TODO write my own. Currently copied from SitePartWorker_PreciousLump
    public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
    {
        base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
        if (part.site.ActualThreatPoints > 0f)
        {
            outExtraDescriptionRules.Add(new Rule_String("lumpThreatDescription", "\n\n" + "PreciousLumpHostileThreat".Translate()));
        }
        else
        {
            outExtraDescriptionRules.Add(new Rule_String("lumpThreatDescription", ""));
        }
    }
}
