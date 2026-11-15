using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_HediffDefOf
{
    public static HediffDef TSOA_AnimaAffinityHediff;

    public static HediffDef TSOA_PsychicLeechHediff;
    
    public static HediffDef TSOA_VirionActiveHediff;

    public static HediffDef TSOA_VirionHediff;

    static TSOA_HediffDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TSOA_HediffDefOf));
	}
}