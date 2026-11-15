// using RimWorld;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Verse;
// using Verse.AI.Group;
// using UnityEngine;
// using Steamworks;

// namespace tsoa.core.anomaly;

// public class PsychicRitualDef_CreateKodama : PsychicRitualDef_Unlocked
// {
//     private static Dictionary<WorkTypeDef, PawnKindDef> dict = new Dictionary<WorkTypeDef, PawnKindDef>();
//     public static Dictionary<WorkTypeDef, PawnKindDef> WorkTypeDict
//     {
//         get
//         {
//             if (dict.Count == 0)
//             {
//                 InitializeWorkTypeDict();
//             }
//             return dict;
//         }
//     }

//     private WorkTypeDef chosenWorkType;

//     public override List<string> FloatMenuOptionStrings
//     {
//         get
//         {
//             List<string> options = new List<string>();
//             foreach (var kvp in WorkTypeDict)
//             {
//                 options.Add(kvp.Key.gerundLabel);
//             }
//             return options;
//         }
//     }

//     public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
//     {
//         List<PsychicRitualToil> list = base.CreateToils(psychicRitual, parent);
//         list.Add(new PsychicRitualToil_CreateKodama(InvokerRole, chosenWorkType));
//         return list;
//     }
//     public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
//     {
//         //int max = assignments.AssignedPawnCount;
//         int max = 7;
//         int average = Mathf.FloorToInt(max * qualityRange.min);
//         return outcomeDescription.Formatted(max, average);
//     }

//     public override void InitializeCast(Map map)
//     {
//         base.InitializeCast(map);

//         if (selectedFloatMenuOptionString != null)
//         {
//             foreach (var kvp in WorkTypeDict)
//             {
//                 if (kvp.Key.gerundLabel == selectedFloatMenuOptionString)
//                 {
//                     chosenWorkType = kvp.Key;
//                 }
//             }
//         }
//     }

//     private static void InitializeWorkTypeDict()
//     {
//         if (dict.Count == 0)
//         {
//             dict[WorkTypeDefOf.Cleaning] = TSOAR_DefOf.TSOA_KodamaCleanerKind;
//             dict[WorkTypeDefOf.Hauling] = TSOAR_DefOf.TSOA_KodamaHaulerKind;
//             dict[WorkTypeDefOf.PlantCutting] = TSOAR_DefOf.TSOA_KodamaCutterKind;
//         }
//     }
// }
