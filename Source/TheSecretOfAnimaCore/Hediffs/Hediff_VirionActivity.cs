using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace tsoa.core;

public class Hediff_VirionActivity : Hediff
{
    // Separating this into its own hediff because Hediff_Virion's Hediff entry in Health tab is already crowded enough. Just no way to also have it tell the player about Activity

    public const float damageTimeFactor = 0.9f; // need to balance. maybe 0.95?
    public const float minimumTimeFactor = 0.25f;

    public float tendQualityRequirement; // set by Hediff_Virion before add
    public FloatRange virionDamageDaysRange; // set by Hediff_Virion before add

    private int ticksUntilNextDamage;
    public int TicksUntilNextDamage => ticksUntilNextDamage;
    private float timeFactor = 1f;

    private Hediff_Virion originalHediff;
    public Hediff_Virion OriginalHediff
    {
        get
        {
            if (originalHediff == null)
            {
                originalHediff = pawn.health.hediffSet.GetFirstHediffOfDef(TSOAA_DefOf.TSOA_VirionHediff) as Hediff_Virion;
            }
            return originalHediff;
        }
    }


    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        SetTicksUntilNextDamage();
    }

    public override void PostRemoved()
    {
        base.PostRemoved();
        Hediff_Virion originalHediff = pawn.health.hediffSet.GetFirstHediffOfDef(TSOAA_DefOf.TSOA_VirionHediff) as Hediff_Virion;
        originalHediff?.Notify_ActivityEnded();
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (OriginalHediff == null)
        {
            Log.Error($"Pawn {pawn.Name} had Hediff_VirionActivity but no Hediff_Virion");
            pawn.health.RemoveHediff(this);
            return;
        }

        if (!OriginalHediff.ShouldTick)
        {
            return;
        }

        ticksUntilNextDamage -= delta;

        if (TicksUntilNextDamage <= 0)
        {
            DoVirionDamage();
            if (!pawn.Dead)
            {
                timeFactor = Mathf.Clamp(timeFactor * damageTimeFactor, minimumTimeFactor, 1);
                SetTicksUntilNextDamage();
            }
        }
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        base.Notify_PawnDied(dinfo, culprit);
        pawn.health.RemoveHediff(this);
    }

    public override bool TendableNow(bool ignoreTimer = false)
    {
        return true;
    }

    public override void Tended(float quality, float maxQuality, int batchPosition = 0)
    {
        bool success = false;
        if (quality >= tendQualityRequirement)
        {
            Messages.Message("TSOA_VirionTendSuccessMessage".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
            success = true;
            pawn.health.RemoveHediff(this);
        }
        else
        {
            Messages.Message("TSOA_VirionTendFailureMessage".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeHealthEvent);
            success = false;
        }

        if (pawn.Spawned)
        {
            if (success)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TSOA_VirionTendSuccessMote".Translate());
            }
            else
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TSOA_VirionTendFailureMote".Translate(quality.ToStringPercent(), tendQualityRequirement.ToStringPercent()), Color.red);
            }
        }
    }

    //private void DoVirionDamage()
    //{
    //    if (!pawn.SpawnedOrAnyParentSpawned)
    //    {
    //        return;
    //    }

    //    EffecterDefOf.MeatExplosionSmall.Spawn(pawn.Position, pawn.Map).Cleanup();
    //    SoundDefOf.Execute_Cut.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

    //    BodyPartRecord torso = pawn.RaceProps.body.corePart;
    //    if (torso == null)
    //    {
    //        return;
    //    }

    //    List<BodyPartRecord> candidates = new List<BodyPartRecord>();
    //    foreach (BodyPartRecord part in torso.GetDirectChildParts())
    //    {
    //        if (part.depth == BodyPartDepth.Inside)
    //        {
    //            candidates.Add(part);
    //        }
    //    }

    //    BodyPartRecord targetPart;

    //    if (candidates.Count > 0 && Rand.Bool) // 50% chance to hit torso vs a random organ
    //    {
    //        targetPart = candidates.RandomElement();
    //    }
    //    else
    //    {
    //        targetPart = torso;
    //    }

    //    float damageAmount = Rand.Range(4f, 10f); // organs have 15-20 hitPoints, on average fatal damage will happen if the same organ is hit 3 times, but could happen in 2

    //    DamageInfo dinfo = new DamageInfo(
    //        DamageDefOf.Cut,
    //        damageAmount,
    //        0f,
    //        -1f,
    //        instigator: null,
    //        hitPart: targetPart
    //    );

    //    pawn.TakeDamage(dinfo);
    //}

    private void DoVirionDamage()
    {
        if (pawn.SpawnedOrAnyParentSpawned)
        {
            Map map = pawn.MapHeld;
            IntVec3 pos = pawn.PositionHeld;

            EffecterDefOf.MeatExplosionSmall.Spawn(pos, map).Cleanup();
            SoundDefOf.Execute_Cut.PlayOneShot(new TargetInfo(pos, map));
        }

        BodyPartRecord targetPart = ChooseTargetPart();
        if (targetPart == null)
            return;

        float damageAmount = Rand.Range(4f, 10f);

        Hediff_Injury injury = (Hediff_Injury)HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, targetPart);
        injury.Severity = damageAmount;
        pawn.health.AddHediff(injury, targetPart);
    }

    private void SetTicksUntilNextDamage()
    {
        ticksUntilNextDamage = (int)(virionDamageDaysRange.RandomInRange * GenDate.TicksPerDay * timeFactor);
    }

    private BodyPartRecord ChooseTargetPart()
    {
        BodyPartRecord torso = pawn.RaceProps.body.corePart;
        if (torso == null)
        {
            return null;
        }

        List<BodyPartRecord> candidates = new List<BodyPartRecord>();
        foreach (BodyPartRecord part in torso.GetDirectChildParts())
        {
            if (part.depth == BodyPartDepth.Inside && !pawn.health.hediffSet.PartIsMissing(part))
            {
                candidates.Add(part);
            }
        }

        BodyPartRecord targetPart;

        if (candidates.Count > 0 && Rand.Bool) // 50% chance to hit torso vs a random organ
        {
            targetPart = candidates.RandomElement();
        }
        else
        {
            targetPart = torso;
        }

        return targetPart;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (!DebugSettings.godMode)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "Dev: Virion damage next tick",
            action = () =>
            {
                ticksUntilNextDamage = 1;
            }
        };

        yield return new Command_Action
        {
            defaultLabel = "Dev: Log ticks until next damage",
            action = () =>
            {
                Log.Message("Ticks until next virion damage: " + TicksUntilNextDamage);
            }
        };
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksUntilNextDamage, "ticksUntilNextDamage", 0);
        Scribe_Values.Look(ref timeFactor, "timeFactor", 1f);
        Scribe_Values.Look(ref tendQualityRequirement, "tendQualityRequirement", 0f);
        Scribe_Values.Look(ref virionDamageDaysRange, "virionDamageDaysRange");
    }
}
