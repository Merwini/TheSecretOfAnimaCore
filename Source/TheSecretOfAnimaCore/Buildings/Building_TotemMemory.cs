using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace tsoa.core;

public class Building_TotemMemory : Building_AnimusTotem
{
    public override void DoTotemEffect()
    {
        RegisterTotem();
    }

    public override void RegisterTotem()
    {
        gameComp.activeMemory.Add(this);
    }

    public override void EndTotemEffect()
    {
        gameComp.activeMemory.Remove(this);
    }

    [HarmonyPatch(typeof(Thought_Memory), nameof(Thought.DurationTicks), MethodType.Getter)]
    public static class Thought_Memory_DurationTicks_Postfix
    {
        public static void Postfix(Thought __instance, ref int __result)
        {
            // Is this the most efficient order for the early returns?
            if (__instance.pawn?.IsPlayerControlled != true)
                return;

            if ((GameComponent_TotemTracker.Instance?.activeMemory?.Count ?? 0) == 0)
                return;

            ThoughtStage stage = __instance.CurStage;
            if (stage == null)
                return;

            float mood = stage.baseMoodEffect;
            if (mood < 0)
            {
                __result = (int)(__result / 2); // TODO balance
            }
            else
            {
                __result = (int)(__result * 1.5f);
            }
        }
    }
}