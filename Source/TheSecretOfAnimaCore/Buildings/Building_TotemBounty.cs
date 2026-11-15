using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace tsoa.core;

public class Building_TotemBounty : Building_AnimusTotem
{
    internal const float growthRate = 1.5f;

    public override void DoTotemEffect()
    {
        RegisterTotem();
    }


    public override void RegisterTotem()
    {
        HashSet<Building_TotemBounty> buildings = gameComp.activeBounty.TryGetValue(this.Map);
        if (buildings == null)
        {
            buildings = new HashSet<Building_TotemBounty>();
            gameComp.activeBounty[this.Map] = buildings;
        }
        buildings.Add(this);
    }

    public override void EndTotemEffect()
    {
         Map map = this.MapHeld;
        if (map == null)
        {
            gameComp.CleanupBounty();
            return;
        }

        HashSet<Building_TotemBounty> buildings = gameComp.activeBounty.TryGetValue(map);
        if (buildings != null)
        {
            buildings.Remove(this);
            if (buildings.Count == 0)
            {
                gameComp.activeBounty.Remove(map);
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRate), MethodType.Getter)]
    public static class Plant_GrowthRate_Postfix
    {
        public static void Postfix(Plant __instance, ref float __result)
        {
            if (GameComponent_TotemTracker.Instance?.activeBounty.ContainsKey(__instance.Map) ?? false)
            {
                __result *= growthRate;
            }
        }
    }
}
