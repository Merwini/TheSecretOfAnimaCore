using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace nuff.tsoa.core
{
    public class Dialog_SapTapThresholds : Window
    {
        private readonly Building_AnimaSapBasin basin;

        public override Vector2 InitialSize => new Vector2(420f, 220f);

        public Dialog_SapTapThresholds(Building_AnimaSapBasin basin)
        {
            this.basin = basin;
            forcePause = true;
            absorbInputAroundWindow = true;
            doCloseButton = true;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect labelRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Widgets.Label(labelRect, "TSOA_SapThresholdsHeader".Translate());

            Rect sliderRect = new Rect(inRect.x, inRect.y + 40f, inRect.width - 20f, 40f);

            Widgets.FloatRange(
                id: 1234567,
                rect: sliderRect,
                range: ref basin.harvestRange,
                min: 0f,
                max: 1f
                //labelKey: "TSOA_SapThresholdsTooltip".Translate()
            );

            // Show exact percentages
            Rect textRect = new Rect(inRect.x, inRect.y + 90f, inRect.width, 30f);
            Widgets.Label(textRect,
                "TSOA_SapThresholdsCurrent".Translate(
                    (basin.harvestRange.min * 100f).ToString("F0"),
                    (basin.harvestRange.max * 100f).ToString("F0")
                ));
        }
    }
}
