using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace tsoa.core.anomaly;

[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleType))]
public static class SavedClassNames_Patch
{
    private static readonly Dictionary<string, Type> aliases =
        new(StringComparer.Ordinal)
        {
            {
                "tsoa.rituals.MapComponent_AnimaRitual",
                typeof(MapComponent_AnimaRitual)
            },
            {
                "tsoa.rituals.PsychicRitualToil_Relocate",
                typeof(PsychicRitualToil_Relocate)
            }
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