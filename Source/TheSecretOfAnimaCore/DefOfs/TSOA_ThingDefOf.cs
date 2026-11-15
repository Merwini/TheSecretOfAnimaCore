using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_ThingDefOf
{
    public static ThingDef TSOA_AnimaAmber;

    public static ThingDef TSOA_AnimaGrassResource;
    
    public static ThingDef TSOA_AnimaPearl;

    public static ThingDef TSOA_AnimaSap;

    public static ThingDef TSOA_AnimaSapBasin;

    public static ThingDef TSOA_FontOfAnima;
    
    [MayRequireAnomaly]
    public static ThingDef TSOA_SignalAction_AnomalyAmbush;
    
    public static ThingDef TSOA_TreeAnimaSilent;
    
    static TSOA_ThingDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TSOA_ThingDefOf));
	}
}
