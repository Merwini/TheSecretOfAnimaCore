using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualToil_Beckoning : PsychicRitualToil_AnimaAffinity
{
    private PsychicRitualRoleDef invokerRole;
    private List<Faction> eligibleFactions;
    private TraderKindDef chosenKind;

    private static readonly IntRange ArrivalDelayTicks = new IntRange(60000, 90000);

    public PsychicRitualToil_Beckoning()
    {
    }

    public PsychicRitualToil_Beckoning(PsychicRitualRoleDef invokerRole, List<Faction> eligibleFactions, TraderKindDef chosenKind)
    {
        this.invokerRole = invokerRole;
        this.eligibleFactions = eligibleFactions;
        this.chosenKind = chosenKind;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        base.Start(psychicRitual, parent);

        Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
            
        bool success = chosenKind == null 
            ? Rand.Chance(((PsychicRitualDef_Beckoning)psychicRitual.def).successCurveAnyTrader.Evaluate(psychicRitual.PowerPercent))
            : Rand.Chance(((PsychicRitualDef_Beckoning)psychicRitual.def).successCurve.Evaluate(psychicRitual.PowerPercent));

        psychicRitual.ReleaseAllPawnsAndBuildings();

        if (invoker != null)
        {
            ApplyOutcome(psychicRitual, invoker, success);
        }
    }

    private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker, bool success)
    {
        IncidentParms incidentParms = new IncidentParms();
        incidentParms.target = invoker.Map;
        incidentParms.forced = true;

        int delayTicks = ArrivalDelayTicks.RandomInRange;

        LetterDef textLetterDef;
        TaggedString text;

        if (success)
        {
            Faction selectedFaction = eligibleFactions.RandomElement();
            textLetterDef = LetterDefOf.PositiveEvent;
            text = "TSOA_BeckoningSuccess".Translate(invoker, psychicRitual.def.Named("RITUAL"));
            incidentParms.faction = selectedFaction;
            incidentParms.traderKind = chosenKind ?? selectedFaction.def.caravanTraderKinds.RandomElement();
            Find.Storyteller.incidentQueue.Add(IncidentDefOf.TraderCaravanArrival, Find.TickManager.TicksGame + delayTicks, incidentParms);
        }
        else
        {
            textLetterDef = LetterDefOf.NegativeEvent; 
            incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(invoker.Map);
            text = "TSOA_BeckoningFailure".Translate(invoker, psychicRitual.def.Named("RITUAL"));
            Find.Storyteller.incidentQueue.Add(IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + delayTicks, incidentParms);
        }

        Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), text, textLetterDef);
    }


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
        Scribe_Defs.Look(ref chosenKind, "chosenKind");
        Scribe_Collections.Look(ref eligibleFactions, "eligibleFactions", LookMode.Reference);
    }
}