using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace tsoa.core.anomaly;

public class WorldObject_VoidGateCaravan : WorldObject, IThingHolder
{
    private const float TravelSpeed = 0.00025f; // TODO change, should be slower than transport pod
    private static readonly Color PlayerCaravanColor = new Color(1f, 0.863f, 0.33f);
    private const int ImmobilizedCacheDuration = 60;

    private PlanetTile initialTile = PlanetTile.Invalid;
    public PlanetTile destinationTile = PlanetTile.Invalid;

    private int uniqueId = -1;
	private string nameInt;
    private bool inAmbush;
    private float traveledPct;
    private bool cachedImmobilized;
    private int cachedImmobilizedForTicks = -99999;

    public ThingOwner<Pawn> pawns;
    public List<Pawn> PawnsListForReading => pawns.InnerListForReading;
    public ThingOwner GetDirectlyHeldThings() => pawns;
    public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

    private Material cachedMat;
    public override Material Material
	{
		get
		{
			if (cachedMat == null)
			{
				cachedMat = MaterialPool.MatFrom(color: (Faction == null) ? Color.white : ((!Faction.IsPlayer) ? Faction.Color : PlayerCaravanColor), texPath: def.texture, shader: ShaderDatabase.WorldOverlayTransparentLit, renderQueue: 3600);
			}
			return cachedMat;
		}
	}

	public string Name { get { return nameInt; } set { nameInt = value;	}}
    public override string Label
{
	get
	{
		if (nameInt != null)
		{
			return nameInt;
		}
		return base.Label;
	}
}
    private Vector3 Start => Find.WorldGrid.GetTileCenter(initialTile);
	private Vector3 End => Find.WorldGrid.GetTileCenter(destinationTile);
    public override Vector3 DrawPos => Vector3.Slerp(Start, End, traveledPct);
    public override bool ExpandingIconFlipHorizontal => GenWorldUI.WorldToUIPosition(Start).x > GenWorldUI.WorldToUIPosition(End).x;
    public override Color ExpandingIconColor => Faction.Color;

    private float TraveledPctStepPerTick
	{
		get
		{
			Vector3 start = Start;
			Vector3 end = End;
			if (start == end)
			{
				return 1f;
			}
			float num = GenMath.SphericalDistance(start.normalized, end.normalized);
			if (num == 0f)
			{
				return 1f;
			}
			return 0.00025f / num;
		}
	}
    public bool IsOutgoing => Find.WorldObjects.AnyWorldObjectOfDefAt(Anomaly_MiscDefOf.TSOA_VoidTunnelExit, destinationTile);

    public float MassCapacity => CollectionsMassCalculator.Capacity(PawnsListForReading);
    public bool ImmobilizedByMass
	{
		get
		{
			if (Find.TickManager.TicksGame - cachedImmobilizedForTicks < 60)
			{
				return cachedImmobilized;
			}
			cachedImmobilized = MassUsage > MassCapacity;
			cachedImmobilizedForTicks = Find.TickManager.TicksGame;
			return cachedImmobilized;
		}
	}

    public bool CantMove
	{
		get
		{
			if (!inAmbush && !AllOwnersHaveMentalBreak && !AllOwnersDowned && !ImmobilizedByMass)
			{
				return false;
			}
			return true;
		}
	}

    public float MassUsage => CollectionsMassCalculator.MassUsage(PawnsListForReading, IgnorePawnsInventoryMode.DontIgnore);

