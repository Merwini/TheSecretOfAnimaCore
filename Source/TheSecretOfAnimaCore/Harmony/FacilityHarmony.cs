using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace tsoa.core;

[StaticConstructorOnStartup]
public class FacilityHarmony
{
    // It would be nice if I could patch DefDatabase<T>.ResolveAllReferences, but Harmony doesn't work with generics
    // Need to have dictionaries cached after defs are all loaded but before DefDatabase<ThingDef>.ResolveAllReferences() is called
    [HarmonyPatch(typeof(DirectXmlCrossRefLoader), nameof(DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences))]
    public static class DirectXmlCrossRefLoader_ResolveAllWantedCrossReferences_Postfix
    {
        public static void Postfix()
        {
            CompProperties_Facility_Grouped.CacheDictionaries();
        }
    }

    [HarmonyPatch(typeof(CompFacility), nameof(CompFacility.DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo))]
    public static class CompFacility_DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo_Detour
    {
        public static bool Prefix(float curX, ref float curY, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            CompProperties_Facility_Grouped grouped = myDef.GetCompProperties<CompProperties_Facility_Grouped>();
            if (grouped != null)
            {
                CompFacility_Grouped.DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo_Grouped(curX, ref curY, myDef, myPos, myRot, map);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompFacility), nameof(CompFacility.LinkToNearbyBuildings))]
    public static class CompFacility_LinkToNearbyBuildings_Detour
    {
        public static bool Prefix(CompFacility __instance)
        {
            if (__instance is CompFacility_Grouped grouped)
            {
                grouped.LinkToNearbyBuildings_Grouped();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompFacility), nameof(CompFacility.AmIActiveForAnyone))]
    public static class CopFacility_AmIactiveForAnyOne_Detour
    {
        public static bool Prefix(CompFacility __instance, bool __result)
        {
            if (__instance is CompFacility_Grouped grouped)
            {
                __result = grouped.AmIActiveForAnyone();
                return false;
            }
            return true;
        }
    }
}