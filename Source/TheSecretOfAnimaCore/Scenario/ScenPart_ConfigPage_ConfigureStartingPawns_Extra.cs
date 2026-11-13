using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace tsoa.core;

public class ScenPart_ConfigPage_ConfigureStartingPawns_Extra : ScenPart_ConfigPage_ConfigureStartingPawnsBase
{
    public List<PawnConfigurationGroup> groups = new();

    public DevelopmentalStage allowedDevelopmentalStages = DevelopmentalStage.Baby | DevelopmentalStage.Child | DevelopmentalStage.Adult;

    public List<SkillDef> requiredSkills;

    public override int TotalPawnCount => groups.Sum(group => group.count);

    // necessary so that Page_ConfigureStartingPawns_Extra can access groups
    public override IEnumerable<Page> GetConfigPages()
    {
        foreach (Page page in base.GetConfigPages())
        {
            if (page is Page_ConfigureStartingPawns_Extra extraPage)
                extraPage.scenPart = this;

            yield return page;
        }
    }

    public override void PostIdeoChosen()
    {
        Find.GameInitData.allowedDevelopmentalStages = allowedDevelopmentalStages;
        Find.GameInitData.startingSkillsRequired = requiredSkills;
        base.PostIdeoChosen();
    }

    // copied from ScenPart_ConfigPage_ConfigureStartingPawns.GenerateStartingPawns()
    public override void GenerateStartingPawns()
    {
        int attempts = 0;
        do
        {
            StartingPawnUtility.ClearAllStartingPawns();
            for (int i = 0; i < TotalPawnCount; i++)
            {
                StartingPawnUtility.AddNewPawn();
            }

            attempts++;
        }
        while (attempts <= 20 && !StartingPawnUtility.WorkTypeRequirementsSatisfied());
    }

    public override void PreMapGenerate()
    {
        base.PreMapGenerate();

        // PrepForMapGen has removed the left behind pawns, so startingAndOptionalPawns is now only starting pawns, in the same order as on the Page
        List<Pawn> pawns = Find.GameInitData.startingAndOptionalPawns;
        int pawnIndex = 0;
        foreach (PawnConfigurationGroup group in groups)
        {
            for (int i = 0; i < group.count && pawnIndex < pawns.Count; i++, pawnIndex++)
            {
                group.Apply(pawns[pawnIndex]);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
        Scribe_Values.Look(ref allowedDevelopmentalStages, "allowedDevelopmentalStages", DevelopmentalStage.Baby | DevelopmentalStage.Child | DevelopmentalStage.Adult);
        Scribe_Collections.Look(ref requiredSkills, "requiredSkills", LookMode.Def);
    }

    public override string Summary(Scenario scen)
    {
		if (TotalPawnCount == 1)
		{
			return "ScenPart_StartWithPawn".Translate();
		}
		return "ScenPart_StartWithPawns".Translate(TotalPawnCount);
	}

    public override bool HasNullDefs()
    {
        return base.HasNullDefs() || groups == null || groups.Any(group => group == null
            || group.hediffs.Any(hediff => hediff?.def == null)
            || group.traits.Any(trait => trait?.def == null));
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
            yield return error;

        if (groups.NullOrEmpty())
        {
            yield return "Starting pawn configuration has no groups.";
            yield break;
        }

        if (pawnChoiceCount < TotalPawnCount)
            yield return "pawnChoiceCount must be at least the sum of the starting pawn group counts.";

        foreach (PawnConfigurationGroup group in groups)
        {
            if (group.count < 1)
                yield return "Starting pawn group counts must be positive.";
            if (group.groupLabel.NullOrEmpty())
                yield return "Starting pawn group is missing its groupLabel.";

            foreach (ConfigGroupHediff hediff in group.hediffs)
                if (hediff?.def == null)
                    yield return $"Starting pawn group '{group.groupLabel}' has a missing hediff def.";

            foreach (ConfigGroupTrait trait in group.traits)
            {
                if (trait?.def == null)
                    yield return $"Starting pawn group '{group.groupLabel}' has a missing trait def.";
                else if (!trait.def.degreeDatas.Any(data => data.degree == trait.degree))
                    yield return $"Starting pawn group '{group.groupLabel}' has an invalid degree for {trait.def.defName}.";
            }
        }
    }
}

public class PawnConfigurationGroup : IExposable
{
    public int count = 1;

    [MustTranslate]
    public string groupLabel;

    public List<ConfigGroupHediff> hediffs = new();
    public List<ConfigGroupTrait> traits = new();

    public void Apply(Pawn pawn)
    {
        foreach (ConfigGroupTrait trait in traits)
            trait.Apply(pawn);
        foreach (ConfigGroupHediff hediff in hediffs)
            hediff.Apply(pawn);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref count, "count", 1);
        Scribe_Values.Look(ref groupLabel, "groupLabel");
        Scribe_Collections.Look(ref hediffs, "hediffs", LookMode.Deep);
        Scribe_Collections.Look(ref traits, "traits", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            hediffs ??= new();
            traits ??= new();
        }
    }
}

public class ConfigGroupTrait : IExposable
{
    public TraitDef def;
    public int degree;

    public void Apply(Pawn pawn)
    {
        TraitSet traits = pawn.story?.traits;
        if (traits == null || traits.HasTrait(def, degree))
            return;

        // Replace ordinary conflicting traits without removing genes or an
        // unrelated random trait. Gene conflicts use the normal suppression rules.
        foreach (Trait existing in traits.allTraits.Where(trait => trait.sourceGene == null
                     && (trait.def == def || def.ConflictsWith(trait.def))).ToList())
            traits.RemoveTrait(existing, unsuppressConflicts: true);

        Trait added = new Trait(def, degree, forced: true);
        traits.GainTrait(added, suppressConflicts: true);
        TraitUtility.ApplySkillGainFromTrait(pawn, added);
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref degree, "degree", 0);
    }
}

public class ConfigGroupHediff : IExposable
{
    public HediffDef def;
    public BodyPartDef bodyPart;
    public int stage = -1;

    public void Apply(Pawn pawn)
    {
        BodyPartRecord part = null;
        if (bodyPart != null)
        {
            part = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(record => record.def == bodyPart);
            if (part == null)
            {
                Log.Warning($"[TSOA] Cannot add {def.defName} to {pawn}: no available {bodyPart.defName}.");
                return;
            }
        }
        Hediff hediff = HediffMaker.MakeHediff(def, pawn, part);
        if (hediff != null && stage >= 0 && !def.stages.NullOrEmpty() && stage < def.stages.Count)
        {
            hediff.Severity  = def.stages[stage].minSeverity;
        }

        pawn.health.AddHediff(hediff);
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Defs.Look(ref bodyPart, "bodyPart");
        Scribe_Values.Look(ref stage, "stage", -1);
    }
}
