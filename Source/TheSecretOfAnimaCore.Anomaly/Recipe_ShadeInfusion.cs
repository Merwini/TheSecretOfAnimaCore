using System.Collections.Generic;
using RimWorld;
using Verse;

namespace tsoa.core;

public class Recipe_ShadeInfusion : Recipe_Surgery
{
	public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
	{
		if (!(thing is Pawn pawn))
		{
			return false;
		}
		if (pawn.IsMutant)
		{
			return false;
		}
		if (!pawn.ageTracker.Adult)
		{
			return false;
		}
		return base.AvailableOnNow(thing, part);
	}

	public override TaggedString GetConfirmation(Pawn pawn)
	{
		if (pawn.IsColonist)
		{
			return "TSOA_ShadeConfirmation".Translate(pawn.Named("PAWN"));
		}
		return base.GetConfirmation(pawn);
	}

	public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
	{
		if (!CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
		{
			bool flag = IsViolationOnPawn(pawn, part, Faction.OfPlayer);
			Faction homeFaction = pawn.HomeFaction;
			MutantUtility.SetPawnAsMutantInstantly(pawn, TSOA_DefOf.TSOA_ShadeMutant);
			SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
			melee.Level = 0;
			melee.xpSinceLastLevel = 0f;
			SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
			shooting.Level = 0;
			shooting.xpSinceLastLevel = 0f;
			if (pawn.Faction != Faction.OfPlayer)
			{
				pawn.SetFaction(Faction.OfPlayer);
			}
			if (billDoer != null)
			{
				TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
			}
			if (flag)
			{
				ReportViolation(pawn, billDoer, homeFaction, -50, HistoryEventDefOf.GhoulInfusionPerformed);
			}
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.ColonyGhouls, OpportunityType.GoodToKnow); // TODO worth it to make Shade ConceptDef?
		}
	}
}
