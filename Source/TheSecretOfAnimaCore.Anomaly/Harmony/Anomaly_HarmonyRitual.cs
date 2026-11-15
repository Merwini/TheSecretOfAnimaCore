using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using static RimWorld.PsychicRitualDef_InvocationCircle;
using HarmonyLib;

namespace tsoa.core.anomaly;

public class Anomaly_HarmonyRituals
{
    [HarmonyPatch(typeof(PsychicRitualGizmo))]
    public class PsychicRitualGizmo_GetGizmos_Patch
    {
        private static readonly MethodInfo moveNext =
        AccessTools.EnumeratorMoveNext(
            AccessTools.Method(
                typeof(PsychicRitualGizmo),
                nameof(PsychicRitualGizmo.GetGizmos)));

        static bool Prepare()
        {
            if (moveNext != null)
                return true;

            Log.Error("[TSOA] Could not find GetGizmos iterator; skipping ritual filter patch.");
            return false;
        }

        static MethodBase TargetMethod() => moveNext;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var codes = instructions.ToList();

            FieldInfo targetField = AccessTools.Field(__originalMethod.DeclaringType, "target");

            MethodInfo filterMethod = AccessTools.Method(typeof(Anomaly_HarmonyRituals), nameof(RitualTargetFilter));

            MethodInfo orderBy = typeof(Enumerable).GetMethods()
                .Single(m => m.Name == nameof(Enumerable.OrderBy)
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 2)
                .MakeGenericMethod(
                    typeof(PsychicRitualDef_InvocationCircle),
                    typeof(string));

            List<int> matches = Enumerable.Range(0, codes.Count)
                .Where(i => codes[i].Calls(orderBy))
                .ToList();

            if (targetField == null || targetField.IsStatic
                || targetField.FieldType != typeof(Thing)
                || filterMethod == null || matches.Count != 1)
            {
                Log.Error("[TSOA] GetGizmos IL did not match expectations; leaving it unchanged.");
                return codes;
            }

