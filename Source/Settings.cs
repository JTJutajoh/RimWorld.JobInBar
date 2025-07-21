using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace JobInBar;

internal class Settings : ModSettings
{
    private const float TabHeight = 32f;
    private const float PerformanceSectionHeight = 320f;
    private const float MiscSectionHeight = 158f + 8f;
    private const float JobTitleSectionHeight = 430f;
    private const float CurrentTaskSectionHeight = 600f;
    private const float IdeologySectionHeight = 200f;
    private const float RoyaltySectionHeight = 430f;
    private const float DisplaySectionHeight = 700f;
    private const float ColorPickersHeight = 240f;

    internal static Dictionary<string, object> DefaultSettings = new();

    [Setting] public static int JobLabelVerticalOffset;
    private static string _bufferJobLabelVerticalOffset = JobLabelVerticalOffset.ToString();
    [Setting] public static int OffsetEquippedExtra = -6;
    private static string _bufferOffsetEquippedExtra = OffsetEquippedExtra.ToString();

    [Setting] public static bool CurrentTaskUseAbsolutePosition = false;
    [Setting] public static int CurrentTaskAbsoluteY = 0;
    private static string _bufferCurrentTaskAbsoluteY = CurrentTaskAbsoluteY.ToString();

    private static Vector2 _scrollPositionCache = Vector2.zero;


    private static Vector2 _scrollPositionMainTab = Vector2.zero;
    private static float? _lastMainTabHeight;

    private SettingsTab _currentTab = SettingsTab.Main;
    private static string? _royalTitleHexStringBuffer;
    private static string? _currentTaskHexStringBuffer;
    private static string? _jobTitleHexStringBuffer;

    public Settings()
    {
        HarvestSettingsDefaults(out DefaultSettings);
        Log.Trace("Default settings loaded");
    }

    private void HarvestSettingsDefaults(out Dictionary<string, object> settings, Type? owningType = null)
    {
        settings = new Dictionary<string, object>();
        if (owningType is null) owningType = GetType();

        var fields = AccessTools.GetDeclaredFields(owningType)!;
        Log.Trace($"Harvesting {fields.Count} settings for type '{owningType.FullName}'");
        foreach (var field in fields)
        {
            if (!field.IsStatic) continue;
            var fieldAttr = field.GetCustomAttributes(typeof(SettingAttribute), false);
            if (fieldAttr.Length == 0) continue;
            settings[field.Name] = field.GetValue(this);
            Log.Trace($"Harvested Setting: '{field.Name}' Default: {settings[field.Name]}");
        }
    }

    private static void OnSettingsSave()
    {
        Log.Trace("Settings saved, applying patch changes.");

        PatchManager.SetPatched("ColorName", AllowNameColors);
        PatchManager.SetPatched("PlaySettings", EnablePlaySettingToggle);
    }

    internal static string GetSettingLabel(string key, bool showValue = false)
    {
        if (!showValue) return $"JobInBar_Settings_{key}".Translate();

        var value = AccessTools.Field(typeof(Settings), key)?.GetValue(null!)?.ToString() ?? null;
        if (value is null) return $"JobInBar_Settings_{key}".Translate();

        return $"JobInBar_Settings_{key}".Translate() + ": " + value;
    }

    internal static string GetSettingTooltip(string key)
    {
        var success = $"JobInBar_Settings_{key}_Desc".TryTranslate(out var str);

        if (!success)
        {
            str = "";
            return str;
        }

        if (DefaultSettings.TryGetValue(key, out var setting))
            str += "JobInBar_Settings_DefaultSuffix".Translate(setting!.ToString());

        return str;
    }

