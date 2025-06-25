using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace JobInBar;

/// <summary>
///     Classes with this attribute are checked against the supplied condition(s) before patching
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class HarmonyPatchLegacySupportAttribute : Attribute
{
    /// <summary>
    ///     If this patch is skipped due to wrong RW version, this optional string will be included in the warning.
    /// </summary>
    internal readonly string? UnsupportedVersionString;

    internal HarmonyPatchLegacySupportAttribute(
        RWVersion supportedVersion = RWVersion.All,
        RWVersion unsupportedVersion = RWVersion.None,
        string? unsupportedVersionString = null
    )
    {
        SupportedVersion = supportedVersion & ~unsupportedVersion;
        UnsupportedVersionString = unsupportedVersionString;
    }

    internal RWVersion SupportedVersion { get; }

    internal bool IsSupportedVersion => (LegacySupport.CurrentRWVersion & SupportedVersion) != 0;

    internal bool ConditionResult => IsSupportedVersion;
}

/// <summary>
///     Patch classes with this attribute will have Harmony.DEBUG enabled for patching
/// </summary>
internal class HarmonyDebugAttribute : Attribute
{
} //TODO: Implement this

/// <summary>
///     Helper class for all Harmony patching functionality.
/// </summary>
[StaticConstructorOnStartup]
[UsedImplicitly]
internal static class PatchManager
{
    internal static readonly Harmony Harmony;

    private static int _loadedPatches;
    private static int _failedPatches;
    private static int _skippedPatches;

    static PatchManager()
    {
        Harmony = new Harmony(JobInBarMod.Instance!.Content!.PackageId!);

        Log.Message("Running Harmony patches...");

        try
        {
            PatchAll();
        }
        catch (Exception e)
        {
            Log.Exception(e,
                "Error doing Harmony patches. This likely means either the wrong game version or a hard incompatibility with another mod.");
        }

        var totalPatches = _loadedPatches + _failedPatches + _skippedPatches;
        Log.Message($"{_loadedPatches}/{totalPatches} Harmony patches successful.");
        if (_skippedPatches > 0)
            Log.Message($"{_skippedPatches}/{totalPatches} Harmony patches skipped.");
        if (_failedPatches > 0)
            Log.Warning(
                $"{_failedPatches}/{totalPatches} Harmony patches failed! The mod/game might behave in undesirable ways.");
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

    private static void PatchAll()
    {
        PatchCategory("AddLabels");
        PatchCategory("NamePawn");
        PatchCategory("OffsetEquippedWeapon");
        PatchCategory("PlaySettings");
        PatchCategory("StopTracking");
    }

    /// <summary>
    ///     Wrapper for <see cref="Harmony" />.<see cref="Harmony.PatchCategory(string)" /> that logs any errors that occur and
    ///     skips patches that are disabled in the mod's configs.
    /// </summary>
    /// <param name="category">
    ///     Name of the category to pass to <see cref="Harmony" />.
    ///     <see cref="Harmony.PatchCategory(string)" />
    /// </param>
    private static void PatchCategory(string category)
    {
        var patchTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatchCategory), true)
                .Cast<HarmonyPatchCategory>()
                .Any(attr => attr.info?.category == category))
            .ToList();

        // Find any classes in the assembly with a [HarmonyPatchCategory] attribute that matches the category
        var numMethods = patchTypes.SelectMany(t =>
                t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Count(m => m.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0);

        // Find any [HarmonyPatchCondition] attributes on all the types in the category
        var conditions = patchTypes
            .SelectMany(t => t.GetCustomAttributes(typeof(HarmonyPatchLegacySupportAttribute), true)
                .Cast<HarmonyPatchLegacySupportAttribute>())
            .ToList();

        // bitwise AND all of their supportedVersion
        var supportedVersions = conditions.Aggregate(RWVersion.All,
            (current, condition) => current & condition.SupportedVersion);

        // If the result is not a supported version, fail
        if ((supportedVersions & LegacySupport.CurrentRWVersion) == 0)
        {
            Log.Warning(
                $"Patch category \"{category}\" ({numMethods} methods) skipped.\nOnly supported on RimWorld versions: {supportedVersions.ToString().Replace("_", ".").Replace("v", "")}.");
            _skippedPatches += numMethods;

            foreach (var condition in conditions)
                if (condition.UnsupportedVersionString != null)
                    Log.Message(condition.UnsupportedVersionString);

            return;
        }

        try
        {
            Log.Trace($"Patching category \"{category}\" ({numMethods} methods)...");
            Harmony.PatchCategory(category);
        }
        catch (Exception e)
        {
            Log.Exception(e, $"Error patching category {category}");
            _failedPatches += numMethods;
            return;
        }

        _loadedPatches += numMethods;
    }
}
