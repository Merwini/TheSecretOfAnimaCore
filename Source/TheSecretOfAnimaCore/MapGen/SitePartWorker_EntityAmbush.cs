using RimWorld;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class SitePartWorker_EntityAmbush : SitePartWorker
{
    public override bool IsAvailable()
    {
        if (base.IsAvailable())
        {
            return ModsConfig.AnomalyActive && Faction.OfEntities != null;
        }
        return false;
    }

    public override string GetArrivedLetterPart(Map map, out LetterDef preferredLetterDef, out LookTargets lookTargets)
    {
        string arrivedLetterPart = base.GetArrivedLetterPart(map, out preferredLetterDef, out lookTargets);
        lookTargets = new LookTargets(map.Parent);
        return arrivedLetterPart;
    }

    public override SitePartParams GenerateDefaultParams(float myThreatPoints, PlanetTile tile, Faction faction)
    {
        SitePartParams sitePartParams = base.GenerateDefaultParams(myThreatPoints, tile, faction);
        sitePartParams.threatPoints = Mathf.Max(sitePartParams.threatPoints, FactionDefOf.Entities.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
        return sitePartParams;
    }
}
