using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace nuff.tsoa.core
{
    public class IncidentWorker_AnimaBranchDrop : IncidentWorker
    {
        private const int MaxDropDistance = 3;

        private static readonly Pair<int, float>[] CountChance = new Pair<int, float>[4]
        {
            new Pair<int, float>(1, 1f),
            new Pair<int, float>(2, 0.95f),
            new Pair<int, float>(3, 0.7f),
            new Pair<int, float>(4, 0.4f)
        };

        private int RandomCountToDrop
        {
            get
            {
                float x2 = (float)Find.TickManager.TicksGame / 3600000f;
                float timePassedFactor = Mathf.Clamp(GenMath.LerpDouble(0f, 1.2f, 1f, 0.1f, x2), 0.1f, 1f);
                return CountChance.RandomElementByWeight((Pair<int, float> x) => (x.First == 1) ? x.Second : (x.Second * timePassedFactor)).First;
            }
        }

        public override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            Map map = (Map)parms.target;
            return GetRandomAnimaTree(map) != null;
        }

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            Thing animaTree = GetRandomAnimaTree(map);
            if (animaTree == null)
                return false;

            int count = RandomCountToDrop;
            List<IntVec3> spawnedPositions = SpawnBranchesNearTree(animaTree, map, count);

            if (spawnedPositions.NullOrEmpty())
                return false;

            Messages.Message(
            "TSOA_MessageAnimaBranchDrop".Translate(),
            new TargetInfo(spawnedPositions[0], map),
            MessageTypeDefOf.NeutralEvent);

            return true;
        }

        private List<IntVec3> SpawnBranchesNearTree(Thing tree, Map map, int count)
        {
            List<IntVec3> positions = new List<IntVec3>();

            for (int i = 0; i < count; i++)
            {
                if (TryFindNearbyCell(tree.Position, map, MaxDropDistance, out IntVec3 pos))
                {
                    Thing branch = ThingMaker.MakeThing(ThingDef.Named("TSOA_AnimaBranch"));
                    GenPlace.TryPlaceThing(branch, pos, map, ThingPlaceMode.Near);
                    positions.Add(pos);
                }
            }

            return positions;
        }

        private bool TryFindNearbyCell(IntVec3 tree, Map map, int radius, out IntVec3 result)
        {
            return CellFinder.TryFindRandomCellNear(
                tree,
                map,
                radius,
                cell => cell.InBounds(map) && cell.Walkable(map),
                out result);
        }

        // large maps can spawn a second tree, don't want to just do First and then have it always be the one far from the player
        private Thing GetRandomAnimaTree(Map map)
        {
            List<Thing> trees = GetAllAnimaTrees(map);
            if (trees.NullOrEmpty())
            {
                return null;
            }
            return trees.RandomElement();
        }

        private List<Thing> GetAllAnimaTrees(Map map)
        {
            return map.listerThings.ThingsOfDef(ThingDef.Named("Plant_TreeAnima"));
        }
    }
}
