using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace tsoa.core;

public class Window_ConfirmExtractAmber : Window
{
    Building_AnimaFont font;

    public override Vector2 InitialSize => new Vector2(400f, 180f);

    public Window_ConfirmExtractAmber(Building_AnimaFont font)
    {
        this.font = font;

        doCloseButton = false;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;

        Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "TSOA_ConfirmExtractAmber".Translate());
        Widgets.Label(new Rect(0f, 40f, inRect.width, 60f), "TSOA_ConfirmExtractAmber2".Translate());

        float buttonWidth = 120f;
        float buttonHeight = 38f;
        float spacing = 10f;

        float totalWidth = buttonWidth * 2f + spacing;
        float startX = (inRect.width - totalWidth) / 2f;
        float y = inRect.height - buttonHeight;

        Rect confirmRect = new Rect(startX, y, buttonWidth, buttonHeight);
        Rect cancelRect = new Rect(startX + buttonWidth + spacing, y, buttonWidth, buttonHeight);

        if (Widgets.ButtonText(confirmRect, "Confirm".Translate()))
        {
            font?.ToggleEmptyNow();
            Close();
        }

        if (Widgets.ButtonText(cancelRect, "Cancel".Translate()))
        {
            Close();
        }
    }
}
