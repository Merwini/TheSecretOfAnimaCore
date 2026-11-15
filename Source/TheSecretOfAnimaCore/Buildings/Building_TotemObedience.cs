using Verse;
using RimWorld;
using HarmonyLib;

namespace tsoa.core;

public class Building_TotemObedience : Building_AnimusTotem
{
    public override void DoTotemEffect()
    {
        RegisterTotem();
    }

    public override void RegisterTotem()
    {
        gameComp.activeObedience.Add(this);
    }

    public override void EndTotemEffect()
    {
        gameComp.activeObedience.Remove(this);
    }

    [HarmonyPatch(typeof(TrainableUtility), nameof(TrainableUtility.GetTrainability))]
    public static class TrainableUtility_GetTrainability_Patch
    {
        public static void Postfix(ref TrainabilityDef __result, Pawn pawn)
        {
            if (__result != null && (GameComponent_TotemTracker.Instance?.activeObedience?.Count ?? 0) != 0)
            {
                __result = TrainableUtility.GetNextHighestTrainability(__result);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Roamer), MethodType.Getter)]
    public static class Pawn_Roamer_Getter_Patch
    {
        public static bool Prefix(Pawn __instance, ref bool __result)
        {
            if (GameComponent_TotemTracker.Instance is GameComponent_TotemTracker tracker && tracker.activeObedience.Count != 0)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}