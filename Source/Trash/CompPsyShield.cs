using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace tsoa.core
{
    [StaticConstructorOnStartup]
    public class CompPsyShield : ThingComp
    {
        private Vector3 impactAngleVect;

        protected int lastKeepDisplayTick = -9999;

        private int lastAbsorbDamageTick = -9999;

        private int ticksUntilReset = 0;

        private int KeepDisplayingTicks = 1000;

        private static readonly Material PsyBubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, Color.green);

        public CompProperties_PsyShield Props => (CompProperties_PsyShield)props;

        // roughly based on CompShield
        public ShieldState ShieldState
        {
            get
            {
                if (ticksUntilReset > 0)
                {
                    return ShieldState.Resetting;
                } 
                return ShieldState.Active;
            }
        }

        // copied from CompShield
        protected bool ShouldDisplay
        {
            get
            {
                Pawn pawnOwner = PawnOwner;
                if (!pawnOwner.Spawned || pawnOwner.Dead || pawnOwner.Downed || pawnOwner.GetPsylinkLevel() <= 0)
                {
                    return false;
                }
                if (pawnOwner.InAggroMentalState)
                {
                    return true;
                }
                if (pawnOwner.Drafted)
                {
                    return true;
                }
                if (pawnOwner.Faction.HostileTo(Faction.OfPlayer) && !pawnOwner.IsPrisoner)
                {
                    return true;
                }
                if (Find.TickManager.TicksGame < lastKeepDisplayTick + KeepDisplayingTicks)
                {
                    return true;
                }
                if (ModsConfig.BiotechActive && pawnOwner.IsColonyMech && Find.Selector.SingleSelectedThing == pawnOwner)
                {
                    return true;
                }
                return false;
            }
        }

        // copied from CompShield
        protected Pawn PawnOwner
        {
            get
            {
                if (parent is Apparel apparel)
                {
                    return apparel.Wearer;
                }
                if (parent is Pawn result)
                {
                    return result;
                }
                return null;
            }
        }

        public bool IsApparel => parent is Apparel;

        private bool IsBuiltIn => !IsApparel;

        // copied from CompShield
        public void KeepDisplaying()
        {
            lastKeepDisplayTick = Find.TickManager.TicksGame;
        }

        // copied from CompShield
        private void AbsorbedDamage(DamageInfo dinfo)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(PawnOwner.Position, PawnOwner.Map));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            Vector3 loc = PawnOwner.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
            float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
            FleckMaker.Static(loc, PawnOwner.Map, FleckDefOf.ExplosionFlash, num);
            int num2 = (int)num;
            for (int i = 0; i < num2; i++)
            {
                FleckMaker.ThrowDustPuff(loc, PawnOwner.Map, Rand.Range(0.8f, 1.2f));
            }
            lastAbsorbDamageTick = Find.TickManager.TicksGame;
            KeepDisplaying();
        }

        // copied from CompShield with edits
        private void Break()
        {
            if (parent.Spawned)
            {
                float scale = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, PawnOwner.psychicEntropy.EntropyRelativeValue);
                EffecterDefOf.Shield_Break.SpawnAttached(parent, parent.MapHeld, scale);
                FleckMaker.Static(PawnOwner.TrueCenter(), PawnOwner.Map, FleckDefOf.ExplosionFlash, 12f);
                for (int i = 0; i < 6; i++)
                {
                    FleckMaker.ThrowDustPuff(PawnOwner.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), PawnOwner.Map, Rand.Range(0.8f, 1.2f));
                }
            }
            ticksUntilReset = Props.resetDelayTicks;
        }

        private void Reset()
        {
            if (PawnOwner.Spawned)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(PawnOwner.Position, PawnOwner.Map));
                FleckMaker.ThrowLightningGlow(PawnOwner.TrueCenter(), PawnOwner.Map, 3f);
            }
            ticksUntilReset = 0;
        }

        // copied from CompShield
        public override void CompDrawWornExtras()
        {
            base.CompDrawWornExtras();
            if (IsApparel)
            {
                Draw();
            }
        }

        // copied from CompShield
        public override void PostDraw()
        {
            base.PostDraw();
            if (IsBuiltIn)
            {
                Draw();
            }
        }

        private void Draw()
        {
            if (ShieldState == ShieldState.Active && ShouldDisplay)
            {
                float num = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, (1 - PawnOwner.psychicEntropy.EntropyRelativeValue));
                Vector3 drawPos = PawnOwner.Drawer.DrawPos;
                drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                int num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
                if (num2 < 8)
                {
                    float num3 = (float)(8 - num2) / 8f * 0.05f;
                    drawPos += impactAngleVect * num3;
                    num -= num3;
                }
                float angle = Rand.Range(0, 360);
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, PsyBubbleMat, 0);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (ticksUntilReset > 0 && PawnOwner.psychicEntropy.EntropyRelativeValue < Props.resetHeatPercent)
            {
                ticksUntilReset--;
                if (ticksUntilReset <= 0)
                {
                    Reset();
                }
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (ShieldState != ShieldState.Active || PawnOwner == null || PawnOwner.psychicEntropy == null || PawnOwner.GetPsylinkLevel() <= 0)
                return;

            float damage = dinfo.Amount;
            float heatToAdd = damage * Props.heatPerDamage;

            if (PawnOwner.psychicEntropy.EntropyValue + heatToAdd >= PawnOwner.psychicEntropy.MaxEntropy)
            {
                heatToAdd = PawnOwner.psychicEntropy.MaxEntropy - PawnOwner.psychicEntropy.EntropyValue;
                Break();
            }
            else
            {
                AbsorbedDamage(dinfo);
            }

            absorbed = true;

            PawnOwner.psychicEntropy.TryAddEntropy(heatToAdd, null, false);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ticksUntilReset, "ticksUntilReset", 0);
            Scribe_Values.Look(ref lastKeepDisplayTick, "lastKeepDisplayTick", 0);
        }
    }
}
