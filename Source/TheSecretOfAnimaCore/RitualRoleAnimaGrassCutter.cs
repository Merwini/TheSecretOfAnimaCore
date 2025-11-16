using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TheSecretOfAnimaCore
{
    public class RitualRoleAnimaGrassCutter : RitualRole
    {
        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn pawn = null, bool skipReason = false)
        {
            throw new NotImplementedException();
        }

        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            return base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason);
        }
    }
}
