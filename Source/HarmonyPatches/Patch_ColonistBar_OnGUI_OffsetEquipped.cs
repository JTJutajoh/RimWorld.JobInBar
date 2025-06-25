using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;

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

    static bool Prepare(MethodBase original)
    {
        if (!PatchManager.CheckForMod(Patch_ColonistBar_OnGUI_OffsetEquipped_ShowUtilityApparelCompat.TargetPackageId, out var modMetaData)) return true;

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
        if (Settings.DrawLabelOnlyOnHover && LabelDrawer.HoveredPawn != pawn) return 0f;
        var offset = 0f;

        offset += Settings.JobLabelVerticalOffset;

        if (pawn.ShouldDrawJobLabel())
            offset += OffsetPerLabel;
        if (pawn.ShouldDrawIdeoLabel())
            offset += OffsetPerLabel;
        if (pawn.ShouldDrawRoyaltyLabel())
            offset += OffsetPerLabel;

        if (Settings.MoveWeaponBelowCurrentTask && LabelDrawer.HoveredPawn == pawn)
            offset += OffsetPerLabel;


        return offset + Settings.OffsetEquippedExtra;
    }
}

#region Mod Compatibility

/// <summary>
///     Compatibility patch for "[AV] Show Utility Apparel" mod:
///     https://steamcommunity.com/sharedfiles/filedetails/?id=3266625851 <para />
///     That mod's patch ignores the vanilla rect that the normal patch
///     <see cref="Patch_ColonistBar_OnGUI_OffsetEquipped" />
///     modifies in favor of its own static reference to a new rect.<br />
///     This patch simply applies the same offset logic to that rect.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory("OffsetEquippedWeapon")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Patch_ColonistBar_OnGUI_OffsetEquipped_ShowUtilityApparelCompat
{
    internal const string TargetPackageId = "veltaris.colonistbar";

    private static ModMetaData? ShowUtilityApparelMod;
    private static List<Assembly>? ShowUtilityApparelAssemblies;

    [UsedImplicitly]
    static bool Prepare(MethodBase original)
    {
        if (!PatchManager.CheckForMod(TargetPackageId, out ShowUtilityApparelMod))
        {
            Log.Error(
                $"Detected \"{TargetPackageId}\", but failed to get its metadata for compat patches.");
            return false;
        }

        if (!PatchManager.TryGetModAssembly(ShowUtilityApparelMod?.PackageId ?? TargetPackageId, out ShowUtilityApparelAssemblies))
        {
            Log.Error(
                $"Detected \"{ShowUtilityApparelMod?.Name ?? TargetPackageId}\" mod, but failed to find its assemblies for compat patches.");
            return false;
        }

        Log.Message(
            $"Doing alternate {nameof(Patch_ColonistBar_OnGUI_OffsetEquipped_ShowUtilityApparelCompat)} patch...");
        return true;
    }

    [UsedImplicitly]
    static IEnumerable<MethodBase> TargetMethods()
    {
        if (ShowUtilityApparelAssemblies is null)
            yield break;

        // Patch their patch
        // Method is: AV_ColonistBar.ColonistBar_ColonistBarOnGUI_Patch.ColonistBar_ColonistBarOnGUI.After_Hook()
        foreach (var type in ShowUtilityApparelAssemblies.Select(assembly => assembly.GetTypes()
                     .FirstOrDefault(t => t.Name == "ColonistBar_ColonistBarOnGUI_Patch")!
                     .GetNestedType("ColonistBar_ColonistBarOnGUI")!))
            yield return AccessTools.Method(type, "After_Hook")!;
    }

    [HarmonyPrefix]
    [UsedImplicitly]
    static void OffsetEquipped(Pawn ___currentPawn, ref Rect ___currentRect)
    {
        var offset = Patch_ColonistBar_OnGUI_OffsetEquipped.GetOffsetFor(___currentPawn);

        ___currentRect.y += offset;
    }
}

#endregion Mod Compatibility
