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
        private const float DialogAdditionalHeight = 80f;

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

            ___size = new Vector2(___size.x, ___size.y + DialogAdditionalHeight);
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
            
            var containerRect = inRect.BottomPartPixels(DialogAdditionalHeight - 8f);
            containerRect.ContractedBy(0f, 8f);
            var curY = containerRect.yMin;
            var lineHeight = Text.LineHeightOf(GameFont.Medium);

            Widgets.DrawLineHorizontal(containerRect.xMin, containerRect.yMin, containerRect.width);
            curY += 8f;
            
            // Job title toggle
            var rectToggle = new Rect(containerRect.xMin, curY, containerRect.width, lineHeight);
            Widgets.CheckboxLabeled(
                rectToggle,
                "JobInBar_ShowJobLabelFor".Translate(),
                ref labelsComp[pawn].ShowBackstory,
                placeCheckboxNearText: false
            );
            curY += lineHeight + 4f;

            // Job title color picker
            if (labelsComp[pawn].ShowBackstory)
            {
                var rectColorPick = new Rect(containerRect.xMin, curY, containerRect.width, lineHeight);
                Widgets.Label(rectColorPick, "JobInBar_JobLabelColor".Translate());
                var jobColor = pawn.JobLabelColor();
                var buttonWidth = 80f;
                var rectColor = new Rect(rectColorPick.xMax - lineHeight - buttonWidth - 8f, rectColorPick.yMin, buttonWidth, lineHeight);
                
                if (Widgets.ButtonTextSubtle(rectColor, "JobInBar_Change".Translate()))
                {
                    Find.WindowStack.Add(
                        new Dialog_ChooseColor(
                            "JobInBar_LabelColorPickerHeading".Translate(),
                            jobColor,
                            Settings.AllColors,
                            color =>
                            {
                                LogPrefixed.Debug($"Setting color for {pawn.Name} to {color}");
                                labelsComp[pawn].BackstoryColor = color;
                            }
                        )
                    );
                }
                Widgets.DrawBoxSolid(new Rect(rectColor.xMax + 8f, rectColor.yMin, lineHeight, lineHeight), jobColor);
                
                curY += lineHeight;
            }
        }
    }
}