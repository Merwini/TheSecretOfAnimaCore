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
    public class RitualOutcomeComp_Skill : RitualOutcomeComp
    {
        public SimpleCurve curve;

        public SkillDef skill;

        public int skillMax = 100;

        public override bool Applies(LordJob_Ritual ritual)
        {
            return true;
        }

        // TODO Tick() would need data I think
        //public override RitualOutcomeComp_Data MakeData()
        //{
        //    return new RitualOutcomeComp_DataThingPresence();
        //}

        // TODO maybe speed up progress with more harvesters
        //public override void Tick(LordJob_Ritual ritual, RitualOutcomeComp_Data data, float progressAmount)
        //{

        //}

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            List<Pawn> harvesters = assignments.AssignedPawns("harvester").ToList();
            int totalSkill = 0;
            foreach (Pawn harvester in harvesters)
            {
                totalSkill += harvester.skills.GetSkill(skill).Level;
            }
            float quality = curve.Evaluate(totalSkill);
            return new QualityFactor
            {
                label = label.CapitalizeFirst(),
                count = totalSkill + " / " + skillMax,
                qualityChange = ExpectedOffsetDesc(positive: true, quality),
                quality = quality,
                positive = true,
                priority = 4f
            };
        }
    }
}
