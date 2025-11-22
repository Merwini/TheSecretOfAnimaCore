using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class RitualObligationTargetWorker_AnimaGrass : RitualObligationTargetFilter
    {
        public RitualObligationTargetWorker_AnimaGrass()
        {
        }

        public RitualObligationTargetWorker_AnimaGrass(RitualObligationTargetFilterDef def)
            : base(def)
        {
        }

        public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
        {
            return Enumerable.Empty<TargetInfo>();
        }

        public override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            CompSpawnSubplant comp = target.Thing.TryGetComp<CompSpawnSubplant>();
            if (comp == null)
            {
                return false;
            }
            bool flag = false;
            foreach (Pawn pawn in target.Map.mapPawns.FreeColonistsSpawned)
            {
                if (MeditationFocusDefOf.Natural.CanPawnUse(pawn))
                {
                    flag = true;
                }
            }
            if (comp.SubplantsForReading.Count < 1)
            {
                return "RitualTargetAnimaTreeNotEnoughAnimaGrass".Translate(1);
            }
            if (!flag)
            {
                return "RitualTargetAnimaTreeNoPawnsWithNatureFocus".Translate();
            }
            return true;
        }

        public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
        {
            yield return "RitualTargetAnimaTreeInfo".Translate();
        }
    }
}
