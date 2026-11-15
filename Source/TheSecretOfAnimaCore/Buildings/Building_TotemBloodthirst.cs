using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace tsoa.core;

public class Building_TotemBloodthirst : Building_AnimusTotem
{
    public override void DoTotemEffect()
    {
        RegisterTotem();
    }

    public override void RegisterTotem()
    {
        gameComp.activeBloodthirst.Add(this);
    }

    public override void EndTotemEffect()
    {
        gameComp.activeBloodthirst.Remove(this);
    }

    public static bool CheckIfBloodlustShouldApply(Pawn victim, Pawn instigator)
    {
        if ((GameComponent_TotemTracker.Instance?.activeBloodthirst?.Count ?? 0) == 0)
                return false;

            if (instigator?.IsColonist != true)
                return false;

            if (instigator.Dead || instigator.Destroyed || !instigator.Spawned)
                return false;

            if (!victim.HostileTo(Faction.OfPlayer))
                return false;

            return true;
    }

    public static void ApplyOrUpdateBloodlust(Pawn pawn)
    {
        Hediff_Bloodthirst hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(TSOA_HediffDefOf.TSOA_BloodthirstHediff) as Hediff_Bloodthirst;
        if (hediff == null)
        {
            hediff = (Hediff_Bloodthirst)HediffMaker.MakeHediff(TSOA_HediffDefOf.TSOA_BloodthirstHediff, pawn);
            pawn.health.AddHediff(hediff);
        }
        hediff.AddKill();
    }

    [HarmonyPatch(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnDowned))]
    public static class RecordsUtility_Notify_PawnDowned_Postfix
    {
        public static void Postfix(Pawn downed, Pawn instigator)
        {
            if (CheckIfBloodlustShouldApply(downed, instigator))
            {
                ApplyOrUpdateBloodlust(instigator);
            }
        }
    }
    
    [HarmonyPatch(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled))]
    public static class RecordsUtility_Notify_PawnKilled_Postfix
    {
        public static void Postfix(Pawn killed, Pawn killer)
        {
            if (CheckIfBloodlustShouldApply(killed, killer))
            {
                ApplyOrUpdateBloodlust(killer);
            }
        }
    }
}