    public void DoWindowContents(Rect inRect)
    {
        if (!ModEnabled)
        {
            DoTabMain(inRect.BottomPartPixels(inRect.height - TabHeight - 32f));
            return;
        }

        var tabs = new List<TabRecord>
        {
            new("JobInBar_Settings_Tab_Main".Translate(), () => _currentTab = SettingsTab.Main,
                () => _currentTab == SettingsTab.Main),
            new("JobInBar_Settings_Tab_Patches".Translate(), () => _currentTab = SettingsTab.Patches,
                () => _currentTab == SettingsTab.Patches)
        };

        if (Prefs.DevMode && LabelsTracker_WorldComponent.Instance != null)
            tabs.Add(new TabRecord("JobInBar_Settings_Tab_Cache".Translate(), () => _currentTab = SettingsTab.Cache,
                () => _currentTab == SettingsTab.Cache));

#if v1_5 || v1_6
        TabDrawer.DrawTabsOverflow(inRect.TopPartPixels(TabHeight), tabs, 80f, inRect.width / 2f);
#elif v1_1 || v1_2 || v1_3 || v1_4
        var tabsRect = inRect.TopPartPixels(TabHeight);
        tabsRect.y += TabHeight;
        TabDrawer.DrawTabs(tabsRect, tabs, 400f);
#endif
        Widgets.DrawLineHorizontal(inRect.xMin, inRect.yMin + TabHeight, inRect.width);

        var tabRect = inRect.BottomPartPixels(inRect.height - TabHeight - 32f);
        switch (_currentTab)
        {
            case SettingsTab.Main:
                try
                {
                    DoTabMain(tabRect);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Error drawing main settings tab.", true);
                    throw;
                }

                break;
            case SettingsTab.Patches:
                try
                {
                    DoTabPatches(tabRect);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Error drawing patches settings tab.", true);
                    throw;
                }

                break;
            case SettingsTab.Cache:
                try
                {
                    DoTabCache(tabRect);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Error drawing cache settings tab.", true);
                    throw;
                }

                break;
            default:
                _currentTab = SettingsTab.Main;
                break;
        }
    }

    private static void DoTabMain(Rect inRect)
    {
        var viewRect = new Rect(inRect);
        var outerRect = new Rect(inRect);
#if !(v1_1 || v1_2 || v1_3 || v1_4)
        Widgets.AdjustRectsForScrollView(inRect, ref outerRect, ref viewRect);
#else
        LegacySupport.AdjustRectsForScrollView(inRect, ref outerRect, ref viewRect);
#endif
        viewRect.height = _lastMainTabHeight ?? 9999f;

        Widgets.BeginScrollView(outerRect, ref _scrollPositionMainTab, viewRect);

        var listing = new Listing_Standard();
        var innerRect = viewRect.MiddlePart(0.9f, 1f);
        listing.Begin(innerRect);

        listing.ColumnWidth = innerRect.width / 2.05f;

        listing.CheckboxLabeled(GetSettingLabel("ModEnabled"), ref ModEnabled, GetSettingTooltip("ModEnabled"));

        if (!ModEnabled)
        {
            _lastMainTabHeight = null;
            Widgets.EndScrollView();
            listing.End();
            return;
        }

        listing.Gap();

        DoPerfSection(listing);

        listing.Gap();

        DoMiscSection(listing);

        listing.Gap();

        DoDisplaySection(listing);

        listing.NewColumn();

        DoJobTitleSection(listing);

        listing.Gap();

        DoCurrentTaskSection(listing);

        listing.Gap();

        DoIdeologySection(listing);

        listing.Gap();

        DoRoyaltySection(listing);

        _lastMainTabHeight ??= listing.MaxColumnHeightSeen;
        listing.End();
        Widgets.EndScrollView();
    }

