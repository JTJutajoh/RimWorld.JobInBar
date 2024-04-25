using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace JobInBar
{
    public class LabelsCardUtility
    {
        public static Vector2 BaseLabelCardSize = new Vector2(500f, 455f);

        // If need-be, scale the card to accomodate variable elements here (like royal titles?)
        public static Vector2 LabelCardSize(Pawn pawn) => BaseLabelCardSize;

        public static void DrawLabelsCard(Rect rect, Pawn pawn)
        {
            // Some notes:
            // Use TooltipHandler.TipRegionByKey()
            // UI can be broken up into left/right columns

            // Ratio of the left column to the right column
            float columnRatio = 0.4f;
            Widgets.BeginGroup(rect);

            float margin = 42f;

            Rect leftRect = rect.LeftPartPixels(rect.width*columnRatio - margin/2);
            Rect rightRect = rect.RightPartPixels(rect.width*(1-columnRatio) - margin/2);
            rightRect.max -= new Vector2(17f,17f);

            DoLeftSection(rect, leftRect, pawn);
            Widgets.DrawLineVertical(leftRect.xMax + margin / 2, 0f, rect.height);
            DoRightSection(rect, rightRect, pawn);

            Widgets.EndGroup();
        }

        public static List<Color> AvailableColors = new List<Color>()
        {
            Color.white,
            Color.red,
            Color.blue,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.gray,
            Color.magenta,
            new Color(0.5f,0f,0f),
            new Color(0f,0.5f,0f),
            new Color(0f,0f,0.5f),
            new Color(0.8f,0.35f,0f),
            new Color(0.2f,0.7f,0.7f),
            new Color(0.1f,0.3f,0.9f),
            new Color(0.8f,0.8f,0.2f),
            new Color(0.5f,0.9f,0.4f),
            new Color(0.8f,0.7f,0f),
            new Color(0.7f,0.95f,0.4f),
            new Color(0.5f,0.85f,0.7f),
            new Color(0.1f,0.3f,0.15f),
            new Color(0f,0.2f,0.9f),
            new Color(0.7f,0.5f,0.6f),
            new Color(0.75f,0.75f,0.1f),
            Color.white,
            Color.red,
            Color.blue,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.gray,
            Color.magenta,
            Color.white,
            Color.red,
            Color.blue,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.gray,
            Color.magenta,
        };

        [Obsolete("just for testing")]
        static bool showAnyLabels = true;
        [Obsolete("just for testing")]
        static bool showBackstory = true;
        [Obsolete("just for testing")]
        static bool showRoyalTitle = true;
        [Obsolete("just for testing")]
        static bool showIdeoRole = true;
        [Obsolete("just for testing")]
        static Color testColor = Color.white;
        static Vector2 colorScrollPos = Vector2.zero;
        static void DoLeftSection(Rect rect, Rect leftRect, Pawn pawn)
        {
            var ls = new Listing_Standard();
            ls.Begin(leftRect);

            // Column header
            Text.Font = GameFont.Medium;
            ls.Label("Settings");
            Text.Font = GameFont.Small;

            ls.Gap();
            ls.CheckboxLabeled("JobInBar_ShowAnyLabels".Translate(), ref showAnyLabels);
            if (!showAnyLabels)
            {
                ls.End();
                return;
            }

            ls.GapLine();
            ls.CheckboxLabeled("Backstory".Translate(), ref showBackstory);
            if (showBackstory)
            {
                ls.TextEntry(pawn?.story?.Title ?? "");
                ls.ButtonText("Save".Translate());

                // Color selection
                Rect colorsRect = ls.GetRect(120f);
                Widgets.DrawMenuSection(colorsRect);

                colorsRect = colorsRect.ContractedBy(4f);

                var numColors = AvailableColors.Count;
                int numColorsPerRow = Mathf.CeilToInt((colorsRect.width-20f) / (22f + 4f));
                int numRows = Mathf.CeilToInt(numColors / numColorsPerRow) + 3;
                float height = numRows * (22f + 4f);

                Rect viewRect = new Rect(colorsRect.xMin, colorsRect.yMin, colorsRect.width-20f, height);

                Widgets.BeginScrollView(colorsRect, ref colorScrollPos, viewRect, true);
                Widgets.ColorSelector(viewRect, ref testColor, AvailableColors, out float outHeight);
                Widgets.EndScrollView();
                ls.Gap(16f);
            }
            if (pawn?.royalty?.MainTitle() is RoyalTitleDef royalTitle)
            {
                ls.GapLine();
                ls.CheckboxLabeled("Royal Title".Translate(), ref showRoyalTitle);
                if (showRoyalTitle)
                {
                    GUI.color = LabelUtils.imperialColor;
                    ls.Label(royalTitle.LabelCap);
                    GUI.color = Color.white;
                }
            }
            if (pawn?.ideo?.Ideo?.GetRole(pawn) is Precept_Role role)
            {
                ls.GapLine();
                ls.CheckboxLabeled("Ideology Role".Translate(), ref showIdeoRole);
                if (showIdeoRole)
                {
                    GUI.color = role.ideo.colorDef.color;
                    ls.Label(role.LabelCap);
                    GUI.color = Color.white;
                }
            }

            ls.End();
        }

        static void DoRightSection(Rect rect, Rect rightRect, Pawn pawn)
        {
            if (!showAnyLabels)
                return;

            // Column header
            var headerHeight = 50f;
            Text.Font = GameFont.Medium;
            Rect headerRect = rightRect.TopPartPixels(headerHeight);
            Widgets.Label(headerRect, "Label Presets".Translate());
            Text.Font = GameFont.Small;

            // Content
            Rect contentRect = rightRect;
            contentRect.yMin += headerHeight;
            Widgets.DrawBoxSolid(contentRect, new Color(0f,0f,0f,0.15f));
            Widgets.DrawMenuSection(contentRect);
        }
    }
}
