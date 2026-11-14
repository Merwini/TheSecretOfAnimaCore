using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace tsoa.core;

public abstract class CompTargetable_MultiMutant : CompTargetable_SingleCorpse
{
    public CompProperties_Targetable_MultiMutant multiProps => (CompProperties_Targetable_MultiMutant)props;

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!base.ValidateTarget(target, showMessages))
            return false;

        Pawn pawn = ((Corpse)target.Thing).InnerPawn;
            return multiProps.mutantMultiFilter == null || (pawn.IsMutant && multiProps.mutantMultiFilter.Contains(pawn.mutant.Def));
	}
}