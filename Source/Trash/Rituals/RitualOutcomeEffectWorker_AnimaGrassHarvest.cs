using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace tsoa.core
{
    public class RitualOutcomeEffectWorker_AnimaGrassHarvest : RitualOutcomeEffectWorker
    {
        public override bool SupportsAttachableOutcomeEffect => false;

        public RitualOutcomeEffectWorker_AnimaGrassHarvest()
        {

        }

        public RitualOutcomeEffectWorker_AnimaGrassHarvest(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            RitualOutcomeComp_Skill comp = def.comps.FirstOrDefault(c => c is RitualOutcomeComp_Skill) as RitualOutcomeComp_Skill;
            if (comp == null)
            {
                Log.Error("RitualOutcomeEffectWorker_AnimaGrassHarvest failed to find RitualOutcomeComp_Skill in its def.");
                return;
            }

            float quality = comp.GetQualityFactor(jobRitual.ritual, jobRitual.selectedTarget, jobRitual.obligation, jobRitual.assignments, null).quality;
            Map map = jobRitual.Map;
            Thing animaTree = jobRitual.selectedTarget.Thing;
            if (animaTree == null)
            {
                Log.Error("Failed to get Anima Tree as ritual target.");
                return;
            }

            List<Pawn> harvesters = jobRitual.assignments.AssignedPawns("harvester").ToList();

            //int harvesterSkill = GetHarvesterSkill(harvesters);

            List<Thing> grass = GetGrass(animaTree);

            int animaGrassTotal = grass.Count;
            int animaGrassSuccesses = GetSuccesses(animaGrassTotal, quality);
            int animaGrassFails = GetFails(animaGrassTotal, animaGrassSuccesses);
            int screamsToApply = GetScreamStacks(jobRitual.assignments.SpectatorsForReading, animaGrassFails, harvesters.Count)
;
            DestroyGrass(grass);

            SpawnProduct(animaGrassSuccesses, animaTree.Position, map);

            ApplyAnimaScreams(screamsToApply, harvesters);

            TSOA_DefOf.AnimaTreeScream.PlayOneShot(new TargetInfo(animaTree));

            // TODO Letter?
        }

        private void DestroyGrass(List<Thing> grassToDestroy)
        {
            foreach (Thing grass in grassToDestroy)
            {
                grass.Destroy(DestroyMode.Vanish);
            }
        }

        private void SpawnProduct(int successes, IntVec3 position, Map map)
        {
            int stackLimit = TSOA_DefOf.TSOA_AnimaGrassResource.stackLimit;
            int remaining = successes;
            while (remaining > 0)
            {
                int toSpawn = Math.Min(remaining, stackLimit);
                Thing stack = ThingMaker.MakeThing(TSOA_DefOf.TSOA_AnimaGrassResource);
                stack.stackCount = toSpawn;
                GenPlace.TryPlaceThing(stack, position, map, ThingPlaceMode.Near);
                remaining -= toSpawn;
            }
        }

        private void ApplyAnimaScreams(int screamStacks, List<Pawn> harvesters)
        {
            if (screamStacks <= 0) return;

            for (int i = 0; i < screamStacks; i++)
            {
                foreach (Pawn harvester in harvesters)
                {
                    harvester.needs.mood.thoughts.memories.TryGainMemory(TSOA_DefOf.TSOA_AnimaGrassScream);
                }
            }
        }

        public static List<Thing> GetGrass(Thing animaTree)
        {
            CompSpawnSubplant comp = animaTree.TryGetComp<CompSpawnSubplant>();
            if (comp == null)
            {
                Log.Error("Failed to get CompSpawnSubplant from Anima Tree");
                return new List<Thing>();
            }
            return comp.SubplantsForReading.ToList();
        }

        // I wrote this mirroring the anima linking ritual, then realized since it's all consumed, it doesn't need to be sorted. Might use it if I change to cap the harvest based on skill
        public static List<Thing> GetGrassSorted(Thing animaTree)
        {
            return GetGrass(animaTree).OrderByDescending((Thing p) => p.Position.DistanceTo(animaTree.Position)).ToList();
        }

        public static int GetHarvesterSkill(List<Pawn> harvesters)
        {
            int pawnsPlantSkill = 0;
            foreach (Pawn harvester in harvesters)
            {
                pawnsPlantSkill += harvester.skills.GetSkill(SkillDefOf.Plants).Level;
            }
            return pawnsPlantSkill;
        }

        public static int GetSuccesses(int totalGrass, float quality)
        {
            return (int)(totalGrass * quality);
        }

        public static int GetFails(int totalGrass, int successes)
        {
            return totalGrass - successes;
        }

        public static int GetScreamStacks(List<Pawn> spectators, int animaGrassFails, int harvesterCount)
        {
            float spectatorPsyTotal = 0;
            foreach (Pawn spec in spectators)
            {
                spectatorPsyTotal += spec.psychicEntropy.PsychicSensitivity;
            }

            int stacks = (int)(animaGrassFails / (harvesterCount + spectatorPsyTotal));

            if (DebugSettings.godMode)
            {
                Log.Message($"RitualOutcomeEffectWorker_AnimaGrassHarvest.GetScreamStacks: Calculated {stacks} Anima Scream stacks from {animaGrassFails} fails, {harvesterCount} harvesters, and {spectatorPsyTotal} total spectator psychic sensitivity.");
            }

            return stacks;
        }
    }
}
