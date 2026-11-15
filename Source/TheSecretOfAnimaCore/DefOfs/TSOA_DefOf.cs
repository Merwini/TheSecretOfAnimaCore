using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core;

[DefOf]
public class TSOA_MiscDefOf
{
    public static SoundDef AnimaTreeScream;

    public static DesignationDef TSOA_LoadSapNow;

    public static DesignationDef TSOA_EmptyNow;

    public static ResearchProjectDef TSOA_AnimaOne;

    public static ResearchProjectDef TSOA_AnimaThree;

    [MayRequireAnomaly]
    public static MeditationFocusDef Void;

    [MayRequireAnomaly]
    public static ResearchProjectDef TSOA_VoidOne;

    [MayRequireAnomaly]
    public static MutantDef TSOA_ShadeMutant;

    public static IncidentDef TSOA_AmberRecoveryOffer;

    public static QuestScriptDef TSOA_Opportunity_FontOfAnima;

    public static QuestScriptDef TSOA_RecoverAmberQuest;
}
