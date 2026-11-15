using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using LudeonTK;
using tsoa.core;

namespace tsoa.core;

public class Hediff_Virion : Hediff
{


    private int ticksUntilComplete;
    public int TicksUntilComplete => ticksUntilComplete;
    public int TicksToGestate => (int)(Extension.gestationDays * GenDate.TicksPerDay);
    public bool IsFullyGestated => ticksUntilComplete <= 0;

    public bool ShouldTick => !pawn.Suspended; // I dunno if I'll ever need to handle modded reasons a pawn shouldn't have its gestation ticking

    private int ticksUntilNextActivity;
    public int TicksUntilNextActivity => ticksUntilNextActivity;
    private bool virionActive = false;
    public int RandomTicksUntilNextVirionActivity => (int)(Extension.virionActivityDaysRange.RandomInRange * GenDate.TicksPerDay);
    public float TendQualityRequirement => Extension.tendQualityRequirement;

    private bool extracting = false; // doesn't need saving because it is set and read in the same method call
    private bool ShouldSpawnItem => IsFullyGestated; // If pawn dies with item fully gestated, be nice and give them the item instead of an Entity

    internal RecipeDef virionRecipeDef;

    internal VirionExtension extension;
    internal VirionExtension Extension
    {
        get
        {
            if (extension == null)
            {
                extension = virionRecipeDef?.GetModExtension<VirionExtension>();
            }
            return extension;
        }
    }

    public bool HasQuality
    {
        get
        {
            return Extension.producedItem.HasComp<CompQuality>();
        }
    }

    public override string GetInspectString()
    {
        if (DebugSettings.godMode)
        {
            return $"{TicksUntilComplete} ticks until fully gestated";
        }
        return null;
    }

    public override string LabelInBrackets
    {
        get
        {
            string stageLabel = IsFullyGestated ? "TSOA_CompleteStage".Translate() : "TSOA_GestatingStage".Translate();
            string progress = IsFullyGestated ? "" :  (1f - (float)TicksUntilComplete / TicksToGestate).ToStringPercent();

            return $"{Extension.producedItem.label} - {stageLabel} {progress}";
        }
    }

