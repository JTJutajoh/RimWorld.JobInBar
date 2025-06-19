using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


namespace JobInBar
{
    class Settings : ModSettings
    {
        private class SettingAttribute : Attribute
        {
        }


        // ReSharper disable RedundantDefaultMemberInitializer
        [Setting] public static bool ModEnabled = true;

        [Setting] public static bool DrawLabelOnlyOnHover = false;
        [Setting] public static bool EnablePlaySettingToggle = true;

        [Setting] public static bool DrawJobTitle = true;
        [Setting] public static bool DrawJobTitleBackground = true;

        [Setting] public static Color DefaultJobLabelColor =
            GenMapUI.DefaultThingLabelColor.ClampToValueRange(new FloatRange(0f, 0.7f));

        [Setting] public static int JobLabelVerticalOffset = 0;
        [Setting] public static int ExtraOffsetPerLine = -4; // Legacy setting that I don't think anyone used
        [Setting] public static bool OffsetEquippedByLabels = true;
        [Setting] public static int OffsetEquippedExtra = -6;

        [Setting] public static bool DrawCurrentTask = true;
        [Setting] public static bool DrawCurrentTaskBackground = false;
        [Setting] public static bool MoveWeaponBelowCurrentTask = true;
        [Setting] public static Color CurrentTaskLabelColor = new Color(1f, 0.8f, 0.4f);

        [Setting] public static bool DrawIdeoRoles = true;
        [Setting] public static bool DrawIdeoRoleBackground = true;
        [Setting] public static bool UseIdeoColorForRole = true;
        [Setting] public static bool RoleColorOnlyIfAbilityAvailable = false;
        [Setting] public static Color IdeoRoleColorOverride = Color.white;

        [Setting] public static bool DrawRoyalTitles = true;
        [Setting] public static bool DrawRoyalTitleBackground = true;
        [Setting] public static Color RoyalTitleColor = new Color(0.85f, 0.85f, 0.75f);

        // ReSharper restore RedundantDefaultMemberInitializer

        internal static Dictionary<string, object> DefaultSettings = new();

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

        private SettingsTab _currentTab = SettingsTab.Main;
        private const float TabHeight = 32f;

        public Settings()
        {
            HarvestSettingsDefaults(out DefaultSettings);
            Log.Trace("Default settings loaded");
        }

        private void HarvestSettingsDefaults(out Dictionary<string, object> settings, Type? owningType = null)
        {
            settings = new Dictionary<string, object>();
            if (owningType is null) owningType = GetType();

            var fields = AccessTools.GetDeclaredFields(owningType);
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

        internal static string GetLabel(string key, bool showValue = false)
        {
            if (showValue)
            {
                var value = AccessTools.Field(typeof(Settings), key).GetValue(null).ToString();
                return $"JobInBar_Settings_{key}".Translate() + ": " + value;
            }
            else
            {
                return $"JobInBar_Settings_{key}".Translate();
            }
        }

        internal static string GetTooltip(string key)
        {
            var success = $"JobInBar_Settings_{key}_Desc".TryTranslate(out var str);

            if (!success)
            {
                str = "";
                return str;
            }

            if (DefaultSettings.TryGetValue(key, out var setting))
            {
                str += $"JobInBar_Settings_DefaultSuffix".Translate(setting.ToString());
            }

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
                new TabRecord("JobInBar_Settings_Tab_Main".Translate(), () => _currentTab = SettingsTab.Main,
                    () => _currentTab == SettingsTab.Main),
                new TabRecord("JobInBar_Settings_Tab_Display".Translate(), () => _currentTab = SettingsTab.Display,
                    () => _currentTab == SettingsTab.Display),
                new TabRecord("JobInBar_Settings_Tab_Job".Translate(), () => _currentTab = SettingsTab.Job,
                    () => _currentTab == SettingsTab.Job)
            };
            if (ModsConfig.RoyaltyActive)
            {
                tabs.Add(new TabRecord("JobInBar_Settings_Tab_Royalty".Translate(),
                    () => _currentTab = SettingsTab.Royalty,
                    () => _currentTab == SettingsTab.Royalty));
            }

            if (ModsConfig.IdeologyActive)
            {
                tabs.Add(new TabRecord("JobInBar_Settings_Tab_Ideology".Translate(),
                    () => _currentTab = SettingsTab.Ideology,
                    () => _currentTab == SettingsTab.Ideology));
            }

            if (Prefs.DevMode && LabelsTracker_WorldComponent.Instance != null)
            {
                tabs.Add(new TabRecord("JobInBar_Settings_Tab_Cache".Translate(), () => _currentTab = SettingsTab.Cache,
                    () => _currentTab == SettingsTab.Cache));
            }

#if v1_5 || v1_6
            TabDrawer.DrawTabsOverflow(inRect.TopPartPixels(TabHeight), tabs, 80f, 200f);
#elif v1_1 || v1_2 || v1_3 || v1_4
            TabDrawer.DrawTabs(inRect.TopPartPixels(TabHeight), tabs, 200f);
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
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect.MiddlePart(0.75f, 1f));

