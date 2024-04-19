using System;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public class LabelDrawer
    {
        // Generic method used to draw all custom labels
        public static void DrawCustomLabel(Vector2 pos, string labelToDraw, Color labelColor, float truncateToWidth = 9999f, bool truncate = true)
        {
            //GameFont font2 = Text.Font;
            Text.Font = GameFont.Tiny;
            //Text.Font = font2;

            if (truncate)
                labelToDraw = LabelUtils.TruncateLabel(labelToDraw, truncateToWidth, Text.Font);

            float pawnLabelNameWidth = Text.CalcSize(labelToDraw).x;

            // calculate the sizes
            Rect rect = LabelUtils.GetLabelRect(pos, pawnLabelNameWidth);
            Rect bgRect = LabelUtils.GetLabelBGRect(pos, pawnLabelNameWidth);

            if (Settings.DrawBG)
                GUI.DrawTexture(bgRect, TexUI.GrayTextBG);

            // Override the label color with the global opacity mod setting
            labelColor.a = Settings.labelAlpha;

            GUI.color = labelColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, labelToDraw);

            // Reset the gui drawing settings to what they were before
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawLabels(Pawn colonist, Vector2 pos, ColonistBar bar, Rect rect, float truncateToWidth = 9999f)
        {
            Vector2 lineOffset = new Vector2(0, Text.LineHeightOf(GameFont.Tiny) + Settings.ExtraOffsetPerLine); // 1.3+ only

            // Apply position offsets
            pos = new Vector2(pos.x, pos.y + colonist.GetLabelPositionOffset());
            if (colonist.GetShouldDrawPermanentLabels(rect))
            {
                if (colonist.GetShouldDrawJobLabel())
                {
                    DrawCustomLabel(pos, colonist.GetJobLabel(), colonist.GetJobLabelColorForPawn(), truncateToWidth);
                    pos += lineOffset;
                }
                if (colonist.GetShouldDrawRoyalTitleLabel())
                {
                    Color imperialColor = new Color(0.85f, 0.85f, 0.75f);
                    DrawCustomLabel(pos, colonist.GetRoyalTitleLabel(), imperialColor, truncateToWidth);
                    pos += lineOffset;
                }
                if (colonist.GetShouldDrawIdeoRoleLabel())
                {
                    DrawCustomLabel(pos, colonist.GetIdeoRoleLabel(), colonist.GetIdeoLabelColorForPawn(), truncateToWidth);
                    pos += lineOffset;
                }
            }
            if (Settings.DrawCurrentJob && Mouse.IsOver(rect))
            {
                DrawCustomLabel(pos, colonist.GetJobDescription(), Settings.currentJobLabelColor, truncate: false);
                pos += lineOffset;
            }
        }
    }
}
