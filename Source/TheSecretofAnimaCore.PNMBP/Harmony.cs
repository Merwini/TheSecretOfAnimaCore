using System.Collections.Generic;
using HarmonyLib;
using Verse;
using ProstheticNoMissingBodyParts;

namespace tsoa.core.pnmbp;

[StaticConstructorOnStartup]
public class HarmonyPatches
{


    static HarmonyPatches()
    {
        Harmony harmony = new Harmony("tsoa.core.pnmbp");

        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ProstheticNoMissingBodyPartsMod), "init")]
    public class ProstheticNoMissingBodyPartsMod_init_Patch
    {
        public static void Prefix(bool ___isInitialized, out bool __state)
        {
            __state = !___isInitialized;
        }

        public static void Postfix(
            ref Dictionary<string, bool[]> ___armsWhitelistMap,
            ref Dictionary<string, bool[]> ___legsWhitelistMap,
            ref List<HediffDef> ___armsHediff,
            ref List<HediffDef> ___legsHediff,
            ref ProstheticNoMissingBodyPartsSettings ___settings,
            bool __state)
        {
            if (!__state)
            {
                return;
            }
            
            int armIndex = -1;
            for (int i = 0; i < ___armsHediff.Count; i++)
            {
                HediffDef armDef = ___armsHediff[i];
                if (armDef != null && armDef.defName == "TSOA_BioferriteArmThree")
                {
                    armIndex = i;
                    break;
                }
            }
            if (armIndex != -1)
            {
                HediffCompProperties_Evolving armHediffComp = ___armsHediff[armIndex].comps.Find(c => c is HediffCompProperties_Evolving) as HediffCompProperties_Evolving;
                List<HediffDef> allTiers = armHediffComp?.evolvingImplantDef?.tiers;
                ___armsHediff.RemoveAt(armIndex);
                for (int i = allTiers.Count - 1; i >= 0; i--)
                {
                    HediffDef tierDef = allTiers[i];
                    ___armsHediff.Insert(armIndex, tierDef);
                    ___armsWhitelistMap[tierDef.defName] = new bool[1] { ___settings.ArmsWhitelist.Any(x => x.EqualsIgnoreCase(tierDef.defName)) };
                }
            }
            

            int legIndex = -1;
            for (int i = 0; i < ___legsHediff.Count; i++)
            {
                HediffDef legDef = ___legsHediff[i];
                if (legDef != null && legDef.defName == "TSOA_BioferriteLegThree")
                {
                    legIndex = i;
                    break;
                }
            }
            if (legIndex != -1)
            {
                HediffCompProperties_Evolving legHediffComp = ___legsHediff[legIndex].comps.Find(c => c is HediffCompProperties_Evolving) as HediffCompProperties_Evolving;
                List<HediffDef> allLegTiers = legHediffComp?.evolvingImplantDef?.tiers;
                ___legsHediff.RemoveAt(legIndex);
                for (int i = allLegTiers.Count - 1; i >= 0; i--)
                {
                    HediffDef tierDef = allLegTiers[i];
                    ___legsHediff.Insert(legIndex, tierDef);
                    ___legsWhitelistMap[tierDef.defName] = new bool[1] { ___settings.LegsWhitelist.Any(x => x.EqualsIgnoreCase(tierDef.defName)) };
                }
            }
        }

        // public static void AddAllHediffTiers(HediffDef hediffDef, List<HediffDef> hediffList, Dictionary<string, bool[]> hediffDict, ProstheticNoMissingBodyPartsSettings settings,int index)
        // {
        //     List<HediffDef> allTiers = new List<HediffDef>();
        //     HediffCompProperties_Evolving hediffComp = hediffDef.comps.Find(c => c is HediffCompProperties_Evolving) as HediffCompProperties_Evolving;
        //     if (hediffComp == null || hediffComp.evolvingImplantDef == null)
        //     {
        //         return;
        //     }

        //     EvolvingImplantDef evolvingImplantDef = hediffComp.evolvingImplantDef;

        //     foreach (HediffDef tierDef in evolvingImplantDef.tiers)
        //     {
        //         if (tierDef != null && !hediffList.Contains(tierDef))
        //         {
        //             allTiers.Add(tierDef);
        //         }
        //     }

        //     if (allTiers.Count == 0)
        //     {
        //         return;
        //     }

        //     hediffList.RemoveAt(index);

        //     for (int i = allTiers.Count - 1; i >= 0; i--)
        //     {
        //         HediffDef tierDef = allTiers[i];
        //         hediffList.Insert(index, tierDef);
        //         hediffDict[tierDef.defName] = new bool[1] { settings.};
        //     }
        // }
    }
}
