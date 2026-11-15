// using RimWorld;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using tsoa.core;
// using Verse;
// using Verse.AI;
// using Verse.AI.Group;
// using static UnityEngine.GraphicsBuffer;

// namespace tsoa.core.anomaly;

// public class PsychicRitualToil_CreateKodama : PsychicRitualToil_AnimaAffinity
// {
//     PsychicRitualRoleDef invokerRole;
//     WorkTypeDef chosenWorkType;

//     public PsychicRitualToil_CreateKodama()
//     {
//     }

//     public PsychicRitualToil_CreateKodama(PsychicRitualRoleDef invokerRole, WorkTypeDef chosenWorkType)
//     {
//         this.invokerRole = invokerRole;
//         this.chosenWorkType = chosenWorkType;
//     }

//     public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
//     {
//         base.Start(psychicRitual, parent);

//         Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
//         //psychicRitual.assignments.AssignedPawnCount
//         int maxKodama = 7;
//         int kodamaCount = 1;
//         for (int i = 0; i < maxKodama - 1; i++)
//         {
//             if (Rand.Chance(psychicRitual.PowerPercent))
//             {
//                 kodamaCount++;
//             }
//         }

//         if (invoker != null)
//         {
//             ApplyOutcome(psychicRitual, invoker, kodamaCount);
//         }
//     }

//     private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker, int kodamaCount)
//     {
//         PawnKindDef kind = PsychicRitualDef_CreateKodama.WorkTypeDict.TryGetValue(chosenWorkType);
//         if (kind == null)
//         {
//             Log.Error($"PsychicRitual CreateKodama failed to find a PawnKindDef for the chosen WorkTypeDef {chosenWorkType.defName}. Defaulting to cleaning.");
//             kind = TSOAR_DefOf.TSOA_KodamaCleanerKind;
//         }

//         for (int i = 0; i < kodamaCount; i++)
//         {
//             Pawn kodama = PawnGenerator.GeneratePawn(kind, Faction.OfPlayer);
//             GenSpawn.Spawn(kodama, invoker.Position, invoker.Map);
//             Hediff hediff = HediffMaker.MakeHediff(TSOAR_DefOf.TSOA_KodamaLifespan, kodama);
//             kodama.health.AddHediff(hediff);
//         }

//         Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), "TSOA_CreateKodamaSuccess".Translate(psychicRitual.assignments.FirstAssignedPawn(invokerRole), kodamaCount, psychicRitual.def.Named("RITUAL")), LetterDefOf.NeutralEvent);
//     }

//     public override void ExposeData()
//     {
//         base.ExposeData();
//         Scribe_Defs.Look(ref invokerRole, "invokerRole");
//         Scribe_Defs.Look(ref chosenWorkType, "chosenWorkType");
//     }
// }
