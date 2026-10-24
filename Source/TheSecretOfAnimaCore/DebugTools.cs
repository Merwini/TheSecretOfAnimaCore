using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using LudeonTK;

namespace tsoa.core;

public class DebugTools
{
    [DebugAction("Secret of Anima", "Add anima affinity", true, false, false, true, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void AddAnimaAffinity(Pawn p)
    {
        Hediff_AnimaAffinity.AddOrUpdateAffinityHediff(p, 0.01f);
    }
}
