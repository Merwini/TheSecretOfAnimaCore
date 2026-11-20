using RimWorld;
using Verse;

namespace tsoa.core.anomaly;

public class GenStep_VoidDiveTest : GenStep
{
    public TerrainDef voidTerrain;
    public TerrainDef islandTerrain;
    public ThingDef exitDef;
    public override int SeedPart => 176512419;

    public override void Generate(Map map, GenStepParams parms)
    {
        int radius = System.Math.Min(map.Size.x, map.Size.z) / 3;
        foreach (IntVec3 cell in map.AllCells)
            map.terrainGrid.SetTerrain(cell, cell.DistanceToSquared(map.Center) <= radius * radius ? islandTerrain : voidTerrain);

        Building_VoidGateExit exit = (Building_VoidGateExit)GenSpawn.Spawn(exitDef, map.Center, map);
        exit.SetFaction(Faction.OfPlayer);
        MapGenerator.PlayerStartSpot = exit.InteractionCell;
        MapGenerator.rootsToUnfog.Add(exit.InteractionCell);
    }
}
