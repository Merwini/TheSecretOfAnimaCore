using RimWorld;
using System.Collections.Generic;
using Verse;

namespace nuff.tsoa.core
{
    public class Building_RootGrave : Building_Grave
    {
        private const int ConsumeTicks = 60000; // 1 day, TODO balance
        private const float ProgressPerTick = 0.00000666666f; // 10% of meditation tick, //TODO balance

        private float fractionalDamage;

        private Thing cachedLinkedTree;
        public Thing LinkedTree
        {
            get
            {
                if (cachedLinkedTree != null && (cachedLinkedTree.Destroyed || !cachedLinkedTree.Spawned))
                {
                    cachedLinkedTree = null;
                    cachedComp = null;
                }

                if (cachedLinkedTree == null)
                {
                    CompGroupedFacility compFac = this.TryGetComp<CompGroupedFacility>();
                    if (compFac.LinkedThings.NullOrEmpty())
                        return null;

                    for (int i = 0; i < compFac.LinkedThings.Count; i++)
                    {
                        // TODO check for some custom tag? Want to later implement multiple anima tree growth stages with separate ThingDefs
                        CompSpawnSubplant compPlant = compFac.LinkedThings[i].TryGetComp<CompSpawnSubplant>();
                        if (compPlant != null)
                        {
                            cachedLinkedTree = compFac.LinkedThings[i];
                            cachedComp = compPlant;
                            break;
                        }
                    }
                }

                return cachedLinkedTree;
            }
        }

        private CompSpawnSubplant cachedComp;
        public CompSpawnSubplant CachedComp
        {
            get
            {
                if (LinkedTree == null)
                {
                    cachedComp = null;
                }

                return cachedComp;
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

            if (CachedComp != null)
            {
                cachedComp.AddProgress(CorpsePsychicSensitivity * ProgressPerTick * delta);
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
}