	public bool AllOwnersDowned
	{
		get
		{
			for (int i = 0; i < pawns.Count; i++)
			{
				if (IsOwner(pawns[i]) && !pawns[i].Downed)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool AllOwnersHaveMentalBreak
	{
		get
		{
			for (int i = 0; i < pawns.Count; i++)
			{
				if (IsOwner(pawns[i]) && !pawns[i].InMentalState)
				{
					return false;
				}
			}
			return true;
		}
	}

    public override void PostAdd()
	{
		base.PostAdd();
		initialTile = Tile;
	}

    public override void TickInterval(int delta)
	{
		base.TickInterval(delta);

		traveledPct += TraveledPctStepPerTick * (float)delta;
		if (traveledPct >= 1f)
		{
			traveledPct = 1f;
			DoArrivalAction();
		}
	}

    private void DoArrivalAction()
    {
        // TODO spawn pawns and things around void gate
        // or spawn caravan at the void tunnel exit
        // detect whether destination tile has a void gate exit on it, or a MapParent
    }

    public bool IsOwner(Pawn p)
	{
		if (pawns.Contains(p))
		{
			return CaravanUtility.IsOwner(p, Faction);
		}
		return false;
	}

    public override string GetInspectString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.GetInspectString());
		if (stringBuilder.Length != 0)
		{
			stringBuilder.AppendLine();
		}
		int colonists = 0;
		int animals = 0;
		int prisoners = 0;
		int downedPawns = 0;
		int mentalBrokenPawns = 0;
		int mechanoids = 0;
		for (int i = 0; i < pawns.Count; i++)
		{
			if (pawns[i].IsColonist)
			{
				colonists++;
			}
			else if (pawns[i].RaceProps.Animal)
			{
				animals++;
			}
			else if (pawns[i].IsPrisoner)
			{
				prisoners++;
			}
			else if (pawns[i].RaceProps.IsMechanoid)
			{
				mechanoids++;
			}
			if (pawns[i].Downed && !pawns[i].ageTracker.CurLifeStage.alwaysDowned)
			{
				downedPawns++;
			}
			if (pawns[i].InMentalState)
			{
				mentalBrokenPawns++;
			}
		}
		stringBuilder.Append("CaravanColonistsCount".Translate(colonists, (colonists == 1) ? Faction.OfPlayer.def.pawnSingular : Faction.OfPlayer.def.pawnsPlural));
		if (animals == 1)
		{
			stringBuilder.Append(", " + "CaravanAnimal".Translate());
		}
		else if (animals > 1)
		{
			stringBuilder.Append(", " + "CaravanAnimalsCount".Translate(animals));
		}
		if (prisoners == 1)
		{
			stringBuilder.Append(", " + "CaravanPrisoner".Translate());
		}
		else if (prisoners > 1)
		{
			stringBuilder.Append(", " + "CaravanPrisonersCount".Translate(prisoners));
		}
		if (ModsConfig.BiotechActive)
		{
			if (mechanoids == 1)
			{
				stringBuilder.Append(", " + "CaravanMech".Translate());
			}
			else if (mechanoids > 1)
			{
				stringBuilder.Append(", " + "CaravanMechsCount".Translate(mechanoids));
			}
		}
		stringBuilder.AppendLine();
		if (mentalBrokenPawns > 0)
		{
			stringBuilder.Append("CaravanPawnsInMentalState".Translate(mentalBrokenPawns));
		}
		if (downedPawns > 0)
		{
			if (mentalBrokenPawns > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append("CaravanPawnsDowned".Translate(downedPawns));
		}
		if (mentalBrokenPawns > 0 || downedPawns > 0)
		{
			stringBuilder.AppendLine();
		}
		// if (pather.Moving)
		// {
		// 	if (pather.ArrivalAction != null)
		// 	{
		// 		stringBuilder.Append(pather.ArrivalAction.ReportString);
		// 	}
		// 	else
		// 	{
		// 		stringBuilder.Append("CaravanTraveling".Translate());
		// 	}
		// }
		// else
		// {
		// 	Settlement settlement = CaravanVisitUtility.SettlementVisitedNow(this);
		// 	if (settlement != null)
		// 	{
		// 		stringBuilder.Append("CaravanVisiting".Translate(settlement.Label));
		// 	}
		// 	else
		// 	{
		// 		stringBuilder.Append("CaravanWaiting".Translate());
		// 	}
		// }
		// if (pather.Moving)
		// {
		// 	float num7 = (float)CaravanArrivalTimeEstimator.EstimatedTicksToArrive(this, allowCaching: true) / 60000f;
		// 	stringBuilder.AppendLine();
		// 	stringBuilder.Append("CaravanEstimatedTimeToDestination".Translate(num7.ToString("0.#")));
		// }
		if (AllOwnersDowned)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("AllCaravanMembersDowned".Translate());
		}
		else if (AllOwnersHaveMentalBreak)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("AllCaravanMembersMentalBreak".Translate());
		}
		else if (ImmobilizedByMass)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("CaravanImmobilizedByMass".Translate());
		}
		// if (needs.AnyPawnOutOfFood(out var malnutritionHediff))
		// {
		// 	stringBuilder.AppendLine();
		// 	stringBuilder.Append("CaravanOutOfFood".Translate());
		// 	if (!malnutritionHediff.NullOrEmpty())
		// 	{
		// 		stringBuilder.Append(" ");
		// 		stringBuilder.Append(malnutritionHediff);
		// 		stringBuilder.Append(".");
		// 	}
		// }
		return stringBuilder.ToString();
	}

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
		{
			pawns.RemoveAll((Pawn x) => x.Destroyed);
		}
		Scribe_Deep.Look(ref pawns, "pawns", this);
		Scribe_Values.Look(ref uniqueId, "uniqueId", 0);
		Scribe_Values.Look(ref nameInt, "name");

        Scribe_Values.Look(ref destinationTile, "destinationTile");
        Scribe_Values.Look(ref traveledPct, "traveledPct");
        Scribe_Values.Look(ref inAmbush, "inAmbush");
    }
}