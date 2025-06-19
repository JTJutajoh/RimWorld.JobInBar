using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;

namespace JobInBar.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("PlaySettings")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    // ReSharper disable once InconsistentNaming
    internal static class Patch_PlaySettings_GlobalControl_ToggleLabels
    {
        private static bool _drawLabels = true;

        public static bool DrawLabels => !Settings.EnablePlaySettingToggle || _drawLabels;

        [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
        [HarmonyPostfix]
        [UsedImplicitly]
        static void AddPlaySettings(WidgetRow row, bool worldView)
        {
            if (!Settings.ModEnabled || !Settings.EnablePlaySettingToggle) return;
            try
            {
                if (worldView) return;

                var texture = ContentFinder<Texture2D>.Get("UI/LabelToggle") ?? TexButton.Rename; //TODO: Don't load this dynamically every frame
                row.ToggleableIcon(ref _drawLabels, texture, "JobInBar_PlaySettingsToggle".Translate(), SoundDefOf.Mouseover_ButtonToggle);
            }
            catch (Exception e)
            {
                Log.Exception(e, extraMessage: "Show labels toggle", once: true);
            }
        }
    }
}
