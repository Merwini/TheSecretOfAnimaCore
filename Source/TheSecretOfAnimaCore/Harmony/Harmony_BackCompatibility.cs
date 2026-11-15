using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace tsoa.core;

[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleType))]
public static class BackCompatibility_GetBackCompatibleType_Patch
{
    private static readonly Dictionary<string, Type> aliases =
        new(StringComparer.Ordinal)
        {
            // Arsenal
            { "tsoa.arsenal.Hediff_PsychicLeech", typeof(Hediff_PsychicLeech) },
            { "tsoa.arsenal.Hediff_Virion", typeof(Hediff_Virion) },
            { "tsoa.arsenal.Hediff_VirionActivity", typeof(Hediff_VirionActivity) },
            { "tsoa.arsenal.JobDriver_StatExchangeLink", typeof(JobDriver_StatExchangeLink) },
            { "tsoa.arsenal.JobDriver_StatExchangeUnlink", typeof(JobDriver_StatExchangeUnlink) },

            // Cairns and Totems
            { "tsoa.totems.Building_AnimusCairn", typeof(Building_AnimusCairn) },
            { "tsoa.totems.Building_TotemBloodthirst", typeof(Building_TotemBloodthirst) },
            { "tsoa.totems.Building_TotemBounty", typeof(Building_TotemBounty) },
            { "tsoa.totems.Building_TotemDiscovery", typeof(Building_TotemDiscovery) },
            { "tsoa.totems.Building_TotemFriendship", typeof(Building_TotemFriendship) },
            { "tsoa.totems.Building_TotemMemory", typeof(Building_TotemMemory) },
            { "tsoa.totems.Building_TotemObedience", typeof(Building_TotemObedience) },
            { "tsoa.totems.Building_TotemReflex", typeof(Building_TotemReflex) },
            { "tsoa.totems.Building_TotemVersatility", typeof(Building_TotemVersatility) },
            { "tsoa.totems.GameComponent_TotemTracker", typeof(GameComponent_TotemTracker) },
            { "tsoa.totems.Hediff_Bloodthirst", typeof(Hediff_Bloodthirst) },
            { "tsoa.totems.Hediff_CairnEffect", typeof(Hediff_CairnEffect) },
            { "tsoa.totems.Hediff_Reflex", typeof(Hediff_Reflex) }
        };

    public static void Postfix(Type baseType, string providedClassName, ref Type __result)
    {
        if (__result != null || providedClassName == null)
            return;

        if (aliases.TryGetValue(providedClassName, out Type replacement)
            && baseType.IsAssignableFrom(replacement))
        {
            __result = replacement;
        }
    }
}
