using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class Anomaly_ThingDefOf
{
    public static ThingDef PsychicRitualSpot;

    public static ThingDef TSOA_EruptionTunnel;

    static Anomaly_ThingDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(Anomaly_ThingDefOf));
	}
}
