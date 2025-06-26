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
    /// <summary>
    ///     Vanilla hard-codes its buttons to be 30 px, so this constant just copies that.
    /// </summary>
    private const float ButtonSize = 30f;

    static readonly MethodInfo? NamePawnDialogAnchorMethod =
        AccessTools.Method(typeof(PawnNamingUtility), "NamePawnDialog");

    [HarmonyPatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard))]
    [HarmonyTranspiler]
    [UsedImplicitly]
    static IEnumerable<CodeInstruction> AddButtonNextToRenameButton(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        if (NamePawnDialogAnchorMethod == null)
            throw new InvalidOperationException(
                $"Couldn't find {nameof(NamePawnDialogAnchorMethod)} method for {nameof(Patch_CharacterCardUtility_DrawCharacterCard_AddButton)}.{MethodBase.GetCurrentMethod()} patch");

        var foundAnchor = false;
        var hasPatched = false;

        for (var i = 0; i < codes.Count; i++)
        {
            if (!hasPatched && !foundAnchor && codes[i]!.Calls(NamePawnDialogAnchorMethod)) foundAnchor = true;

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

    static void DoButton(ref float curX, Pawn pawn)
    {
        if (!Settings.ModEnabled ||
            !Settings.EnabledButtonLocations.HasFlag(Settings.ButtonLocations.CharacterCard)) return;
        var buttonRect = new Rect(curX, 0f, ButtonSize, ButtonSize);

        if (Widgets.ButtonImage(buttonRect, Icons.LabelSettingsIcon, true, "JobInBar_NamePawn_GearButton".Translate()))
            Find.WindowStack?.Add(new Dialog_LabelSettings(pawn));

        curX -= 40f;
    }
}
