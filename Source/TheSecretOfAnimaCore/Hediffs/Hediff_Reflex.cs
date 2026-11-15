using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace tsoa.core;

public class Hediff_Reflex : HediffWithComps
{
    public const int DurationTicks = 3600;

    public void RefreshDuration()
    {
        GetComp<HediffComp_Disappears>().SetDuration(DurationTicks);
    }
}
