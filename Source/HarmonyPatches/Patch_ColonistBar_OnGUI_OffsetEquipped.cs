using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;

namespace JobInBar.HarmonyPatches;

/// <summary>
///     Transpiler that modifies the rect used to draw the equipped weapon in the colonist bar and offset it by an amount
///     set in settings
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory("OffsetEquippedWeapon")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
// ReSharper disable once InconsistentNaming
internal static class Patch_ColonistBar_OnGUI_OffsetEquipped
{
    private static readonly MethodInfo? IsWeaponGetterAnchor = AccessTools.PropertyGetter(typeof(ThingDef), "IsWeapon");
    private static float OffsetPerLabel => 16f + Settings.ExtraOffsetPerLine;

    [UsedImplicitly]
    static bool Prepare(MethodBase original)
    {
        if (!ModCompatibility.CheckForMod(Patch_ShowUtilityApparelCompat.TargetPackageId,
                out var modMetaData)) return true;

        Log.Message(
            $"\"{modMetaData!.Name}\" is active, skipping default {nameof(Patch_ColonistBar_OnGUI_OffsetEquipped)} patch...");
        return false;
    }

    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    [HarmonyTranspiler]
    [UsedImplicitly]
    static IEnumerable<CodeInstruction> OffsetWeaponIcon(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        if (IsWeaponGetterAnchor == null)
            throw new InvalidOperationException(
                $"Couldn't find {nameof(IsWeaponGetterAnchor)} method for {nameof(Patch_ColonistBar_OnGUI_OffsetEquipped)}.{MethodBase.GetCurrentMethod()} patch");

        for (var i = 0; i < codes.Count; i++)
        {
            // Search for right after the condition that checks if the equipped thing is a weapon
            if (i > 7 && codes[i - 6]!.Calls(IsWeaponGetterAnchor))
            {
                // Load the current entry (local variable 6)
                yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                // Load the current entry's pawn
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ColonistBar.Entry), "pawn")!);
                // Load the offset from settings onto the stack
                yield return CodeInstruction.Call(typeof(Patch_ColonistBar_OnGUI_OffsetEquipped),
                    nameof(GetOffsetFor))!;
                // Add the offset to rect.y before it is used to construct a new Rect
                yield return new CodeInstruction(OpCodes.Add);
            }

            yield return codes[i]!;
        }
    }

    internal static float GetOffsetFor(Pawn pawn)
    {
        if (!Settings.ModEnabled) return 0f;
        if (!Settings.OffsetEquippedByLabels) return Settings.OffsetEquippedExtra;
        if (Settings.DrawLabelOnlyOnHover && PawnCache.HoveredPawn != pawn) return 0f;

        var cache = PawnCache.GetOrCache(pawn);

        var offset = 0f;

        offset += Settings.JobLabelVerticalOffset;

        if (cache.DrawJobLabel)
            offset += OffsetPerLabel;
        if (cache.DrawIdeoRole)
            offset += OffsetPerLabel;
        if (cache.DrawRoyalTitle)
            offset += OffsetPerLabel;

        if ((Settings.MoveWeaponBelowCurrentTask && !Settings.CurrentTaskUseAbsolutePosition) && cache is { IsHovered: true, CurrentTask: not null })
            offset += OffsetPerLabel;


        return offset + Settings.OffsetEquippedExtra;
    }
}
