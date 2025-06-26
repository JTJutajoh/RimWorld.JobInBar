using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace JobInBar.HarmonyPatches;

//BUG: Need an alternate patch for legacy versions before the new NamePawn dialog

/// <summary>
///     Patch that adds a button next to the "Title" textbox in the pawn rename dialog. Clicking the button opens
///     <see cref="Dialog_LabelSettings" />
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory("NamePawn")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
internal static class Patch_Dialog_NamePawn_NameContext_TitleRowButton
{
    [UsedImplicitly]
    static bool Prepare()
    {
        var skip = LegacySupport.CurrentRWVersion <= RWVersion.v1_3;
        if (skip)
            Log.Warning(
                $"Skipping {nameof(Patch_Dialog_NamePawn_NameContext_TitleRowButton)} patch, requires RimWorld 1.4+.");
        return !skip;
    }

    // Cache the modified textbox width so it only gets modified once
    private static float _textboxWidth = -1f;

    [HarmonyPatch("NameContext", "MakeRow")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static void AddButtonToTitleRow(object __instance, ref RectDivider divider, ref float ___textboxWidth, Pawn pawn)
    {
        if (!Settings.ModEnabled || !Settings.EnabledButtonLocations.HasFlag(Settings.ButtonLocations.NamePawn)) return;

        // First check which textbox in the dialog this row is based on the name of the box
        var textboxName = (string?)Traverse.Create(__instance)?.Field("textboxName")?.GetValue() ?? "";
        // Skip all the textboxes we don't care about
        if (textboxName != "BackstoryTitle") return;

        // Ensure that this only runs while there's a valid world component, AKA in-game
        var labelsComp = LabelsTracker_WorldComponent.Instance;
        if (labelsComp is null) return;

        var buttonSize = divider.Rect.height;

        const float rectDividerMargin = 17f; // Just grabbed from vanilla RectDivider

        // Calculate the modified textbox width only once
        if (_textboxWidth < 0f) _textboxWidth = ___textboxWidth - buttonSize - rectDividerMargin;

        // Resize the textbox to make room for the added button
        ___textboxWidth = _textboxWidth;

        var rect = divider.NewCol(buttonSize);

        CustomWidgets.LabelSettingsButton(pawn, rect);
    }
}

[HarmonyPatch]
[HarmonyPatchCategory("NamePawn")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
internal static class Patch_Dialog_NamePawn_Legacy_AddButton
{
    [UsedImplicitly]
    static bool Prepare(MethodBase original)
    {
        var doPatch = LegacySupport.CurrentRWVersion <= RWVersion.v1_3;
        if (doPatch && original is not null)
            Log.Message(
                $"Doing {nameof(Patch_Dialog_NamePawn_Legacy_AddButton)} patch for legacy RimWorld version...");
        return doPatch;
    }

    [HarmonyPatch(typeof(Dialog_NamePawn), nameof(Dialog_NamePawn.DoWindowContents))]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void AddButton(Dialog_NamePawn __instance, Pawn ___pawn, Rect inRect)
    {
        if (!Settings.ModEnabled || !Settings.EnabledButtonLocations.HasFlag(Settings.ButtonLocations.NamePawn)) return;

        var rect = inRect.BottomPartPixels(32f).LeftPartPixels(32f);

        CustomWidgets.LabelSettingsButton(___pawn, rect);
    }
}
