using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_DamageDefOf
{
    public static DamageDef TSOA_EntropicDischarge;

    public static DamageDef TSOA_EntropyExtraDamage;

    static TSOA_DamageDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(TSOA_DamageDefOf));
	}
}
