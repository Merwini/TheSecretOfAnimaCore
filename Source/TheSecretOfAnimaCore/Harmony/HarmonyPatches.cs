using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.AI;

namespace tsoa.core;

[StaticConstructorOnStartup]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        Harmony harmony = new Harmony("tsoa.core");

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
            comfort *= __instance.pawn.GetStatValue(TSOA_StatDefOf.TSOA_ComfortFactor);
        }
    }

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

            if (skillRequirementsNullIndex == -1 || continueIndex == -1)
            {
                Log.Error($"The Secret of Anima crafting psylink requirement transpiler failed. Please report this to the mod developer. Failed to find skillRequirement index {skillRequirementsNullIndex == -1}. Failed to find continue index: {continueIndex == -1}.");
                return codes.AsEnumerable();
            }

            newCodes[0].labels.Add(myCodeLabel); // Label the start of my instructions

            codes.InsertRange(continueIndex + 1, newCodes); // Insert new instructions after skill check

            codes[skillRequirementsNullIndex].operand = myCodeLabel; // If skill check passes, go to psylink level check

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


    [HarmonyPatch(typeof(JobDriver_Meditate))]
    public static class JobDriver_Meditate_MakeNewToils_Patch
    {
        static readonly MethodInfo helperMethod = AccessTools.Method(typeof(HarmonyPatches), "JobDriver_Meditate_Helper");
        static readonly MethodInfo meditationTickMethod = AccessTools.Method(typeof(JobDriver_Meditate), "MeditationTick");
        static readonly MethodInfo royaltyGetterMethod = AccessTools.PropertyGetter(typeof(ModsConfig), "RoyaltyActive");
        static readonly FieldInfo pawnField = AccessTools.Field(typeof(JobDriver_Meditate), "pawn");

        private static MethodInfo target;

        public static bool Prepare()
        {
            var matches = AccessTools.GetDeclaredMethods(typeof(JobDriver_Meditate))
                .Where(m => !m.IsStatic && m.ReturnType == typeof(void)
                    && m.GetParameters().Length == 0
                    && m.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .Where(m => PatchProcessor.GetOriginalInstructions(m)
                    .Any(code => code.Calls(meditationTickMethod)))
                .ToList();

            if (matches.Count != 1)
            {
                Log.Warning("[TSOA] Could not uniquely locate the meditation tick delegate; skipping patch.");
                return false;
            }

            target = matches[0];
            return true;
        }
        static MethodBase TargetMethod() => target;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int royaltyCheckIndex = -1;
            int branchLessThanIndex = -1;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(royaltyGetterMethod))
                {
                    royaltyCheckIndex = i;
                }
                if (royaltyCheckIndex != -1 && (codes[i].opcode == OpCodes.Blt_S || codes[i].opcode == OpCodes.Blt))
                {
                    branchLessThanIndex = i;
                    break;
                }
            }

            if (royaltyCheckIndex < 0 || branchLessThanIndex <= royaltyCheckIndex)
            {
                Log.Error("[TSOA] JobDriver_Meditate transpiler did not find expected instructions; skipping patch. Please report this to the developer along with your mod list!");
                return codes;
            }

            List<CodeInstruction> newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0), // Load this JobDriver_Meditate
                new CodeInstruction(OpCodes.Call, helperMethod) // Call helper method, consumes JobDriver_Meditate
            };

            int countToRemove = branchLessThanIndex - royaltyCheckIndex + 1;

            codes.RemoveRange(royaltyCheckIndex, countToRemove);
            codes.InsertRange(royaltyCheckIndex, newCodes);

            return codes.AsEnumerable();
        }
    }

    public static void JobDriver_Meditate_Helper(JobDriver_Meditate driver)
    {
        Pawn pawn = driver.pawn;
        if (pawn == null)
            return;

        CompSpecialMeditationFocus_Anima comp = MeditationFocusCache.GetOrFind(driver, pawn);
        if (comp == null)
            return;

        MeditationFocusDef req = comp.Props?.requiredFocusType;
        if (req != null && req.CanPawnUse(pawn))
        {
            comp.DoMeditationTick(pawn);
        }
    }

    [HarmonyPatch(typeof(MeditationFocusTypeAvailabilityCache), nameof(MeditationFocusTypeAvailabilityCache.PawnCanUse))]
    public static class MeditationFocusTypeAvailabilityCache_PawnCanUse_Prefix
    {
        public static bool Prefix(Pawn p, MeditationFocusDef type, ref bool __result)
        {
            if (type == MeditationFocusDefOf.Natural && TSOA_MiscDefOf.TSOA_AnimaOne.IsFinished && p.IsColonist)
            {
                __result = true;
                return false;
            }

            if (TSOA_MiscDefOf.Void is MeditationFocusDef voidF && type == voidF && TSOA_MiscDefOf.TSOA_VoidOne is ResearchProjectDef rpd && rpd.IsFinished && p.IsColonist)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetCaravanGizmos))]
    public static class Settlement_GetCaravanGizmos_Postfix
    {
        private const int silverCost = 1500;
        private const int goodwillRequirement = 40;

        public static void Postfix(Settlement __instance, ref IEnumerable<Gizmo> __result, Caravan caravan)
        {
            if (__instance.CanTradeNow && TSOA_MiscDefOf.TSOA_AnimaThree.IsFinished)
            {
                List<Gizmo> gizmosList = __result.ToList();

                Gizmo fontGizmo = TSOA_Utils.GetFontGizmo(caravan, silverCost, goodwillRequirement, __instance.Faction);

                if (fontGizmo != null)
                {
                    gizmosList.Add(fontGizmo);
                }

                __result = gizmosList.AsEnumerable();
            }
        }
    }
}
