using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace tsoa.core;

public class Page_ConfigureStartingPawns_Extra : Page_ConfigureStartingPawns
{
    internal ScenPart_ConfigPage_ConfigureStartingPawns_Extra scenPart;

    // copied from Page_ConfigureStartingPawns.DoWindowContents(Rect rect) except calling DrawGroupedPawnList() instead of DrawPawnList()
    public override void DoWindowContents(Rect rect)
    {
        DrawPageTitle(rect);
        DrawApparelOptions(rect);
        rect.yMin += 45f;
        DoBottomButtons(rect, "Start".Translate(), null, null, showNext: true, doNextOnKeypress: false);
        DrawXenotypeEditorButton(rect);
        AcceptanceReport extraCanDoNextReport = ExtraCanDoNextReport;
        if (!extraCanDoNextReport.Accepted && !extraCanDoNextReport.Reason.NullOrEmpty())
        {
            Rect reportRect = new Rect(rect.center.x + BottomButSize.x / 2f + 4f, rect.y + rect.height - BottomButSize.y, BottomButSize.x, BottomButSize.y);
            reportRect.xMax = rect.xMax - BottomButSize.x - 4f;
            string text = extraCanDoNextReport.Reason.TruncateHeight(reportRect.width, reportRect.height);
            using (new TextBlock(GameFont.Tiny, Color.red))
            {
                Widgets.Label(reportRect, text);
            }
            if (extraCanDoNextReport.Reason != text && Mouse.IsOver(reportRect))
            {
                Widgets.DrawHighlight(reportRect);
                TooltipHandler.TipRegion(reportRect, extraCanDoNextReport.Reason);
            }
        }
        rect.yMax -= 48f;
        Rect listRect = rect;
        listRect.width = 140f;
        DrawGroupedPawnList(listRect);
        UIHighlighter.HighlightOpportunity(listRect, "ReorderPawn");
        Rect portraitRect = rect;
        portraitRect.xMin += 140f;
        Rect skillsRect = portraitRect.BottomPartPixels(127f);
        portraitRect.yMax = skillsRect.yMin;
        portraitRect = portraitRect.ContractedBy(4f);
        skillsRect = skillsRect.ContractedBy(4f);
        StartingPawnUtility.DrawPortraitArea(portraitRect, curPawnIndex, renderClothes, renderHeadgear);
        StartingPawnUtility.DrawSkillSummaries(skillsRect);
    }

    private void DrawGroupedPawnList(Rect rect)
    {
        Rect position = rect;
        position.yMax -= 22f;
        float optionalHeaderHeight = Find.GameInitData.startingPawnCount < Find.GameInitData.startingAndOptionalPawns.Count ? 22f : 0f;
        float contentHeight = Find.GameInitData.startingAndOptionalPawns.Count * 60f + scenPart.groups.Count * 22f + optionalHeaderHeight;
        float scrollbarWidth = contentHeight > position.height ? 16f : 0f;
        Rect viewRect = new Rect(0f, 0f, position.width - scrollbarWidth, contentHeight);
        Rect entryRect = viewRect;
        entryRect.height = 60f;
        entryRect = entryRect.ContractedBy(4f);
        scroll = GUI.BeginScrollView(position, scroll, viewRect);
#if NET48
        if (Event.current.type == EventType.Repaint)
#else
        if (Event.current.type == EventType.repaint)
#endif
        {
            reorderableGroupID = ReorderableWidget.NewGroup(delegate(int from, int to)
            {
                if (TutorSystem.AllowAction("ReorderPawn"))
                {
                    Pawn item = Find.GameInitData.startingAndOptionalPawns[from];
                    Find.GameInitData.startingAndOptionalPawns.Insert(to, item);
                    Find.GameInitData.startingAndOptionalPawns.RemoveAt(from < to ? from : from + 1);
                    StartingPawnUtility.ReorderRequests(from, to);
                    TutorSystem.Notify_Event("ReorderPawn");
                    if (to < Find.GameInitData.startingPawnCount && from >= Find.GameInitData.startingPawnCount)
                        TutorSystem.Notify_Event("ReorderPawnOptionalToStarting");

                    curPawnIndex = from < to ? to - 1 : to;
                }
            }, ReorderableDirection.Vertical, rect, -1f, null, playSoundOnStartReorder: false);
        }

        int groupIndex = 0;
        int nextGroupStart = 0;
        for (int pawnIndex = 0; pawnIndex < Find.GameInitData.startingAndOptionalPawns.Count; pawnIndex++)
        {
            if (pawnIndex == nextGroupStart && groupIndex < scenPart.groups.Count)
            {
                PawnConfigurationGroup group = scenPart.groups[groupIndex++];
                DrawPawnListLabel(ref entryRect, group.groupLabel.CapitalizeFirst());
                nextGroupStart += group.count;
            }
            if (pawnIndex == Find.GameInitData.startingPawnCount)
            {
                DrawPawnListLabel(ref entryRect, "StartingPawnsLeftBehind".Translate());
            }

            Pawn pawn = Find.GameInitData.startingAndOptionalPawns[pawnIndex];
            Widgets.BeginGroup(entryRect.ExpandedBy(4f));
            Rect optionRect = new Rect(new Vector2(4f, 4f), entryRect.size);
            Widgets.DrawOptionBackground(optionRect, curPawnIndex == pawnIndex);
            MouseoverSounds.DoRegion(optionRect);
            Widgets.BeginGroup(optionRect);
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Rect portraitRect = new Rect(110f - PawnSelectorPortraitSize.x / 2f, 40f - PawnSelectorPortraitSize.y / 2f, PawnSelectorPortraitSize.x, PawnSelectorPortraitSize.y);
            GUI.DrawTexture(portraitRect, PortraitsCache.Get(pawn, PawnSelectorPortraitSize, Rot4.South, default(Vector3), 1f, supersample: true, compensateForUIScale: true, renderHeadgear, renderClothes));
            GUI.color = Color.white;
            Widgets.Label(optionRect.TopPart(0.5f).Rounded(), pawn.Name is not NameTriple nameTriple ? pawn.LabelShort : string.IsNullOrEmpty(nameTriple.Nick) ? nameTriple.First : nameTriple.Nick);
            Widgets.Label(optionRect.BottomPart(0.5f).Rounded(), Text.CalcSize(pawn.story.TitleCap).x > optionRect.width ? pawn.story.TitleShortCap : pawn.story.TitleCap);
#if NET48
            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(optionRect))
#else
            if (Event.current.type == EventType.mouseDown && Mouse.IsOver(optionRect))
#endif
            {
                curPawnIndex = pawnIndex;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            Widgets.EndGroup();
            Widgets.EndGroup();
            if (ReorderableWidget.Reorderable(reorderableGroupID, entryRect.ExpandedBy(4f)))
            {
                Widgets.DrawRectFast(entryRect, Widgets.WindowBGFillColor * new Color(1f, 1f, 1f, 0.5f));
            }
            if (Mouse.IsOver(entryRect))
            {
                TooltipHandler.TipRegion(entryRect, new TipSignal("DragToReorder".Translate(), pawn.GetHashCode() * 3499));
            }

            entryRect.y += 60f;
        }
        GUI.EndScrollView();
        Rect hintRect = rect;
        hintRect.yMin = position.yMax;
        using (new TextBlock(Color.gray))
        {
            Widgets.Label(hintRect, "DragToReorder".Translate());
        }
    }
}
