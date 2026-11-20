using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace tsoa.core.anomaly;

[DefOf]
public class Anomaly_MiscDefOf
{
    public static DutyDef TSOA_DefendMinerals;

    public static PsychicRitualDef_FlowerPower TSOA_FlowerPowerRitual;

    [MayRequireIdeology]
    public static MemeDef Blindsight;

    public static MapGeneratorDef TSOA_VoidDive_Test;
    public static WorldObjectDef TSOA_VoidTunnelExit;

    static Anomaly_MiscDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(Anomaly_MiscDefOf));
	}
}