    private static void DoPerfSection(Listing_Standard listing)
    {
        var section = listing.BeginSection(PerformanceSectionHeight)!;

        section.SectionHeader("JobInBar_Settings_PerformanceWarningHeader");
        section.SubLabel("JobInBar_Settings_PerformanceWarning".Translate(), 1f);

        CacheRefreshRate = section.SliderLabeled(GetSettingLabel("CacheRefreshRate", true), CacheRefreshRate, 50f,
            10000f,
            labelPct: 0.7f, tooltip: GetSettingTooltip("CacheRefreshRate"));
        CacheRefreshRate -= (CacheRefreshRate % 50f);
        if (section.ButtonText("Default".Translate()))
        {
            CacheRefreshRate = DefaultSettings["CacheRefreshRate"] as float? ?? 250f;
        }

        section.Gap(24f);

        section.CheckboxLabeled(GetSettingLabel("DrawLabelOnlyOnHover"), ref DrawLabelOnlyOnHover,
            GetSettingTooltip("DrawLabelOnlyOnHover"), 36f);

        listing.EndSection(section);
    }

    private static void DoMiscSection(Listing_Standard listing)
    {
        var section = listing.BeginSection(MiscSectionHeight)!;

        section.SectionHeader("JobInBar_Settings_Section_Misc");

        section.CheckboxLabeled(GetSettingLabel("EnablePlaySettingToggle"), ref EnablePlaySettingToggle,
            GetSettingTooltip("EnablePlaySettingToggle"), 36f);

        section.Gap();
        section.Label(GetSettingLabel("EnabledButtonLocations"));

        var locNamePawn = EnabledButtonLocations.HasFlag(ButtonLocations.NamePawn);
        section.CheckboxLabeled(GetSettingLabel("ButtonLocation_NamePawn"), ref locNamePawn,
            GetSettingTooltip("ButtonLocation_NamePawn"), 24f);
        if (locNamePawn)
            EnabledButtonLocations |= ButtonLocations.NamePawn;
        else
            EnabledButtonLocations &= ~ButtonLocations.NamePawn;

        var locCharacterCard = EnabledButtonLocations.HasFlag(ButtonLocations.CharacterCard);
        section.CheckboxLabeled(GetSettingLabel("ButtonLocation_CharacterCard"), ref locCharacterCard,
            GetSettingTooltip("ButtonLocation_CharacterCard"), 24f);
        if (locCharacterCard)
            EnabledButtonLocations |= ButtonLocations.CharacterCard;
        else
            EnabledButtonLocations &= ~ButtonLocations.CharacterCard;

        listing.EndSection(section);
    }


    private static bool _draggingJobTitleColorPicker;

    private static void DoJobTitleSection(Listing_Standard listing)
    {
        var section = listing.BeginSection(JobTitleSectionHeight)!;

        section.SectionHeader("JobInBar_Settings_Section_JobTitle");

        section.CheckboxLabeled(GetSettingLabel("DrawJobTitle"), ref DrawJobTitle,
            GetSettingTooltip("DrawJobTitle"), 36f);

        section.SubLabel("JobInBar_Settings_JobTitleNote".Translate(), 1f);

        section.CheckboxLabeled(GetSettingLabel("OnlyDrawCustomJobTitles"), ref OnlyDrawCustomJobTitles, !DrawJobTitle,
            GetSettingTooltip("OnlyDrawCustomJobTitles"), 36f);

        CustomWidgets.LabelColorPicker(
            section.GetRect(ColorPickersHeight),
            ref DefaultJobLabelColor,
            DrawJobTitleBackground,
            "JobInBar_Settings_DefaultJobLabelColor".Translate(),
            ref _draggingJobTitleColorPicker,
            ref _jobTitleHexStringBuffer,
            defaultButton: true,
            defaultColor: DefaultSettings["DefaultJobLabelColor"] as Color?,
            disabled: !DrawJobTitle);

        section.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawJobTitleBackground, !DrawJobTitle,
            GetSettingTooltip("DrawBackground"), 36f);

