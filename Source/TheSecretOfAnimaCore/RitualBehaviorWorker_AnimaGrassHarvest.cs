using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace nuff.tsoa.core
{
    public class RitualBehaviorWorker_AnimaGrassHarvest : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_AnimaGrassHarvest()
        {

        }

        public RitualBehaviorWorker_AnimaGrassHarvest(RitualBehaviorDef def) : base(def)
        {

        }

        // I think this is redundant, already check for grass in RitualObligationTargetWorker_AnimaGrass
        //public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        //{
        //    Thing tree = target.Thing;
        //    if (tree == null || tree.def != ThingDefOf.Plant_TreeAnima)
        //    {
        //        return "TSOA_RitualMustTargetAnimaTree".Translate();
        //    }

        //    CompSpawnSubplant comp = tree.TryGetComp<CompSpawnSubplant>();
        //    if (tree == null || tree.def != ThingDefOf.Plant_TreeAnima)
        //    {
        //        Log.Error("Failed to get CompSpawnSubplant from Anima Tree.");
        //    }

        //    int grassCount = comp.SubplantsForReading.Count;
        //    if (grassCount < 1)
        //    {
        //        return "TSOA_RitualNeedsAnimaGrass".Translate();
        //    }

        //    return null;
        //}

        public override string GetExplanation(Precept_Ritual ritual, RitualRoleAssignments assignments, float quality)
        {
            ThingWithComps animaTree = assignments.ritualTarget.Thing as ThingWithComps; //this should be the anima tree

            List<Thing> sortedGrass = RitualOutcomeEffectWorker_AnimaGrassHarvest.GetGrass(animaTree);

            int animaGrassTotal = sortedGrass.Count;

            List<Pawn> harvesters = assignments.AssignedPawns("harvester").ToList();

            int pawnsPlantSkill = RitualOutcomeEffectWorker_AnimaGrassHarvest.GetHarvesterSkill(harvesters);

            int animaGrassSuccesses = RitualOutcomeEffectWorker_AnimaGrassHarvest.GetSuccesses(animaGrassTotal, quality);

            int animaGrassFails = RitualOutcomeEffectWorker_AnimaGrassHarvest.GetFails(animaGrassTotal, animaGrassSuccesses);

            int animaScreamStacks = RitualOutcomeEffectWorker_AnimaGrassHarvest.GetScreamStacks(assignments.SpectatorsForReading, animaGrassFails, harvesters.Count);

            TaggedString taggedString = "TSOA_AnaimaGrassHarvestExplanationBase".Translate(animaGrassTotal, pawnsPlantSkill, animaGrassSuccesses, animaGrassFails, animaScreamStacks);

            return taggedString;
        }
    }
}
