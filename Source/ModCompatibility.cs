using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using JobInBar.HarmonyPatches;
using UnityEngine;

namespace JobInBar;

/// <summary>
/// Class for sending warnings and changing settings if certain mods are detected.
/// </summary>
[StaticConstructorOnStartup]
internal static class ModCompatibility
{
    private readonly struct ModCompatibilityBehavior
    {
        private readonly string _modPackageId;
        private readonly string _warningMessage;
        private readonly bool _warn;
        private readonly Action<ModMetaData>? _compatAction;

        /// <summary>
        /// Register a given mod's PackageId as potentially having compatibility issues.
        /// </summary>
        /// <param name="modPackageId">The exact, case-insensitive package id of the other mod.</param>
        /// <param name="warningMessage">If the other mod is detected, include this string in the message logged to the player.</param>
        /// <param name="warn">If true, the message will be sent as a warning. Meant for incompatibilities that do not have patches and require user action.</param>
        /// <param name="compatAction">An optional callback to perform if the other mod is detected, will be supplied with the mod's <see cref="ModMetaData"/></param>
        internal ModCompatibilityBehavior(string modPackageId, string warningMessage, bool warn = false,
            Action<ModMetaData>? compatAction = null)
        {
            _modPackageId = modPackageId;
            _warningMessage = warningMessage;
            _warn = warn;
            _compatAction = compatAction;
        }

        /// <summary>
        /// Check if the mod in question is active and print a warning, then invoke the action if any.
        /// </summary>
        /// <returns>true if the mod is active.</returns>
        internal bool Check()
        {
            if (!CheckForMod(_modPackageId, out var mod)) return false;

            Log.CompatibilityWarning($"{mod?.Name} [{_modPackageId}] detected.\n{_warningMessage}", _warn);
            if (mod is not null)
                _compatAction?.Invoke(mod);
            return true;
        }
    }

    /// <summary>
    /// List of mods that are known to have compatibility issues with this mod.<para/>
    /// Some may have compatibility patches, either external to or part of this mod. Others may be hard incompatible.
    /// </summary>
    private static readonly List<ModCompatibilityBehavior> ModCompatibilityBehaviors = new()
    {
        new ModCompatibilityBehavior("derekbickley.ltocolonygroupsfinal",
            "Colony Groups replaces the entire vanilla colonist bar, but it contains a compatibility patch for this mod, so they should work together fine."),
        new ModCompatibilityBehavior("veltaris.colonistbar",
            "Will attempt to apply alternate patch to offset equipped weapon icons to be compatible."),
        new ModCompatibilityBehavior("andromeda.usefulmarks",
            "That mod also adds labels to the colonist bar, recommended to disable them for one or both mods in their respective mod settings pages.",
            true),
    };

    static ModCompatibility()
    {
        foreach (var behavior in ModCompatibilityBehaviors)
            behavior.Check();
    }

    internal static bool CheckForMod(string modIdentifier, out ModMetaData? otherMod)
    {
        otherMod = ModLister.GetActiveModWithIdentifier(modIdentifier);
        return otherMod != null;
    }

    internal static bool TryGetModAssembly(string packageId, out List<Assembly>? assemblies)
    {
        var mod = LoadedModManager.RunningModsListForReading?.FirstOrDefault(m =>
            m.PackageId!.ToLower() == packageId.ToLower());
        assemblies = mod?.assemblies?.loadedAssemblies;
        return assemblies != null;
    }

    internal static bool HasExistingPatches(this MethodInfo method, bool warn = true)
    {
        var patches = Harmony.GetPatchInfo(method);
        if (patches is null) return false;

        var hasPatches = patches.Prefixes?.Count > 0 || patches.Postfixes?.Count > 0 || patches.Transpilers?.Count > 0;

        if (warn && hasPatches)
        {
            Log.Warning(
                $"Found existing Harmony patches for {method.DeclaringType?.FullName}.{method.Name}. If you encounter compatibility issues, please report it on the Workshop page or GitHub issues.\nYou can safely ignore this warning if nothing seems broken.");
            foreach (var patch in patches.Prefixes!.Union(patches.Postfixes!).Union(patches.Transpilers!))
                Log.Message($"Patch: {patch.PatchMethod!.Module.Assembly.FullName}::{patch.PatchMethod.Name}");
        }

        return hasPatches;
    }
}

#region Stub methods

/// <summary>
/// Colony Groups compatibility. It replaces the whole colonist bar and manually calls methods of mods that modify the bar (like this one),
/// but it finds the method by name. So this class exists just as a redirect with a consistent name.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class ColonistBarColonistDrawer_DrawColonist_Patch
{
    [UsedImplicitly]
    public static void Postfix(Rect rect, Pawn colonist, Map pawnMap, bool highlight, bool reordering)
    {
        Patch_ColonistBarDrawer_DrawColonist_AddLabels.AddLabels(rect, colonist, pawnMap, highlight, reordering);
    }
}

#endregion Stub methods

#region Harmony Patches

/// <summary>
///     Compatibility patch for "[AV] Show Utility Apparel" mod:
///     https://steamcommunity.com/sharedfiles/filedetails/?id=3266625851
///     <para />
///     That mod's patch ignores the vanilla rect that the normal patch
///     <see cref="Patch_ColonistBar_OnGUI_OffsetEquipped" />
///     modifies in favor of its own static reference to a new rect.<br />
///     This patch simply applies the same offset logic to that rect.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory("OffsetEquippedWeapon")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Patch_ShowUtilityApparelCompat
{
    internal const string TargetPackageId = "veltaris.colonistbar";

    private static ModMetaData? ShowUtilityApparelMod;
    private static List<Assembly>? ShowUtilityApparelAssemblies;

    [UsedImplicitly]
    static bool Prepare(MethodBase original)
    {
        if (!ModCompatibility.CheckForMod(TargetPackageId, out ShowUtilityApparelMod)) return false;

        if (ShowUtilityApparelMod?.PackageId is null)
        {
            Log.Error(
                $"Detected \"{TargetPackageId}\", but failed to get its metadata for compat patches.");
            return false;
        }

        if (!ModCompatibility.TryGetModAssembly(ShowUtilityApparelMod.PackageId ?? TargetPackageId,
                out ShowUtilityApparelAssemblies))
        {
            Log.Error(
                $"Detected \"{ShowUtilityApparelMod.Name ?? TargetPackageId}\" mod, but failed to find its assemblies for compat patches.");
            return false;
        }

        Log.Message(
            $"Doing alternate {nameof(Patch_ShowUtilityApparelCompat)} patch...");
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

#endregion Harmony Patches
