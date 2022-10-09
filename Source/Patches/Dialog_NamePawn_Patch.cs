using System;
using System.Linq;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using DarkColourPicker_Forked;

namespace JobInBar
{

    [HarmonyPatch(typeof(Dialog_NamePawn))]
    [HarmonyPatch("get_InitialSize")]
    // Expand the dialog to make room for new buttons
    public class Dialog_NamePawn_get_InitialSize_Patch
    {
        public static void Postfix(ref Vector2 __result)
        {
            __result.y += 36f;
        }
    }

    [HarmonyPatch(typeof(Dialog_NamePawn))]
    [HarmonyPatch("DoWindowContents")]
    public class Dialog_NamePawn_DoWindowContents_Patch
    {
        public static void Postfix(ref Pawn ___pawn, Rect inRect)
        {
            Pawn pawn = ___pawn;

            Rect regionRect = inRect;
            regionRect.width -= 64f;
            regionRect.x += 32f;
            regionRect.height = 36f;
            regionRect.y += 90f;

            PawnLabelCustomColors_WorldComponent labelsComp = PawnLabelCustomColors_WorldComponent.instance;
            if (labelsComp == null)
            {
                Log.Error("Could not find PawnLabelCustomColors_WorldComponent. Colors and show settings won't work.");
            }

            labelsComp.GetJobLabelColorFor(pawn, out Color jobCol);

            Rect rectToggle = new Rect(inRect.width - 112f, inRect.height - 90f, 64f, 36f);
            Rect rectColorPicker = new Rect(inRect.width - 42f, inRect.height - 90f, 36f, 36f);

            if (Widgets.ButtonInvisible(rectColorPicker, true))
            {
                Find.WindowStack.Add(
                    new Dialog_ColourPicker(
                        jobCol.ToOpaque(),
                        (newColor) => labelsComp.SetJobLabelColorFor(pawn, newColor)
                    )
                );
            }

            Widgets.DrawBoxSolid(rectColorPicker, jobCol);

            bool shouldDraw = labelsComp.GetDrawJobLabelFor(pawn);
            Widgets.CheckboxLabeled(rectToggle, "JobInBar_ShowJobLabelFor".Translate(), ref shouldDraw, placeCheckboxNearText: true);
            labelsComp.SetDrawJobLabelFor(pawn, shouldDraw);
        }
    }
}