using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace nuff.tsoa.core
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("nuff.tsoa.core");

            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(MeditationUtility))]
        [HarmonyPatch("CountsAsArtificialBuilding")]
        [HarmonyPatch(new Type[] { typeof(ThingDef), typeof(Faction) })]
        public class MeditiationUtility_CountsAsArtificialBuilding_Patch
        {
            public static bool Prefix(ThingDef def, ref bool __result)
            {
                bool isBuilding = def.category == ThingCategory.Building || def.thingCategories.NotNullAndContains(ThingCategoryDefOf.BuildingsSpecial);

                if (!isBuilding)
                {
                    __result = false;
                    return false;
                }

                bool hasPower = def.comps.Any(c => c is CompProperties_Power);

                __result = hasPower;
                return false;
            }
        }

        [HarmonyPatch(typeof(Need_Comfort), nameof(Need_Comfort.ComfortUsed))]
        public static class Need_Comfort_ComfortUsed_Patch
        {
            public static void Prefix(Need_Comfort __instance, ref float comfort)
            {
                comfort *= __instance.pawn.GetStatValue(TSOA_DefOf.TSOA_ComfortGainFactor);
            }
        }


        /*
			SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
			if (skillRequirement != null)
			{
				JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
				continue;
			}
            
            STUFF GOES HERE

			if (bill is Bill_Medical bill_Medical)
        */
        [HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.StartOrResumeBillJob))]
        public static class WorkGiver_DoBill_StartOrResumeBillJob_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var codes = new List<CodeInstruction>(instructions);

                MethodInfo extHelperMethod = typeof(HarmonyPatches).GetMethod("PsylinkExtensionHelper", BindingFlags.Public | BindingFlags.Static);
                MethodInfo linkHelperMethod = typeof(HarmonyPatches).GetMethod("PsyLinkComparisonHelper", BindingFlags.Public | BindingFlags.Static);
                MethodInfo levelHelperMethod = typeof(HarmonyPatches).GetMethod("ExtensionLevelHelper", BindingFlags.Public | BindingFlags.Static);
                MethodInfo stringHelperMethod = typeof(HarmonyPatches).GetMethod("StringHelper", BindingFlags.Public | BindingFlags.Static);

                int skillRequirementsNullIndex = -1;
                int continueIndex = -1;

                Label originalJumpLabel = new Label(); // Need to assign or new code instructions are unhappy
                Label continueTargetLabel = new Label(); // Need to assign or new code instructions are unhappy
                Label myCodeLabel = generator.DefineLabel();

                for (int i = 0; i < codes.Count; i++)
                {
                    // Find where the skillRequirements check jumps to the next check
                        // Unique line before that is a callvirt on Verse.SkillRequirement
                    if (skillRequirementsNullIndex == -1 && codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("Verse.SkillRequirement"))
                    {
                        // Next brfalse.s is the one I want
                        for (int j = i; j < codes.Count; j++)
                        {
                            if (codes[j].opcode == OpCodes.Brfalse_S && codes[j].operand is Label label)
                            {
                                skillRequirementsNullIndex = j;
                                originalJumpLabel = label;
                                break;
                            }
                        }

                        // Find the next br to grab the target for continuing the loop
                        for (int j = i; j < codes.Count; j++)
                        {
                            if (codes[j].opcode == OpCodes.Br && codes[j].operand is Label label)
                            {
                                continueIndex = j;
                                continueTargetLabel = label;
                                break;
                            }
                        }
                    }
                }

                List<CodeInstruction> newCodes = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldloc_2), // Load bill
                    new CodeInstruction(OpCodes.Call, extHelperMethod), // Call helper method, consuming bill and returning bool representing whether the bill's def has my extension
                    new CodeInstruction(OpCodes.Brfalse_S, originalJumpLabel), // Consume bool, if false (no extension), go to the next check on the bill (if (bill is Bill_Medical bill_Medical))

                    new CodeInstruction(OpCodes.Ldarg_1), // Load pawn
                    new CodeInstruction(OpCodes.Ldloc_2), // Load bill
                    new CodeInstruction(OpCodes.Call, linkHelperMethod), // Call helper method, consuming pawn and bill and returning bool representing whether pawn's psylink level is high enough to perform bill
                    new CodeInstruction(OpCodes.Brtrue_S, originalJumpLabel), // Consume bool, if true (meets psylink requirement, go to the next check on the bill (if (bill is Bill_Medical bill_Medical))

                    new CodeInstruction(OpCodes.Ldstr, "TSOA_BillPsylinkTooLow"), // If above bool check was false, proceed with preparing the JobFailReason
                    new CodeInstruction(OpCodes.Ldloc_2), // Load bill
                    new CodeInstruction(OpCodes.Call, levelHelperMethod), // Call helper method, consuming bill and returning extension's minimum psylink level as int
                    new CodeInstruction(OpCodes.Call, stringHelperMethod), // Call helper method consuming string and int and returning string (translated / tagged)
                    new CodeInstruction(OpCodes.Ldloc_2), // Load bill
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Bill), nameof(Bill.Label))), // Consume bill, return label as string
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(JobFailReason), nameof(JobFailReason.Is))), // Consume both strings to set JobFailReason
                    new CodeInstruction(OpCodes.Br, continueTargetLabel) // Continue
                };

                newCodes[0].labels.Add(myCodeLabel); // Label the start of my instructions

                codes.InsertRange(continueIndex, newCodes); // Insert my instructions after the continue at the end of the skillRequirement check

                codes[skillRequirementsNullIndex] = new CodeInstruction(OpCodes.Brfalse_S, myCodeLabel); // Change end of skillRequirement check to go to my instructions instead of the Bill_Medical check

                return codes.AsEnumerable();
            }

            
        }

        // returns false if bill does not have extension
        public static bool PsylinkExtensionHelper(Bill bill)
        {
            if (bill == null || !(bill.recipe is RecipeDef def) || def == null)
            {
                return false;
            }

            return (def.GetModExtension<PsyLinkRecipeExtension>() != null);
        }

        // returns false if pawn fails to meet psylink requirement
        public static bool PsyLinkComparisonHelper(Pawn pawn, Bill bill)
        {
            if (pawn == null)
            {
                return false;
            }

            // Already confirmed that bill.recipe has my extension in order to reach this point, no need to safety check
            PsyLinkRecipeExtension ext = bill.recipe.GetModExtension<PsyLinkRecipeExtension>();

            return (pawn.GetPsylinkLevel() >= ext.minPsylinkLevel);
        }

        public static int ExtensionLevelHelper(Bill bill)
        {
            // Already confirmed extension exists in order to reach this point
            PsyLinkRecipeExtension ext = bill.recipe.GetModExtension<PsyLinkRecipeExtension>();
            return ext.minPsylinkLevel;
        }

        public static string StringHelper(string str, int num)
        {
            return str.Translate(num);
        }
    }
}

