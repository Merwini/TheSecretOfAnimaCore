using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core;

public class GenStep_AnomalyAmbush : GenStep_Ambush
{
    public override int SeedPart => 186892493;

    public override RectTrigger MakeRectTrigger()
    {
        RectTrigger rectTrigger = base.MakeRectTrigger();
        rectTrigger.activateOnExplosion = true;
        return rectTrigger;
    }

    public override SignalAction_Ambush MakeAmbushSignalAction(CellRect rectToDefend, IntVec3 root, GenStepParams parms)
    {
        SignalAction_AnomalyAmbush signalAction_AnomalyAmbush = (SignalAction_AnomalyAmbush)ThingMaker.MakeThing(TSOA_DefOf.TSOA_SignalAction_AnomalyAmbush);
        if (parms.sitePart != null)
        {
            signalAction_AnomalyAmbush.points = parms.sitePart.parms.threatPoints;
        }
        else
        {
            signalAction_AnomalyAmbush.points = defaultPointsRange.RandomInRange;
        }

        if (root.IsValid)
        {
            signalAction_AnomalyAmbush.spawnNear = root;
        }
        else
        {
            signalAction_AnomalyAmbush.spawnAround = rectToDefend;
        }
        return signalAction_AnomalyAmbush;
    }
}
