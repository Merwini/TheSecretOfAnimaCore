using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core;

public class IncidentWorker_AmberRecoveryOffer : IncidentWorker
{
    public override bool CanFireNowSub(IncidentParms parms)
    {
        Map map = parms.target as Map ?? Find.AnyPlayerHomeMap;
        if (map == null)
        {
            return false;
        }

        if (Mathf.RoundToInt(parms.points) <= 0)
        {
            return false;
        }

        if (TSOA_DefOf.TSOA_RecoverAmberQuest == null)
        {
            Log.Error("TSOA_RecoverAmberQuest def is missing.");
            return false;
        }

        return true;
    }

    public override bool TryExecuteWorker(IncidentParms parms)
    {
        Map map = parms.target as Map ?? Find.AnyPlayerHomeMap;
        if (map == null)
        {
            return false;
        }

        int amberAmount = Mathf.RoundToInt(parms.points);
        if (amberAmount <= 0)
        {
            Log.Warning("Amber recovery incident fired with no amber amount stored in parms.points.");
            return false;
        }

        float sitePoints = GetSiteThreatPoints(map, amberAmount);
        parms.points = sitePoints;
        parms.target = map;

        Thing amber = ThingMaker.MakeThing(TSOA_DefOf.TSOA_AnimaAmber);
        amber.stackCount = amberAmount;
        List<Thing> itemPodsContents = new List<Thing> { amber };

        Slate slate = new Slate();
        slate.Set("map", map);
        slate.Set("points", sitePoints);
        slate.Set("itemPodsContents", itemPodsContents);

        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(TSOA_DefOf.TSOA_RecoverAmberQuest, slate);
        if (quest == null)
        {
            Log.Error("Failed to generate amber recovery quest.");
            return false;
        }

        quest.hidden = false;

        QuestUtility.SendLetterQuestAvailable(quest);

        return true;
    }

    private float GetSiteThreatPoints(Map map, int amberAmount)
    {
        float basePoints = StorytellerUtility.DefaultThreatPointsNow(map);

        float amberBonus = amberAmount * 100f;

        return Mathf.Max(5000, basePoints + amberBonus);
    }
}
