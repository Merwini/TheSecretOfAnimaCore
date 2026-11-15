using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace tsoa.core;

public class Building_TotemVersatility : Building_AnimusTotem
{
    public override void DoTotemEffect()
    {
        RegisterTotem();
    }

    public override void RegisterTotem()
    {
        HashSet<Building_TotemVersatility> buildings = gameComp.activeVersatility.TryGetValue(this.Map);
        if (buildings == null)
        {
            buildings = new HashSet<Building_TotemVersatility>();
            gameComp.activeVersatility[this.Map] = buildings;
        }
        buildings.Add(this);
    }

    public override void EndTotemEffect()
    {
        Map map = this.MapHeld;
        if (map == null)
        {
            gameComp.CleanupVersatility();
            return;
        }

        HashSet<Building_TotemVersatility> buildings = gameComp.activeVersatility.TryGetValue(map);
        if (buildings != null)
        {
            buildings.Remove(this);
            if (buildings.Count == 0)
            {
                gameComp.activeVersatility.Remove(map);
            }
        }
    }

    public static float AdjustFactor(float factor, SkillRecord skill)
    {
        Pawn pawn = skill?.Pawn;
        if (factor >= 1f || pawn == null || !pawn.IsColonist || !pawn.Spawned)
            return factor;

        if (GameComponent_TotemTracker.Instance?.activeVersatility.ContainsKey(pawn.Map) ?? false)
            return 1f;

        return factor;
    }

    /*
        IL_0015: switch (IL_0028, IL_0030, IL_0038)

        IL_0026: br.s IL_0040

        IL_0028: ldc.r4 0.35
        IL_002d: stloc.0
        IL_002e: br.s IL_0061

        IL_0030: ldc.r4 1
        IL_0035: stloc.0
        IL_0036: br.s IL_0061

        IL_0038: ldc.r4 1.5
        IL_003d: stloc.0
        IL_003e: br.s IL_0061

        // if (!direct)
        IL_0040: ldstr "Passion level "
        IL_0045: ldarg.0
        IL_0046: ldflda valuetype RimWorld.Passion RimWorld.SkillRecord::passion
        IL_004b: constrained. RimWorld.Passion
        IL_0051: callvirt instance string [mscorlib]System.Object::ToString()
        IL_0056: call string [mscorlib]System.String::Concat(string, string)
        IL_005b: newobj instance void [mscorlib]System.NotImplementedException::.ctor(string)
        IL_0060: throw

        --> insert new codes here, to evaluate value at loc.0 and floor it to 1.0
        --> need to have switch cases point here instead of IL_0061

        IL_0061: ldarg.1
        IL_0062: brtrue.s IL_00ab
    */
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.LearnRateFactor))]
    public static class SkillRecord_LearnRateFactor_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);

            MethodInfo helperMethod = AccessTools.Method(typeof(Building_TotemVersatility), nameof(AdjustFactor));

            List<int> switchJumpIndices = new List<int>();
            bool skipFirst = true;

            int ldargIndex = -1;

            Label myCodeLabel = generator.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (switchJumpIndices.Count < 3 && codes[i].opcode == OpCodes.Br_S)
                {
                    // skips first br.s, which is the one that handles throwing an exception for unrecognized passion cases
                    if (skipFirst)
                    {
                        skipFirst = false;
                    }
                    else
                    {
                        switchJumpIndices.Add(i);
                    }
                }

                if (codes[i].opcode == OpCodes.Ldarg_1) // where all the switch cases jump to
                {
                    ldargIndex = i;
                }
            }

            if (switchJumpIndices.Count != 3 || ldargIndex == -1)
            {
                Log.Error("Failed to find all switch jump indices or ldarg_1 index in SkillRecord.LearnRateFactor transpiler.");
                return codes.AsEnumerable();
            }

            List<CodeInstruction> newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_0), // load the stored factor onto the stack
                new CodeInstruction(OpCodes.Ldarg_0), // load the SkillRecord instance onto the stack
                new CodeInstruction(OpCodes.Call, helperMethod), // call helper method, consumes the float and the SkillRecord instance, returns a new float
                new CodeInstruction(OpCodes.Stloc_0), // store the result back into loc.0
            };

            newCodes[0].labels.Add(myCodeLabel); // label the start of my codes

            codes.InsertRange(ldargIndex, newCodes); // insert them before the ldarg_1 instruction

            for (int i = 0; i < switchJumpIndices.Count; i++)
            {
                codes[switchJumpIndices[i]].operand = myCodeLabel; // redirect the switch cases to jump to my code instead of the original code
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    public static class LearnRateFactorCache_LearnRateFactorBase_Conditional_Postfix
    {
        private static MethodBase VSE_Exists
        {
            get
            {
                Type learnRateFactorCache = AccessTools.TypeByName("VSE.Passions.LearnRateFactorCache");
                return learnRateFactorCache == null ? null : AccessTools.Method(learnRateFactorCache, "LearnRateFactorBase");
            }
        }

        public static bool Prepare() => VSE_Exists != null;

        public static MethodBase TargetMethod() => VSE_Exists;

        public static void Postfix(SkillRecord __0, ref float __result)
        {
            __result = AdjustFactor(__result, __0);
        }
    }
}
