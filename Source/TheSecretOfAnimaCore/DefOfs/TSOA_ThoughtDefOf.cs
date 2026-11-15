using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_ThoughtDefOf
{
    public static ThoughtDef TSOA_AnimaArrowScream;

    static TSOA_ThoughtDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TSOA_ThoughtDefOf));
	}
}
