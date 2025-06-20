using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace JobInBar;

[UsedImplicitly]
public class JobInBarMod : Mod
{
    public JobInBarMod(ModContentPack content) : base(content)
    {
        Instance = this;
        // ReSharper disable once RedundantArgumentDefaultValue
        Log.Initialize(this, "cyan");

        GetSettings<Settings>();
    }

    public static JobInBarMod? Instance { get; private set; }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        try
        {
            GetSettings<Settings>()?.DoWindowContents(inRect);
        }
        catch (Exception e)
        {
            Log.Exception(e, "Error drawing mod settings window.", true);
            Widgets.DrawBoxSolid(inRect, new Color(0, 0, 0, 0.5f));
            var errorRect = inRect.MiddlePart(0.4f, 0.25f);
            Widgets.DrawBoxSolidWithOutline(errorRect, Widgets.WindowBGFillColor, Color.red, 5);
            Widgets.Label(errorRect.ContractedBy(16f),
                $"Error rendering settings window:\n\"{e.Message}\", see log for stack trace.\nPlease report this to the mod author.");
        }
    }

    public override string SettingsCategory()
    {
        return "JobInBar_SettingsCategory".Translate();
    }
}

[StaticConstructorOnStartup]
internal static class Icons
{
    internal static readonly Texture2D? LabelToggleIcon = ContentFinder<Texture2D>.Get("UI/LabelToggle");
}

[StaticConstructorOnStartup]
internal static class LoadHarmony
{
    internal static readonly Harmony Harmony;

    private static int _loadedPatches;
    private static int _failedPatches;

    static LoadHarmony()
    {
        Harmony = new Harmony(JobInBarMod.Instance!.Content!.PackageId!);

#if DEBUG
        // Harmony.DEBUG = true; // For debugging transpilers. DO NOT uncomment this unless you need it!
#endif

        Log.Message("Running Harmony patches...");

        try
        {
            Patch_Vanilla();
        }
        catch (Exception e)
        {
            Log.Exception(e,
                "Error patching vanilla. This likely means either the wrong game version or a hard incompatibility with another mod.");
        }

        Log.Message($"{_loadedPatches}/{_loadedPatches + _failedPatches} Harmony patches successful.");
        if (_failedPatches > 0)
            Log.Warning($"{_failedPatches} Harmony patches failed! The mod/game might behave in undesirable ways.");
    }

    private static void Patch_Vanilla()
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
    /// <param name="condition">
    ///     Optional condition that must be true or the patch will be skipped.<br />
    ///     Used for conditionally skipping patches based on mod configs.
    /// </param>
    private static void PatchCategory(string category, bool condition = true)
    {
        if (!condition) //TODO: Come up with a way to conditionally RE-patch categories if they're enabled in settings without requiring a restart
        {
            Log.Message($"Patch \"{category}\" skipped, disabled in mod config.");
            return;
        }

        try
        {
            Log.Trace($"Patching category \"{category}\"...");
            Harmony.PatchCategory(category);
        }
        catch (Exception e)
        {
            Log.Exception(e, $"Error patching category {category}");
            _failedPatches++;
            return;
        }

        _loadedPatches++;
    }
}
