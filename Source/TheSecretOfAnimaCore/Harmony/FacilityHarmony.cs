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
    // So dictionaries will recache the first time a CompProperties_Facility_Grouped has ResolveRefernces called
    [HarmonyPatch(typeof(PlayDataLoader), nameof(PlayDataLoader.DoPlayLoad))]
    public static class PlayDataLoader_DoPlayLoad_Prefix
    {
        public static void Prefix()
        {
            CompProperties_Facility_Grouped.dictionariesCached = false;
        }
    }

    [HarmonyPatch(typeof(PlayDataLoader), nameof(PlayDataLoader.HotReloadDefs))]
    public static class PlayDataLoader_HotReloadDefs_Prefix
    {
        public static void Prefix()
        {
            CompProperties_Facility_Grouped.dictionariesCached = false;
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
        public static bool Prefix(CompFacility __instance, ref bool __result)
        {
            if (__instance is CompFacility_Grouped grouped)
            {
                __result = grouped.AmIActiveForAnyone_Grouped();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompFacility), nameof(CompFacility.UnlinkAll))]
    public static class CopFacility_UnlinkAll_Detour
    {
        public static bool Prefix(CompFacility __instance)
        {
            if (__instance is CompFacility_Grouped grouped)
            {
                grouped.UnlinkAll_Grouped();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompQuality), "SetQuality")]
    public static class CompQuality_SetQuality_Postfix
    {
        public static void Postfix(CompQuality __instance)
        {
            __instance.parent.TryGetComp<CompFacility_Grouped>()?.PostQualitySet();
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.CanLinkTo))]
    public static class CompAffectedByFacilities_CanLinkTo_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance, Thing facility, ref bool __result)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                __result = grouped.CanLinkTo_Grouped(facility);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.CanPotentiallyLinkTo))]
    public static class CompAffectedByFacilities_CanPotentiallyLinkTo_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance, ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ref bool __result)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                __result = grouped.CanPotentiallyLinkTo_Grouped(facilityDef, facilityPos, facilityRot);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.CanPotentiallyLinkTo_Static), new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Rot4), typeof(ThingDef), typeof(IntVec3), typeof(Rot4), typeof(Map) })]
    public static class CompAffectedByFacilities_CanPotentiallyLinkTo_Static_Detour
    {
        public static bool Prefix(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map myMap, ref bool __result)
        {
            CompProperties_AffectedByFacilities_Grouped comp = myDef.GetCompProperties<CompProperties_AffectedByFacilities_Grouped>();
            if (comp != null)
            {
                __result = CompAffectedByFacilities_Grouped.CanPotentiallyLinkTo_Static_Grouped(facilityDef, facilityPos, facilityRot, myDef, myPos, myRot, myMap);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.IsPotentiallyValidFacilityForMe))]
    public static class CompAffectedByFacilities_IsPotentiallyValidFacilityForMe_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance, ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ref bool __result)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                __result = grouped.IsPotentiallyValidFacilityForMe_Grouped(facilityDef, facilityPos, facilityRot);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.IsPotentiallyValidFacilityForMe_Static),  new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Rot4), typeof(ThingDef), typeof(IntVec3), typeof(Rot4), typeof(Map) })]
    public static class CompAffectedByFacilities_IsPotentiallyValidFacilityForMe_Static_Detour
    {
        public static bool Prefix(ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map, ref bool __result)
        {
            CompProperties_AffectedByFacilities_Grouped comp = myDef.GetCompProperties<CompProperties_AffectedByFacilities_Grouped>();
            if (comp != null)
            {
                __result = CompAffectedByFacilities_Grouped.IsPotentiallyValidFacilityForMe_Static_Grouped(facilityDef, facilityPos, facilityRot, myDef, myPos, myRot, map);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.Notify_NewLink))]
    public static class CompAffectedByFacilities_Notify_NewLink_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance, Thing facility)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                grouped.Notify_NewLink_Grouped(facility);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.IsBetter))]
    public static class CompAffectedByFacilities_IsBetter_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance, ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, Thing thanThisFacility, ref bool __result)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                __result = grouped.IsBetter_Grouped(facilityDef, facilityPos, facilityRot, thanThisFacility);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.PotentialThingsToLinkTo))]
    public static class CompAffectedByFacilities_PotentialThingsToLinkTo_Detour
    {
        public static bool Prefix(ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map myMap, ref IEnumerable<Thing> __result)
        {
            CompProperties_AffectedByFacilities_Grouped comp = myDef.GetCompProperties<CompProperties_AffectedByFacilities_Grouped>();
            if (comp != null)
            {
                __result = CompAffectedByFacilities_Grouped.PotentialThingsToLinkTo_Grouped(myDef, myPos, myRot, myMap);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.GetPotentiallySupplantedFacility))]
    public static class CompAffectedByFacilities_GetPotentiallySupplantedFacility_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance, ThingDef facilityDef, IntVec3 facilityPos, Rot4 facilityRot, ref Thing __result)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                __result = grouped.GetPotentiallySupplantedFacility_Grouped(facilityDef, facilityPos, facilityRot);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.IsFacilityActive))]
    public static class CompAffectedByFacilities_IsFacilityActive_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance, Thing facility, ref bool __result)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                __result = grouped.IsFacilityActive_Grouped(facility);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.LinkToNearbyFacilities))]
    public static class CompAffectedByFacilities_LinkToNearbyFacilities_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                grouped.LinkToNearbyFacilities_Grouped();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompAffectedByFacilities), nameof(CompAffectedByFacilities.UnlinkAll))]
    public static class CompAffectedByFacilities_UnlinkAll_Detour
    {
        public static bool Prefix(CompAffectedByFacilities __instance)
        {
            if (__instance is CompAffectedByFacilities_Grouped grouped)
            {
                grouped.UnlinkAll_Grouped();
                return false;
            }
            return true;
        }
    }
}
