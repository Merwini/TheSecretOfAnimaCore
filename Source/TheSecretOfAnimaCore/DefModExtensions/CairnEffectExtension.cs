using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace tsoa.core;

public class CairnEffectExtension : DefModExtension
{
    public List<StatModifier> statOffsets;
    public List<StatModifier> statFactors;

    public int ticksToApplyEffect = 2500;
}
