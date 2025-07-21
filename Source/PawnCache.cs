using System;
using System.Collections.Generic;
using System.Linq;
using JobInBar.HarmonyPatches;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Random = System.Random;

namespace JobInBar;

/// <summary>
///  Stores all of the relevant information used by the mod for things like determining which labels to draw<br />
/// Static properties store the actual full cache, which is composed of instances.<para />
/// This cache implementation is heavily inspired by CM Colored Mood Bar
/// </summary>
internal class PawnCache
{
    // Static cache
    /// <summary>
    /// Global value if NO non-custom labels should ever be drawn.
    /// </summary>
    internal static bool OnlyDrawCustomJobTitles { get; private set; }

    internal static Dictionary<int, PawnCache> Cache = new();
    internal static Pawn? HoveredPawn { get; set; }

    /// <summary>
    /// The current width that every label should be truncated to, set by the <see cref="Patch_ColonistBarDrawer_DrawColonist_AddLabels"/>
    /// patch. Used to determine if the most recently cached string needs to be recalculated based on the last truncated width.
    /// </summary>
    internal static float CurLabelWidth { get; set; }

    private static string TruncateLabel(string labelString, float truncateToWidth, GameFont font)
    {
        var oldFont = Text.Font;
        Text.Font = font;
        labelString = labelString.Truncate(truncateToWidth)!;
        Text.Font = oldFont;

        return labelString;
    }

    [Obsolete("Use TryGet instead")]
    internal static PawnCache? Get(Pawn pawn)
    {
        TryGet(pawn, out var cache);

        return cache;
    }

    internal static bool TryGet(Pawn pawn, out PawnCache? cache)
    {
        return Cache.TryGetValue(pawn.GetHashCode(), out cache);
    }

    internal static PawnCache GetOrCache(Pawn pawn)
    {
        return (TryGet(pawn, out var cache) ? cache! : new PawnCache(pawn));
    }

    internal static void Clear()
    {
        Log.Trace("Clearing cache");
        Cache.Clear();
    }

    // Instance properties
    internal readonly Pawn? Pawn;
    internal long LastCached = -1;
    internal int TickOffset { get; private set; }

    /// <summary>
    /// If set to true, the next time that <see cref="NeedsRecache"/> is checked, the refresh rate will be overridden and
    /// a recache will be triggered no matter what.<br />
    /// Always set to false after every <see cref="Recache"/>.
    /// </summary>
    internal bool Dirty;

    // ReSharper disable once PossibleLossOfFraction
    internal bool NeedsRecache => Dirty ||
                                  ((((DateTime.UtcNow.Ticks) - LastCached) / 10000) >
                                   Settings.CacheRefreshRate + TickOffset) ||
                                  !Mathf.Approximately(CurrentTruncatedWidth, CurLabelWidth);

    /// <summary>
    /// The width that was used to cache the truncated label most recently. If this is not equal to the current
    /// truncation width (<see cref="CurLabelWidth"/>), a recache will be triggered to calculate a new truncated string
    /// </summary>
    internal float CurrentTruncatedWidth { get; private set; }

    /// <summary>
    /// If false, then NONE of the permanent labels (all labels other than Current Task for now) should be drawn.<br />
    /// For example, the pawn is an ignored type (guest, ghoul, etc.) or "only drafted" is turned on in settings.
    /// </summary>
    internal bool DrawAnyPermanentLabels { get; private set; }

    internal bool OnlyDrawWhenHovered { get; private set; }

    /// <summary>
    /// The job title to be drawn, or null if none should be drawn (such as if only custom labels are enabled and there
    /// is no custom title).
    /// </summary>
    internal bool HasCustomTitle => Pawn?.story?.title != null;

    // Note that pawn name is NOT included because it would just add extra overhead, not less

