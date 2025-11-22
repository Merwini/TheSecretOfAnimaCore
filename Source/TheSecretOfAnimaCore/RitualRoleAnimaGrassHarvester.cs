using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace nuff.tsoa.core
{
    public class RitualRoleAnimaGrassHarvester : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = "";
            if (!p.Faction.IsPlayerSafe())
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeColonist".Translate(base.Label);
                }
                return false;
            }
            if (!MeditationFocusDefOf.Natural.CanPawnUse(p))
            {
                if (!skipReason)
                {
                    reason = "RitualTargetAnimaTreeMustBeCapableOfNature".Translate();
                }
                return false;
            }
            if (!p.psychicEntropy.IsPsychicallySensitive)
            {
                if (!skipReason)
                {
                    reason = "RitualTargetAnimaTreeMustBePsychicallySensitive".Translate();
                }
                return false;
            }
            if (p.skills.GetSkill(SkillDefOf.Plants).TotallyDisabled)
            {
                if (!skipReason)
                {
                    reason = "TSOA_CapableOfPlants".Translate();
                }
                return false;
            }
            return true;
        }

        // Copied from RitualRoleAnimaLinker, don't fully understand it. I think it prevents the Role from being locked to an ideo role?
        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn pawn = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }
    }
}
