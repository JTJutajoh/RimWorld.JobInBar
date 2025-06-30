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
/// Static properties store the actual full cache, which is composed of instances.
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

    internal static PawnCache? Get(Pawn pawn)
    {
        return Cache.TryGetValue(pawn.GetHashCode(), out var cache) ? cache : null;
    }

    internal static PawnCache GetOrCache(Pawn pawn)
    {
        return Get(pawn) ?? new PawnCache(pawn);
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
    internal bool Dirty = false;

    // ReSharper disable once PossibleLossOfFraction
    internal bool NeedsRecache => Dirty ||
                                  ((((DateTime.UtcNow.Ticks) - LastCached) / 10000) >
                                   Settings.CacheRefreshRate + TickOffset);

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
    internal string? Title { get; private set; }
    internal Color JobColor { get; private set; }
    internal bool DrawJobLabel { get; private set; }
    internal RoyalTitleDef? RoyalTitle { get; private set; }
    internal Color RoyalTitleColor { get; private set; }
    internal bool DrawRoyalTitle { get; private set; }
    internal Precept_Role? IdeoRole { get; private set; }
    internal Color IdeoRoleColor { get; private set; }
    internal bool DrawIdeoRole { get; private set; }
    internal bool IdeoRoleAbilityIsReady { get; private set; }
    internal string? CurrentTask { get; private set; }
    internal Color CurrentTaskColor { get; private set; }

    internal bool IsGuest { get; private set; }
    internal bool IsSlave { get; private set; }
    internal bool IsSubhuman { get; private set; }

    internal bool IsHovered
    {
        get => HoveredPawn == Pawn;
        set
        {
            if (value)
            {
                // Trigger a recache whenever a pawn is first hovered
                Dirty = HoveredPawn != Pawn;
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
        Recache();
        Cache[pawn.GetHashCode()] = this;
        Log.Trace($"Created new cache entry for pawn {pawn.Name}");
    }


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

        // Overall rules
        DrawAnyPermanentLabels = GetDrawAnyPermanentLabels();

        OnlyDrawWhenHovered = Settings.DrawLabelOnlyOnHover;
        if (!OnlyDrawWhenHovered && Find.ColonistBar?.Scale < Settings.MinColonistBarScale)
        {
            OnlyDrawWhenHovered = scaleBehavior == Settings.MinScaleBehavior.ShowOnHover ||
                                  (scaleBehavior == Settings.MinScaleBehavior.ShowOnlyCustomExceptOnHover &&
                                   !HasCustomTitle);
        }

        // Title/backstory
        if (OnlyDrawCustomJobTitles)
        {
            Title = Pawn.story?.title;
        }
        else
        {
            Title = Pawn.story?.title ?? Pawn.story?.TitleShortCap;
        }

        DrawJobLabel = GetDrawJobLabel();

        // Current task
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

        // Royalty things
        if (ModsConfig.RoyaltyActive)
        {
            RoyalTitle = Pawn.royalty?.MainTitle();
            DrawRoyalTitle = GetDrawRoyalTitle();
        }
        else
        {
            DrawRoyalTitle = false;
        }

        // Ideology things
#if !(v1_2)
        IsGuest = (Pawn.HomeFaction != Faction.OfPlayer) && !(Pawn.IsSlaveOfColony);

        if (ModsConfig.IdeologyActive)
        {
            IsSlave = Pawn.IsSlave;

            IdeoRole = Pawn.ideo?.Ideo?.GetRole(Pawn);
            if (IdeoRole != null)
            {
                IdeoRoleAbilityIsReady = IdeoRole?.AbilitiesFor(Pawn)?.FirstOrDefault()?.CanCast ?? false;
            }

            DrawIdeoRole = GetDrawIdeoRole();
        }
        else
        {
            IsSlave = false;
            DrawIdeoRole = false;
        }
#else
        IsGuest = Pawn.HomeFaction != Faction.OfPlayer;
#endif

        // Anomaly things
#if !(v1_1 || v1_2 || v1_3 || v1_4 || v1_5)
        IsSubhuman = Pawn.IsSubhuman;
#elif v1_5 // 1.5 didn't include the IsSubhuman property, but did include relevant pawn types (ghouls)
        IsSubhuman = Pawn.IsMutant;
#endif

        LastCached = DateTime.UtcNow.Ticks;
        Dirty = false;

        return this;
    }

    // Cache updating methods
    private bool GetDrawAnyPermanentLabels()
    {
        if (Settings.ModEnabled == false || Patch_PlaySettings_GlobalControl_ToggleLabels.DrawLabels == false)
            return false;

        if (Settings.IgnoreGuests && IsGuest)
            return false;

        if (Settings.IgnoreSlaves && IsSlave)
            return false;

#if !(v1_1 || v1_2 || v1_3 || v1_4)
        if (Settings.IgnoreSubhuman && IsSubhuman)
            return false;
#endif

        if (Settings.OnlyDrafted && !(Pawn?.Drafted ?? false))
            return false;

        if (Settings.OnlyCurrentMap && Pawn?.Map != Find.CurrentMap)
            return false;

        return true;
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

    internal bool GetJobLabel(out string label)
    {
        label = Title ?? Pawn?.story?.title ?? "";
        return DrawJobLabel;
    }

    private bool GetDrawRoyalTitle()
    {
        if (Pawn == null || !Settings.DrawRoyalTitles || !DrawAnyPermanentLabels || !ModsConfig.RoyaltyActive)
            return false;

        return RoyalTitle != null;
    }

    internal bool GetRoyalTitle(out string label)
    {
        label = RoyalTitle?.LabelCap ?? "";
        return DrawRoyalTitle;
    }

    private bool GetDrawIdeoRole()
    {
        if (Pawn == null || !Settings.DrawIdeoRoles || !DrawAnyPermanentLabels || !ModsConfig.IdeologyActive)
            return false;

        return IdeoRole != null;
    }

    internal bool GetIdeoRole(out string label)
    {
        label = IdeoRole?.LabelCap ?? "";
        return DrawIdeoRole;
    }
}
