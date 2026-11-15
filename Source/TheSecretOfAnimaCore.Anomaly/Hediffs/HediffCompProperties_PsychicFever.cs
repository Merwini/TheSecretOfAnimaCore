using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

public class HediffCompProperties_PsychicFever : HediffCompProperties
{
    public float hoursToLethal = 6f;
    public float severityAfterTend = 0f;

    public float durationDays = -1f;
    public FloatRange durationDaysRange;

    public HediffCompProperties_PsychicFever()
    {
        compClass = typeof(HediffComp_PsychicFever);
    }
}