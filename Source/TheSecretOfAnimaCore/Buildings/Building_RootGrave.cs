using RimWorld;
using System.Collections.Generic;
using Verse;

namespace tsoa.core;

public class Building_RootGrave : Building_Grave
{
    private const int ConsumeTicks = 60000; // 1 day, TODO balance
    protected const float ProgressPerTick = 0.00000666666f; // 10% of meditation tick, //TODO balance

    public virtual float ConsumeRate => ProgressPerTick;

    private float fractionalDamage;

    // Comp on the grave
    private CompGroupedFacility cachedCompGroupFac;
    public CompGroupedFacility CachedCompGroupFac
    {
        get
        {
            if (cachedCompGroupFac == null)
            {
                cachedCompGroupFac = this.TryGetComp<CompGroupedFacility>();
                if (cachedCompGroupFac == null)
                {
                    Log.Error($"Misconfigured Building_RootGrave. Building: {this.def.defName} from mod: {this.def.modContentPack.PackageId}. Building_RootGrave requires CompProperties_GroupedFacility.");
                    return null;
                }
            }

            return cachedCompGroupFac;
        }
    }

    private Thing cachedLinkedTree;
    public Thing CachedLinkedTree
    {
        get
        {
            if (cachedLinkedTree != null && (cachedLinkedTree.Destroyed || !cachedLinkedTree.Spawned))
            {
                InvalidateCaches();
            }

            if (cachedLinkedTree == null)
            {
                TryRebuildCaches();
            }

            return cachedLinkedTree;
        }
    }

    private CompSpecialMeditationFocus_Anima cachedCompFocus;
    public CompSpecialMeditationFocus_Anima CachedCompFocus
    {
        get
        {
            Thing tree = CachedLinkedTree;
            if (tree != null && (tree.Destroyed || !tree.Spawned || !CachedCompGroupFac.IsLinked(tree)))
            {
                InvalidateCaches();
            }

            //should have been run in CachedLinkedTree, but doesn't hurt much to recheck here
            if (cachedCompFocus == null)
            {
                TryRebuildCaches();
            }

            return cachedCompFocus;
        }
    }

    private Corpse cachedCorpse; // cached so I know when to break psychic sensitivity cache, also slightly cheaper to reference

    private float cachedCorpsePsychicSensitivity = -1f;
    public float CorpsePsychicSensitivity
    {
        get
        {
            if (innerContainer.NullOrEmpty()) // maybe Corpse == null instead? Which is the cheaper call?
            {
                cachedCorpsePsychicSensitivity = -1;
                return 0;
            }

            if (cachedCorpsePsychicSensitivity == -1 || cachedCorpse != Corpse)
            {
                cachedCorpse = Corpse;
                cachedCorpsePsychicSensitivity = cachedCorpse.InnerPawn.GetStatValue(StatDefOf.PsychicSensitivity);
            }

            return cachedCorpsePsychicSensitivity;
        }
    }
    public override void TickRare()
    {
        base.TickRare();
        TickInterval(250);
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (Corpse == null)
        {
            return;
        }

        // This is cheaper than letting it try to get CachedCompGroupFocus and having it then try to recache and fail
        if (CachedCompGroupFac.LinkedThings.NullOrEmpty())
        {
            return;
        }

        CompSpecialMeditationFocus_Anima compFocus = CachedCompFocus;
        if (compFocus != null)
        {
            float progress = CorpsePsychicSensitivity * ConsumeRate * delta;
            compFocus.AddExternalProgress(progress);

            fractionalDamage += ((float)delta / ConsumeTicks) * Corpse.MaxHitPoints;
            while (fractionalDamage >= 1f)
            {
                Corpse.HitPoints -= 1;
                fractionalDamage -= 1;
            }
        }

        if (Corpse.HitPoints <= 0)
        {
            DestroyCorpse();
        }
    }

    void DestroyCorpse()
    {
        cachedCorpse = null;
        cachedCorpsePsychicSensitivity = -1f;
        Corpse corpse = Corpse;
        if (corpse != null)
        {
            innerContainer.Remove(corpse);
            corpse.Destroy();
        }
        FleckMaker.ThrowLightningGlow(this.TrueCenter(), this.Map, 1.5f);
        this.DirtyMapMesh(Map);
    }

    void InvalidateCaches()
    {
        cachedLinkedTree = null;
        cachedCompFocus = null;
    }

    void TryRebuildCaches()
    {
        if (CachedCompGroupFac.LinkedThings.NullOrEmpty())
            return;

        List<Thing> linkedThings = CachedCompGroupFac.LinkedThings;
        for (int i = 0; i < linkedThings.Count; i++)
        {
            // TODO check for some custom tag? Want to later implement multiple anima tree growth stages with separate ThingDefs
            CompSpecialMeditationFocus_Anima compFocus = linkedThings[i].TryGetComp<CompSpecialMeditationFocus_Anima>();
            if (compFocus != null)
            {
                cachedLinkedTree = linkedThings[i];
                cachedCompFocus = compFocus;
                break;
            }
        }
    }

    public override void ExposeData()
    {
        // Don't bother saving cached values, they can recache on first tick
        Scribe_Values.Look(ref fractionalDamage, "fractionalDamage", 0);
        base.ExposeData();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        if (DebugSettings.godMode && Corpse != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Set corpse hit points to 1",
                defaultDesc = "Sets corpse's hit points to 1.",
                action = () =>
                {
                    Corpse.HitPoints = 1;
                }
            };
        }
    }
}
