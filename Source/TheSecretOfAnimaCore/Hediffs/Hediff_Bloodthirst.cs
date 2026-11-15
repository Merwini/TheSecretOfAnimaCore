using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace tsoa.core;

public class Hediff_Bloodthirst : HediffWithComps
{
    private int killCount = 0;

    private HediffStage curStage;
    public override HediffStage CurStage
    {
        get
        {
            if (curStage == null)
            {
                float factor = killCount * -0.05f;

                curStage = new HediffStage();
                curStage.statOffsets = new List<StatModifier>()
                {
                    new StatModifier()
                    {
                        stat = StatDefOf.IncomingDamageFactor,
                        value = factor
                    },
                    new StatModifier()
                    {
                        stat = StatDefOf.MeleeCooldownFactor,
                        value = factor
                    },
                    new StatModifier()
                    {
                        stat = StatDefOf.RangedCooldownFactor,
                        value = factor
                    }
                };
            }
            return curStage;
        }
    }

    private HediffComp_Disappears disappears;
    public HediffComp_Disappears Disappears
    {
        get
        {
            if (disappears == null)
            {
                disappears = GetComp<HediffComp_Disappears>();
            }
            return disappears;
        }
    }

    public void AddKill()
    {
        killCount = Math.Clamp(killCount + 1, 0, 10);
        curStage = null;
        Disappears.ResetElapsedTicks();
    }

    public override string LabelInBrackets => "TSOA_BloodthirstLabel".Translate(killCount);

    // TODO maybe some sort of visual effect that increases with kill count

    public override void ExposeData()
    {
        Scribe_Values.Look(ref killCount, "killCount", 0);

        base.ExposeData();
    }
}