    internal string? Title { get; private set; }
    internal float TitleLabelWidth { get; private set; }
    internal Color JobColor { get; private set; }
    internal bool DrawJobLabel { get; private set; }
    internal RoyalTitleDef? RoyalTitle { get; private set; }
    internal float RoyaltyLabelWidth { get; private set; }
    internal string? RoyalTitleString { get; private set; }
    internal Color RoyalTitleColor { get; private set; }
    internal bool DrawRoyalTitle { get; private set; }
#if !(v1_1 || v1_2)
    internal Precept_Role? IdeoRole { get; private set; }
#endif
    internal float IdeoRoleLabelWidth { get; private set; }
    internal string? IdeoRoleString { get; private set; }
    internal Color IdeoRoleColor { get; private set; }
    internal bool DrawIdeoRole { get; private set; }
    internal bool IdeoRoleAbilityIsReady { get; private set; }
    internal string? CurrentTask { get; private set; }
    internal float CurrentTaskLabelWidth { get; private set; }
    internal Color CurrentTaskColor { get; private set; }

    internal List<LabelType> LabelOrder { get; private set; }

    internal bool IsGuest { get; private set; }
    internal bool IsSlave { get; private set; }
    internal bool IsSubhuman { get; private set; }

    internal bool IsHovered
    {
        get => HoveredPawn == Pawn;
        set
        {
            if (value != IsHovered)
                Dirty = true;
            if (value)
            {
                // Trigger a recache whenever a pawn is first hovered
                HoveredPawn = Pawn;
            }
            // Clear the hovered pawn only if this pawn was the hovered pawn
            else if (!value && IsHovered)
                HoveredPawn = null;
        }
    }

    internal PawnCache(Pawn pawn)
    {
        Pawn = pawn;
        var rand = new Random(Pawn.GetHashCode());
        TickOffset = rand.Next(0, 60);
        LabelOrder = new List<LabelType> { LabelType.JobTitle, LabelType.RoyalTitle, LabelType.IdeoRole };
        Recache();
        Cache[pawn.GetHashCode()] = this;
        Log.Trace($"Created new cache entry for pawn {pawn.Name}");
    }

    /// <summary>
    /// Method that performs all of the caching operations at once. Called primarily by <see cref="Patch_ColonistBarDrawer_DrawColonist_AddLabels"/>
    /// every frame that a pawn is drawn if <see cref="NeedsRecache"/> evaluates to true.
    /// </summary>
    internal PawnCache? Recache()
    {
        if (Pawn == null)
        {
            Log.Message($"Null pawn for cached label data. Removing.");
            return null;
        }

        // Static values for the whole cache
        OnlyDrawCustomJobTitles = Settings.OnlyDrawCustomJobTitles;
        var scaleBehavior = Settings.MinColonistBarScaleBehavior;
        if (!OnlyDrawCustomJobTitles && Find.ColonistBar?.Scale < Settings.MinColonistBarScale)
        {
            OnlyDrawCustomJobTitles = scaleBehavior == Settings.MinScaleBehavior.ShowOnlyCustom;
        }

#if !v1_2
        IsGuest = (Pawn.HomeFaction != Faction.OfPlayer) && !(Pawn.IsSlaveOfColony);
        IsSlave = ModsConfig.IdeologyActive && Pawn.IsSlave;
#else
        IsGuest = Pawn.FactionOrExtraMiniOrHomeFaction != Faction.OfPlayer;
        IsSlave = false;
#endif

        // Anomaly things
#if !(v1_1 || v1_2 || v1_3 || v1_4 || v1_5)
        IsSubhuman = Pawn.IsSubhuman;
#elif v1_5 // 1.5 didn't include the IsSubhuman property, but did include relevant pawn types (ghouls)
        IsSubhuman = Pawn.IsMutant;
#endif

        // Overall rules
        CalcDrawAnyPermanentLabels();

        OnlyDrawWhenHovered = Settings.DrawLabelOnlyOnHover;
        if (!OnlyDrawWhenHovered && Find.ColonistBar?.Scale < Settings.MinColonistBarScale)
        {
            OnlyDrawWhenHovered = scaleBehavior == Settings.MinScaleBehavior.ShowOnHover ||
                                  (scaleBehavior == Settings.MinScaleBehavior.ShowOnlyCustomExceptOnHover &&
                                   !HasCustomTitle);
        }

        LabelOrder = LabelsTracker_WorldComponent.Instance?[Pawn].LabelOrder!;

        CalcJobLabel();
        CalcRoyaltyLabel();
        CalcIdeoLabel();
        CalcCurrentTaskLabel();

        CurrentTruncatedWidth = CurLabelWidth;

        LastCached = DateTime.UtcNow.Ticks;
        Dirty = false;

        return this;
    }

