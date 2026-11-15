using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace tsoa.core;

public class CompProperties_EmptyStateGraphicStuffed : CompProperties_EmptyStateGraphic
{
    public CompProperties_EmptyStateGraphicStuffed()
    {
        compClass = typeof(CompEmptyStateGraphicStuffed);
    }
}