    public override bool ShouldRemove => false;


    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);

        if (Extension == null)
        {
            Log.Error("Added Hediff_Virion, but implanted Virion did not have valid extension. Removing it.");
            pawn.health.RemoveHediff(this);
            return;
        }

        SetCompletionTicks();
        SetNextActivityTick();
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (IsFullyGestated || !ShouldTick)
            return;

        ticksUntilComplete -= delta;

        if (TicksUntilComplete <= 0)
        {
            CompleteGestation();
        }

        if (!virionActive)
        {
            ticksUntilNextActivity -= delta;

            if (TicksUntilNextActivity <= 0)
            {
                StartVirionActivity();
            }
        }
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        if (!pawn.SpawnedOrAnyParentSpawned)
        {
            Messages.Message("TSOA_VirionNotSpawned".Translate(pawn.LabelCap), MessageTypeDefOf.NegativeHealthEvent);
            pawn.health.RemoveHediff(this);
        }
        else
        {
            DoEscapeOrExtraction();
        }
    }

    public override void PostRemoved()
    {
        base.PostRemoved();

        RemoveActivityHediff();
    }

    public void Notify_VirionExtracted()
    {
        extracting = true;
        DoEscapeOrExtraction();
    }

    // called after stage has already advanced. leftover will be a negative number, from delta ticks > 1 overshooting the end of a stage
    private void SetCompletionTicks()
    {
        ticksUntilComplete = TicksToGestate;
    }

    private void SetNextActivityTick()
    {
        ticksUntilNextActivity = RandomTicksUntilNextVirionActivity;
    }

    private void StartVirionActivity()
    {
        Hediff_VirionActivity existingActivity = pawn.health.hediffSet.GetFirstHediffOfDef(TSOA_HediffDefOf.TSOA_VirionActiveHediff) as Hediff_VirionActivity;
        if (existingActivity != null)
        {
            virionActive = true;
            Log.Error("Hediff_Virion: tried to start virion activity but one already exists on pawn " + pawn.Name);
            return;
        }

        Hediff_VirionActivity hediff = HediffMaker.MakeHediff(TSOA_HediffDefOf.TSOA_VirionActiveHediff, pawn) as Hediff_VirionActivity;
        hediff.tendQualityRequirement = TendQualityRequirement;
        hediff.virionDamageDaysRange = Extension.virionDamageDaysRange;
        pawn.health.AddHediff(hediff);

        virionActive = true;

        // TODO should be a letter instead?
        Messages.Message("TSOA_VirionActiveMessage".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeHealthEvent);
    }

    public void Notify_ActivityEnded()
    {
        virionActive = false;
        if (!IsFullyGestated)
        {
            SetNextActivityTick();
        }
    }

    public void CompleteGestation()
    {
        ticksUntilNextActivity = int.MaxValue;
        if (virionActive)
        {
            RemoveActivityHediff();
            virionActive = false;
        }
    }

    private void DoEscapeOrExtraction()
    {
        Map map = pawn.MapHeld;
        IntVec3 pos = pawn.PositionHeld;

        DoEmergingEffects(map, pos);
        SpawnResult(map, pos);

        pawn.health.RemoveHediff(this);
        if (Extension.alwaysKillsHost)
        {
            HealthUtility.DamageUntilDead(pawn); // if the player finds a way to cheat death, so be it 
        }
    }

    private void DoEmergingEffects(Map map, IntVec3 pos)
    {
        EffecterDefOf.MetalhorrorEmerging.Spawn(pos, map).Cleanup();
        CellRect cellRect = CellRect.CenteredOn(pos, 2);
        for (int i = 0; i < 5; i++)
        {
            IntVec3 randomCell = cellRect.RandomCell;
            if (randomCell.InBounds(map) && GenSight.LineOfSight(randomCell, pos, map))
            {
                FilthMaker.TryMakeFilth(randomCell, map, (i % 2 == 0) ? ThingDefOf.Filth_Blood : ThingDefOf.Filth_GrayFlesh);
            }
        }
        SoundDefOf.Crunch.PlayOneShot(new TargetInfo(pos, map));
    }

    private void SpawnResult(Map map, IntVec3 pos)
    {
        if (ShouldSpawnItem)
        {
            SpawnItem(map, pos);
        }
        else
        {
            SpawnEntity(map, pos);
        }
    }

    private void SpawnItem(Map map, IntVec3 pos)
    {
        ThingDef def = Extension.producedItem;
        Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? Extension.productStuff : null);
        SetThingQuality(thing);
        thing.stackCount = Extension.producedCount;
        GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
        string message;
        if (extracting)
        {
            if (Extension.alwaysKillsHost)
            {
                message = "TSOA_VirionExtractedKilled".Translate(pawn.LabelCap);
            }
            else
            {
                message = "TSOA_VirionExtractedAlive".Translate(pawn.LabelCap);
            }
        }
        else
        {
            message = "TSOA_VirionDiedComplete".Translate(pawn.LabelCap);
        }
        Messages.Message(message, new LookTargets(pos, map), MessageTypeDefOf.PositiveEvent);
    }

    private void SpawnEntity(Map map, IntVec3 pos)
    {
        Pawn entity;

        VirionSpawnEntry spawnEntry = Extension.entitySpawn != null ? Extension.entitySpawn : VirionExtension.MetalhorrorDefault;

        for (int i = 0; i < spawnEntry.count; i++)
        {
            entity = PawnGenerator.GeneratePawn(new PawnGenerationRequest(spawnEntry.kind, Faction.OfEntities));
            if (spawnEntry.forcedLifeStageIndex >= 0)
            {
                entity.ageTracker.LockCurrentLifeStageIndex(spawnEntry.forcedLifeStageIndex);
            }
            GenSpawn.Spawn(entity, pos, map);
        }

        Messages.Message("TSOA_VirionIncomplete".Translate(pawn.LabelCap), new LookTargets(pos, map), MessageTypeDefOf.NegativeHealthEvent);
    }

    // Determine quality of product based on host's highest skill level, as if they were crafting the item using that skill. Non-mutant hosts get 1 higher quality, so Legendary is possible
    private void SetThingQuality(Thing thing)
    {
        if (!HasQuality)
            return;

        CompQuality comp = thing.TryGetComp<CompQuality>();
        if (comp == null)
            return;

        int highestHostSkill = 0;
        foreach (var skill in pawn.skills.skills)
        {
            int amt = skill.Level;
            if (amt > highestHostSkill)
            {
                highestHostSkill = amt;
            }
        }

        var quality = QualityUtility.GenerateQualityCreatedByPawn(highestHostSkill, false);
        if (!pawn.IsMutant)
            quality++;

        comp.SetQuality(quality, null);
    }

    public void RemoveActivityHediff()
    {
        Hediff_VirionActivity activity = pawn.health.hediffSet.GetFirstHediffOfDef(TSOA_HediffDefOf.TSOA_VirionActiveHediff) as Hediff_VirionActivity;
        if (activity != null)
        {
            pawn.health.RemoveHediff(activity);
        }
        virionActive = false;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (!DebugSettings.godMode)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "Dev: Advanced gestation time by 1 day",
            action = () =>
            {
                ticksUntilComplete -= GenDate.TicksPerDay;
            }
        };

        yield return new Command_Action
        {
            defaultLabel = "Dev: Virion activity next tick",
            action = () =>
            {
                ticksUntilNextActivity = 1;
            }
        };

        yield return new Command_Action
        {
            defaultLabel = "Dev: Log ticks until next activity",
            action = () =>
            {
                Log.Message("Ticks until next virion activity: " + TicksUntilNextActivity);
            }
        };
    }

    // keeping this here just to prevent errors in existing saves
    private int ticksUntilNextStage;
    private int curStageIndex;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref virionRecipeDef, "virionRecipeDef");
        Scribe_Values.Look(ref ticksUntilComplete, "ticksUntilComplete", 0);

        Scribe_Values.Look(ref ticksUntilNextActivity, "ticksUntilNextActivity", 0);

        // to not break existing saves
        Scribe_Values.Look(ref ticksUntilNextStage, "ticksUntilNextStage", 0);
        Scribe_Values.Look(ref curStageIndex, "curStageIndex", 0);
        
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            virionActive = pawn.health.hediffSet.HasHediff(TSOA_HediffDefOf.TSOA_VirionActiveHediff);

            // fix ticks if player had a virion in progress. Will just act as if current stage duration is completion duration
            bool wasComplete = HasQuality ? curStageIndex == 7 : curStageIndex == 1;
            if (wasComplete)
            {
                ticksUntilComplete = 0;
            }
            else if (ticksUntilNextStage != 0)
            {
                ticksUntilComplete = ticksUntilNextStage > TicksToGestate ? TicksToGestate : ticksUntilNextStage;
                ticksUntilNextStage = 0;
            }
        }
    }
}
