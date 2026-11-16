using System.Collections.Generic;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace tsoa.core.anomaly;

public class CompPsychicRitualSpotSolid : CompPsychicRitualSpot
{
    CompAffectedByFacilities compAffected;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compAffected = parent.TryGetComp<CompAffectedByFacilities>();
    }

    public override void CompTick()
	{
        // skip base.CompTick(). Hopefully no mods hook into it
		obstructingThings.Clear();
		foreach (IntVec3 occupiedCell in occupiedCells)
		{
			List<Thing> list = parent.Map.thingGrid.ThingsListAt(occupiedCell);
			for (int i = 0; i < list.Count; i++)
			{
                if (list[i] == parent || (compAffected?.linkedFacilities?.Contains(list[i]) ?? false))
                {
                    continue;
                }
				if (list[i].def.passability != Traversability.Standable)
				{
					obstructingThings.Add(list[i]);
				}
			}
		}
		PsychicRitual psychicRitual = GetPsychicRitual();
		if (psychicRitual != null && parent.Faction == Faction.OfPlayer && Obstructed)
		{
			psychicRitual.CancelPsychicRitual("PsychicRitualAreaObstructed".Translate());
		}
	}
}


public class CompProperties_PsychicRitualSpotSolid : CompProperties
{
    public CompProperties_PsychicRitualSpotSolid()
	{
		compClass = typeof(CompPsychicRitualSpotSolid);
	}
}