            codes.InsertRange(matches[0] + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0), // Load "this"
                new CodeInstruction(OpCodes.Ldfld, targetField), // Load the "target" field from "this"
                new CodeInstruction(OpCodes.Call, filterMethod) // Call the RitualTargetFilter method
            });

            return codes;
        }
    }
    public static IOrderedEnumerable<PsychicRitualDef_InvocationCircle> RitualTargetFilter(IOrderedEnumerable<PsychicRitualDef_InvocationCircle> unfiltered, Thing target)
    {
        bool isPsychicRitualSpot = target != null && target.def == TSOAR_DefOf.PsychicRitualSpot;

        return unfiltered
            .Where(ritualDef =>
            {
                PsychicRitualDef_Unlocked locationUnlocked = ritualDef as PsychicRitualDef_Unlocked;

                // Case: it's a psychic ritual spot and the ritual is NOT location unlocked, or is and the target is psychic ritual spot
                if (isPsychicRitualSpot)
                {
                    return locationUnlocked == null || locationUnlocked.ritualFocuses.Contains(TSOAR_DefOf.PsychicRitualSpot);
                }

                // Case: it's not a psychic ritual spot and the ritual is location unlocked and the ritual focus matches the target
                return locationUnlocked != null && locationUnlocked.ritualFocuses.Contains(target.def);
            })
            .OrderBy(rd => rd.label);
    }

    [HarmonyPatch(typeof(PsychicRitualGizmo), nameof(PsychicRitualGizmo.InitializePsychicRitual))]
    public static class PsychicRitualGizmo_InitializePsychicRitual_Prefix
    {
        public static bool Prefix(PsychicRitualDef_InvocationCircle psychicRitualDef, Thing target)
        {
            if (psychicRitualDef is not PsychicRitualDef_Unlocked unlockedRitual)
                return true;

            unlockedRitual.ritualFocus = target;

            Map map = Find.CurrentMap;

            if (unlockedRitual.targetsCell)
            {
                Find.Targeter.BeginTargeting(
                new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetBuildings = false,
                    canTargetPawns = false,
                    validator = t =>
                    {
                        IntVec3 cell = t.Cell;
                        return cell.InBounds(map) && unlockedRitual.ExtraCellValidator(cell, t.Map);
                    }
                },
                localTarget =>
                {
                    unlockedRitual.targetCell = localTarget.Cell;

                    OriginalActions();
                });

                return false;
            }
            else if (unlockedRitual.targetsPawn)
            {
                Find.Targeter.BeginTargeting(
                new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetBuildings = false,
                    validator = t =>
                        t.Thing is Pawn &&
                        unlockedRitual.ExtraThingValidator(t.Thing)
                },
                localTarget =>
                {
                    Pawn pawn = localTarget.Pawn;
                    if (pawn == null)
                        return;

                    unlockedRitual.targetPawn = pawn;

                    OriginalActions();
                });

                return false;
            }
            else if (unlockedRitual.targetsThingOfDef != null)
            {
                ThingDef wantedDef = unlockedRitual.targetsThingOfDef;

                Find.Targeter.BeginTargeting(
                    new TargetingParameters
                    {
                        canTargetPawns = false,
                        canTargetBuildings = true,
                        canTargetPlants = true,
                        canTargetItems = true,
                        validator = t =>
                            t.Thing != null &&
                            t.Thing.def == wantedDef &&
                            // Probably redundant
                            //t.Thing.Spawned &&
                            //t.Thing.Map == map && 
                            unlockedRitual.ExtraThingValidator(t.Thing)
                    },
                    localTarget =>
                    {
                        unlockedRitual.targetThing = localTarget.Thing;

                        OriginalActions();
                    });

                return false;
            }
            else if (unlockedRitual.gizmoMakesFloatMenu)
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();

                foreach (string item in unlockedRitual.FloatMenuOptionStrings)
                {
                    opts.Add(new FloatMenuOption(
                        item,
                        //() => unlockedRitual.HandleFloatMenuOption(item)));
                        () =>
                        {
                            unlockedRitual.selectedFloatMenuOptionString = item;

                            OriginalActions();
                        }
                        ));
                }

                Find.WindowStack.Add(new FloatMenu(opts));

                return false;
            }

            // implied else
            return true;

            void OriginalActions()
            {
                TargetInfo target2 = new TargetInfo(target);
                PsychicRitualRoleAssignments assignments = psychicRitualDef.BuildRoleAssignments(target2);
                PsychicRitualCandidatePool candidatePool = psychicRitualDef.FindCandidatePool();
                Map currentMap = Find.CurrentMap;
                psychicRitualDef.InitializeCast(currentMap);
                Find.WindowStack.Add(new Dialog_BeginPsychicRitual(psychicRitualDef, candidatePool, assignments, currentMap));
            }
        }
    }

    //[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    //public static class Game_FinalizeInit_Patch
    //{
    //    public static void Postfix()
    //    {
    //        CompBiHeatPusherRitualized.psychicRitualManager = Current.Game.GetComponent<GameComponent_PsychicRitualManager>();
    //    }
    //}

    [HarmonyPatch(typeof(PsychicRitualDef_InvocationCircle), "CalculateMaxPower")]
    public static class PsychicRitualDef_InvocationCircle_CalculateMaxPower_Postfix
    {
        public static void Postfix(PsychicRitualRoleAssignments assignments, List<QualityFactor> powerFactorsOut, ref float power)
        {
            if (assignments.Target.Thing is ThingWithComps thing)
            {
                CalculateGroupedFacilityQualityOffset(powerFactorsOut, ref power, thing);
            }
        }

        private static void CalculateGroupedFacilityQualityOffset(List<QualityFactor> powerFactorsOut, ref float power, ThingWithComps focus)
        {
            CompAffectedByFacilities_Grouped comp = focus.TryGetComp<CompAffectedByFacilities_Grouped>();
            if (comp == null)
            {
                return;
            }

            Dictionary<ThingDef, RitualQualityOffsetCount> dictionary = new Dictionary<ThingDef, RitualQualityOffsetCount>();
            List<Thing> groupedFacilitiesListForReading = focus.GetComp<CompAffectedByFacilities_Grouped>().LinkedFacilitiesListForReading;
            for (int i = 0; i < groupedFacilitiesListForReading.Count; i++)
            {
                Thing thing = groupedFacilitiesListForReading[i];
                CompFacility_Grouped compGroupedFacility = thing.TryGetComp<CompFacility_Grouped>();
                if (compGroupedFacility?.StatOffsets == null || !compGroupedFacility.CanBeActive)
                {
                    continue;
                }
                for (int j = 0; j < compGroupedFacility.StatOffsets.Count; j++)
                {
                    StatModifier statModifier = compGroupedFacility.StatOffsets[j];
                    if (statModifier.stat == StatDefOf.PsychicRitualQuality)
                    {
                        if (dictionary.TryGetValue(thing.def, out var value))
                        {
                            value.count++;
                            value.offset += statModifier.value;
                        }
                        else
                        {
                            dictionary.Add(thing.def, new RitualQualityOffsetCount(1, statModifier.value));
                        }
                    }
                }
            }
            foreach (KeyValuePair<ThingDef, RitualQualityOffsetCount> item in dictionary)
            {
                powerFactorsOut?.Add(new QualityFactor
                {
                    label = Find.ActiveLanguageWorker.Pluralize(item.Key.label).CapitalizeFirst(),
                    positive = true,
                    count = item.Value.count.ToString(),
                    quality = item.Value.offset,
                    toolTip = "PsychicRitualDef_InvocationCircle_QualityFactor_Increase_Tooltip".Translate().CapitalizeFirst().EndWithPeriod()
                });
                power += item.Value.offset;
            }
        }
    }

    [HarmonyPatch( typeof(PsychicRitualDef_InvocationCircle), nameof(PsychicRitualDef_InvocationCircle.GenerateRolePositions))]
    public static class PsychicRitualDef_InvocationCircle_GenerateRolePositions_Postfix
    {
        public static void Postfix(PsychicRitualDef_InvocationCircle __instance, PsychicRitualRoleAssignments assignments, ref IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> __result)
        {
            if (__instance is not PsychicRitualDef_Unlocked)
                return;

            Map map = assignments.Target.Map;
            IntVec3 ritualFocus = assignments.Target.Cell;

            if (map == null || __result == null)
                return;

            Pawn invoker = assignments.FirstAssignedPawn(__instance.InvokerRole);

            HashSet<IntVec3> alreadyUsedCells = new();
            Dictionary<PsychicRitualRoleDef, List<IntVec3>> newDict = new();

            // Will find a spot for the invoker(s) first, then target(s), then everyone else.
            var roles = __result.Keys.OrderBy(role =>
                role == __instance.InvokerRole ? 0 :
                role == __instance.TargetRole ? 1 : 2);

            foreach (PsychicRitualRoleDef role in roles)
            {
                List<Pawn> pawns = assignments.AssignedPawns(role).ToList();
                List<IntVec3> originalCells = __result[role];
                List<IntVec3> positions = new(originalCells.Count);

                if (pawns.Count != originalCells.Count)
                {
                    Log.Error($"[TSOA] Role position count mismatch for {__instance.defName} role {role.defName}.");
                    return;
                }

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];

                    // A downed or imprisoned pawn might be being carried by the invoker
                    Pawn movingPawn = role == __instance.TargetRole
                        && (pawn.Downed || pawn.IsPrisoner)
                            ? invoker
                            : pawn;

                    bool Valid(IntVec3 cell) =>
                        cell.InBounds(map)
                        && cell != ritualFocus
                        && !alreadyUsedCells.Contains(cell)
                        && cell.Standable(map)
                        && movingPawn != null
                        && movingPawn.Map == map
                        && movingPawn.CanReach(
                            cell, PathEndMode.OnCell, Danger.Deadly);

                    // Start the search at the original position if it's a valid IntVec3, otherwise start at the ritual focus
                    IntVec3 preferredCell = originalCells[i].IsValid
                        ? originalCells[i]
                        : ritualFocus;

                    // Find a good cell in a small radius near the preferredCell
                    IntVec3 chosenCell = CellFinder.StandableCellNear(preferredCell, map, 3f, Valid);

                    // If we failed to find one above, try a larger radius around the ritual focus
                    if (!chosenCell.IsValid)
                    {
                        chosenCell = CellFinder.StandableCellNear(ritualFocus, map, __instance.invocationCircleRadius + 2f, Valid);
                    }

                    if (!chosenCell.IsValid)
                    {
                        Log.Error($"[TSOA] No accessible ritual position for " + $"{pawn.LabelShort} in {__instance.defName}.");
                        // TODO see how likely it is to get complete failure, see if I need even more failure handling
                        return;
                    }

                    positions.Add(chosenCell);
                    alreadyUsedCells.Add(chosenCell);
                }

                newDict.Add(role, positions);
            }

            __result = newDict;
        }
    }
}