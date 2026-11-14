using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace tsoa.core;

[StaticConstructorOnStartup]
public class HarmonyPatches_Anomaly
{
    static HarmonyPatches_Anomaly()
    {
        Harmony harmonyAnoamly = new Harmony("tsoa.core.anomaly");

        harmonyAnoamly.PatchAll();
    }

    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Interval))]
    public class SkillRecord_Interval_Patch
    {
        public static bool Prefix(SkillRecord __instance)
        {
            if (__instance.pawn.IsMutant && __instance.pawn.mutant.def == TSOA_DefOf.TSOA_ShadeMutant)
            {
                switch (__instance.levelInt)
                {
                case 0:
                    __instance.Learn(-0.05f);
                    break;
                case 1:
                    __instance.Learn(-0.1f);
                    break;
                case 2:
                    __instance.Learn(-0.2f);
                    break;
                case 3:
                    __instance.Learn(-0.4f);
                    break;
                case 4:
                    __instance.Learn(-0.6f);
                    break;
                case 5:
                    __instance.Learn(-1f);
                    break;
                case 6:
                    __instance.Learn(-1.8f);
                    break;
                case 7:
                    __instance.Learn(-2.8f);
                    break;
                case 8:
                    __instance.Learn(-4f);
                    break;
                case 9:
                    __instance.Learn(-6f);
                    break;
                case 10:
                    __instance.Learn(-8f);
                    break;
                case 11:
                    __instance.Learn(-12f);
                    break;
                default:
                    __instance.Learn(-24f);
                    break;

                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.Pawns), MethodType.Getter)]
    public static class MainTabWindow_Work_Pawns_Patch
    {
        public static void Postfix(ref IEnumerable<Pawn> __result)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            IEnumerable<Pawn> shades = map.mapPawns.AllPawns.Where(pawn =>
                !pawn.Dead &&
                pawn.Faction == Faction.OfPlayer &&
                (pawn.HostFaction == null || pawn.IsSlave) &&
                pawn.IsMutant &&
                pawn.mutant.Def == TSOA_DefOf.TSOA_ShadeMutant &&
                !pawn.DevelopmentalStage.Baby() &&
                pawn.workSettings?.EverWork == true);

            __result = __result.Union(shades);
        }
    }
}