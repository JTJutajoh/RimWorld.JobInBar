using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace JobInBar.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("NamePawn")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    // ReSharper disable once InconsistentNaming
    internal static class Patch_Dialog_NamePawn_AddOptions
    {
        private static float DialogAdditionalHeight => 48f;

        private static float _startY = DialogAdditionalHeight;

        /// <summary>
        /// Used to ignore non-colonist pawns and pets.
        /// </summary>
        private static bool IsValidPawn(Pawn pawn)
        {
            return (pawn.RaceProps?.Humanlike ?? false) && pawn.IsColonist;
        }

        /// <summary>
        /// Patch that expands the rename dialog before anything is added to it.
        /// </summary>
        [HarmonyPatch(typeof(Dialog_NamePawn), "DoWindowContents")]
        [HarmonyPrefix]
        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void ExpandWindow(ref Vector2 ___size, ref Pawn? ___pawn)
        {
            if (!Settings.ModEnabled) return;
            if (___pawn == null || !IsValidPawn(___pawn)) return;

            var additionalY = DialogAdditionalHeight;

            if (ModsConfig.RoyaltyActive && Settings.DrawRoyalTitles && ___pawn.royalty?.MainTitle() is not null)
            {
                additionalY += 32f;
            }

            if (ModsConfig.IdeologyActive && Settings.DrawIdeoRoles && ___pawn.ideo?.Ideo?.GetRole(___pawn) is not null)
            {
                additionalY += 32f;
            }

            _startY = additionalY - 8f;

            ___size = new Vector2(___size.x, ___size.y + additionalY);
        }

        /// <summary>
        /// Patch that adds the custom GUI to the rename dialog
        /// </summary>
        [HarmonyPatch(typeof(Dialog_NamePawn), "DoWindowContents")]
        [HarmonyPostfix]
        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void AddGUI(ref Pawn? ___pawn, Rect inRect)
        {
            if (!Settings.ModEnabled) return;
            if (___pawn == null || !IsValidPawn(___pawn)) return;

            try
            {
                DoExtraWindowContents(___pawn, inRect);
            }
            catch (Exception e)
            {
                Log.Exception(e, extraMessage: "Rename dialog", once: true);
            }
        }

        private static void DoExtraWindowContents(Pawn pawn, Rect inRect)
        {
            if (!Settings.ModEnabled) return;
            var labelsComp = LabelsTracker_WorldComponent.Instance;
            if (labelsComp is null)
            {
                Log.Error("Error while trying to add to rename dialog");
                return;
            }

            var containerRect = inRect.BottomPartPixels(_startY);
            containerRect.ContractedBy(0f, 8f);
            var curY = containerRect.yMin;

            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(containerRect.xMin, containerRect.yMin, containerRect.width);
            GUI.color = Color.white;
            curY += 8f;

            // Job title toggle
            DoLabelRow(
                containerRect,
                ref curY,
                "JobInBar_ShowJobLabelFor".Translate(),
                ref labelsComp[pawn].ShowBackstory,
                "DefaultJobLabelColor",
                pawn.story.Title,
                pawn,
                color => labelsComp[pawn].BackstoryColor = color,
                labelsComp[pawn].BackstoryColor ?? Settings.DefaultJobLabelColor,
                Settings.DrawJobTitleBackground
            );

            if (ModsConfig.RoyaltyActive && Settings.DrawRoyalTitles && pawn.royalty?.MainTitle() is not null)
            {
                DoLabelRow(
                    containerRect,
                    ref curY,
                    "JobInBar_ShowRoyaltyLabelFor".Translate(),
                    ref labelsComp[pawn].ShowRoyalTitle
                );
            }

            if (ModsConfig.IdeologyActive && Settings.DrawIdeoRoles && pawn.ideo?.Ideo?.GetRole(pawn) is not null)
            {
                DoLabelRow(
                    containerRect,
                    ref curY,
                    "JobInBar_ShowIdeoLabelFor".Translate(),
                    ref labelsComp[pawn].ShowIdeoRole
                );
            }
        }

        private static void DoLabelRow(
            Rect containerRect,
            ref float curY,
            string label,
            ref bool checkOn
        )
        {
            var lineHeight = Text.LineHeightOf(GameFont.Medium);

            var rectToggle = new Rect(containerRect.xMin, curY, containerRect.width, lineHeight);
            Widgets.CheckboxLabeled(
                rectToggle,
                label,
                ref checkOn,
                placeCheckboxNearText: false
            );
            
            curY += lineHeight + 4f;
        }

        private static void DoLabelRow(
            Rect containerRect,
            ref float curY,
            string label,
            ref bool checkOn,
            string key,
            string exampleText,
            Pawn pawn,
            Action<Color>? onColorApply,
            Color color,
            bool drawBackground = true
        )
        {
            var lineHeight = Text.LineHeightOf(GameFont.Medium);
            var colorButtonSize = lineHeight - 4f;
            
            // Draw the original row without a color button
            var rowRect = containerRect;
            if (checkOn)
            {
                rowRect = rowRect.LeftPartPixels(containerRect.width - colorButtonSize - 8f);
            }
            DoLabelRow(rowRect,
                ref curY,
                label,
                ref checkOn);

#if v1_4 || v1_5 || v1_6
            if (!checkOn) return;

            // Color picker
            var rectColor = containerRect.RightPartPixels(colorButtonSize);
            rectColor.yMin = curY - lineHeight - 2f;
            rectColor.yMax = curY - 2f;
            rectColor = rectColor.MiddlePartPixels(colorButtonSize,
                colorButtonSize);

            Widgets.DrawLightHighlight(rectColor);
            Widgets.DrawBoxSolid(rectColor,
                color);
            
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawBox(rectColor);
            GUI.color = Color.white;
            
            Widgets.DrawHighlightIfMouseover(rectColor);

            if (Widgets.ButtonInvisible(rectColor))
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Find.WindowStack.Add(
                    new Dialog_LabelColorPicker(pawn,
                        key,
                        color,
                        drawBackground,
                        onColorApply,
                        Settings.DefaultJobLabelColor,
                        exampleText)
                );
            }
#endif
        }
    }
}