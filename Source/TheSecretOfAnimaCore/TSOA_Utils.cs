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

[StaticConstructorOnStartup]
public class TSOA_Utils
{
    private static readonly Texture2D FontCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/Trade"); // TODO change to custom icon

    public static float GetPsyScalingFactor(float psyScaling, float sensitivity)
    {
        float val = psyScaling * (sensitivity - 1);
        //return psyScaling * (1 - sensitivity);
        return val;
    }

    public static float GetPsyScaledValue(float initialVal, float psyScaling, float sensitivity)
    {
        float val = initialVal * (1 + GetPsyScalingFactor(psyScaling, sensitivity));
        return val;
        //return initialVal * GetPsyScalingFactor(psyScaling, sensitivity);
    }

    public static Command GetFontGizmo(Caravan caravan, int silverCost, int goodwillRequired, Faction faction = null)
    {
        Command_Action command = new Command_Action();
        command.defaultLabel = "TSOA_AskForFontLabel".Translate();
        command.defaultDesc = "TSOA_AskForFontDesc".Translate(goodwillRequired, silverCost);
        command.icon = FontCommandTex;

        bool canAfford = CanAfford();
        bool hasGoodwill = HasGoodwill();
        bool noFont = NoFont();

        command.disabled = !canAfford || !hasGoodwill || !noFont;
        TaggedString reason = null;
        if (!noFont)
        {
            reason = "TSOA_AskForFontAlreadySpawned".Translate();
        }
        else if (!hasGoodwill)
        {
            reason = "TSOA_AskForFontDisabledGoodwill".Translate(goodwillRequired);
        }
        else if (!canAfford)
        {
            reason = "TSOA_AskForFontDisabledCost".Translate(silverCost);
        }
        if (command.disabled)
        {
            command.disabledReason = reason;
        }
        command.action = () =>
        {
            PayCost();
            // TODO add quest / autoaccept to spawn the font
        };

        return command;

        bool CanAfford()
        {
            return CaravanInventoryUtility.HasThings(caravan, ThingDefOf.Silver, silverCost);
        }

        bool HasGoodwill()
        {
            if (faction == null)
                return false;

            return faction.PlayerGoodwill >= 40;

        }

        bool NoFont()
        {
            return !Find.WorldObjects.AllWorldObjects.Any(o => o is FontOfAnimaWorldObject);
        }

        void PayCost()
        {
            int leftToPay = silverCost;
            List<Thing> silver = CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == ThingDefOf.Silver).ToList();
            for (int i = silver.Count - 1; i >= 0; i--)
            {
                Thing thing = silver[i];
                if (thing.stackCount > leftToPay)
                {
                    thing.SplitOff(leftToPay).Destroy();
                    break;
                }
                else
                {
                    leftToPay -= thing.stackCount;
                    thing.Destroy();
                }
            }
        }
    }

    public static void SpawnFontOfAnima()
    {
        // TODO generate quest
        // TODO spawn world object
        // TODO ???
    }
}
