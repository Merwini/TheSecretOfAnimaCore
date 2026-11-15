using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using HarmonyLib;
using System.Security.Cryptography;
using LudeonTK;

namespace tsoa.core;

public class Building_TotemDiscovery : Building_AnimusTotem
{
    private FloatRange nextDiscoveryTicks = new FloatRange(90000, 210000); // 1.5 - 3.5 days
    private QuestScriptDef ItemStashQuest = DefDatabase<QuestScriptDef>.GetNamed("OpportunitySite_ItemStash");
    internal int ticksUntilNextDiscovery = 0;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        if (!respawningAfterLoad) // I think this will also run after being minified/installed. TODO decide if I want that to reset ticks
        {
            SetNextDiscoveryTicks();
        }

        base.SpawnSetup(map, respawningAfterLoad);
    }

    public override void DoTotemEffect()
    {
        ticksUntilNextDiscovery--;

        if (ticksUntilNextDiscovery <= 0)
        {
            if (FireDiscoveryIncident())
            {
                SetNextDiscoveryTicks();
            }
            else
            {
                Log.Error("Totem of Discovery failed to make Item Stash quest. Trying again in an hour. If this continues, send your save game to the mod developer for debugging.");
                ticksUntilNextDiscovery = 2500;
            }
        }
    }

    private void SetNextDiscoveryTicks()
    {
        ticksUntilNextDiscovery = (int)nextDiscoveryTicks.RandomInRange;
    }

    private bool FireDiscoveryIncident()
    {
        Slate slate = new Slate();
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Map));
        slate.Set("map", Map);
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(ItemStashQuest, slate);

        if (quest != null)
        {
            QuestUtility.SendLetterQuestAvailable(quest);
            return true;
        }
        return false;
    }

    public override string GetInspectString()
    {
        string b = base.GetInspectString();

        if (DebugSettings.godMode)
            b += $"\nTicks until next discovery: {ticksUntilNextDiscovery}";

        return b;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos())
            yield return g;

        if (DebugSettings.godMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "discovery next tick",
                action = () => {ticksUntilNextDiscovery = 0;}
            };
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref ticksUntilNextDiscovery, "ticksUntilNextDiscovery", 0);

        base.ExposeData();
    }
}