    // Cache updating methods
    private void CalcDrawAnyPermanentLabels()
    {
        DrawAnyPermanentLabels = false;

        if (Settings.ModEnabled == false || Patch_PlaySettings_GlobalControl_ToggleLabels.DrawLabels == false)
            return;

        if (Settings.IgnoreGuests && IsGuest)
            return;

        if (Settings.IgnoreSlaves && IsSlave)
            return;

#if !(v1_1 || v1_2 || v1_3 || v1_4)
        if (Settings.IgnoreSubhuman && IsSubhuman)
            return;
#endif

        if (Settings.OnlyDrafted && !(Pawn?.Drafted ?? false))
            return;

        if (Settings.OnlyCurrentMap && Pawn?.Map != Find.CurrentMap)
            return;

        DrawAnyPermanentLabels = true;
    }

    private bool GetDrawJobLabel()
    {
        if (Pawn == null)
            return false;

        if (!Settings.DrawJobTitle)
            return false;

        if (!DrawAnyPermanentLabels)
            return false;

        if (!LabelsTracker_WorldComponent.Instance?[Pawn].ShowBackstory ?? false)
            return false;

        if (OnlyDrawWhenHovered && !IsHovered)
            return false;

        // If only custom titles should be drawn, Title will be null
        return Title != null;
    }

    private void CalcJobLabel()
    {
        if (Pawn == null)
        {
            Title = null;
            return;
        }

        if (OnlyDrawCustomJobTitles)
        {
            Title = Pawn.story?.title;
        }
        else
        {
            Title = Pawn.story?.title ?? Pawn.story?.TitleShortCap;
        }

        DrawJobLabel = GetDrawJobLabel();
        if (!DrawJobLabel || Title == null) return;

        if (Settings.TruncateLongLabels)
        {
            Title = TruncateLabel(Title, CurLabelWidth, Text.Font);
        }

        TitleLabelWidth = Text.CalcSize(Title).x;

        JobColor = LabelsTracker_WorldComponent.Instance?[Pawn].BackstoryColor ?? Settings.DefaultJobLabelColor;
    }

    internal bool GetJobLabel(out string label)
    {
        label = Title ?? Pawn?.story?.title ?? "";

        return DrawJobLabel && label != "";
    }

    private bool GetDrawRoyalTitle()
    {
        if (Pawn == null || !Settings.DrawRoyalTitles || !DrawAnyPermanentLabels || !ModsConfig.RoyaltyActive)
            return false;

        if (!LabelsTracker_WorldComponent.Instance?[Pawn].ShowRoyalTitle ?? false)
            return false;

        return RoyalTitle != null;
    }

    private void CalcRoyaltyLabel()
    {
        if (Pawn == null)
        {
            RoyalTitle = null;
            RoyalTitleString = null;
            return;
        }

        if (ModsConfig.RoyaltyActive)
        {
            RoyalTitle = Pawn.royalty?.MainTitle();
            RoyalTitleString = RoyalTitle?.GetLabelCapFor(Pawn);

            DrawRoyalTitle = GetDrawRoyalTitle();
            if (!DrawRoyalTitle || RoyalTitleString == null) return;

            if (Settings.TruncateLongLabels)
            {
                RoyalTitleString = TruncateLabel(RoyalTitleString, CurLabelWidth, Text.Font);
            }

            RoyaltyLabelWidth = Text.CalcSize(RoyalTitleString).x;

            RoyalTitleColor = LabelsTracker_WorldComponent.Instance?[Pawn].RoyalTitleColor ??
                              Settings.RoyalTitleColorDefault;
        }
        else
        {
            DrawRoyalTitle = false;
        }
    }

