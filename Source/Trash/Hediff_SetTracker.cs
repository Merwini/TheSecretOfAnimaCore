using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace nuff.tsoa.core
{
    public class Hediff_SetTracker : Hediff
    {
        public int count = 0;

        public void AdjustCount(int delta)
        {
            count += delta;
            if (count <= 0)
            {
                pawn.health.RemoveHediff(this);
            }
        }

        public override string LabelBase => base.LabelBase + $" ({count})";

        public override string TipStringExtra => $"Total {def.label.CapitalizeFirst()} implants: {count}";
    }
}
