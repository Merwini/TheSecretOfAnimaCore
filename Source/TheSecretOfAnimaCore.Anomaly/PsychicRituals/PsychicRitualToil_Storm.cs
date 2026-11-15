using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_Storm : PsychicRitualToil_AnimaAffinity
{
    private PsychicRitualRoleDef invokerRole;

    public PsychicRitualToil_Storm()
    {
    }

    public PsychicRitualToil_Storm(PsychicRitualRoleDef invokerRole)
    {
        this.invokerRole = invokerRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);
        Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
        // Can you have multiples of the same game condition? Probably? Best to let it account for multiples
        // Want to re-gather these here instead of passing from def, in case anything has changed in between
        List<GameCondition> droughts = psychicRitual.Map.GameConditionManager.ActiveConditions.Where(gc => gc.def.defName == "Drought").ToList();
        bool positiveOutcome = Rand.Chance(((PsychicRitualDef_Storm)psychicRitual.def).successCurve.Evaluate(psychicRitual.PowerPercent) * (droughts.NullOrEmpty() ? 1f : 0.5f));
        psychicRitual.ReleaseAllPawnsAndBuildings();
        if (pawn != null)
        {
            ApplyOutcome(psychicRitual, pawn, positiveOutcome, droughts);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker, bool positiveOutcome, List<GameCondition> draughts)
    {
        LetterDef textLetterDef;
        TaggedString text;

        WeatherDef newWeather = WeatherDefOf.Clear;
        if (positiveOutcome)
        {
            if (!draughts.NullOrEmpty())
            {
                foreach (GameCondition draught in draughts)
                {
                    if (!draught.Permanent && draught.TicksLeft > 0)
                    {
                        draught.End();
                    }
                }
            }
            if (invoker.Map.mapTemperature.OutdoorTemp >= -0.5f)
            {
                if (Rand.Chance(0.5f))
                {
                    newWeather = TSOAR_DefOf.Rain;
                }
                else if (Rand.Chance(0.5f))
                {
                    newWeather = TSOAR_DefOf.RainyThunderstorm;
                }
                else
                {
                    newWeather = WeatherDefOf.FoggyRain;
                }
            }
            else
            {
                if (Rand.Chance(0.66f))
                {
                    newWeather = TSOAR_DefOf.SnowGentle;
                }
                else
                {
                    newWeather = TSOAR_DefOf.SnowHard;
                }
            }
            textLetterDef = LetterDefOf.RitualOutcomePositive;
            text = "TSOA_StormSuccess".Translate(invoker, newWeather.label, psychicRitual.def.Named("RITUAL"));
        }
        else
        {
            newWeather = TSOAR_DefOf.DryThunderstorm;
            textLetterDef = LetterDefOf.RitualOutcomeNegative;
            text = "TSOA_StormFailure".Translate(invoker, newWeather.label, psychicRitual.def.Named("RITUAL"));
        }

        invoker.Map.weatherManager.TransitionTo(newWeather);

        Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), text, textLetterDef);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
    }
}