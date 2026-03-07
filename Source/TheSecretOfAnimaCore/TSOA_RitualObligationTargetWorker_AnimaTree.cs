using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core;

public class TSOA_RitualObligationTargetWorker_AnimaTree : RitualObligationTargetFilter
{
    public TSOA_RitualObligationTargetWorker_AnimaTree()
    {
    }

    public TSOA_RitualObligationTargetWorker_AnimaTree(RitualObligationTargetFilterDef def)
        : base(def)
    {
    }

    public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
    {
        return Enumerable.Empty<TargetInfo>();
    }

    public override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
    {
        Log.Warning("TSOA_RitualObligationTargetWorker_AnimaTree");
        CompPsylinkable compPsylinkable = target.Thing.TryGetComp<CompPsylinkable>();
        if (compPsylinkable == null)
        {
            return false;
        }
        bool flag = false;
        foreach (Pawn item in target.Map.mapPawns.FreeColonistsSpawned)
        {
            if (item.GetPsylinkLevel() < item.GetMaxPsylinkLevel())
            {
                flag = true;
            }
        }
        if (compPsylinkable.CompSubplant.SubplantsForReading.Count < compPsylinkable.Props.requiredSubplantCountPerPsylinkLevel[0])
        {
            return "RitualTargetAnimaTreeNotEnoughAnimaGrass".Translate(compPsylinkable.Props.requiredSubplantCountPerPsylinkLevel[0]);
        }
        if (!flag)
        {
            return "TSOA_AllAlreadyMaxPsylink".Translate();
        }
        return true;
    }

    public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
    {
        yield return "RitualTargetAnimaTreeInfo".Translate();
    }
}
