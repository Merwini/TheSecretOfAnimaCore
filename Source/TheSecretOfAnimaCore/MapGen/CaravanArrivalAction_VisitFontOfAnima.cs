using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace tsoa.core;

public class CaravanArrivalAction_VisitFontOfAnima : CaravanArrivalAction
{
    private MapParent mapParent;

    public override string Label => "VisitEscapeShip".Translate(mapParent.Label);

    public override string ReportString => "CaravanVisiting".Translate(mapParent.Label);

    public CaravanArrivalAction_VisitFontOfAnima()
    {
    }

    public CaravanArrivalAction_VisitFontOfAnima(FontOfAnimaComp fontComp)
    {
        mapParent = (MapParent)fontComp.parent;
    }

    public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
    {
        FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
        if (!floatMenuAcceptanceReport)
        {
            return floatMenuAcceptanceReport;
        }
        if (mapParent != null && mapParent.Tile != destinationTile)
        {
            return false;
        }
        return CanVisit(caravan, mapParent);
    }

    public override void Arrived(Caravan caravan)
    {
        if (!mapParent.HasMap)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                DoEnter(caravan);
            }, "GeneratingMapForNewEncounter".Translate(), doAsynchronously: false, null);
        }
        else
        {
            DoEnter(caravan);
        }
    }

    private void DoEnter(Caravan caravan)
    {
        bool initialVisit = !mapParent.HasMap;
        if (initialVisit)
        {
            mapParent.SetFaction(Faction.OfPlayer);
        }
        Map map = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, null);
        Pawn caravanLeader = caravan.PawnsListForReading[0];
        CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.UnloadIndividually);

        Thing font = map.spawnedThings.FirstOrDefault(t => t.def == TSOA_DefOf.TSOA_FontOfAnima);

        if (initialVisit && font != null)
        {
            Find.LetterStack.ReceiveLetter("TSOA_AnimaFontArrivedLabel".Translate(), "TSOA_AnimaFontArriveDesc".Translate(caravan.Label), LetterDefOf.PositiveEvent, new GlobalTargetInfo(font.Position, map));
            Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
        }
        else
        {
            Find.LetterStack.ReceiveLetter("LetterLabelCaravanEnteredMap".Translate(mapParent), "LetterCaravanEnteredMap".Translate(caravan.Label, mapParent).CapitalizeFirst(), LetterDefOf.NeutralEvent, caravanLeader);
        }
        // TODO handle null font. Map should not be spawned with a null font
    }

    public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, MapParent animaFont)
    {
        if (animaFont == null || !animaFont.Spawned)
        {
            return false;
        }
        if (animaFont.EnterCooldownBlocksEntering())
        {
            return FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(animaFont.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
        }
        return true;
    }

    public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, MapParent animaFont)
    {
        return CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(caravan, animaFont), () => new CaravanArrivalAction_VisitFontOfAnima(animaFont.GetComponent<FontOfAnimaComp>()), "TSOA_AnimaFontVisit".Translate(animaFont.Label), caravan, animaFont.Tile, animaFont);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref mapParent, "mapParent");

        base.ExposeData();
    }
}
