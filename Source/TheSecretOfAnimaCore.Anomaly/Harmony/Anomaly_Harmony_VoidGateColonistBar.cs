using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

internal static class Anomaly_Harmony_VoidGateColonistBar
{
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.CheckRecacheEntries))]
    private static class ColonistBar_CheckRecacheEntries_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            MethodInfo calculateDrawLocs = AccessTools.Method(typeof(ColonistBarDrawLocsFinder),
                nameof(ColonistBarDrawLocsFinder.CalculateDrawLocs), new[] { typeof(List<Vector2>), typeof(float).MakeByRefType(), typeof(int) });
            int layoutIndex = codes.FindIndex(code => code.Calls(calculateDrawLocs)); // need to make all our changes before this is called
            if (layoutIndex < 0)
            {
                Log.Error("[TSOA] ColonistBar_CheckRecacheEntries_Patch Could not find the colonist bar layout call.");
                return codes;
            }

            CodeInstruction loadBar = new(OpCodes.Ldarg_0);
            loadBar.labels.AddRange(codes[layoutIndex].labels);
            codes[layoutIndex].labels.Clear();
            codes.InsertRange(layoutIndex, new[]
            {
                // int representing group count is already on the stack, "IL_0465: ldloc.0"
                loadBar, // load ColonistBar; this instruction is labeled
                CodeInstruction.LoadField(typeof(ColonistBar), nameof(ColonistBar.cachedEntries)), // load list of cached colonist entries, consume ColonistBar on stack
                new CodeInstruction(OpCodes.Ldarg_0), // load ColonistBar; no label on this instruction
                CodeInstruction.LoadField(typeof(ColonistBar), nameof(ColonistBar.cachedReorderableGroups)), // load list of ints (each one is a colonist group), consumes ColonistBar on stack
                CodeInstruction.Call(typeof(Anomaly_Harmony_VoidGateColonistBar), nameof(ColonistBar_CheckRecacheEntries_Helper)) // call helper method, consumes group count, cachedEntries, and cachedReorderableGroups
                // ColonistBar_CheckRecacheEntries_Helper returns modified group count int to replace the one we consumed
            });
            return codes;
        }
    }

    private static int ColonistBar_CheckRecacheEntries_Helper(int groupsCount, List<ColonistBar.Entry> entries, List<int> reorderableGroups)
    {
        foreach (WorldObject_VoidTunnelCaravan voidCaravan in Find.WorldObjects.AllWorldObjects.OfType<WorldObject_VoidTunnelCaravan>().OrderBy(vc => vc.ID))
        {
            List<Pawn> colonists = voidCaravan.LivingPassengers.Where(pawn => pawn.IsColonist || pawn.IsColonySubhumanPlayerControlled).ToList();
            if (colonists.Count == 0)
                continue;

            PlayerPawnsDisplayOrderUtility.Sort(colonists);
            foreach (Pawn pawn in colonists)
            {
                entries.Add(new ColonistBar.Entry(pawn, null, groupsCount));
                reorderableGroups.Add(-1);
            }
            groupsCount++;
        }
        return groupsCount;
    }

    [HarmonyPatch]
    private static class SelectUnderMouse_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Selector), nameof(Selector.SelectUnderMouse), Type.EmptyTypes);
            yield return AccessTools.Method(typeof(WorldSelector), nameof(WorldSelector.SelectUnderMouse), new[] { typeof(bool) });
        }

        private static bool Prefix()
        {
            if (Current.ProgramState != ProgramState.Playing
                || Find.ColonistBar.ColonistOrCorpseAt(UI.MousePositionOnUIInverted) is not Pawn pawn
                || pawn.ParentHolder is not ActiveTransporterInfo { parent: WorldObject_VoidTunnelCaravan voidCaravan }
                || !voidCaravan.Spawned)
                return true;

            CameraJumper.TryJumpAndSelect(voidCaravan);
            Find.WorldCameraDriver.JumpTo(voidCaravan.DrawPos);
            return false;
        }
    }
}
