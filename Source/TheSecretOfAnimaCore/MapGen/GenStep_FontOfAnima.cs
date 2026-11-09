using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace tsoa.core;

public class GenStep_FontOfAnima : GenStep
{
    public override int SeedPart => 621764206;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (!CellFinder.TryFindRandomCellNear(map.Center, map, 16, ValidLocation, out IntVec3 fontCell))
        {
            fontCell = map.AllCells
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .Where(ValidLocation)
                .DefaultIfEmpty(IntVec3.Invalid)
                .First();
        }

        if (!fontCell.IsValid)
        {
            Log.Error("[TSOA] Could not find a valid Font of Anima location.");
            return;
        }

        GenSpawn.Spawn(TSOA_DefOf.TSOA_FontOfAnima, fontCell, map, Rot4.North);

        bool ValidLocation(IntVec3 center)
        {
            CellRect clearing = CellRect.CenteredOn(center, 7, 7);
            if (!clearing.InBounds(map))
                return false;

            foreach (IntVec3 cell in clearing)
            {
                if (cell.GetTerrain(map).IsWater ||
                    !DropCellFinder.IsGoodDropSpot(
                        cell, map,
                        allowFogged: false,
                        canRoofPunch: false,
                        allowIndoors: false))
                {
                    return false;
                }
            }

            IntVec3 interactionCell = ThingUtility.InteractionCellWhenAt(TSOA_DefOf.TSOA_FontOfAnima, center, Rot4.North, map);

            return map.reachability.CanReachMapEdge(interactionCell, TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater));
        }
    }
}
