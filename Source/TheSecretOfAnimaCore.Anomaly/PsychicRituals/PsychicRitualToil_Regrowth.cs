using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_Regrowth : PsychicRitualToil_AnimaAffinity
{
    private PsychicRitualRoleDef invokerRole;
    private PsychicRitualRoleDef targetRole;

    public PsychicRitualToil_Regrowth()
    {
    }

    public PsychicRitualToil_Regrowth(PsychicRitualRoleDef invokerRole, PsychicRitualRoleDef targetRole)
    {
        this.invokerRole = invokerRole;
        this.targetRole = targetRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);
        Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
        Pawn target = psychicRitual.assignments.FirstAssignedPawn(targetRole);

        bool success = Rand.Chance(((PsychicRitualDef_Regrowth)psychicRitual.def).successCurve.Evaluate(psychicRitual.PowerPercent));

        psychicRitual.ReleaseAllPawnsAndBuildings();

        if (invoker != null && target != null)
        {
            ApplyOutcome(psychicRitual, success, invoker, target);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, bool success, Pawn invokerPawn, Pawn targetPawn)
    {
        List<Hediff> missingParts = new List<Hediff>();
        List<Hediff> otherPermanentHediffs = new List<Hediff>();

        foreach (Hediff hediff in targetPawn.health.hediffSet.hediffs)
        {
            if (hediff is Hediff_MissingPart mp && mp.Part != null)
            {
                if (ModsConfig.IdeologyActive && targetPawn.Ideo != null && targetPawn.Ideo.HasMeme(TSOAR_DefOf.Blindsight) && mp.Part.def == BodyPartDefOf.Eye)
                    continue;

                missingParts.Add(hediff);
                continue;
            }

            if (hediff is Hediff_Injury injury && injury.IsPermanent())
            {
                otherPermanentHediffs.Add(hediff);
                continue;
            }
        }

        List<Hediff> filteredMissingParts = RemoveChildParts(missingParts);

        Hediff random;
        bool curingMissingPart = false;
        if (!filteredMissingParts.NullOrEmpty())
        {
            random = filteredMissingParts.RandomElement();
            curingMissingPart = true;
        }
        else if (!otherPermanentHediffs.NullOrEmpty())
        {
            random = otherPermanentHediffs.RandomElement();
        }
        else
        {
            random = null;
        }

        if (curingMissingPart && Hediff_RegrowingPart.TryRemoveMissingFrom(targetPawn, random.Part))
        {

            Hediff_RegrowingPart regrow = (Hediff_RegrowingPart)HediffMaker.MakeHediff(TSOAR_DefOf.TSOA_RegrowingPart, targetPawn, random.Part);
            targetPawn.health.AddHediff(regrow);
        }
        else if (random != null)
        {
            HealthUtility.Cure(random);
        }
        else
        {
            Messages.Message("TSOA_RegrowthRitualNoInjuryOnComplete".Translate(), MessageTypeDefOf.NeutralEvent);
        }
            
        if (!success && random != null)
        {
            GiveRandomCarcinoma(targetPawn);
        }
    }

    private List<Hediff> RemoveChildParts(List<Hediff> missingParts)
    {
        HashSet<BodyPartRecord> hediffParts = new HashSet<BodyPartRecord>();
        foreach (Hediff hediff in missingParts)
        {
            if (hediff.Part != null)
            {
                hediffParts.Add(hediff.Part);
            }
        }

        List<Hediff> filtered = new List<Hediff>();
        foreach (Hediff hediff in missingParts)
        {
            if (hediff.Part?.parent != null && hediffParts.Contains(hediff.Part.parent))
            {
                continue;
            }
            else
            {
                filtered.Add(hediff);
            }
        }
        return filtered;
    }

    // :(
    private void GiveRandomCarcinoma(Pawn pawn)
    {
        HediffDef carcinomaDef = DefDatabase<HediffDef>.GetNamedSilentFail("Carcinoma");
        if (carcinomaDef == null)
            return;

        List<BodyPartRecord> partsAndOrgans = new List<BodyPartRecord>();

        foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
        {
            if (part == null)
                continue;

            if (pawn.health.hediffSet.PartIsMissing(part))
                continue;

            if (pawn.health.hediffSet.HasDirectlyAddedPartFor(part))
                continue;

            partsAndOrgans.Add(part);
        }

        partsAndOrgans.Shuffle();

        foreach (BodyPartRecord part in partsAndOrgans)
        {
            Hediff hediff = HediffMaker.MakeHediff(carcinomaDef, pawn, part);
            if (hediff == null)
                continue;

            pawn.health.AddHediff(hediff, part);
            return;
        }

        return;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
        Scribe_Defs.Look(ref targetRole, "targetRole");
    }
}