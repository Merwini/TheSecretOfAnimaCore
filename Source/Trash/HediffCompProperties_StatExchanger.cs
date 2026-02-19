using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace tsoa.core
{
    public class HediffCompProperties_StatExchanger : HediffCompProperties
    {
        public bool isMaster = false;  // controls the link
        public bool isDonor = false; // loses the stat

        public bool showLinkGizmo = false;
        public bool showUnlinkGizmo = false;

        public List<HediffDef> targetHediffs = null;

        public StatDef affectedStat;

        public float donorLossPercent = 0f;
        public float recipientGainPercent = 0f;
        public float recipientGainFlat = 0f;

        public int linkJobDuration = 600;
        public int unlinkJobDuration = 300;

        public EffecterDef linkCompleteEffecter;
        public float linkCompleteEffecterScale = 1;
        public SoundDef linkCompleteSound;

        public EffecterDef unlinkCompleteEffecter;
        public float unlinkCompleteEffecterScale = 1;
        public SoundDef unlinkCompleteSound;

        public int maxLinks = 1;

        public string bondLabel;

        public HediffCompProperties_StatExchanger()
        {
            this.compClass = typeof(HediffComp_StatExchanger);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            if (isMaster && targetHediffs.NullOrEmpty())
                yield return $"HediffCompProperties_StatExchanger on {parentDef}: masters must have targetHediffs";

            if (isMaster && targetHediffs.Any(h =>
            {
                var prop = h.comps
                    .OfType<HediffCompProperties_StatExchanger>()
                    .FirstOrDefault();
                return prop != null && prop.isMaster;
            }))
            {
                yield return $"HediffCompProperties_StatExchanger on {parentDef}: masters cannot link to other masters";
            }

            if (isMaster && isDonor && targetHediffs.Any(h =>
            {
                var prop = h.comps
                    .OfType<HediffCompProperties_StatExchanger>()
                    .FirstOrDefault();
                return prop != null && prop.isDonor;
            }))
            {
                yield return $"HediffCompProperties_StatExchanger on {parentDef}: donors cannot link to other donors";
            }

            if (isMaster && !isDonor && targetHediffs.Any(h =>
            {
                var prop = h.comps
                    .OfType<HediffCompProperties_StatExchanger>()
                    .FirstOrDefault();
                return prop != null && !prop.isDonor;
            }))
            {
                yield return $"HediffCompProperties_StatExchanger on {parentDef}: recipients cannot link to other recipients";
            }
        }
    }
}