            listingStandard.CheckboxLabeled(GetLabel("ModEnabled"), ref ModEnabled, GetTooltip("ModEnabled"));

            if (!ModEnabled)
            {
                listingStandard.End();
                return;
            }

            listingStandard.Gap();

            listingStandard.SubLabel("JobInBar_Settings_PerformanceWarning".Translate(), 0.8f);
            listingStandard.Gap();

            listingStandard.CheckboxLabeled(GetLabel("DrawLabelOnlyOnHover"), ref DrawLabelOnlyOnHover,
                GetTooltip("DrawLabelOnlyOnHover"), 36f, 0.90f);

            listingStandard.CheckboxLabeled(GetLabel("EnablePlaySettingToggle"), ref EnablePlaySettingToggle,
                GetTooltip("EnablePlaySettingToggle"), 36f, 0.90f);

            // Current task
            listingStandard.GapLine();

            listingStandard.Indent();

            var sectionRect = listingStandard.GetRect(0f);

            listingStandard.CheckboxLabeled(GetLabel("DrawCurrentTask"), ref DrawCurrentTask,
                GetTooltip("DrawCurrentTask"), 36f, 0.38f);

            if (DrawCurrentTask)
            {
                listingStandard.CheckboxLabeled(GetLabel("DrawBackground"), ref DrawCurrentTaskBackground,
                    GetTooltip("DrawBackground"), 36f, 0.38f);

                var labelRect = listingStandard.SubLabel("JobInBar_Settings_CurrentJobNote".Translate(), 0.36f);

                var colorPickerWidth = listingStandard.ColumnWidth * 0.5f - 32f;
                var colorPickerRect = new Rect(listingStandard.ColumnWidth - colorPickerWidth - 16f,
                    sectionRect.yMin + 4f, colorPickerWidth, 200f);
                Dialog_LabelColorPicker.LabelColorPicker(colorPickerRect, ref CurrentTaskLabelColor,
                    "CurrentTaskLabelColor", DrawCurrentTaskBackground,
                    defaultButton: true, defaultColor: DefaultSettings["CurrentTaskLabelColor"] as Color?);
                // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
                listingStandard.GetRect(colorPickerRect.yMax - labelRect.yMax);

                listingStandard.CheckboxLabeled(GetLabel("MoveWeaponBelowCurrentTask"), ref MoveWeaponBelowCurrentTask,
                    GetTooltip("MoveWeaponBelowCurrentTask"), 36f, 0.38f);
            }

            listingStandard.Outdent();
            listingStandard.GapLine();

            listingStandard.End();
        }

        private static string _bufferJobLabelVerticalOffset = JobLabelVerticalOffset.ToString();
        private static string _bufferOffsetEquippedExtra = OffsetEquippedExtra.ToString();

        private static void DoTabDisplay(Rect inRect)
        {
            _bufferJobLabelVerticalOffset = JobLabelVerticalOffset.ToString();
            _bufferOffsetEquippedExtra = OffsetEquippedExtra.ToString();
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect.MiddlePart(0.75f, 1f));

            listingStandard.CheckboxLabeled(GetLabel("MoveWeaponBelowCurrentTask"), ref DrawJobTitleBackground,
                GetTooltip("DrawBackground"), 36f, 0.90f);

            listingStandard.GapLine();

            listingStandard.Label("JobInBar_Settings_Positioning".Translate());

            IntSetting(listingStandard, null, ref JobLabelVerticalOffset,
                "JobLabelVerticalOffset", ref _bufferJobLabelVerticalOffset, 2, -150, 150);

            listingStandard.Gap(24f);

            listingStandard.Label("JobInBar_Settings_Equipped".Translate());
            listingStandard.SubLabel("JobInBar_Settings_EquippedSubLabel".Translate(), 0.7f);

