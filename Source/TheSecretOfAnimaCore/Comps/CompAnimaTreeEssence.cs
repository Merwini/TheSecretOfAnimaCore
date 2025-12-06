using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace nuff.tsoa.core
{
    public class CompAnimaTreeEssence : ThingComp
    {
        private int storedEssence;
        private int longTicks = 0;

        public int StoredEssence => storedEssence;

        public CompProperties_AnimaTreeEssence Props => (CompProperties_AnimaTreeEssence)props;

        public void AddEssence(int amount)
        {
            storedEssence += amount;
            if (storedEssence > Props.maximumEssence)
            {
                storedEssence = Props.maximumEssence;
            }
        }

        public override void CompTickLong()
        {
            longTicks++;
            if (longTicks >= 3)
            {
                longTicks = 0;
                AddEssence(Props.refillRate);
            }
        }

        public bool TryRemoveEssence(int amount)
        {
            if (amount <= storedEssence)
            {
                storedEssence -= amount;
                return true;
            }
            return false;
        }

        public override string CompInspectStringExtra()
        {
            return "TSOA_StoredEssence".Translate(storedEssence, Props.maximumEssence);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            if (!Prefs.DevMode) yield break;

            yield return new Command_Action
            {
                defaultLabel = "DEV: Add 100 essence",
                defaultDesc = "Adds 100 essence.",
                action = () =>
                {
                    AddEssence(100);
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "DEV: Remove 100 essence",
                defaultDesc = "Try to remove 100 essence.",
                action = () =>
                {
                    TryRemoveEssence(100);
                }
            };
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedEssence, "storedEssence", 0);
            Scribe_Values.Look(ref longTicks, "longTicks", 0);
        }
    }
}
