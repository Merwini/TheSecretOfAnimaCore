using Verse;
using RimWorld;

namespace tsoa.core;

public class Hediff_PsychicLeech : HediffWithComps
{
    private float sensitivityOffset = 0;

    private HediffStage curStage;
    public override HediffStage CurStage
    {
        get
        {
            if (curStage == null)
            {
                curStage = new();
                curStage.statOffsets = new()
                {
                    new()
                    {
                        stat = StatDefOf.PsychicSensitivity,
                        value = sensitivityOffset
                    }
                };
            }

            return curStage;
        }
    }

    private HediffComp_Disappears disappears;
    public HediffComp_Disappears Disappears
    {
        get
        {
            if (disappears == null)
            {
                disappears = GetComp<HediffComp_Disappears>();
            }
            return disappears;
        }
    }

    public override bool ShouldRemove => sensitivityOffset == 0 || base.ShouldRemove;

    public override string Description => sensitivityOffset > 0 ? "TSOA_PsychicLeechDescription".Translate() : "TSOA_PsychicLeechVictimDescription".Translate();

    public void UpdateOffset(float amount)
    {
        sensitivityOffset += amount;
        if (curStage != null)
        {
            curStage.statOffsets.FirstOrDefault(x => x.stat == StatDefOf.PsychicSensitivity).value = sensitivityOffset;
        }
        Disappears.ResetElapsedTicks();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref sensitivityOffset, "sensitivityOffset", 0);
    }
}
