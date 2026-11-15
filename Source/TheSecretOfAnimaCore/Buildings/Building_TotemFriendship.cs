using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace tsoa.core;

public class Building_TotemFriendship : Building_AnimusTotem
{
    List<Faction> affectableFactions;
    private const int goodwillTicks = 30000; // half a day
    private const int maxGoodwillAboveNatural = 100; // arbitrary, TODO balance. Most factions start well above their natural goodwill. Factions like gentle tribe have -60 natural, rough have -180
    private int ticksToNextGoodwill = goodwillTicks;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        affectableFactions = new List<Faction>();

        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading.Where(f => !f.def.permanentEnemy && !f.def.hidden && f != Faction.OfPlayer))
        {
            affectableFactions.Add(faction);
        }
        base.SpawnSetup(map, respawningAfterLoad);
    }

    public override void DoTotemEffect()
    {
        if (ticksToNextGoodwill > 0)
        {
            ticksToNextGoodwill -= 1;
        }
        else
        {
            for (int i = 0; i < affectableFactions.Count; i++)
            {
                Faction faction = affectableFactions[i];
                Faction player = Faction.OfPlayer;
                if (faction.GoodwillWith(player) < faction.NaturalGoodwill + maxGoodwillAboveNatural)
                {
                    faction.TryAffectGoodwillWith(Faction.OfPlayer, 1, canSendMessage: false);
                }
            }

            ticksToNextGoodwill = goodwillTicks;
        }

        base.DoTotemEffect();
    }

    public override void EndTotemEffect()
    {
        return;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref ticksToNextGoodwill, "ticksToNextGoodwill", goodwillTicks);

        base.ExposeData();
    }
}
