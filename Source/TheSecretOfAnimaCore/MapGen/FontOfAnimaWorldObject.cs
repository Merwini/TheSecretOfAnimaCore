using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld.Planet;
using RimWorld;

namespace tsoa.core;

public class FontOfAnimaWorldObject : MapParent
{
    int lastCaravanLeftTick;
    bool madeAnyAmber;
    int amberLeftBehind = 0;

    public Quest quest;

    private Building_AnimaFont font;
    public Building_AnimaFont Font
    {
        get
        {
            if (font != null && (font.Destroyed || !font.Spawned))
            {
                return null;
            }

            if (font == null)
            {
                font = Map.listerThings.ThingsOfDef(TSOA_DefOf.TSOA_FontOfAnima).FirstOrDefault() as Building_AnimaFont;
            }

            return font;
        }
        set
        {
            if (value.def == TSOA_DefOf.TSOA_FontOfAnima)
            {
                font = value;
            }
            else
            {
                Log.Error("FontOfAnimaWorldObject tried to set font using non-font Thing");
            }
        }
    }

    // Maybe there's a better way to do this, but when the map is removed (no colonists remaining) I want to track if the last pawn left via caravan or died
    public override void Notify_CaravanFormed(Caravan caravan)
    {
        lastCaravanLeftTick = Find.TickManager.TicksGame;

        base.Notify_CaravanFormed(caravan);
    }

    public override void Notify_MyMapAboutToBeRemoved()
    {
        if (Font != null)
        {
            // TODO check if it still has amber in it, add to amberLeftBehind
        }

        foreach (var amber in Map.listerThings.ThingsOfDef(TSOA_DefOf.TSOA_AnimaAmber))
        {
            amberLeftBehind += amber.stackCount;
        }

        base.Notify_MyMapAboutToBeRemoved();
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        if (!Map.mapPawns.AnyPawnBlockingMapRemoval)
        {
            alsoRemoveWorldObject = true;
            return true;
        }

        return false;
    }

    public override void PostRemove()
    {
        // TODO
        if (Find.TickManager.TicksGame == lastCaravanLeftTick)
        {
            if (madeAnyAmber)
            {
                // letter for made amber and successfully left
            }
            else
            {
                // letter for giving up without making any amber
            }
        }
        else
        {
            if (madeAnyAmber)
            {
                // letter for making amber and then dying
            }
            else
            {
                // letter for dying without making any amber
            }
        }

        if (amberLeftBehind != 0)
        {
            QueueAmberOffer();
        }

        base.PostRemove();
    }

    private void QueueAmberOffer()
    {
        // TODO if amber is left behind, an offer will be made to sell it back to the player
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref lastCaravanLeftTick, "lastCaravanLeftTick");
        Scribe_Values.Look(ref madeAnyAmber, "madeAnyAmber");
        Scribe_Values.Look(ref amberLeftBehind, "amberLeftBehind");
        Scribe_Values.Look(ref quest, "quest");

        base.ExposeData();
    }
}