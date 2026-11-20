using System.Collections.Generic;
using RimWorld;
using Verse;

namespace tsoa.core.anomaly;

public class CompVoidGateTransporter : CompTransporter
{
    // This just exists so I can reuse everything in CompTransporter except the gizmos
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield break;
    }
}
