using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class CompProperties_PsyShield : CompProperties
{
    public float heatPerDamage = 1f;

    public float resetHeatPercent = 0.8f;

    public int resetDelayTicks = 120;

    public float minDrawSize = 1.2f;

    public float maxDrawSize = 1.55f;

    public Dictionary<QualityCategory, float> qualityHeatMultipliers = new Dictionary<QualityCategory, float>
    {
        { QualityCategory.Awful, 1.2f },
        { QualityCategory.Poor, 1.1f },
        { QualityCategory.Normal, 1f },
        { QualityCategory.Good, 0.9f },
        { QualityCategory.Excellent, 0.8f },
        { QualityCategory.Masterwork, 0.65f },
        { QualityCategory.Legendary, 0.5f }
    };

    public CompProperties_PsyShield()
    {
        this.compClass = typeof(CompPsyShield);
    }
}
