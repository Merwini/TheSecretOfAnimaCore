using System;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace tsoa.core;

public class StatWorker_EntropyDamageDisplay : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        ThingDef def = null;
        if (req.HasThing)
            def = req.Thing.def;
        else if (req.Def is ThingDef)
            def = req.Def as ThingDef;

        if (def == null) return false;

        EntropyDamageExtension extension = def.GetModExtension<EntropyDamageExtension>();

        if (extension == null) return false;

        if (stat == TSOAA_DefOf.TSOA_EntropyDamagePercent)
            return extension.entropyCountedPercent > 0;

        if (stat == TSOAA_DefOf.TSOA_EntropyDamageFlat)
            return extension.entropyCountedPercent <= 0 && extension.entropyCountedFlat > 0;

        return false;
    }

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        ThingDef def = null;
        if (req.HasThing)
            def = req.Thing.def;
        else if (req.Def is ThingDef)
            def = req.Def as ThingDef;

        if (def == null) return 0f;

        if (def.GetModExtension<EntropyDamageExtension>() is EntropyDamageExtension extension)
                return extension.entropyCountedPercent > 0 ? extension.entropyCountedPercent : extension.entropyCountedFlat * extension.damagePerEntropy;

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

        EntropyDamageExtension extension = def.GetModExtension<EntropyDamageExtension>();
        if (extension == null) return "[TSOA] something has gone wrong, this should not be visible.";

        if (extension.entropyCountedPercent != 0)
        {
            return "TSOA_EntropyDamageWorkerExplanationPercent".Translate(
                extension.entropyCountedPercent.ToStringPercent(), 
                extension.entropySpentPercent.ToStringPercent(), 
                extension.damagePerEntropy);
        }
        else
        {
            bool removes = extension.entropySpentFlat > 0;
            return "TSOA_EntropyDamageWorkerExplanationFlat".Translate(
                removes ? "TSOA_Removes".Translate() : "TSOA_Adds".Translate(),
                removes ? "TSOA_From".Translate() : "TSOA_To".Translate(),
                extension.entropyCountedFlat * extension.damagePerEntropy,
                Math.Abs(extension.entropySpentFlat)
                );
        }
    }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        return null;
    }
}
