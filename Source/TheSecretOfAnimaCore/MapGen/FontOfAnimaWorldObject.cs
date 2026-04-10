using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;

namespace tsoa.core;

public class FontOfAnimaWorldObject : MapParent, IThingGlower
{
    private FloatRange attackCooldown = new FloatRange(0.5f, 1.5f); // 0.5 to 1.5 days, TODO might change
    private int ticksPerAmber = 60000; // 1 day, TODO might change
    private float recoveryDivisor = 2f; // arbitrary, TODO balance, maybe give full?
    private float threatPointBase = 1000;
    private float threatPointPerAmber = 500;

    int lastCaravanLeftTick;
    bool madeAnyAmber = false;
    int amberLeftBehind = 0;
    bool crystallizationStarted = false;
    int nextWaveTick = -1;
    int nextAmberTick = -1;

    public int NextWaveTick
    {
        get
        {
            return nextWaveTick;
        }
        set
        {
            nextWaveTick = Math.Clamp(value, 0, int.MaxValue);
        }
    }

    public int NextAmberTick
    {
        get
        {
            return nextAmberTick;
        }
        set
        {
            nextAmberTick = Math.Clamp(value, 0, int.MaxValue);
        }
    }

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

    public int AmberHeld
    {
        get
        {
            if (Font != null)
            {
                return Font.AmberAmount;
            }

            return 0;
        }
    }

    public bool ShouldBeLitNow()
    {
        return crystallizationStarted;
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
            SpawnNextWave();
        }

        if (currentTick >= nextAmberTick)
        {
            SpawnNextAmber();
        }

        base.Tick();
    }

    private void SpawnNextAmber()
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
                EndCrystallization();
            }
        }
    }

    private void SetNextAmberTick()
    {
        nextAmberTick = Find.TickManager.TicksGame + ticksPerAmber;
    }

    private void SpawnNextWave()
    {
        StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
        IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Map);
        IncidentDef incident = SelectIncident();
        parms.forced = true;
        parms.points = GetThreatPoints();
        parms.target = Map;
        if (incident.defName != "ShamblerAssault") // setting a raidArrivalMode causes an exception when generating letter for some reason
        {
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
        }

        incident.Worker.TryExecute(parms);
        SetNextWaveTick();
    }

    private IncidentDef SelectIncident()
    {
        List<string> incidentDefNames = new List<string>
        {
            "RaidEnemy"
        };
        if (ModsConfig.AnomalyActive)
        {
            incidentDefNames.Add("SightstealerSwarm");
            incidentDefNames.Add("ShamblerAssault");
            incidentDefNames.Add("PsychicRitualSiege");
            incidentDefNames.Add("HateChanters");
            incidentDefNames.Add("FleshbeastAttack");
            incidentDefNames.Add("GorehulkAssault");
            incidentDefNames.Add("DevourerAssault");
            incidentDefNames.Add("ChimeraAssault");
        }

        IncidentDef chosen = null;
        while (chosen == null && incidentDefNames.Count != 0)
        {
            string chosenDefName = incidentDefNames.RandomElement();
            chosen = DefDatabase<IncidentDef>.GetNamedSilentFail(chosenDefName);
            if (chosen == null)
            {
                incidentDefNames.Remove(chosenDefName);
            }
        }

        if (chosen == null)
        {
            Log.Error("Could not select an incident for FontOfAnima wave");
        }

        return chosen;
    }

    private float GetThreatPoints()
    {
        return threatPointBase + threatPointPerAmber * Font.AmberAmount;
    }

    private void SetNextWaveTick()
    {
        nextWaveTick = Find.TickManager.TicksGame + (int)(attackCooldown.RandomInRange * GenDate.TicksPerDay);
    }

    private void EndCrystallization()
    {
        crystallizationStarted = false;
        Messages.Message("TSOA_CrystallizationFinishedMessage".Translate(), Font, MessageTypeDefOf.NeutralEvent);
        DoEffectAndSound();
    }

    private void DoEffectAndSound()
    {
        EffecterDef effDef = EffecterDefOf.ForcedVisible;
        Effecter eff = effDef.Spawn();
        eff.scale = 4f;
        eff.Trigger(Font, Font);

        SoundDef sndDef = SoundDefOf.PsychicSootheGlobal;
        sndDef.PlayOneShot(Font);
    }

    public void Notify_CrystallizationStarted()
    {
        if (!crystallizationStarted)
        {
            crystallizationStarted = true;
            Messages.Message("TSOA_CrystallizationStartedMessage".Translate(), Font, MessageTypeDefOf.NeutralEvent);
            DoEffectAndSound();
            SetNextWaveTick();
            SetNextAmberTick();
        }
    }

    public void Notify_FontDestroyed(int amberAmount, IntVec3 cell)
    {
        crystallizationStarted = false;
        Messages.Message("TSOA_FontDestroyedMessage".Translate(), new LookTargets(cell, Map), MessageTypeDefOf.NegativeEvent);
        Thing amber = GenSpawn.Spawn(TSOA_DefOf.TSOA_AnimaAmber, cell, Map);
        amber.stackCount = (int)(amberAmount / recoveryDivisor);
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
        if (Find.TickManager.TicksGame == lastCaravanLeftTick)
        {
            Find.LetterStack.ReceiveLetter("TSOA_FontDespawnLetterLabel".Translate(), "TSOA_FontDespawnCaravanDesc".Translate(), LetterDefOf.NeutralEvent);
        }
        else
        {
            Find.LetterStack.ReceiveLetter("TSOA_FontDespawnLetterLabel".Translate(), "TSOA_FontDespawnDiedDesc".Translate(), LetterDefOf.NeutralEvent);
        }

        if (amberLeftBehind != 0)
        {
            Find.LetterStack.ReceiveLetter("TSOA_FontDespawnAmberLeftLabel".Translate(), "TSOA_FontDespawnAmberLeftDesc".Translate(), LetterDefOf.NeutralEvent);
            QueueAmberOffer();
        }

        base.PostRemove();
    }

    private void QueueAmberOffer()
    {
        int recoverableAmber = Mathf.Max(1, Mathf.FloorToInt(amberLeftBehind / recoveryDivisor));
        int fireTick = Find.TickManager.TicksGame + Rand.RangeInclusive(3, 7) * GenDate.TicksPerDay;

        Map map = Find.AnyPlayerHomeMap;
        if (map == null)
        {
            Log.Warning("Could not queue amber recovery offer because there is no player home map.");
            return;
        }

        IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.GiveQuest, map);
        parms.forced = true;
        parms.target = map;
        parms.points = recoverableAmber; // just stashing this here, incident worker will read this then reassign a real point value

        Find.Storyteller.incidentQueue.Add(TSOA_DefOf.TSOA_AmberRecoveryOffer, fireTick, parms);
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
        Scribe_Values.Look(ref nextWaveTick, "nextWaveTick");
        Scribe_Values.Look(ref nextAmberTick, "nextAmberTick");

        base.ExposeData();
    }
}