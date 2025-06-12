using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace JobInBar
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public class DoPlaySettingsGlobalControls_ShowLabelsToggle
    {
        public static bool DrawLabels = true;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (!Settings.ModEnabled || worldView) return;

            var texture = ContentFinder<Texture2D>.Get("UI/LabelToggle", true) ?? TexButton.Rename;
            row.ToggleableIcon(ref DrawLabels, texture, "JobInBar_PlaySettingsToggle".Translate(), SoundDefOf.Mouseover_ButtonToggle);
        }
    }
}