        listing.EndSection(section);
    }


    private static bool _draggingCurrentTaskColorPicker;

    private static void DoCurrentTaskSection(Listing_Standard listing)
    {
        var section = listing.BeginSection(CurrentTaskSectionHeight)!;

        section.SectionHeader("JobInBar_Settings_Section_CurrentTask");

        section.CheckboxLabeled(GetSettingLabel("DrawCurrentTask"), ref DrawCurrentTask,
            GetSettingTooltip("DrawCurrentTask"), 36f);

        section.SubLabel("JobInBar_Settings_CurrentJobNote".Translate(), 1f);

        section.CheckboxLabeled(GetSettingLabel("MoveWeaponBelowCurrentTask"), ref MoveWeaponBelowCurrentTask,
            !DrawCurrentTask || CurrentTaskUseAbsolutePosition, GetSettingTooltip("MoveWeaponBelowCurrentTask"), 36f);

        section.CheckboxLabeled(GetSettingLabel("CurrentTaskUseAbsolutePosition"), ref CurrentTaskUseAbsolutePosition,
            !DrawCurrentTask, GetSettingTooltip("CurrentTaskUseAbsolutePosition"), 36f);

        section.IntSetting(ref CurrentTaskAbsoluteY, "CurrentTaskAbsoluteY", ref _bufferCurrentTaskAbsoluteY, null, 2,
            -64, 64, true, !DrawCurrentTask || !CurrentTaskUseAbsolutePosition);

        section.Gap();

        CustomWidgets.LabelColorPicker(
            section.GetRect(ColorPickersHeight),
            ref CurrentTaskLabelColor,
            DrawCurrentTaskBackground,
            "JobInBar_Settings_CurrentTaskLabelColor".Translate(),
            ref _draggingCurrentTaskColorPicker,
            ref _currentTaskHexStringBuffer,
            defaultButton: true,
            defaultColor: DefaultSettings["CurrentTaskLabelColor"] as Color?,
            disabled: !DrawCurrentTask);

        section.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawCurrentTaskBackground, !DrawCurrentTask,
            GetSettingTooltip("DrawBackground"), 36f);

        listing.EndSection(section);
    }


    private static void DoIdeologySection(Listing_Standard listing)
    {
        if (!ModsConfig.IdeologyActive) return;

        var section = listing.BeginSection(IdeologySectionHeight)!;

        section.SectionHeader("JobInBar_Settings_Section_Ideology");

        section.CheckboxLabeled(GetSettingLabel("DrawIdeoRoles"), ref DrawIdeoRoles,
            GetSettingTooltip("DrawIdeoRoles"), 36f);


        section.CheckboxLabeled(GetSettingLabel("UseIdeoColorForRole"), ref UseIdeoColorForRole, !DrawIdeoRoles,
            GetSettingTooltip("UseIdeoColorForRole"), 36f);

        section.CheckboxLabeled(GetSettingLabel("RoleColorAbility"), ref RoleColorOnlyIfAbilityAvailable,
            (!DrawIdeoRoles || !UseIdeoColorForRole),
            GetSettingTooltip("RoleColorAbility"), 36f);

        section.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawIdeoRoleBackground, !DrawIdeoRoles,
            GetSettingTooltip("DrawBackground"), 36f);

        listing.EndSection(section);
    }

    private static void DoDisplaySection(Listing_Standard listing)
    {
        _bufferJobLabelVerticalOffset = JobLabelVerticalOffset.ToString();
        _bufferOffsetEquippedExtra = OffsetEquippedExtra.ToString();

        var section = listing.BeginSection(DisplaySectionHeight)!;

        section.SectionHeader("JobInBar_Settings_Section_Display");

        section.CheckboxLabeled(GetSettingLabel("TruncateLongLabels"), ref TruncateLongLabels,
            GetSettingTooltip("TruncateLongLabels"), 36f);

        if (section.ButtonTextLabeled(GetSettingLabel("MinColonistBarScaleBehavior"),
                $"JobInBar_Settings_{MinColonistBarScaleBehavior.ToString()}".Translate()))
        {
            var floatMenuOptions = (from object scaleBehavior in Enum.GetValues(typeof(MinScaleBehavior))
                select new FloatMenuOption($"JobInBar_Settings_{scaleBehavior}".Translate(),
                    () => { MinColonistBarScaleBehavior = (MinScaleBehavior)scaleBehavior; })).ToList();

            Find.WindowStack?.Add(new FloatMenu(floatMenuOptions));
        }

        var newMinScale = section.SliderLabeled(GetSettingLabel("MinColonistBarScale", true), MinColonistBarScale, 0f,
            1f,
            tooltip: GetSettingTooltip("MinColonistBarScale"));
        if (MinColonistBarScaleBehavior != MinScaleBehavior.ShowAll)
        {
            MinColonistBarScale = newMinScale - (newMinScale % 0.01f);
        }
        else
        {
            Widgets.DrawBoxSolid(new Rect(0f, section.CurHeight - 32f, section.ColumnWidth, 32f),
                Widgets.MenuSectionBGFillColor.WithAlpha(0.7f));
        }

        section.GapLine();

        section.CheckboxLabeled(GetSettingLabel("AllowNameColors"), ref AllowNameColors,
            GetSettingTooltip("AllowNameColors"));

        section.CheckboxLabeled(GetSettingLabel("IgnoreGuests"), ref IgnoreGuests,
            GetSettingTooltip("IgnoreGuests"));

        section.CheckboxLabeled(GetSettingLabel("IgnoreSlaves"), ref IgnoreSlaves,
            GetSettingTooltip("IgnoreSlaves"));

        section.CheckboxLabeled(GetSettingLabel("IgnoreSubhuman"), ref IgnoreSubhuman,
            GetSettingTooltip("IgnoreSubhuman"));

        section.CheckboxLabeled(GetSettingLabel("OnlyDrafted"), ref OnlyDrafted,
            GetSettingTooltip("OnlyDrafted"));

        section.CheckboxLabeled(GetSettingLabel("OnlyCurrentMap"), ref OnlyCurrentMap,
            GetSettingTooltip("OnlyCurrentMap"));

        section.GapLine();

        section.Label("JobInBar_Settings_Positioning".Translate());

        section.IntSetting(ref JobLabelVerticalOffset,
            "JobLabelVerticalOffset", ref _bufferJobLabelVerticalOffset, null, 2, -150, 150);

        section.GapLine();

        section.Label("JobInBar_Settings_Equipped".Translate());
        section.SubLabel("JobInBar_Settings_EquippedSubLabel".Translate(), 1f);

        section.CheckboxLabeled(GetSettingLabel("OffsetEquippedByLabels"), ref OffsetEquippedByLabels,
            GetSettingTooltip("OffsetEquippedByLabels"), labelPct: 1f);

        section.IntSetting(ref OffsetEquippedExtra,
            "OffsetEquippedExtra", ref _bufferOffsetEquippedExtra, null, 2, -150, 150);

        listing.EndSection(section);
    }

    private static bool _draggingRoyalTitleColorPicker;

    private static void DoRoyaltySection(Listing_Standard listing)
    {
        if (!ModsConfig.RoyaltyActive) return;

        var section = listing.BeginSection(RoyaltySectionHeight)!;

        section.SectionHeader("JobInBar_Settings_Section_Royalty");

        section.CheckboxLabeled(GetSettingLabel("DrawRoyalTitles"), ref DrawRoyalTitles,
            GetSettingTooltip("DrawRoyalTitles"), 36f);
        section.Gap();

        section.SubLabel("JobInBar_Settings_RoyalTitleNote".Translate(), 1f);

        CustomWidgets.LabelColorPicker(
            section.GetRect(ColorPickersHeight),
            ref RoyalTitleColorDefault,
            DrawRoyalTitleBackground,
            "JobInBar_Settings_RoyalTitleColor".Translate(),
            ref _draggingRoyalTitleColorPicker,
            ref _royalTitleHexStringBuffer,
            defaultButton: true,
            defaultColor: DefaultSettings["RoyalTitleColorDefault"] as Color?,
            disabled: !DrawRoyalTitles
        );

        section.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawRoyalTitleBackground, !DrawRoyalTitles,
            GetSettingTooltip("DrawBackground"), 36f);

        listing.EndSection(section);
    }

    private static void DoTabCache(Rect inRect)
    {
        if (LabelsTracker_WorldComponent.Instance is not { } labelsComp) return;


        var labelRect = inRect.TopPartPixels(32f);
        Widgets.Label(labelRect, "JobInBar_Settings_Cache_Label".Translate(nameof(LabelsTracker_WorldComponent)));
        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        Widgets.Label(new Rect(labelRect.xMin + 8f, labelRect.yMax + 4f, labelRect.width - 8f, labelRect.height),
            "JobInBar_Settings_Cache_Tip".Translate());
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        var outerRect = inRect.BottomPart(0.9f);
        var viewRect = inRect.BottomPart(0.9f);
#if !(v1_1 || v1_2 || v1_3 || v1_4)
        Widgets.AdjustRectsForScrollView(inRect.BottomPart(0.9f), ref outerRect, ref viewRect);
#else
            LegacySupport.AdjustRectsForScrollView(inRect.BottomPart(0.9f), ref outerRect, ref viewRect);
#endif
        viewRect.height = labelsComp.TrackedPawns.Count * 48f + 32f;
        Widgets.BeginScrollView(outerRect, ref _scrollPositionCache, viewRect);
        var curY = viewRect.yMin;
        Pawn? remove = null;
        foreach (var p in labelsComp.TrackedPawns)
        {
            var label =
                $"{p.Key?.NameShortColored}: [Pawn: \"{p.Value?.Pawn?.NameShortColored}\", ShowBackstory: {p.Value?.ShowBackstory}, BackstoryColor: {p.Value?.BackstoryColor}, ShowRoyalTitle: {p.Value?.ShowRoyalTitle}, ShowIdeoRole: {p.Value?.ShowIdeoRole}]";
            var rect = new Rect(viewRect.xMin, curY, viewRect.width,
                Text.CalcHeight(label, viewRect.width * 0.95f));
            Widgets.DrawLineHorizontal(rect.xMin, rect.yMin, rect.width);
            Widgets.DrawBoxSolid(rect.LeftPart(0.95f), Widgets.MenuSectionBGFillColor);
            var entryLabelRect = rect.LeftPart(0.85f);
            entryLabelRect.xMin += 4f;
            Widgets.Label(entryLabelRect, label);
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(rect.xMin, rect.yMax, rect.width);

            GUI.color = Color.red;
            if (p.Key != null && Widgets.ButtonTextSubtle(rect.RightPart(0.05f).MiddlePartPixels(50f, 24f),
                    "JobInBar_Reset".Translate()))
                remove = p.Key;

            curY += rect.height + 4f;
            GUI.color = Color.white;
        }

        if (remove != null) labelsComp.TrackedPawns.Remove(remove);


        Widgets.EndScrollView();
    }

    private static void DoTabPatches(Rect inRect)
    {
        var listing = new Listing_Standard();
        var innerRect = inRect.MiddlePart(0.9f, 1f);
        listing.Begin(innerRect);

        listing.Label("JobInBar_Settings_Patches_Label".Translate());
        listing.SubLabel("JobInBar_Settings_Patches_Note".Translate(), 1f);
        listing.Gap(4f);
        GUI.color = ColorLibrary.RedReadable;
        var applyButtonWidthPct = 0.8f;
        TooltipHandler.TipRegionByKey(
            new Rect(innerRect.xMin, listing.CurHeight, innerRect.width * applyButtonWidthPct, 40f),
            "JobInBar_Settings_RefreshPatches_Desc");
        if (listing.ButtonText("JobInBar_Settings_RefreshPatches".Translate(), null!, 0.3f))
        {
            JobInBarMod.Instance?.GetSettings<Settings>()?.Write();
            PatchManager.RepatchAll();
        }

        listing.SubLabel("JobInBar_Settings_RequiresRestart".Translate(), applyButtonWidthPct);
        GUI.color = Color.white;

        listing.GapLine();
        listing.Label("JobInBar_Settings_Patches_Enabled".Translate());
        foreach (var patch in PatchManager.AllPatchCategories)
        {
            var enabled = !DisabledPatchCategories.Contains(patch);
            listing.CheckboxLabeled($"JobInBar_Settings_PatchCategory_{patch}".Translate(), ref enabled, 80f);
            if (enabled)
                DisabledPatchCategories.Remove(patch);
            else
                DisabledPatchCategories.AddDistinct(patch);
        }

        DisabledPatchCategories.Sort();

        listing.End();
    }

    [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
    public override void ExposeData()
    {
        Log.Trace($"Settings ExposeData(). Mode: {Scribe.mode}");

        Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);

        Scribe_Values.Look(ref CacheRefreshRate, "CacheRefreshRate", 250f);

        Scribe_Values.Look(ref DrawJobTitle, "DrawJobTitle", true);
        Scribe_Values.Look(ref OnlyDrawCustomJobTitles, "OnlyDrawCustomJobTitles", false);
        Scribe_Values.Look(ref JobLabelVerticalOffset, "JobLabelVerticalOffset", 14);
        Scribe_Values.Look(ref OffsetEquippedExtra, "OffsetEquippedExtra");
        Scribe_Values.Look(ref OffsetEquippedByLabels, "OffsetEquippedByLabels", true);

        Scribe_Values.Look(ref CurrentTaskUseAbsolutePosition, "CurrentTaskUseAbsolutePosition", false);
        Scribe_Values.Look(ref CurrentTaskAbsoluteY, "CurrentTaskAbsoluteY", 0);

        Scribe_Values.Look(ref AllowNameColors, "AllowNameColors", true);

        Scribe_Values.Look(ref MinColonistBarScale, "MinColonistBarScale", 0.9f);
        Scribe_Values.Look(ref MinColonistBarScaleBehavior, "MinColonistBarScaleBehavior",
            MinScaleBehavior.ShowOnlyCustomExceptOnHover);
        Scribe_Values.Look(ref IgnoreGuests, "IgnoreGuests", true);
        Scribe_Values.Look(ref IgnoreSlaves, "IgnoreSlaves", false);
        Scribe_Values.Look(ref IgnoreSubhuman, "IgnoreSubhuman", true);
        Scribe_Values.Look(ref OnlyDrafted, "OnlyDrafted", false);
        Scribe_Values.Look(ref OnlyCurrentMap, "OnlyCurrentMap", true);

        Scribe_Values.Look(ref DefaultJobLabelColor, "DefaultJobLabelColor", GenMapUI.DefaultThingLabelColor);
        Scribe_Values.Look(ref CurrentTaskLabelColor, "CurrentTaskLabelColor", new Color(1f, 0.8f, 0.4f, 0.8f));

        Scribe_Values.Look(ref DrawJobTitleBackground, "DrawJobTitleBackground", true);
        Scribe_Values.Look(ref DrawRoyalTitleBackground, "DrawRoyalTitleBackground", true);
        Scribe_Values.Look(ref DrawIdeoRoleBackground, "DrawIdeoRoleBackground", true);
        Scribe_Values.Look(ref DrawCurrentTaskBackground, "DrawCurrentTaskBackground");
        Scribe_Values.Look(ref EnablePlaySettingToggle, "EnablePlaySettingToggle", true);
        Scribe_Values.Look(ref TruncateLongLabels, "TruncateLongLabels", true);
        Scribe_Values.Look(ref DrawLabelOnlyOnHover, "DrawLabelOnlyOnHover");

        Scribe_Values.Look(ref DrawIdeoRoles, "DrawIdeoRoles", true);
        Scribe_Values.Look(ref UseIdeoColorForRole, "UseIdeoColorForRole", true);
        Scribe_Values.Look(ref RoleColorOnlyIfAbilityAvailable, "RoleColorOnlyIfAbilityAvailable");

        Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);
        Scribe_Values.Look(ref RoyalTitleColorDefault, "RoyalTitleColor", new Color(0.85f, 0.85f, 0.75f));

        Scribe_Values.Look(ref DrawCurrentTask, "DrawCurrentTask", true);
        Scribe_Values.Look(ref MoveWeaponBelowCurrentTask, "MoveWeaponBelowCurrentTask", true);

        Scribe_Values.Look(ref EnabledButtonLocations, "EnabledButtonLocations", ButtonLocations.All);

        // Do this before saving the patch categories since it modifies them
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            OnSettingsSave();
        }

        var disabledPatchesTMP = DisabledPatchCategories.ToList();

        Scribe_Collections.Look(ref disabledPatchesTMP, "DisabledPatchCategories", LookMode.Value);


        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            DisabledPatchCategories = disabledPatchesTMP ?? DisabledPatchCategories.ToList();
            DisabledPatchCategories.Sort();
        }


        base.ExposeData();
    }

    private class SettingAttribute : Attribute
    {
    }

    // GUI Stuff
    private enum SettingsTab
    {
        Main,
        Patches,
        Cache
    }

    [Flags]
    internal enum ButtonLocations
    {
        None = 0,
        NamePawn = 2,
        CharacterCard = 4,
        All = NamePawn | CharacterCard
    }

    internal enum MinScaleBehavior
    {
        HideAll,
        ShowOnlyCustom,
        ShowOnlyCustomExceptOnHover,
        ShowOnHover,
        ShowAll
    }


    // ReSharper disable RedundantDefaultMemberInitializer
    [Setting] internal static bool ModEnabled = true;

    // milliseconds
    [Setting] internal static float CacheRefreshRate = 1000f;

    [Setting] internal static bool EnablePlaySettingToggle = true;

    [Setting] internal static bool DrawLabelOnlyOnHover = false;
    [Setting] internal static bool TruncateLongLabels = true;

    [Setting] internal static bool DrawJobTitle = true;
    [Setting] internal static bool DrawJobTitleBackground = true;
    [Setting] internal static bool OnlyDrawCustomJobTitles = false;

    [Setting] internal static Color DefaultJobLabelColor = GenMapUI.DefaultThingLabelColor;


    [Setting] internal static bool AllowNameColors = true;


    [Setting] internal static int ExtraOffsetPerLine = -4; // Legacy setting that I don't think anyone used
    [Setting] internal static bool OffsetEquippedByLabels = true;

    [Setting] internal static float MinColonistBarScale = 0.8f;

    [Setting] internal static MinScaleBehavior MinColonistBarScaleBehavior =
        MinScaleBehavior.ShowOnlyCustomExceptOnHover;

    [Setting] internal static bool IgnoreGuests = true;
    [Setting] internal static bool IgnoreSlaves = false;
    [Setting] internal static bool IgnoreSubhuman = true;
    [Setting] internal static bool OnlyDrafted = false;
    [Setting] internal static bool OnlyCurrentMap = true;


    [Setting] internal static bool DrawCurrentTask = true;
    [Setting] internal static bool DrawCurrentTaskBackground = false;
    [Setting] internal static bool MoveWeaponBelowCurrentTask = true;
    [Setting] internal static Color CurrentTaskLabelColor = new(1f, 0.8f, 0.4f);

    [Setting] internal static bool DrawIdeoRoles = true;
    [Setting] internal static bool DrawIdeoRoleBackground = true;
    [Setting] internal static bool UseIdeoColorForRole = true;
    [Setting] internal static bool RoleColorOnlyIfAbilityAvailable = false;

    [Setting] internal static bool DrawRoyalTitles = true;
    [Setting] internal static bool DrawRoyalTitleBackground = true;
    [Setting] internal static Color RoyalTitleColorDefault = new(0.85f, 0.85f, 0.75f);

    [Setting] internal static ButtonLocations EnabledButtonLocations = ButtonLocations.All;

    [Setting] internal static List<string> DisabledPatchCategories = new();
    // ReSharper restore RedundantDefaultMemberInitializer
}
