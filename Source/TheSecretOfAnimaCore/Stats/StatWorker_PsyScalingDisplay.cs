using System;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace tsoa.core;

class StatWorker_PsyScalingDisplay : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        if (req.HasThing)
            return req.HasThing && req.Thing.def.statBases.StatListContains(TSOAA_DefOf.TSOA_PsyScaling);

        if (req.Def is ThingDef thingDef)
            return thingDef.statBases != null && thingDef.statBases.StatListContains(TSOAA_DefOf.TSOA_PsyScaling);

        return false;
    }

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        if (req.HasThing)
            return req.Thing.def.statBases.GetStatValueFromList(TSOAA_DefOf.TSOA_PsyScaling, 0f);

        if (req.Def is ThingDef thingDef)
            return thingDef.statBases?.GetStatValueFromList(TSOAA_DefOf.TSOA_PsyScaling, 0f) ?? 0f;

        return 0f;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        ThingDef def;
        if (req.HasThing)
            def = req.Thing.def;
        else
            def = req.Def as ThingDef;

        float scale = def.statBases.GetStatValueFromList(TSOAA_DefOf.TSOA_PsyScaling, 0);

        return "TSOA_PsyScalingWorkerExplanation".Translate(scale.ToStringPercent());
    }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        return null;
    }
}
