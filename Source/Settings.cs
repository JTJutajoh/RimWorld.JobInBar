using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using UnityEngine;

namespace JobInBar;

internal class Settings : ModSettings
{
    private const float TabHeight = 32f;

    internal static Dictionary<string, object> DefaultSettings = new();

    [Setting] public static int JobLabelVerticalOffset;
    private static string _bufferJobLabelVerticalOffset = JobLabelVerticalOffset.ToString();
    [Setting] public static int OffsetEquippedExtra = -6;
    private static string _bufferOffsetEquippedExtra = OffsetEquippedExtra.ToString();

    private static Vector2 _scrollPositionCache = Vector2.zero;


    private static Vector2 _scrollPositionMainTab = Vector2.zero;
    private static float? _lastMainTabHeight;
    private static float? _miscSectionHeight;
    private static float? _perfSectionHeight;

    private SettingsTab _currentTab = SettingsTab.Main;

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
            new("JobInBar_Settings_Tab_Display".Translate(), () => _currentTab = SettingsTab.Display,
                () => _currentTab == SettingsTab.Display),
            new("JobInBar_Settings_Tab_Job".Translate(), () => _currentTab = SettingsTab.Job,
                () => _currentTab == SettingsTab.Job)
        };
#if !(v1_1)
        if (ModsConfig.RoyaltyActive)
            tabs.Add(new TabRecord("JobInBar_Settings_Tab_Royalty".Translate(),
                () => _currentTab = SettingsTab.Royalty,
                () => _currentTab == SettingsTab.Royalty));
#endif

#if !(v1_1 || v1_2)
        if (ModsConfig.IdeologyActive)
            tabs.Add(new TabRecord("JobInBar_Settings_Tab_Ideology".Translate(),
                () => _currentTab = SettingsTab.Ideology,
                () => _currentTab == SettingsTab.Ideology));
#endif

        if (Prefs.DevMode && LabelsTracker_WorldComponent.Instance != null)
            tabs.Add(new TabRecord("JobInBar_Settings_Tab_Cache".Translate(), () => _currentTab = SettingsTab.Cache,
                () => _currentTab == SettingsTab.Cache));

#if v1_5 || v1_6
        TabDrawer.DrawTabsOverflow(inRect.TopPartPixels(TabHeight), tabs, 80f, 200f);
#elif v1_1 || v1_2 || v1_3 || v1_4
        var tabsRect = inRect.TopPartPixels(TabHeight);
        tabsRect.y += TabHeight;
        TabDrawer.DrawTabs(tabsRect, tabs, 200f);
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
            case SettingsTab.Display:
                try
                {
                    DoTabDisplay(tabRect);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Error drawing display settings tab.", true);
                    throw;
                }

                break;
            case SettingsTab.Job:
                try
                {
                    DoTabJob(tabRect);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Error drawing colors settings tab.", true);
                    throw;
                }

                break;
#if !(v1_1 || v1_2)
            case SettingsTab.Ideology:
                if (!ModsConfig.IdeologyActive)
                {
                    _currentTab = SettingsTab.Main;
                    break;
                }

                try
                {
                    DoTabIdeology(tabRect);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Error drawing ideology settings tab.", true);
                    throw;
                }

                break;
#endif
#if !(v1_1)
            case SettingsTab.Royalty:
                if (!ModsConfig.RoyaltyActive)
                {
                    _currentTab = SettingsTab.Main;
                    break;
                }

                try
                {
                    DoTabRoyalty(tabRect);
                }
                catch (Exception e)
                {
                    Log.Exception(e, "Error drawing royalty settings tab.", true);
                    throw;
                }

                break;
