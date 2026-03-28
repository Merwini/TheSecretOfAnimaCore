using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace tsoa.core;

public class FontOfAnimaComp : WorldObjectComp
{
    int timeoutTick = -1;

    public override void Initialize(WorldObjectCompProperties props)
    {
        WorldObjectCompProperties_FontOfAnima Props = props as WorldObjectCompProperties_FontOfAnima;
        timeoutTick = (int)(Props.timeoutDays * GenDate.TicksPerDay) + Find.TickManager.TicksGame;

        base.Initialize(props);
    }

    public override void CompTick()
    {
        // TODO have the quest check instead?
        // is it cheaper to only check on an interval?
        if (Find.TickManager.TicksGame >= timeoutTick)
        {
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("TSOA_AnimaFontExpiredLabel"), TranslatorFormattedStringExtensions.Translate("TSOA_AnimaFontExpiredDescription"), LetterDefOf.NegativeEvent);
            Find.WorldObjects.Remove(parent);
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        foreach (FloatMenuOption floatMenuOption in CaravanArrivalAction_VisitFontOfAnima.GetFloatMenuOptions(caravan, (MapParent)parent))
        {
            yield return floatMenuOption;
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref timeoutTick, "timeoutTick", -1);

        base.PostExposeData();
    }
}
