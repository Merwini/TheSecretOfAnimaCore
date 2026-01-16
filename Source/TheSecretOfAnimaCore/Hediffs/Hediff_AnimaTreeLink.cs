using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace tsoa.core
{
    public class Hediff_AnimaTreeLink : Hediff
    {
        private const int CheckInterval = 120;

        private Thing animaTree;

        public Thing AnimaTree
        {
            get; set; // not sure if I'll ever need custom logic, maybe just make animaTree public?
        }

        private float cachedBonus = 0;

        public override bool ShouldRemove => animaTree == null || animaTree.Destroyed || !animaTree.Spawned;

        private HediffStage curStage;

        public override HediffStage CurStage
        {
            get
            {
                if (curStage == null && cachedBonus > 0)
                {
                    StatModifier statModifier = new StatModifier();
                    statModifier.stat = StatDefOf.PsychicSensitivity;
                    statModifier.value = cachedBonus;
                    curStage = new HediffStage();
                    curStage.statOffsets = new List<StatModifier> { statModifier };
                }
                return curStage;
            }
        }

        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (pawn.IsHashIntervalTick(CheckInterval, delta))
            {
                RecacheBonus();
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            RecacheBonus();
        }

        public void RecacheBonus()
        {
            float old = cachedBonus;
            cachedBonus = 0;

            CompSpawnSubplant compSubplant = AnimaTree?.TryGetComp<CompSpawnSubplant>();
            CompAnimaTreePawnLink compAnimaTreePawnLink = AnimaTree?.TryGetComp<CompAnimaTreePawnLink>();
            if (compSubplant != null && compAnimaTreePawnLink != null)
            {
                if (!compAnimaTreePawnLink.linkedPawns.NullOrEmpty() && compAnimaTreePawnLink.linkedPawns.Contains(pawn)) // maybe 
                {
                    float num2 = compSubplant.SubplantsForReading.Count;
                    num2 /= compAnimaTreePawnLink.linkedPawns.Count;
                    num2 *= compAnimaTreePawnLink.Props.psychicSensitivityPerSubplant;
                    cachedBonus = num2;
                }
            }
            if (!Mathf.Approximately(old, cachedBonus))
            {
                curStage = null;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cachedBonus, "cachedBonus", 0);
            Scribe_References.Look(ref animaTree, "animaTree");
        }
    }
}