#endif
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
        Widgets.AdjustRectsForScrollView(inRect, ref outerRect, ref viewRect);
        viewRect.height = _lastMainTabHeight ?? inRect.height * 2f;

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
        var perfSection = listing.BeginSection(_perfSectionHeight ?? 9999f)!;

        perfSection.SectionHeader("JobInBar_Settings_PerformanceWarningHeader");
        perfSection.SubLabel("JobInBar_Settings_PerformanceWarning".Translate(), 1f);

        perfSection.CheckboxLabeled(GetSettingLabel("DrawLabelOnlyOnHover"), ref DrawLabelOnlyOnHover,
            GetSettingTooltip("DrawLabelOnlyOnHover"), 36f);

        perfSection.CheckboxLabeled(GetSettingLabel("OnlyDrawCustomJobTitles"), ref OnlyDrawCustomJobTitles,
            GetSettingTooltip("OnlyDrawCustomJobTitles"), 36f);

        _perfSectionHeight = perfSection.MaxColumnHeightSeen;
        listing.EndSection(perfSection);
        listing.Gap();

        var miscSection = listing.BeginSection(_miscSectionHeight ?? 9999f)!;

        miscSection.SectionHeader("JobInBar_Settings_Section_Misc");

        miscSection.CheckboxLabeled(GetSettingLabel("EnablePlaySettingToggle"), ref EnablePlaySettingToggle,
            GetSettingTooltip("EnablePlaySettingToggle"), 36f, 0.90f);

        miscSection.Gap();
        miscSection.Label(GetSettingLabel("EnabledButtonLocations"));

        var locNamePawn = EnabledButtonLocations.HasFlag(ButtonLocations.NamePawn);
        miscSection.CheckboxLabeled(GetSettingLabel("ButtonLocation_NamePawn"), ref locNamePawn,
            GetSettingTooltip("ButtonLocation_NamePawn"), 24f, 0.90f);
        if (locNamePawn)
            EnabledButtonLocations |= ButtonLocations.NamePawn;
        else
            EnabledButtonLocations &= ~ButtonLocations.NamePawn;

        var locCharacterCard = EnabledButtonLocations.HasFlag(ButtonLocations.CharacterCard);
        miscSection.CheckboxLabeled(GetSettingLabel("ButtonLocation_CharacterCard"), ref locCharacterCard,
            GetSettingTooltip("ButtonLocation_CharacterCard"), 24f, 0.90f);
        if (locCharacterCard)
            EnabledButtonLocations |= ButtonLocations.CharacterCard;
        else
            EnabledButtonLocations &= ~ButtonLocations.CharacterCard;

        _miscSectionHeight = miscSection.MaxColumnHeightSeen;
        listing.EndSection(miscSection);

        listing.NewColumn();

        // Current task
        var sectionRect = listing.GetRect(0f);

        listing.CheckboxLabeled(GetSettingLabel("DrawCurrentTask"), ref DrawCurrentTask,
            GetSettingTooltip("DrawCurrentTask"), 36f);

        if (DrawCurrentTask)
        {
            listing.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawCurrentTaskBackground,
                GetSettingTooltip("DrawBackground"), 36f);

            var labelRect = listing.SubLabel("JobInBar_Settings_CurrentJobNote".Translate(), 1f);

            var colorPickerRect = listing.GetRect(200f);
            CustomWidgets.LabelColorPicker(colorPickerRect, ref CurrentTaskLabelColor,
                DrawCurrentTaskBackground, "JobInBar_Settings_CurrentTaskLabelColor".Translate(),
                defaultButton: true, defaultColor: DefaultSettings["CurrentTaskLabelColor"] as Color?);
            // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
            // listing.GetRect(colorPickerRect.yMax - labelRect.yMax);
        }

        _lastMainTabHeight = listing.MaxColumnHeightSeen;
        listing.End();
        Widgets.EndScrollView();
    }

    private static void DoTabDisplay(Rect inRect)
    {
        _bufferJobLabelVerticalOffset = JobLabelVerticalOffset.ToString();
        _bufferOffsetEquippedExtra = OffsetEquippedExtra.ToString();
        var listing = new Listing_Standard();
        listing.Begin(inRect.MiddlePart(0.75f, 1f));

        listing.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawJobTitleBackground,
            GetSettingTooltip("DrawBackground"), 36f, 0.50f);

        listing.CheckboxLabeled(GetSettingLabel("TruncateLongLabels"), ref TruncateLongLabels,
            GetSettingTooltip("TruncateLongLabels"), 36f, 0.50f);

        listing.GapLine();

        listing.Label("JobInBar_Settings_Positioning".Translate());

        listing.IntSetting(ref JobLabelVerticalOffset,
            "JobLabelVerticalOffset", ref _bufferJobLabelVerticalOffset, null, 2, -150, 150);

        listing.Gap(24f);

        listing.Label("JobInBar_Settings_Equipped".Translate());
        listing.SubLabel("JobInBar_Settings_EquippedSubLabel".Translate(), 0.7f);

        listing.CheckboxLabeled(GetSettingLabel("OffsetEquippedByLabels"), ref OffsetEquippedByLabels,
            GetSettingTooltip("OffsetEquippedByLabels"), labelPct: 0.50f);

        if (DrawCurrentTask)
            listing.CheckboxLabeled(GetSettingLabel("MoveWeaponBelowCurrentTask"),
                ref MoveWeaponBelowCurrentTask,
                GetSettingTooltip("MoveWeaponBelowCurrentTask"), 36f, 0.50f);

        listing.IntSetting(ref OffsetEquippedExtra,
            "OffsetEquippedExtra", ref _bufferOffsetEquippedExtra, null, 2, -150, 150);

        listing.GapLine();


        listing.End();
    }

    private static void DoTabJob(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect.MiddlePart(0.75f, 1f));

        var sectionRect = listing.GetRect(0f);

        listing.CheckboxLabeled(GetSettingLabel("DrawJobTitle"), ref DrawJobTitle,
            GetSettingTooltip("DrawJobTitle"), 36f, 0.40f);

        var labelRect = listing.SubLabel("JobInBar_Settings_JobTitleNote".Translate(), 0.36f);

        listing.Gap();

