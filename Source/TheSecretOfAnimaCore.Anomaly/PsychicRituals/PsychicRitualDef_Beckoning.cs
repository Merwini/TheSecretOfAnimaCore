using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_Beckoning : PsychicRitualDef_Unlocked
{
    public SimpleCurve successCurve;
    public SimpleCurve successCurveAnyTrader;
    public string anyTraderOptionString;

    private TraderKindDef chosenTraderKind;
    private List<Faction> eligibleFactions = new List<Faction>();
    private readonly Dictionary<string, TraderKindDef> traderDict = new Dictionary<string, TraderKindDef>();

    public override List<string> FloatMenuOptionStrings
    {
        get
        {
            List<string> options = new List<string>();
            if (advancedResearchProject.IsFinished)
            {
                foreach (var kvp in traderDict)
                {
                    options.Add(kvp.Key);
                }
            }

            List<string> filteredOptions = new List<string>();
            filteredOptions.Add(anyTraderOptionString);
            foreach (var option in options)
            {
                foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
                {
                    if (!faction.def.caravanTraderKinds.NullOrEmpty() && faction.def.caravanTraderKinds.Contains(traderDict[option]))
                    {
                        filteredOptions.Add(option);
                        break;
                    }
                }
            }
                
            return filteredOptions;
        }
    }

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
        list.Add(new PsychicRitualToil_Beckoning(InvokerRole, eligibleFactions, chosenTraderKind));
        return list;
    }

    public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
    {
        if (chosenTraderKind == null) // i.e. "any trader" was selected
        {
            return outcomeDescription.Formatted(successCurveAnyTrader.Evaluate(qualityRange.min).ToStringPercent());
        }
        return outcomeDescription.Formatted(successCurve.Evaluate(qualityRange.min).ToStringPercent());
    }

    public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
    {
        foreach (string item in base.BlockingIssues(assignments, map))
        {
            yield return item;
        }
        if (eligibleFactions.NullOrEmpty())
        {
            yield return "TSOA_BeckoningRitualBlocker".Translate();
        }
    }

    public override void InitializeCast(Map map)
    {
        base.InitializeCast(map);

        chosenTraderKind = ResolveTraderKind(selectedFloatMenuOptionString);
        eligibleFactions = GetFactionsThatCanSendTraderKind(map, chosenTraderKind);
    }

    // Will return null if "any trader" is selected
    private TraderKindDef ResolveTraderKind(string chosen)
    {
        return traderDict.TryGetValue(chosen);
    }

    public override void ResolveReferences()
    {
        traderDict.Clear();
        foreach (var kind in DefDatabase<TraderKindDef>.AllDefs)
        {
            if (kind.label.NullOrEmpty())
                continue;

            string optionLabel = kind.label;

            // for if multiple ore ThingDefs have the same label; increment the optionLabel until it is unique
            int suffix = 2;
            while (traderDict.ContainsKey(optionLabel) || optionLabel == anyTraderOptionString)
            {
                optionLabel = $"{kind.label} {suffix++}";
            }

            traderDict[optionLabel] = kind;
        }

        base.ResolveReferences();
    }

    public static List<Faction> GetFactionsThatCanSendTraderKind(Map map, TraderKindDef chosenKind)
    {
        List<Faction> factions = new List<Faction>();

        foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
        {
            // Rule out factions that can't send any kind of trader
            if (faction == null ||
                faction.def.caravanTraderKinds.NullOrEmpty() ||
                faction.IsPlayer ||
                faction.HostileTo(Faction.OfPlayer) ||
                !faction.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp) ||
                NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction))
            {
                continue;
            }

            // If "any trader" is selected, add it. Already ruled out trader-less factions above 
            if (chosenKind == null)
            {
                factions.Add(faction);
                continue;
            }
            else
            {
                if (faction.def.caravanTraderKinds.Contains(chosenKind))
                {
                    factions.Add(faction);
                    continue;
                }
            }
        }

        return factions;
    }
}
