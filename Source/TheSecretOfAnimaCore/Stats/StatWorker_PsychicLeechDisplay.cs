using System;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace tsoa.core;

public class StatWorker_PsychicLeechDisplay : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        ThingDef def = null;
        if (req.HasThing)
            def = req.Thing.def;
        else if (req.Def is ThingDef)
            def = req.Def as ThingDef;

        if (def == null) return false;

        PsychicLeechExtension extension = def.GetModExtension<PsychicLeechExtension>();

        return extension != null;
    }

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        ThingDef def = null;
        if (req.HasThing)
            def = req.Thing.def;
        else if (req.Def is ThingDef)
            def = req.Def as ThingDef;

        if (def == null) return 0f;

        if (def.GetModExtension<PsychicLeechExtension>() is PsychicLeechExtension extension)
                return extension.sensitivityOffset;

        return 0f;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        ThingDef def = null;
        if (req.HasThing)
            def = req.Thing.def;
        else if (req.Def is ThingDef)
            def = req.Def as ThingDef;

        if (def == null) return "[TSOA] something has gone wrong, this should not be visible.";

        PsychicLeechExtension extension = def.GetModExtension<PsychicLeechExtension>();

        return "TSOA_PsychicLeechWorkerExplanation".Translate(extension.sensitivityOffset.ToStringPercent());
    }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        return null;
    }
}
