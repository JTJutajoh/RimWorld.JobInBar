using System;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public class LabelDrawer
    {
        /// <summary>
        /// Draws a custom label at the specified position with the specified text, color, and truncation options.
        /// </summary>
        /// <param name="pos">The position at which to draw the label.</param>
        /// <param name="labelToDraw">The text of the label to draw.</param>
        /// <param name="labelColor">The color of the label to draw.</param>
        /// <param name="truncateToWidth">The maximum width, in pixels, of the label after truncation.</param>
        /// <param name="truncate">A value indicating whether to truncate the label if it exceeds the specified width.</param>
        public static void DrawCustomLabel(Vector2 pos, string labelToDraw, Color labelColor, float truncateToWidth = 9999f, bool truncate = true)
        {
            // Save the current font and restore it after drawing the label
            //GameFont font2 = Text.Font;
            Text.Font = GameFont.Tiny;
            //Text.Font = font2;

            if (truncate)
                labelToDraw = LabelUtils.TruncateLabel(labelToDraw, truncateToWidth, Text.Font);

            float pawnLabelNameWidth = Text.CalcSize(labelToDraw).x;

            Rect rect = LabelUtils.GetLabelRect(pos, pawnLabelNameWidth);
            Rect bgRect = LabelUtils.GetLabelBGRect(pos, pawnLabelNameWidth);

            if (Settings.DrawBG)
                GUI.DrawTexture(bgRect, TexUI.GrayTextBG);

            // Override the label color with the global opacity mod setting
            labelColor.a = Settings.labelAlpha;

            GUI.color = labelColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, labelToDraw);

            // Reset the GUI color to white
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
                    DrawCustomLabel(pos, colonist.GetRoyalTitleLabel(), LabelUtils.imperialColor, truncateToWidth);
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
