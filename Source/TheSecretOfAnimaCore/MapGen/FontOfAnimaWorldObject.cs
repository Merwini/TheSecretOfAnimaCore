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
    private FloatRange attackCooldown = new FloatRange(0.5f, 1.5f); // 0.5 to 1.5 days, TODO might change
    private int ticksPerAmber = 60000; // 1 day, TODO might change

    int lastCaravanLeftTick;
    bool madeAnyAmber = false;
    int amberLeftBehind = 0;
    bool crystallizationStarted = false;
    int wavesSurvived = 0;
    int nextWaveTick = -1;
    int nextAmberTick = -1;


    public Quest relatedQuest;
    public Quest RelatedQuest
    {
        get
        {
            if (relatedQuest == null)
            {
                List<Quest> quests = Find.QuestManager.QuestsListForReading;
                for (int i = 0; i < quests.Count; i++)
                {
                    Quest quest = quests[i];
                    if (!quest.hidden && !quest.Historical && !quest.dismissed && quest.QuestLookTargets.Contains(this))
                    {
                        relatedQuest = quest;
                    }
                }
            }
            return relatedQuest;
        }
    }

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

    public override void Tick()
    {
        if (!crystallizationStarted)
        {
            return;
        }

        int currentTick = Find.TickManager.TicksGame;

        if (currentTick >= nextWaveTick)
        {
            // TODO spawn wave
            wavesSurvived++;
            SetNextWaveTick();
        }

        if (currentTick >= nextAmberTick)
        {
            if (Font != null && !Font.Destroyed && Font.Spawned)
            {
                if (Font.Crystallize())
                {
                    madeAnyAmber = true;
                }

                if (Font.CanCrystallize())
                {
                    SetNextAmberTick();
                }
                else
                {
                    // TODO end crystallization, maybe some visual effect + sound
                    Messages.Message("TSOA_CrystallizationFinishedMessage".Translate(), Font, MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        base.Tick();
    }

    private void SetNextWaveTick()
    {
        nextWaveTick = Find.TickManager.TicksGame + (int)(attackCooldown.RandomInRange * 60000);
    }

    private void SetNextAmberTick()
    {
        nextAmberTick = Find.TickManager.TicksGame + ticksPerAmber;
    }

    private void SpawnNextWave()
    {
        // TODO fire a raid. Anomaly if active, ??? if not
        // Raid points 1000 + 500 per wave survived? Maybe more? Player should have acolyte gear (industrial equivalent), but is not on their home base
    }

    public void Notify_CrystallizationStarted()
    {
        if (!crystallizationStarted)
        {
            crystallizationStarted = true;
            Messages.Message("TSOA_CrystallizationStartedMessage".Translate(), Font, MessageTypeDefOf.NeutralEvent);
            SetNextWaveTick();
            SetNextAmberTick();
        }
    }

    public void Notify_FontDestroyed()
    {
        // TODO
    }

    // Maybe there's a better way to do this, but when the map is removed (no colonists remaining) I want to track if the last pawn left via caravan or died
    public override void Notify_CaravanFormed(Caravan caravan)
    {
        lastCaravanLeftTick = Find.TickManager.TicksGame;

        base.Notify_CaravanFormed(caravan);
    }

    public override void Notify_MyMapAboutToBeRemoved()
    {
        if (Font != null && !Font.Destroyed && Font.Spawned)
        {
            amberLeftBehind += Font.AmberAmount;
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
                // letter for leaving without making any amber
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

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        foreach (FloatMenuOption floatMenuOption in CaravanArrivalAction_VisitFontOfAnima.GetFloatMenuOptions(caravan, this))
        {
            yield return floatMenuOption;
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref lastCaravanLeftTick, "lastCaravanLeftTick");
        Scribe_Values.Look(ref madeAnyAmber, "madeAnyAmber");
        Scribe_Values.Look(ref amberLeftBehind, "amberLeftBehind");
        Scribe_Values.Look(ref crystallizationStarted, "crystallizationStarted");
        Scribe_Values.Look(ref wavesSurvived, "wavesSurvived");
        Scribe_Values.Look(ref nextWaveTick, "nextWaveTick");
        Scribe_Values.Look(ref nextAmberTick, "nextAmberTick");

        base.ExposeData();
    }
}