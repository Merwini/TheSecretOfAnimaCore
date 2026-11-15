using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_JobDefOf
{
    
    public static JobDef TSOA_AddAnimaSapToFontJob;
    
    public static JobDef TSOA_ComfortAnimaTree;
    
    public static JobDef TSOA_PlantAnimaPearl;

    public static JobDef TSOA_TakeAnimaSapOutOfBasinJob;

    public static JobDef TSOA_TakeAnimaAmberOutOfFontJob;



    static TSOA_JobDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TSOA_JobDefOf));
	}
}