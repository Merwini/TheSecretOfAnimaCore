using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace nuff.tsoa.core
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("nuff.tsoa.core");

            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(MeditationUtility))]
        [HarmonyPatch("CountsAsArtificialBuilding")]
        [HarmonyPatch(new Type[] { typeof(ThingDef), typeof(Faction) })]
        public class MeditiationUtility_CountsAsArtificialBuilding_Patch
        {
            public static bool Prefix(ThingDef def, ref bool __result)
            {
                bool isBuilding = def.category == ThingCategory.Building || def.thingCategories.NotNullAndContains(ThingCategoryDefOf.BuildingsSpecial);

                if (!isBuilding)
                {
                    __result = false;
                    return false;
                }

                bool hasPower = def.comps.Any(c => c is CompProperties_Power);

                __result = hasPower;
                return false;
            }
        }
    }
}

