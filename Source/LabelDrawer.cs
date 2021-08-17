using System;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace JobInBar
{
    public class LabelDrawer
    {

        // Method used to draw all custom labels
        public static void DrawCustomLabel(Vector2 pos, string labelToDraw, Color labelColor)
        {
            //GameFont font2 = Text.Font;
            Text.Font = GameFont.Tiny;
            //Text.Font = font2;

            float pawnLabelNameWidth = Text.CalcSize(labelToDraw).x;

            // calculate the sizes
            Rect rect = JobInBarUtils.GetLabelRect(pos, pawnLabelNameWidth);
            Rect bgRect = JobInBarUtils.GetLabelBGRect(pos, pawnLabelNameWidth);

            if (Settings.DrawBG)
                GUI.DrawTexture(bgRect, TexUI.GrayTextBG);

            // Override the label color with the global opacity mod setting
            labelColor.a = Settings.labelAlpha;

            GUI.color = labelColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, labelToDraw);

            // Reset the gui drawing settings
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawJobLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string jobLabel = JobInBarUtils.GetJobLabel(colonist, truncateToWidth, GameFont.Tiny);

            DrawCustomLabel(pos, jobLabel, JobInBarUtils.GetLabelColorForPawn(colonist));
        }
        public static void DrawIdeoRoleLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string roleLabel = JobInBarUtils.GetIdeoRoleLabel(colonist, truncateToWidth, GameFont.Tiny);

            DrawCustomLabel(pos, roleLabel, JobInBarUtils.GetIdeoLabelColorForPawn(colonist));
        }
        public static void DrawRoyalTitleLabel(Vector2 pos, Pawn colonist, float truncateToWidth = 9999f)
        {
            string titleLabel = JobInBarUtils.GetRoyalTitleLabel(colonist, truncateToWidth, GameFont.Tiny);

            Color imperialColor = new Color(0.85f, 0.85f, 0.75f);

            DrawCustomLabel(pos, titleLabel, imperialColor);
        }
    }
}
