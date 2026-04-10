using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_DefOf
{
    public static ThingDef TSOA_AnimaGrassResource;

    public static ThingDef TSOA_AnimaSap;

    public static ThingDef TSOA_AnimaAmber;

    public static ThingDef TSOA_AnimaSapBasin;

    public static ThingDef TSOA_FontOfAnima;

    //public static ThoughtDef TSOA_AnimaGrassScream;

    //public static JobDef TSOA_HarvestAnimaGrassJob;

    public static JobDef TSOA_AddAnimaSapToFontJob;

    public static JobDef TSOA_TakeAnimaSapOutOfBasinJob;

    public static JobDef TSOA_TakeAnimaAmberOutOfFontJob;

    public static JobDef TSOA_PlantAnimaPearl;

    public static SoundDef AnimaTreeScream;

    public static DesignationDef TSOA_LoadSapNow;

    public static DesignationDef TSOA_EmptyNow;

    public static StatDef TSOA_ComfortFactor;

    public static ResearchProjectDef TSOA_AnimaOne;

    public static ResearchProjectDef TSOA_AnimaThree;

    public static ThingDef TSOA_TreeAnimaSilent;

    [MayRequireAnomaly] // should never be referenced with Anomaly not active anyway
    public static ThingDef TSOA_SignalAction_AnomalyAmbush;

    public static IncidentDef TSOA_AmberRecoveryOffer;

    public static QuestScriptDef TSOA_Opportunity_FontOfAnima;

    public static QuestScriptDef TSOA_RecoverAmberQuest;
}
