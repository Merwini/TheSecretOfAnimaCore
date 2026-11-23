using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace tsoa.core.anomaly;

[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleType))]
public static class BackCompatibility_GetBackCompatibleType_Patch
{
    private static readonly Dictionary<string, Type> aliases =
        new(StringComparer.Ordinal)
        {
            // Rituals
            { "tsoa.rituals.Building_RootGraveRitualized", typeof(Building_RootGraveRitualized) },
            { "tsoa.rituals.TunnelHiveSpawner_Eruption", typeof(TunnelHiveSpawner_Eruption) },
            { "tsoa.rituals.Hediff_RegrowingPart", typeof(Hediff_RegrowingPart) },
            { "tsoa.rituals.LordJob_DefendMinerals", typeof(LordJob_DefendMinerals) },
            { "tsoa.rituals.MapComponent_AnimaRitual", typeof(MapComponent_AnimaRitual) },

            { "tsoa.rituals.PsychicRitualToil_Animagenesis", typeof(PsychicRitualToil_Animagenesis) },
            { "tsoa.rituals.PsychicRitualToil_Beckoning", typeof(PsychicRitualToil_Beckoning) },
            { "tsoa.rituals.PsychicRitualToil_Eruption", typeof(PsychicRitualToil_Eruption) },
            { "tsoa.rituals.PsychicRitualToil_Fever", typeof(PsychicRitualToil_Fever) },
            { "tsoa.rituals.PsychicRitualToil_FlowerPower", typeof(PsychicRitualToil_FlowerPower) },
            { "tsoa.rituals.PsychicRitualToil_HungryGrave", typeof(PsychicRitualToil_HungryGrave) },
            { "tsoa.rituals.PsychicRitualToil_Regrowth", typeof(PsychicRitualToil_Regrowth) },
            { "tsoa.rituals.PsychicRitualToil_Relocate", typeof(PsychicRitualToil_Relocate) },
            { "tsoa.rituals.PsychicRitualToil_Severing", typeof(PsychicRitualToil_Severing) },
            { "tsoa.rituals.PsychicRitualToil_Storm", typeof(PsychicRitualToil_Storm) },

            { "tsoa.core.anomaly.WorldObject_VoidGateCaravan", typeof(WorldObject_VoidTunnelCaravan) },
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
