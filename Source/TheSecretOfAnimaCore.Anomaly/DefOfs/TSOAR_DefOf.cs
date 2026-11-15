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
public class TSOAR_DefOf
{
    public static ThingDef PsychicRitualSpot;

    public static ThingDef TSOA_EruptionTunnel;

    public static DutyDef TSOA_DefendMinerals;

    public static HediffDef TSOA_PsychicSevering;

    public static HediffDef TSOA_PsychicFever_Success;

    public static HediffDef TSOA_PsychicFever_Failure;

    public static HediffDef TSOA_RegrowingPart;

    // public static HediffDef TSOA_KodamaLifespan;

    public static WeatherDef Rain;

    public static WeatherDef RainyThunderstorm;

    public static WeatherDef DryThunderstorm;

    public static WeatherDef SnowHard;

    public static WeatherDef SnowGentle;

    public static PsychicRitualDef_FlowerPower TSOA_FlowerPowerRitual;

    // public static PawnKindDef TSOA_KodamaHaulerKind;

    // public static PawnKindDef TSOA_KodamaCleanerKind;

    // public static PawnKindDef TSOA_KodamaCutterKind;

    [MayRequireIdeology]
    public static MemeDef Blindsight;
}
