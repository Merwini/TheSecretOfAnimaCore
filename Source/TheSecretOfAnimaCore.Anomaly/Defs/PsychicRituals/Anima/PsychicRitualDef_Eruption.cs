using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_Eruption : PsychicRitualDef_Unlocked
{
    public SimpleCurve successCurve;
    public SimpleCurve successCurveAnyOre;
    public string anyOreOptionString;

    private ThingDef chosenOreDef;

    private readonly Dictionary<string, ThingDef> oreDict = new Dictionary<string, ThingDef>();

    public override List<string> FloatMenuOptionStrings
    {
        get
        {
            List<string> options = new List<string>();
            options.Add(anyOreOptionString);

            if (advancedResearchProject.IsFinished)
            {
                foreach (var kvp in oreDict)
                {
                    options.Add(kvp.Key);
                }
            }

            return options;
        }
    }

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
    {
        List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
        list.Add(new PsychicRitualToil_Eruption(InvokerRole, chosenOreDef));
        return list;
    }

    public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
    {
        if (chosenOreDef == null) // i.e. "any ore" was selected
        {
            return outcomeDescription.Formatted(successCurveAnyOre.Evaluate(qualityRange.min).ToStringPercent());
        }
        return outcomeDescription.Formatted(successCurve.Evaluate(qualityRange.min).ToStringPercent());
    }

    public override void InitializeCast(Map map)
    {
        base.InitializeCast(map);

        chosenOreDef = ResolveOreDef(selectedFloatMenuOptionString);
    }

    private ThingDef ResolveOreDef(string chosen)
    {
        return oreDict.TryGetValue(chosen);
    }

    public override void ResolveReferences()
    {
        oreDict.Clear();
        IEnumerable<ThingDef> ores = DefDatabase<ThingDef>.AllDefs.Where(td => td.building?.isResourceRock == true);
        foreach (var ore in ores)
        {
            string optionLabel = ore.label;

            // for if multiple ore ThingDefs have the same label; increment the optionLabel until it is unique
            int suffix = 2;
            while (oreDict.ContainsKey(optionLabel) || optionLabel == anyOreOptionString)
            {
                optionLabel = $"{ore.label} {suffix++}";
            }

            oreDict[optionLabel] = ore;
        }

        base.ResolveReferences();
    }
}
