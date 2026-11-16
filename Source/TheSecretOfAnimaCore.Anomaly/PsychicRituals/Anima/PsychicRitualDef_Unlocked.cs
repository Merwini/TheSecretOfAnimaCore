using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace tsoa.core.anomaly;

public class PsychicRitualDef_Unlocked : PsychicRitualDef_InvocationCircle
{
    public List<ThingDef> ritualFocuses; // List of allowed ritual focuses for this ritual
    public Thing ritualFocus; // The actual focus used in the ritual instance

    public ResearchProjectDef advancedResearchProject;

    public bool invokerUseInteractionCell;
    public IntVec3 invokerOffset = IntVec3.Invalid; // default invoker position is either 3 or 4 cells to the right of the focus, TODO double check which

    public bool targetsCell;
    public IntVec3 targetCell;

    public bool targetsPawn;
    public Pawn targetPawn;

    public ThingDef targetsThingOfDef;
    public Thing targetThing;

    public float invokerAffinity = 0.05f;
    public float targetAffinity = 0.03f;
    public float chanterAffinity = 0.01f;

    public bool gizmoMakesFloatMenu;
    public virtual List<string> FloatMenuOptionStrings => null;
    public string selectedFloatMenuOptionString = null;

    public virtual bool ExtraCellValidator(IntVec3 cell, Map map)
    {
        return true;
    }

    public virtual bool ExtraThingValidator(Thing thing)
    {
        return true;
    }

    // Didn't find a use case, might re-enable later but would require rewriting eruption and beckoning rituals
    //public virtual void HandleFloatMenuOption(string option)
    //{
    //}

    public override void ResolveReferences()
    {
        if (ritualFocuses.NullOrEmpty())
        {
            ritualFocuses = new List<ThingDef>();
            Log.Error($"[TSOA] Config error in {defName} from {modContentPack.Name}. Using class PsychicRitualDef_LocationUnlocked but has no list of ritual focuses.");
        }

        if ((targetsCell && targetsPawn) || (targetsCell && targetsThingOfDef != null) || (targetsPawn && targetsThingOfDef != null))
        {
            Log.Error($"[TSOA] Config error in {defName} from {modContentPack.Name}. Using class PsychicRitualDef_LocationUnlocked but has multiple target types set.");
        }

        if (gizmoMakesFloatMenu && (targetsCell || targetsPawn || targetsThingOfDef != null))
        {
            Log.Error($"[TSOA] Config error in {defName} from {modContentPack.Name}. Using class PsychicRitualDef_LocationUnlocked and is configured to make a float menu AND target something.");
        }

        base.ResolveReferences();
    }
}