            listingStandard.CheckboxLabeled(GetLabel("OffsetEquippedByLabels"), ref OffsetEquippedByLabels,
                GetTooltip("OffsetEquippedByLabels"), labelPct: 0.50f);

            IntSetting(listingStandard, null, ref OffsetEquippedExtra,
                "OffsetEquippedExtra", ref _bufferOffsetEquippedExtra, 2, -150, 150);

            listingStandard.End();
        }

        private static void DoTabJob(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect.MiddlePart(0.75f, 1f));

            var sectionRect = listingStandard.GetRect(0f);

            listingStandard.CheckboxLabeled(GetLabel("DrawJobTitle"), ref DrawJobTitle,
                GetTooltip("DrawJobTitle"), 36f, 0.40f);
            listingStandard.Gap();

            var labelRect = listingStandard.SubLabel("JobInBar_Settings_JobTitleNote".Translate(), 0.36f);


#if v1_4 || v1_5 || v1_6
            if (DrawJobTitle)
            {
                var colorPickerWidth = listingStandard.ColumnWidth * 0.5f - 32f;
                var colorPickerRect = new Rect(listingStandard.ColumnWidth - colorPickerWidth - 16f,
                    sectionRect.yMin + 4f, colorPickerWidth, 200f);
                Dialog_LabelColorPicker.LabelColorPicker(colorPickerRect, ref DefaultJobLabelColor,
                    "DefaultJobLabelColor",
                    DrawJobTitleBackground,
                    defaultButton: true, defaultColor: DefaultSettings["DefaultJobLabelColor"] as Color?);
                // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
                listingStandard.GetRect(colorPickerRect.yMax - sectionRect.yMax);
            }
