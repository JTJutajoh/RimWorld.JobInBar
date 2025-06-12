using System;
using System.Collections.Generic;
using System.Linq;
using DarkLog;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace JobInBar
{
    [HarmonyPatch(typeof(Dialog_NamePawn))]
    [HarmonyPatch("DoWindowContents")]
    public class Dialog_NamePawn_DoWindowContents_Patch
    {
        private static float DialogAdditionalHeight => 80f;

        private static float _startY = DialogAdditionalHeight;

        /// <summary>
        /// Used to ignore non-colonist pawns and pets.
        /// </summary>
        private static bool IsValidPawn(Pawn pawn)
        {
            return (pawn.RaceProps?.Humanlike ?? false) && pawn.IsColonist;
        }
        
        /// <summary>
        /// Patch that the rename dialog before anything is added to it.
        /// </summary>
        public static void Prefix(ref Vector2 ___size, ref Pawn? ___pawn)
        {
            if (___pawn == null || !IsValidPawn(___pawn)) return;

            var additionalY = DialogAdditionalHeight;

            if (Verse.ModsConfig.RoyaltyActive && Settings.DrawRoyalTitles && ___pawn.royalty?.MainTitle() is not null)
            {
                additionalY += 32f;
            }
            if (Verse.ModsConfig.IdeologyActive && Settings.DrawIdeoRoles && ___pawn.ideo?.Ideo?.GetRole(___pawn) is not null)
            {
                additionalY += 32f;
            }

            _startY = additionalY - 8f;

            ___size = new Vector2(___size.x, ___size.y + additionalY);
        }
        
        /// <summary>
        /// Patch that adds the custom GUI to the rename dialog
        /// </summary>
        public static void Postfix(ref Pawn? ___pawn, Rect inRect)
        {
            if (___pawn == null || !IsValidPawn(___pawn)) return;
            
            DoExtraWindowContents(___pawn, inRect);
        }

        private static void DoExtraWindowContents(Pawn  pawn, Rect inRect)
        {
            var labelsComp = LabelsTracker_WorldComponent.Instance;
            if (labelsComp is null)
            {
                LogPrefixed.Error("Error while trying to add to rename dialog");
                return;
            }
            
            var containerRect = inRect.BottomPartPixels(_startY);
            containerRect.ContractedBy(0f, 8f);
            var curY = containerRect.yMin;

            Widgets.DrawLineHorizontal(containerRect.xMin, containerRect.yMin, containerRect.width);
            curY += 8f;
            
            // Job title toggle
            DoLabelRow(
                containerRect, 
                ref curY,
                "JobInBar_ShowJobLabelFor".Translate(),
                ref labelsComp[pawn].ShowBackstory,
                labelsComp[pawn].BackstoryColor,
                color => labelsComp[pawn].BackstoryColor = color
            );
            
            if (Verse.ModsConfig.RoyaltyActive && Settings.DrawRoyalTitles && pawn.royalty?.MainTitle() is not null)
            {
                DoLabelRow(
                    containerRect,
                    ref curY,
                    "JobInBar_ShowRoyaltyLabelFor".Translate(),
                    ref labelsComp[pawn].ShowRoyalTitle
                );
            }
            if (Verse.ModsConfig.IdeologyActive && Settings.DrawIdeoRoles && pawn.ideo?.Ideo?.GetRole(pawn) is not null)
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
            ref bool checkOn,
            Color? color = null,
            Action<Color>? onColorApply = null
        )
        {
            var labelsComp = LabelsTracker_WorldComponent.Instance;
            if (labelsComp is null)
            {
                LogPrefixed.Error("Error while trying to add to rename dialog");
                return;
            }
            var lineHeight = Text.LineHeightOf(GameFont.Medium);
            
            var rectToggle = new Rect(containerRect.xMin, curY, containerRect.width, lineHeight);
            Widgets.CheckboxLabeled(
                rectToggle,
                label,
                ref checkOn,
                placeCheckboxNearText: false
            );
            curY += lineHeight + 4f;

#if v1_4 || v1_5 || v1_6
            // Color picker
            if (checkOn && color != null && onColorApply != null)
            {
                var rectColorPick = new Rect(containerRect.xMin, curY, containerRect.width, lineHeight);
                Widgets.Label(rectColorPick, "JobInBar_JobLabelColor".Translate());
                var buttonWidth = 80f;
                var rectColor = new Rect(rectColorPick.xMax - lineHeight - buttonWidth - 8f, rectColorPick.yMin, buttonWidth, lineHeight);
                
                if (Widgets.ButtonTextSubtle(rectColor, "JobInBar_Change".Translate()))
                {
                    Find.WindowStack.Add(
                        new Dialog_ChooseColor(
                            "JobInBar_LabelColorPickerHeading".Translate(),
                            color.Value,
                            Settings.AllColors,
                            onColorApply
                        )
                    );
                }
                Widgets.DrawBoxSolid(new Rect(rectColor.xMax + 8f, rectColor.yMin, lineHeight, lineHeight), color.Value);
                
                curY += lineHeight;
            }
#endif
        }
    }
}