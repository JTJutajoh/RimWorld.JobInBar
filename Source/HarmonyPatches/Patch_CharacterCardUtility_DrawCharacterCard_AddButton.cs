using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;

namespace JobInBar.HarmonyPatches;

/// <summary>
///     Patch that inserts a button to the left of the vanilla Rename button, shifting any subsequence buttons to the left
///     to make room.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory("BioTabButton")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Patch_CharacterCardUtility_DrawCharacterCard_AddButton
{
    [UsedImplicitly]
    static bool Prepare()
    {
        var skip = LegacySupport.CurrentRWVersion <= RWVersion.v1_3;
        if (skip)
            Log.Warning(
                $"Skipping {nameof(Patch_CharacterCardUtility_DrawCharacterCard_AddButton)} patch, requires RimWorld 1.4+.");
        return !skip;
    }

    /// <summary>
    ///     Vanilla hard-codes its buttons to be 30 px, so this constant just copies that.
    /// </summary>
    internal const float ButtonSize = 30f;

    static readonly MethodInfo? AnchorMethod =
        AccessTools.Method(typeof(PawnNamingUtility), "NamePawnDialog");

    [HarmonyPatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard))]
    [HarmonyTranspiler]
    [UsedImplicitly]
    static IEnumerable<CodeInstruction> AddButtonNextToRenameButton(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        if (AnchorMethod == null)
            throw new InvalidOperationException(
                $"Couldn't find {nameof(AnchorMethod)} method for {nameof(Patch_CharacterCardUtility_DrawCharacterCard_AddButton)}.{MethodBase.GetCurrentMethod()} patch");

        var foundAnchor = false;
        var hasPatched = false;

        for (var i = 0; i < codes.Count; i++)
        {
            if (!hasPatched && !foundAnchor && codes[i]!.Calls(AnchorMethod)) foundAnchor = true;

            // Find the next time that curX is stored after the rename button gets added
            if (!hasPatched && foundAnchor && codes[i]!.opcode == OpCodes.Stloc_S)
            {
                yield return codes[i++]!;
                // Load the curX used by the vanilla method
                yield return CodeInstruction.LoadLocal(20, true)!;
                // Load the pawn
                yield return CodeInstruction.LoadArgument(1)!;
                yield return CodeInstruction.Call(typeof(Patch_CharacterCardUtility_DrawCharacterCard_AddButton),
                    nameof(DoButton))!;
                hasPatched = true;
            }

            yield return codes[i]!;
        }

        if (!foundAnchor || !hasPatched)
            Log.Error("Failed to patch CharacterCardUtility.DrawCharacterCard");
    }

    internal static void DoButton(ref float curX, Pawn pawn)
    {
        if (!Settings.ModEnabled ||
            !Settings.EnabledButtonLocations.HasFlag(Settings.ButtonLocations.CharacterCard)) return;
        var buttonRect = new Rect(curX, 0f, ButtonSize, ButtonSize);

        CustomWidgets.LabelSettingsButton(pawn, buttonRect);

        curX -= 40f;
    }
}


[HarmonyPatch]
[HarmonyPatchCategory("BioTabButton")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Patch_CharacterCardUtility_DrawCharacterCard_LegacyPatch
{
    [UsedImplicitly]
    static bool Prepare(MethodBase original)
    {
        var doPatch = LegacySupport.CurrentRWVersion <= RWVersion.v1_3;
        if (doPatch && original is not null)
            Log.Message(
                $"Doing {nameof(Patch_CharacterCardUtility_DrawCharacterCard_LegacyPatch)} patch for legacy RimWorld version...");
        return doPatch;
    }

    private static readonly FieldInfo? TexButtonFieldAnchor = AccessTools.Field(typeof(TexButton), "Rename");

    [HarmonyPatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard))]
    [HarmonyTranspiler]
    [UsedImplicitly]
    static IEnumerable<CodeInstruction> AddButtonNextToRenameButton(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        if (TexButtonFieldAnchor == null)
            throw new InvalidOperationException(
                $"Couldn't find {nameof(TexButtonFieldAnchor)} field for {nameof(Patch_CharacterCardUtility_DrawCharacterCard_LegacyPatch)}.{MethodBase.GetCurrentMethod()} patch");

        var foundAnchor = false;
        var hasPatched = false;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < codes.Count; i++)
        {
            if (!foundAnchor && !hasPatched && codes[i]!.LoadsField(TexButtonFieldAnchor))
            {
                foundAnchor = true;
            }

            if (!hasPatched && foundAnchor && codes[i]!.opcode == OpCodes.Stloc_S)
            {
                yield return codes[i++]!;
                yield return CodeInstruction.LoadLocal(15, true)!;
                yield return CodeInstruction.LoadArgument(1)!;
                yield return CodeInstruction.Call(typeof(Patch_CharacterCardUtility_DrawCharacterCard_AddButton), nameof
                    (Patch_CharacterCardUtility_DrawCharacterCard_AddButton.DoButton))!;
                hasPatched = true;
            }

            yield return codes[i]!;
        }

        if (!foundAnchor || !hasPatched)
            Log.Error("Failed to patch CharacterCardUtility.DrawCharacterCard");
    }
}