#endif


            listingStandard.End();
        }

        private static void DoTabIdeology(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect.MiddlePart(0.75f, 1f));

            listingStandard.CheckboxLabeled(GetLabel("DrawIdeoRoles"), ref DrawIdeoRoles,
                GetTooltip("DrawIdeoRoles"), 36f, 0.40f);

            var sectionRect = listingStandard.GetRect(0f);

            if (!DrawIdeoRoles)
            {
                listingStandard.End();
                return;
            }

            listingStandard.CheckboxLabeled(GetLabel("DrawBackground"), ref DrawIdeoRoleBackground,
                GetTooltip("DrawBackground"), 36f, 0.40f);

            listingStandard.CheckboxLabeled(GetLabel("UseIdeoColorForRole"), ref UseIdeoColorForRole,
                GetTooltip("UseIdeoColorForRole"), 36f, 0.40f);

            if (!UseIdeoColorForRole)
            {
                var colorPickerWidth = listingStandard.ColumnWidth * 0.5f - 32f;
                var colorPickerRect = new Rect(listingStandard.ColumnWidth - colorPickerWidth - 16f,
                    sectionRect.yMin + 4f, colorPickerWidth, 200f);
                Dialog_LabelColorPicker.LabelColorPicker(colorPickerRect, ref IdeoRoleColorOverride,
                    "IdeoRoleColorOverride", DrawIdeoRoleBackground,
                    defaultButton: true, defaultColor: DefaultSettings["IdeoRoleColorOverride"] as Color?);
                
                // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
                listingStandard.GetRect(colorPickerRect.yMax - sectionRect.yMax);
            }
            else
            {
                listingStandard.Indent();
                if (UseIdeoColorForRole)
                {
                    listingStandard.Indent();
                    listingStandard.CheckboxLabeled(GetLabel("RoleColorAbility"), ref RoleColorOnlyIfAbilityAvailable,
                        GetTooltip("RoleColorAbility"), 36f, 0.40f);
                    listingStandard.Outdent();
                }

                listingStandard.Outdent();

                listingStandard.Gap();
            }


            listingStandard.End();
        }

        private static void DoTabRoyalty(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect.MiddlePart(0.75f, 1f));

            var sectionRect = listingStandard.GetRect(0f);

            listingStandard.CheckboxLabeled(GetLabel("DrawRoyalTitles"), ref DrawRoyalTitles,
                GetTooltip("DrawRoyalTitles"), 36f, 0.40f);
            listingStandard.Gap();

            var labelRect = listingStandard.SubLabel("JobInBar_Settings_RoyalTitleNote".Translate(), 0.36f);

            if (DrawRoyalTitles)
            {
                listingStandard.CheckboxLabeled(GetLabel("DrawBackground"), ref DrawRoyalTitleBackground,
                    GetTooltip("DrawBackground"), 36f, 0.40f);

                var colorPickerWidth = listingStandard.ColumnWidth * 0.5f - 32f;
                var colorPickerRect = new Rect(listingStandard.ColumnWidth - colorPickerWidth - 16f,
                    sectionRect.yMin + 4f, colorPickerWidth, 200f);
                Dialog_LabelColorPicker.LabelColorPicker(colorPickerRect, ref RoyalTitleColor, "RoyalTitleColor",
                    DrawRoyalTitleBackground,
                    defaultButton: true, defaultColor: DefaultSettings["RoyalTitleColor"] as Color?);
                // Add extra space to the listing standard based on how much larger this section was than the bottom of the last label
                listingStandard.GetRect(colorPickerRect.yMax - labelRect.yMax);
            }

            listingStandard.End();
        }

        private static Vector2 _scrollPositionCache = Vector2.zero;

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
            viewRect.height = (labelsComp.TrackedPawns.Count * 48f) + 32f;
            Widgets.BeginScrollView(outerRect, ref _scrollPositionCache, viewRect);
            var curY = viewRect.yMin;
            Pawn? remove = null;
            foreach (var (key, value) in labelsComp.TrackedPawns)
            {
                var label =
                    $"{key?.NameShortColored}: [Pawn: \"{value?.Pawn?.NameShortColored}\", ShowBackstory: {value?.ShowBackstory}, BackstoryColor: {value?.BackstoryColor}, ShowRoyalTitle: {value?.ShowRoyalTitle}, ShowIdeoRole: {value?.ShowIdeoRole}]";
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
                if (key != null && Widgets.ButtonTextSubtle(rect.RightPart(0.05f).MiddlePartPixels(50f, 24f), "JobInBar_Reset".Translate()))
                {
                    remove = key;
                }

                curY += rect.height + 4f;
                GUI.color = Color.white;
            }

            if (remove != null)
            {
                labelsComp.TrackedPawns.Remove(remove);
            }


            Widgets.EndScrollView();
        }

        private static void IntSetting(Listing_Standard listingStandard, string? label, ref int value, string key,
            ref string editBuffer,
            int multiplier = 1, int min = 0, int max = 999999, bool defaultButton = true)
        {
            if (label == null)
                listingStandard.Label(label);
            var labelRect = listingStandard.Label(GetLabel(key, true));
            var tooltip = GetTooltip(key);
            if (tooltip != "")
            {
                TooltipHandler.TipRegion(
                    new Rect(labelRect.xMin, labelRect.yMin, labelRect.width, labelRect.height + 30f), tooltip);
            }

#if !(v1_2 || v1_3 || v1_4 || v1_5)
            listingStandard.IntEntry(ref value, ref editBuffer, multiplier, min);
#else
            listingStandard.IntEntry(ref value, ref editBuffer, multiplier);
#endif
            if (defaultButton)
            {
                listingStandard.IntSetter(ref value, (int)DefaultSettings[key],
                    "JobInBar_Settings_Default".Translate());
            }

            value = Mathf.Clamp(value, min, max);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ModEnabled, "ModEnabled", true);

            Scribe_Values.Look(ref DrawJobTitle, "DrawJobTitle", true);
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
            Scribe_Values.Look(ref DrawLabelOnlyOnHover, "DrawLabelOnlyOnHover");

            Scribe_Values.Look(ref DrawIdeoRoles, "DrawIdeoRoles", true);
            Scribe_Values.Look(ref UseIdeoColorForRole, "UseIdeoColorForRole", true);
            Scribe_Values.Look(ref RoleColorOnlyIfAbilityAvailable, "RoleColorOnlyIfAbilityAvailable");
            Scribe_Values.Look(ref IdeoRoleColorOverride, "IdeoRoleColorOverride", Color.white);

            Scribe_Values.Look(ref DrawRoyalTitles, "DrawRoyalTitles", true);
            Scribe_Values.Look(ref RoyalTitleColor, "RoyalTitleColor", new Color(0.85f, 0.85f, 0.75f));

            Scribe_Values.Look(ref DrawCurrentTask, "DrawCurrentTask", true);
            Scribe_Values.Look(ref MoveWeaponBelowCurrentTask, "MoveWeaponBelowCurrentTask", true);

            base.ExposeData();
        }
    }
}