#if v1_4 || v1_5 || v1_6
        if (DrawJobTitle)
        {
            var colorPickerWidth = listing.ColumnWidth * 0.5f - 32f;
            var colorPickerRect = new Rect(listing.ColumnWidth - colorPickerWidth - 16f,
                sectionRect.yMin + 4f, colorPickerWidth, 200f);
            CustomWidgets.LabelColorPicker(colorPickerRect, ref DefaultJobLabelColor,
                DrawJobTitleBackground,
                "JobInBar_Settings_DefaultJobLabelColor".Translate(),
                defaultButton: true, defaultColor: DefaultSettings["DefaultJobLabelColor"] as Color?);
            // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
            listing.GetRect(colorPickerRect.yMax - sectionRect.yMax);
        }
#endif


        listing.End();
    }

    private static void DoTabIdeology(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect.MiddlePart(0.75f, 1f));

        listing.CheckboxLabeled(GetSettingLabel("DrawIdeoRoles"), ref DrawIdeoRoles,
            GetSettingTooltip("DrawIdeoRoles"), 36f, 0.40f);

        var sectionRect = listing.GetRect(0f);

        if (!DrawIdeoRoles)
        {
            listing.End();
            return;
        }

        listing.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawIdeoRoleBackground,
            GetSettingTooltip("DrawBackground"), 36f, 0.40f);

        listing.CheckboxLabeled(GetSettingLabel("UseIdeoColorForRole"), ref UseIdeoColorForRole,
            GetSettingTooltip("UseIdeoColorForRole"), 36f, 0.40f);

        if (!UseIdeoColorForRole)
        {
            var colorPickerWidth = listing.ColumnWidth * 0.5f - 32f;
            var colorPickerRect = new Rect(listing.ColumnWidth - colorPickerWidth - 16f,
                sectionRect.yMin + 4f, colorPickerWidth, 200f);
            CustomWidgets.LabelColorPicker(colorPickerRect, ref IdeoRoleColorOverride,
                DrawIdeoRoleBackground, "JobInBar_Settings_IdeoRoleColorOverride".Translate(),
                defaultButton: true, defaultColor: DefaultSettings["IdeoRoleColorOverride"] as Color?);

            // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
            listing.GetRect(colorPickerRect.yMax - sectionRect.yMax);
        }
        else
        {
            listing.Indent();
            if (UseIdeoColorForRole)
            {
                listing.Indent();
                listing.CheckboxLabeled(GetSettingLabel("RoleColorAbility"),
                    ref RoleColorOnlyIfAbilityAvailable,
                    GetSettingTooltip("RoleColorAbility"), 36f, 0.40f);
                listing.Outdent();
            }

            listing.Outdent();

            listing.Gap();
        }


        listing.End();
    }

    private static void DoTabRoyalty(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect.MiddlePart(0.75f, 1f));

        var sectionRect = listing.GetRect(0f);

        listing.CheckboxLabeled(GetSettingLabel("DrawRoyalTitles"), ref DrawRoyalTitles,
            GetSettingTooltip("DrawRoyalTitles"), 36f, 0.40f);
        listing.Gap();

        var labelRect = listing.SubLabel("JobInBar_Settings_RoyalTitleNote".Translate(), 0.36f);

        if (DrawRoyalTitles)
        {
            listing.CheckboxLabeled(GetSettingLabel("DrawBackground"), ref DrawRoyalTitleBackground,
                GetSettingTooltip("DrawBackground"), 36f, 0.40f);

            var colorPickerWidth = listing.ColumnWidth * 0.5f - 32f;
            var colorPickerRect = new Rect(listing.ColumnWidth - colorPickerWidth - 16f,
                sectionRect.yMin + 4f, colorPickerWidth, 200f);
            CustomWidgets.LabelColorPicker(colorPickerRect, ref RoyalTitleColor, DrawRoyalTitleBackground,
                "JobInBar_Settings_RoyalTitleColor".Translate(),
                defaultButton: true, defaultColor: DefaultSettings["RoyalTitleColor"] as Color?);
            // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
            listing.GetRect(colorPickerRect.yMax - labelRect.yMax);
        }

        listing.End();
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

    [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
    public override void ExposeData()
    {
        Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);

        Scribe_Values.Look(ref DrawJobTitle, "DrawJobTitle", true);
        Scribe_Values.Look(ref OnlyDrawCustomJobTitles, "OnlyDrawCustomJobTitles", false);
        Scribe_Values.Look(ref JobLabelVerticalOffset, "JobLabelVerticalOffset", 14);
        Scribe_Values.Look(ref OffsetEquippedExtra, "OffsetEquippedExtra");
        Scribe_Values.Look(ref OffsetEquippedByLabels, "OffsetEquippedByLabels", true);

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
        Scribe_Values.Look(ref IdeoRoleColorOverride, "IdeoRoleColorOverride", Color.white);

        Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);
        Scribe_Values.Look(ref RoyalTitleColor, "RoyalTitleColor", new Color(0.85f, 0.85f, 0.75f));

        Scribe_Values.Look(ref DrawCurrentTask, "DrawCurrentTask", true);
        Scribe_Values.Look(ref MoveWeaponBelowCurrentTask, "MoveWeaponBelowCurrentTask", true);

        Scribe_Values.Look(ref EnabledButtonLocations, "EnabledButtonLocations", ButtonLocations.All);

        base.ExposeData();
    }

    private class SettingAttribute : Attribute
    {
    }

    // GUI Stuff
    private enum SettingsTab
    {
        Main,
        Royalty,
        Ideology,
        Job,
        Display,
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


    // ReSharper disable RedundantDefaultMemberInitializer
    [Setting] internal static bool ModEnabled = true;

    [Setting] internal static bool DrawLabelOnlyOnHover = false;
    [Setting] internal static bool EnablePlaySettingToggle = true;
    [Setting] internal static bool TruncateLongLabels = true;

    [Setting] internal static bool DrawJobTitle = true;
    [Setting] internal static bool DrawJobTitleBackground = true;
    [Setting] internal static bool OnlyDrawCustomJobTitles = false;

    [Setting] internal static Color DefaultJobLabelColor =
        GenMapUI.DefaultThingLabelColor.ClampToValueRange(new FloatRange(0f, 0.7f));


    [Setting] internal static int ExtraOffsetPerLine = -4; // Legacy setting that I don't think anyone used
    [Setting] internal static bool OffsetEquippedByLabels = true;


    [Setting] internal static bool DrawCurrentTask = true;
    [Setting] internal static bool DrawCurrentTaskBackground = false;
    [Setting] internal static bool MoveWeaponBelowCurrentTask = true;
    [Setting] internal static Color CurrentTaskLabelColor = new(1f, 0.8f, 0.4f);

    [Setting] internal static bool DrawIdeoRoles = true;
    [Setting] internal static bool DrawIdeoRoleBackground = true;
    [Setting] internal static bool UseIdeoColorForRole = true;
    [Setting] internal static bool RoleColorOnlyIfAbilityAvailable = false;
    [Setting] internal static Color IdeoRoleColorOverride = Color.white;

    [Setting] internal static bool DrawRoyalTitles = true;
    [Setting] internal static bool DrawRoyalTitleBackground = true;
    [Setting] internal static Color RoyalTitleColor = new(0.85f, 0.85f, 0.75f);

    [Setting] internal static ButtonLocations EnabledButtonLocations = ButtonLocations.All;
    // ReSharper restore RedundantDefaultMemberInitializer
}
