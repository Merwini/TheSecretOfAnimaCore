using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core
{
    public static class MeditationFocusCache
    {
        private class MeditationCache
        {
            public Thing focusThing;
            public CompSpecialMeditationFocus_Anima focusComp;
            public int lastTickValidated;
        }

        private static readonly ConditionalWeakTable<JobDriver_Meditate, MeditationCache> table = new ConditionalWeakTable<JobDriver_Meditate, MeditationCache>();

        public static CompSpecialMeditationFocus_Anima GetOrFind(JobDriver_Meditate driver, Pawn pawn)
        {
            if (driver == null || pawn == null)
                return null;

            if (pawn.Map == null)
                return null;

            MeditationCache cache = table.GetOrCreateValue(driver);
            int now = Find.TickManager.TicksGame;

            if (cache.focusComp != null &&
                cache.focusThing != null &&
                cache.focusThing.Spawned &&
                !cache.focusThing.Destroyed &&
                cache.focusThing.Map == pawn.Map)
            {
                if (now - cache.lastTickValidated >= 60)
                {
                    cache.lastTickValidated = now;
                }
                return cache.focusComp;
            }

            cache.focusThing = null;
            cache.focusComp = FindNearbyFocusComp(pawn);

            if (cache.focusComp != null)
            {
                cache.focusThing = cache.focusComp.parent;
                cache.lastTickValidated = now;
            }

            return cache.focusComp;
        }

        private static CompSpecialMeditationFocus_Anima FindNearbyFocusComp(Pawn pawn)
        {
            Map map = pawn.Map;
            int num = GenRadial.NumCellsInRadius(MeditationUtility.FocusObjectSearchRadius);

            for (int i = 0; i < num; i++)
            {
                IntVec3 c = pawn.Position + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                    continue;

                // This is still a scan, but happens once per session, not per tick.
                var list = c.GetThingList(map);
                for (int j = 0; j < list.Count; j++)
                {
                    var comp = list[j].TryGetComp<CompSpecialMeditationFocus_Anima>();
                    if (comp != null)
                        return comp;
                }
            }

            return null;
        }
    }
}
