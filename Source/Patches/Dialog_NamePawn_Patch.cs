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
            //Log.Message("Running patch");
            Pawn pawn = ___pawn;

            Rect regionRect = inRect;
            regionRect.width -= 64f;
            regionRect.x += 32f;
            regionRect.height = 36f;
            regionRect.y += 90f;

            float swatchSize = 24f;

            PawnLabelCustomColors_WorldComponent labelsComp;
            //labelsComp = Find.World.components.OfType<PawnLabelCustomColors_WorldComponent>().First();
            labelsComp = PawnLabelCustomColors_WorldComponent.instance;
            if (labelsComp == null)
            {
                Log.Error("Could not find PawnLabelCustomColors_WorldComponent. Colors and show settings won't work.");
            }

            //Color nameCol;
            //Color jobCol;

            labelsComp.GetJobLabelColorFor(pawn, out Color jobCol);

            Rect leftRect = new Rect(15f, 88f, inRect.width / 2f - 20f, 36f);
            // Name recoloring disabled for now
            /*

            if (Widgets.ButtonText(nameRect, "JobInBar_NameColorButton".Translate(), true, true, true) || Widgets.ButtonInvisible(nameRect, true))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(nameCol,
                (newColor) =>
                {
                    labelsComp.SetNameColorFor(pawn, newColor);
                }
                ));
            }
            Rect swatchRectName = new Rect(nameRect.x+8f, nameRect.y+6f, swatchSize, swatchSize);
            Widgets.DrawBoxSolid(swatchRectName, nameCol);*/

            Rect rightRect = new Rect(inRect.width/2, 88f, inRect.width/2f - 20f, 36f);

            if (Widgets.ButtonText(rightRect, "JobInBar_JobColorButton".Translate(), true, true, true) || Widgets.ButtonInvisible(rightRect, true))
            {
                Find.WindowStack.Add(new Dialog_ColourPicker(jobCol.ToOpaque(),
                (newColor) =>
                {
                    labelsComp.SetJobLabelColorFor(pawn, newColor);
                }
                ));
            }
            Rect swatchRectJob = new Rect(rightRect.x+rightRect.width - swatchSize - 8f, rightRect.y+6f, swatchSize, swatchSize);
            Widgets.DrawBoxSolid(swatchRectJob, jobCol);

            bool shouldDraw = labelsComp.GetDrawJobLabelFor(pawn);
            Widgets.CheckboxLabeled(leftRect, "JobInBar_ShowJobLabelFor".Translate(), ref shouldDraw, placeCheckboxNearText: true);
            labelsComp.SetDrawJobLabelFor(pawn, shouldDraw);
        }
    }
}