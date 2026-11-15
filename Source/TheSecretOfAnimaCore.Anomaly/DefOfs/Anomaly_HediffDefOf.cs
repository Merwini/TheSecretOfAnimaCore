using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class Anomaly_HediffDefOf
{
    public static HediffDef TSOA_PsychicFever_Failure;

    public static HediffDef TSOA_PsychicFever_Success;

    public static HediffDef TSOA_PsychicSevering;

    public static HediffDef TSOA_RegrowingPart;

    static Anomaly_HediffDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(Anomaly_HediffDefOf));
	}
}
