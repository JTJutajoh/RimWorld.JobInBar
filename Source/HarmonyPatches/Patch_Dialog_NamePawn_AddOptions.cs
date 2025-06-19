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
        /// Patch that expands the rename dialog before anything is added to it based on what labels will be added to it
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
            //TODO: Maybe replace these checkboxes with dropdowns that include an "Only when hovered" option
            var color = labelsComp[pawn].BackstoryColor ?? Settings.DefaultJobLabelColor;
            DoLabelRow(
                containerRect,
                ref curY,
                checkboxLabel: "JobInBar_ShowJobLabelFor".Translate(),
                checkOn: ref labelsComp[pawn].ShowBackstory,
                color: color,
                onColorButtonClick: () =>
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    Find.WindowStack.Add(
                        new Dialog_LabelColorPicker(
                            color: labelsComp[pawn].BackstoryColor,
                            defaultColor: Settings.DefaultJobLabelColor,
                            exampleText: pawn.story.TitleCap,
                            exampleBackgrounds: Settings.DrawJobTitleBackground,
                            onColorApply: newColor =>
                            {
                                // If the new color is the default color, cache null instead
                                labelsComp[pawn].BackstoryColor =
                                    newColor.IndistinguishableFrom(Settings.DefaultJobLabelColor) ? null : newColor;
                            },
                            onColorChanged: newColor =>
                            {
                                // Naively cache the new color, it can wait to be checked against the default
                                // This makes the actual label in the colonist bar update as you change the values
                                labelsComp[pawn].BackstoryColor = newColor;
                            },
                            onCancel: delegate
                            {
                                // Reset to the color it was before the dialog was spawned if the user clicked Cancel
                                labelsComp[pawn].BackstoryColor = color;
                            }
                        )
                    );
                }
            );

            //TODO: Change how individual toggles work to be able to override the global setting
            if (ModsConfig.RoyaltyActive && Settings.DrawRoyalTitles && pawn.royalty?.MainTitle() is not null)
            {
                DoLabelRow(containerRect, ref curY, "JobInBar_ShowRoyaltyLabelFor".Translate(),
                    ref labelsComp[pawn].ShowRoyalTitle, null);
            }

            if (ModsConfig.IdeologyActive && Settings.DrawIdeoRoles && pawn.ideo?.Ideo?.GetRole(pawn) is not null)
            {
                DoLabelRow(containerRect, ref curY, "JobInBar_ShowIdeoLabelFor".Translate(),
                    ref labelsComp[pawn].ShowIdeoRole, null);
            }
        }

        /// <summary>
        /// Draws a row in the pawn NamePawn dialog with a checkbox to toggle each label individually for the specific pawn
        /// </summary>
        /// <param name="containerRect">The rect containing ALL rows, not just this one.</param>
        /// <param name="curY">Ref that vertically lays out rows when carried over between them.</param>
        /// <param name="checkboxLabel">String displayed as the primary content of the row, what the checkbox value represents.</param>
        /// <param name="checkOn">The state of the checkbox, whether the corresponding label is enabled or not.</param>
        /// <param name="extraGUIOnChecked">Optional delegate to add elements to the row. It should return a modified rect that the checkbox will be drawn within.</param>
        private static void DoLabelRow(Rect containerRect, ref float curY, string checkboxLabel, ref bool checkOn,
            Func<Rect, Rect>? extraGUIOnChecked)
        {
            var lineHeight = Text.LineHeightOf(GameFont.Medium);
            var rowRect = new Rect(containerRect.xMin, curY, containerRect.width, lineHeight);

            if (checkOn)
            {
                rowRect = extraGUIOnChecked?.Invoke(rowRect) ?? rowRect;
            }

            Widgets.CheckboxLabeled(rowRect, checkboxLabel, ref checkOn, placeCheckboxNearText: false);

            curY += lineHeight + 4f;
        }

        /// <summary>
        /// Draws a row in the pawn NamePawn dialog with settings to control the appearance of the pawn's label.<br />
        /// This overload also adds a button that opens a <see cref="Dialog_LabelColorPicker"/> to set the corresponding
        /// label's color in <see cref="LabelsTracker_WorldComponent"/>
        /// </summary>
        /// <param name="containerRect">The rect containing ALL rows, not just this one.</param>
        /// <param name="curY">Ref that vertically lays out rows when carried over between them.</param>
        /// <param name="checkboxLabel">String displayed as the primary content of the row, what the checkbox value represents.</param>
        /// <param name="checkOn">The state of the checkbox, whether the corresponding label is enabled or not.</param>
        /// <param name="color">The starting color that this setting represents.</param>
        /// <param name="onColorButtonClick">Delegate called when the button is clicked, should spawn the dialog</param>
        private static void DoLabelRow(
            Rect containerRect,
            ref float curY,
            string checkboxLabel,
            ref bool checkOn,
            Color color,
            Action onColorButtonClick
        )
        {
            DoLabelRow(containerRect, ref curY, checkboxLabel, ref checkOn, extraGUIOnChecked: (rowRect) =>
            {
#if v1_4 || v1_5 || v1_6 // Color picking doesn't really work in 1.3 or earlier, so disable all of this
                var lineHeight = Text.LineHeightOf(GameFont.Medium);
                var colorButtonSize = lineHeight - 4f;

                var colorButtonRect = rowRect.RightPartPixels(colorButtonSize);
                colorButtonRect = colorButtonRect.MiddlePartPixels(colorButtonSize, colorButtonSize);
                colorButtonRect.y += 2f;

                ColorButton(colorButtonRect, color, onColorButtonClick);

                rowRect.xMax = colorButtonRect.xMin - 8f;
                return rowRect;
#endif
            });
        }

        /// <summary>
        /// Draw a simple colored box that when clicked fires an action
        /// </summary>
        private static void ColorButton(Rect rect, Color color, Action onColorButtonClick)
        {
            Widgets.DrawLightHighlight(rect);
            Widgets.DrawBoxSolid(rect,
                color);

            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            Widgets.DrawHighlightIfMouseover(rect);

            if (Widgets.ButtonInvisible(rect))
            {
                onColorButtonClick.Invoke();
            }
        }
    }
}