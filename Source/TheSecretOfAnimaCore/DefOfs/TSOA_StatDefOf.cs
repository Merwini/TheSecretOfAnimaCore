using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_StatDefOf
{
    public static StatDef TSOA_ComfortFactor;

    public static StatDef TSOA_EntropyDamageFlat;

    public static StatDef TSOA_EntropyDamagePercent;

    public static StatDef TSOA_PsyScaling;

    static TSOA_StatDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TSOA_StatDefOf));
	}
}