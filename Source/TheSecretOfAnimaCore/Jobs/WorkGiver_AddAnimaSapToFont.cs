using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace tsoa.core;

public class WorkGiver_AddAnimaSapToFont : WorkGiver
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn.Map?.Parent is not FontOfAnimaWorldObject fontWO)
        {
            return true;
        }

        if (fontWO.Font is not Building_AnimaFont font)
        {
            return true;
        }

        if (!font.ShouldLoad)
        {
            return true;
        }

        if (font.IsBurning())
        {
            return true;
        }

        if (!pawn.CanReserve(font, 1, -1, null, forced))
        {
            return true;
        }

        return false;
    }

    public override Job NonScanJob(Pawn pawn)
    {
        if (pawn.Map?.Parent is not FontOfAnimaWorldObject fontWO)
        {
            // Should have never gotten to this point due to ShouldSkip, just being safe
            Log.Error("WorkGiver_AddAnimaSapToFont.NonScanJob run on map other than Font of Anima world object");
            return null;
        }

        if (fontWO.Font == null)
        {
            // Should have never gotten to this point due to ShouldSkip, just being safe
            Log.Error("WorkGiver_TakeAnimaAmbWorkGiver_AddAnimaSapToFonterOutOfFont.NonScanJob run with null Font");
            return null;
        }

        return JobMaker.MakeJob(TSOA_DefOf.TSOA_TakeAnimaAmberOutOfFontJob, fontWO.Font);
    }
}
