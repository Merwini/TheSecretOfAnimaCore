using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class Anomaly_WeatherDefOf
{
    public static WeatherDef DryThunderstorm;

    public static WeatherDef Rain;

    public static WeatherDef RainyThunderstorm;

    public static WeatherDef SnowGentle;

    public static WeatherDef SnowHard;

    static Anomaly_WeatherDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(Anomaly_WeatherDefOf));
	}
}
