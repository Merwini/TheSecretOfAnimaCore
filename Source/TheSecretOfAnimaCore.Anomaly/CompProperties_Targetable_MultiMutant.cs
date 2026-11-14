using System.Collections.Generic;
using RimWorld;
using Verse;

namespace tsoa.core;

public class CompProperties_Targetable_MultiMutant : CompProperties_Targetable
{
    public List<MutantDef> mutantMultiFilter;

    public CompProperties_Targetable_MultiMutant()
	{
		compClass = typeof(CompTargetable_MultiMutant);
	}
}