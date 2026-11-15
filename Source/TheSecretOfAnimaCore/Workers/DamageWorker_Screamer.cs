using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core;

public class DamageWorker_Screamer : DamageWorker
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        DamageInfo newDinfo = new DamageInfo(dinfo);
        newDinfo.SetAmount(0);
        newDinfo.SetIgnoreArmor(true);

        if (victim is not Pawn pawn)
        {
            return base.Apply(newDinfo, victim);
        }

        if (pawn.RaceProps.Humanlike)
        {
            if (pawn.needs?.mood?.thoughts?.memories is MemoryThoughtHandler handler)
            {
                handler.TryGainMemory(TSOA_ThoughtDefOf.TSOA_AnimaArrowScream);
            }
        }
        else if (pawn.RaceProps.Animal && pawn.Faction == null)
        {
            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
        }

        Effecter eff = EffecterDefOf.ForcedVisible.Spawn();
        eff.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), new TargetInfo(pawn.Position, pawn.Map, false));

        return base.Apply(newDinfo, victim);
    }
}