    internal bool GetRoyalTitle(out string label)
    {
        label = RoyalTitleString ?? "";

        return DrawRoyalTitle && RoyalTitleString != null;
    }

    private bool GetDrawIdeoRole()
    {
#if !(v1_1 || v1_2)
        if (Pawn == null || !Settings.DrawIdeoRoles || !DrawAnyPermanentLabels || !ModsConfig.IdeologyActive)
            return false;

        if (!LabelsTracker_WorldComponent.Instance?[Pawn].ShowIdeoRole ?? false)
            return false;

        return IdeoRole != null;
#else
        return false;
#endif
    }

    private void CalcIdeoLabel()
    {
#if !(v1_1 || v1_2)
        if (Pawn == null)
        {
            IdeoRole = null;
            IdeoRoleString = null;
            DrawIdeoRole = false;
            return;
        }
        if (ModsConfig.IdeologyActive)
        {
            IdeoRole = Pawn.ideo?.Ideo?.GetRole(Pawn);
            IdeoRoleString = IdeoRole?.LabelCap;

            DrawIdeoRole = GetDrawIdeoRole();
            if (!DrawIdeoRole || IdeoRoleString == null) return;

            if (Settings.TruncateLongLabels)
            {
                IdeoRoleString = TruncateLabel(IdeoRoleString, CurLabelWidth, Text.Font);
                CurrentTruncatedWidth = CurLabelWidth;
            }

            IdeoRoleLabelWidth = Text.CalcSize(IdeoRoleString).x;

            IdeoRoleAbilityIsReady = IdeoRole?.AbilitiesFor(Pawn)?.FirstOrDefault()?.CanCast ?? false;
            IdeoRoleColor = CalcIdeoRoleColor();
        }
        else
        {
            DrawIdeoRole = false;
        }
#else
        IdeoRoleString = null;
        DrawIdeoRole = false;
        IdeoRoleAbilityIsReady = false;
#endif
    }

    internal bool GetIdeoRole(out string label)
    {
        label = IdeoRoleString ?? "";

        return DrawIdeoRole && IdeoRoleString != null;
    }

    private Color CalcIdeoRoleColor()
    {
#if !(v1_1 || v1_2)
        var fallbackColor = GenMapUI.DefaultThingLabelColor;

        if (Pawn is null || IdeoRole is null) return fallbackColor;

        // If the user has disabled the automatic color assignment, check for any individual color settings and otherwise use the global setting
        if (!Settings.UseIdeoColorForRole)
            return LabelsTracker_WorldComponent.Instance?[Pawn].IdeoRoleColor ?? fallbackColor;

        // Get a cached color override (or null if no override has been set)
        Color? ideoColor = LabelsTracker_WorldComponent.Instance?[Pawn].IdeoRoleColor ??
                           Color.Lerp(IdeoRole.ideo?.colorDef?.color ?? fallbackColor, Color.white, 0.35f);

        if (Settings.RoleColorOnlyIfAbilityAvailable && !IdeoRoleAbilityIsReady)
            return fallbackColor;

        return ideoColor.Value;
#else
        return Color.white;
#endif
    }

    private void CalcCurrentTaskLabel()
    {
        if (Pawn == null)
        {
            CurrentTask = null;
            return;
        }

        if (Settings.DrawCurrentTask && IsHovered)
        {
            if (Pawn.ParentHolder is Caravan caravan)
            {
                CurrentTask = caravan.LabelCap + ": " + caravan.GetInspectString();
                if (CurrentTask.Contains('\n'))
                    CurrentTask = CurrentTask.Substring(0, CurrentTask.IndexOf('\n')).CapitalizeFirst();
            }
            else if (Pawn.jobs?.curDriver?.GetReport() is { } report)
            {
                CurrentTask = report.CapitalizeFirst();
            }
            else
            {
                CurrentTask = null;
            }
        }
        else
        {
            CurrentTask = null;
        }

        if (CurrentTask == null) return;

        CurrentTaskLabelWidth = Text.CalcSize(CurrentTask).x;

        CurrentTaskColor = Settings.CurrentTaskLabelColor;
    }
}
