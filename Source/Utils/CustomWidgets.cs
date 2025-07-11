﻿using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JobInBar.Utils;

/// <summary>
///     Helper static class that contains methods to create common widgets I use in my mods.<br />
///     Many of these are implemented as extension methods, some of which are overloads for vanilla methods (such as ones
///     for <see cref="Listing_Standard" />).
/// </summary>
internal static class CustomWidgets
{
    /// <summary>
    ///     Helper function to draw a full color picker with example pawn labels exactly how they appear in the Colonist Bar
    ///     (literally calls the same function in <see cref="LabelDrawer" />).<br />
    ///     Auto-detects if it should do a horizontal or vertical layout based on which dimension of the input rect is larger.
    ///     <br />
    /// </summary>
    /// <param name="rect">The rect to draw it within.</param>
    /// <param name="color">The color to be set</param>
    /// <param name="labelBackgrounds">Add the background to the label examples</param>
    /// <param name="exampleText">Used for the label example text</param>
    /// <param name="currentlyDraggingColorPicker">Unique bool ref for each color picker instance.</param>
    /// <param name="hexStringBuffer">Ref string needed for the hex code text field</param>
    /// <param name="doBackground">If true, adds a background around the whole element</param>
    /// <param name="defaultButton">Include a "Default" button to reset to the harvested default value</param>
    /// <param name="onDefault">Invoked whenever the Default button is clicked</param>
    /// <param name="defaultColor">Optional color to be used when the Default button is clicked</param>
    /// <param name="header">Optional string to show as a header section above the color picker.</param>
    /// <param name="disabled">Is the color picker interactive or not.</param>
    internal static void LabelColorPicker(
        Rect rect,
        ref Color color,
        bool labelBackgrounds,
        string exampleText,
        ref bool currentlyDraggingColorPicker,
        ref string? hexStringBuffer,
        bool doBackground = true,
        bool defaultButton = false,
        Action? onDefault = null,
        Color? defaultColor = null,
        string? header = null,
        bool disabled = false
    )
    {
        if (doBackground)
        {
            Widgets.DrawMenuSection(rect);
            rect = rect.ContractedBy(4f);
        }

        var settingsRect = new Rect(rect);
        var curY = settingsRect.yMin + 4f;

        if (header != null)
        {
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(settingsRect.xMin, ref curY, settingsRect.width, header);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(settingsRect.xMin, curY, settingsRect.width);
            GUI.color = Color.white;
            curY += 3f;
        }

        var colorLabel = exampleText;
        Widgets.DrawBoxSolid(new Rect(settingsRect.xMin, curY - 2f, settingsRect.width, 16f),
            ColorLibrary.DarkBrown);
        LabelDrawer.DrawCustomLabel(new Vector2(settingsRect.center.x, curY), colorLabel, Text.CalcSize(colorLabel).x, color, TextAnchor.MiddleCenter,
            drawBg: labelBackgrounds);
        curY += 12f + 4f;

        Widgets.DrawBoxSolid(new Rect(settingsRect.xMin, curY - 2, settingsRect.width, 16f),
            ColorLibrary.Beige);
        LabelDrawer.DrawCustomLabel(new Vector2(settingsRect.center.x, curY), colorLabel, Text.CalcSize(colorLabel).x, color, TextAnchor.MiddleCenter,
            drawBg: labelBackgrounds);
        curY += 12f + 4f;

        var colorPickerHeight = rect.height - (curY - rect.yMin);
        if (defaultButton) colorPickerHeight -= 32f + 4f;

        var colorPickerRect = new Rect(rect.xMin, curY, rect.width, colorPickerHeight);
        ColorPicker(colorPickerRect, ref color, disabled, ref currentlyDraggingColorPicker, ref hexStringBuffer, false);


        if (!defaultButton) return;

        var defaultButtonRect = new Rect(rect.xMin, colorPickerRect.yMax + 2f, rect.width, 32f);
        if (Widgets.ButtonText(defaultButtonRect, "JobInBar_Settings_Default".Translate(), active: !disabled))
        {
            color = defaultColor ?? color;
            hexStringBuffer = "#" + ColorUtility.ToHtmlStringRGBA(color);
            onDefault?.Invoke();
        }

        if (disabled)
            Widgets.DrawBoxSolid(rect, Widgets.InactiveColor);
    }

    /// <summary>
    ///     Creates an HSV color picker along with HSVA sliders.<br />
    ///     Uses the HSV color wheel widget added in RW 1.4. If using an older version, the wheel will simply be skipped.
    /// </summary>
    internal static void ColorPicker(
        Rect rect,
        ref Color color,
        bool disabled,
        ref bool currentlyDraggingColorPicker,
        ref string? hexStringBuffer,
        bool windowBackground = true
    )
    {
        if (windowBackground)
        {
            Widgets.DrawWindowBackground(rect);
            rect = rect.ContractedBy(4f);
        }

        //TODO: Remove (or make use of) vertical layout support from the ColorPicker. It's a bunch of extra logic that isn't doing anything
        var horizontalLayout = rect.width >= rect.height;

        var curY = rect.yMin;
        Rect hsvRect;
        Rect slidersRect;
        if (horizontalLayout)
        {
            rect.height = Mathf.Min(rect.height, 200f);
            hsvRect = rect.LeftPartPixels(rect.height);
            slidersRect = rect.RightPartPixels(rect.width - hsvRect.width - 8f);
            slidersRect.yMin += 8f;
        }
        else
        {
            rect.width = Mathf.Min(rect.width, 200f);
            hsvRect = rect.TopPartPixels(rect.width);
            curY += hsvRect.height + 4f;
            slidersRect = new Rect(rect.xMin, curY, rect.width, rect.height - hsvRect.height - 4f);
        }

        var prevColor = color;

#if !(v1_1 || v1_2 || v1_3) // HSVColorWheel was added in RW 1.4+
        var newColor = color;
        Widgets.HSVColorWheel(hsvRect, ref newColor, ref currentlyDraggingColorPicker);
        if (!disabled)
            color = newColor;
#else
        // For legacy RW versions: Just draw a box with the color. The sliders still work to actually adjust it.
        Widgets.DrawBoxSolidWithOutline(hsvRect, color, Widgets.SeparatorLineColor);
#endif
        Color.RGBToHSV(color, out var hue, out var saturation, out var value);

        curY += 4f;

// RW 1.4 marked this Widgets.HorizontalSlider overload obsolete for some reason. Ignore the warning.
#pragma warning disable CS0612 // Type or member is obsolete
        hue =
            Widgets.HorizontalSlider(new Rect(slidersRect.xMin, curY, slidersRect.width, 24f), hue, 0f, 1f, true,
                "JobInBar_Hue".Translate());
        curY += 24f + 2f;

        saturation =
            Widgets.HorizontalSlider(new Rect(slidersRect.xMin, curY, slidersRect.width, 24f), saturation, 0f, 1f,
                true, "JobInBar_Saturation".Translate());
        curY += 24f + 2f;

        value =
            Widgets.HorizontalSlider(new Rect(slidersRect.xMin, curY, slidersRect.width, 24f), value, 0f, 1f, true,
                "JobInBar_Value".Translate());
        curY += 24f + 2f;

        var oldAlpha = color.a;
        if (!disabled)
            color = Color.HSVToRGB(hue, saturation, value);
        color.a = oldAlpha;

        var newAlpha =
            Widgets.HorizontalSlider(new Rect(slidersRect.xMin, curY, slidersRect.width, 24f), color.a, 0f, 1f,
                true, "JobInBar_Alpha".Translate());
        if (!disabled)
            color.a = newAlpha;
        curY += 24f + 2f;

        curY += 8f;
        var hexFieldHeight = 24f;
        var hexFieldWidth = 128f;
        var hexFieldY = Mathf.Max(curY, rect.yMax - 4f - hexFieldHeight);

        Regex? hexColorRegex = null; //new Regex("^#[0-9A-Fa-f]{8}$");
        if (!color.IndistinguishableFrom(prevColor) || hexStringBuffer == null)
        {
            hexStringBuffer = "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        hexStringBuffer = Widgets.TextField(
            new Rect(slidersRect.xMax - hexFieldWidth, hexFieldY, hexFieldWidth, hexFieldHeight), hexStringBuffer, 9,
            hexColorRegex!) ?? hexStringBuffer;
        if (!disabled && ColorUtility.TryParseHtmlString(hexStringBuffer, out var col))
        {
            color = col;
            // hexStringBuffer = "#" + ColorUtility.ToHtmlStringRGBA(color);
        }
#pragma warning restore CS0612 // Type or member is obsolete
    }

    /// <summary>
    ///     Extension method for <see cref="Listing_Standard" />.<br />
    ///     Creates a nicely formatted set of widgets to adjust and set an integer value corresponding to a field in
    ///     <see cref="Settings" />. The supplied setting name must exactly match the field name.
    /// </summary>
    /// <param name="listingStandard">Listing standard instance</param>
    /// <param name="value">The int value to be adjusted by this widget.</param>
    /// <param name="settingName">The name of the field on <see cref="Settings" /> that this widget modifies.</param>
    /// <param name="editBuffer">The string buffer used to temporarily store and edit the value.</param>
    /// <param name="label">Optional label for the block. If null, no label will be shown.</param>
    /// <param name="multiplier">A multiplier value applied during input adjustment.</param>
    /// <param name="min">The minimum allowed value for the integer.</param>
    /// <param name="max">The maximum allowed value for the integer.</param>
    /// <param name="doDefaultButton">Optionally, disable the "Default" button.</param>
    internal static void IntSetting(this Listing_Standard listingStandard,
        ref int value,
        string settingName,
        ref string editBuffer,
        string? label = null,
        int multiplier = 1,
        int min = 0,
        int max = 999999,
        bool doDefaultButton = true)
    {
        if (label != null) listingStandard.Label(label);
        var labelRect = listingStandard.Label(Settings.GetSettingLabel(settingName,
            true));
        var tooltip = Settings.GetSettingTooltip(settingName);
        if (tooltip != "")
            TooltipHandler.TipRegion(
                new Rect(labelRect.xMin,
                    labelRect.yMin,
                    labelRect.width,
                    labelRect.height + 30f),
                tooltip);

        IntEntry(listingStandard,
            ref value,
            ref editBuffer,
            (int)Settings.DefaultSettings[settingName],
            multiplier,
            min,
            max);
    }

    /// <summary>
    ///     Extension method for <see cref="Listing_Standard" />.<br />
    ///     Wrapper for vanilla <see cref="Listing_Standard.IntAdjuster" /> that includes a button to reset to a specified
    ///     default value.<br />
    ///     Ensures integer value is clamped within the specified range and supports editing through a buffer.<br />
    ///     Acts as an overload for vanilla <see cref="Listing_Standard.IntEntry" />.
    /// </summary>
    /// <param name="listingStandard">The Listing_Standard instance for extending functionality.</param>
    /// <param name="value">The integer value to be modified.</param>
    /// <param name="defaultValue">The default value to reset to when the default button is clicked.</param>
    /// <param name="editBuffer">The string buffer used to temporarily store and edit the value.</param>
    /// <param name="multiplier">A multiplier value applied during input adjustment.</param>
    /// <param name="min">The minimum allowed value for the integer.</param>
    /// <param name="max">The maximum allowed value for the integer.</param>
    internal static void IntEntry(this Listing_Standard listingStandard,
        ref int value,
        ref string editBuffer,
        int defaultValue,
        int multiplier = 1,
        int min = 0,
        int max = 999999)
    {
#if !(v1_2 || v1_3 || v1_4 || v1_5) // RW 1.6 fixed a bug with IntEntry that forced the value to be a positive number
        listingStandard.IntEntry(ref value, ref editBuffer, multiplier, min);
#else
        listingStandard.IntEntryWithNegative(ref value, ref editBuffer, multiplier, min);
#endif
        listingStandard.IntSetter(ref value, defaultValue, "JobInBar_Settings_Default".Translate());

        value = Mathf.Clamp(value, min, max);
    }

    // // Half-baked implementation of color preset selection. Currently replaced with the color wheel.
    // private static void ColorSelector(string label, Rect rect, ref Color color, float maxHeight = 120f,
    //     Texture? icon = null,
    //     int colorSize = 22, int colorPadding = 2, float paddingLeft = 32f, float paddingRight = 32f)
    // {
    //     rect.xMin += paddingLeft;
    //     rect.xMax -= paddingRight;
    //     Widgets.DrawWindowBackground(rect);
    //     rect = rect.ContractedBy(4f);
    //
    //     GUI.color = color;
    //     var labelRect = rect.TopPartPixels(Text.CalcHeight(label, rect.width - 8f));
    //     Widgets.Label(labelRect, label);
    //     GUI.color = Color.white;
    //
    //     var colSelRect = rect.BottomPartPixels(maxHeight - labelRect.height - 4f);
    //     var colSelRectScroll = new Rect(colSelRect);
    //     var colSelRectScrollView = new Rect(colSelRect);
    //     Widgets.AdjustRectsForScrollView(colSelRect, ref colSelRectScroll, ref colSelRectScrollView);
    //     Widgets.BeginScrollView(colSelRectScroll, ref _scrollPositionColors, colSelRectScrollView);
    //
    //     // Widgets.ColorSelector(colSelRectScrollView, ref color, AllColors, out var colorsHeight, icon, colorSize,
    //     //     colorPadding);
    //
    //     Widgets.EndScrollView();
    // }
    //
    // private static void ColorSelector(string label, ref Listing_Standard listingStandard, ref Color color,
    //     float maxHeight = 120f, Texture? icon = null,
    //     int colorSize = 22, int colorPadding = 2, float paddingLeft = 32f, float paddingRight = 32f)
    // {
    //     var rect = listingStandard.GetRect(maxHeight);
    //     ColorSelector(label, rect, ref color, maxHeight, icon, colorSize, colorPadding, paddingLeft, paddingRight);
    // }

    internal static void SectionHeader(this Listing_Standard listing, string sectionKey)
    {
        Text.Anchor = TextAnchor.UpperCenter;
        listing.Label(sectionKey.Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        listing.GapLine(8f);
    }

    internal static void CheckboxLabeled(
        // ReSharper disable once InconsistentNaming
        this Listing_Standard _this,
        string label,
        ref bool checkOn,
        bool disabled,
        string? tooltip = null,
        float height = 0.0f,
        float labelPct = 1f)
    {
        Rect rect = _this.GetRect(height != 0.0 ? height : Text.CalcHeight(label, _this.ColumnWidth * labelPct),
            labelPct);
        rect.width = Math.Min(rect.width + 24f, _this.ColumnWidth);
        Rect? boundingRectCached = _this.BoundingRectCached;
        if (boundingRectCached.HasValue)
        {
            ref Rect local = ref rect;
            boundingRectCached = _this.BoundingRectCached!;
            Rect other = boundingRectCached.Value;
            if (!local.Overlaps(other))
                goto label_7;
        }

        if (!tooltip!.NullOrEmpty())
        {
            if (Mouse.IsOver(rect))
                Widgets.DrawHighlight(rect);
            TooltipHandler.TipRegion(rect, (TipSignal)tooltip);
        }

        Widgets.CheckboxLabeled(rect, label, ref checkOn, disabled: disabled);
        label_7:
        _this.Gap(_this.verticalSpacing);
    }

    internal static void LabelSettingsButton(Pawn pawn, Rect rect)
    {
        if (Widgets.ButtonImage(rect, Icons.LabelSettingsIcon))
            Find.WindowStack?.Add(new Dialog_LabelSettings(pawn));
        TooltipHandler.TipRegionByKey(rect, "JobInBar_NamePawn_GearButton");
    }

    internal static void DrawSeparatorLine(float x, ref float curY, float width)
    {
        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(x, curY, width);
        GUI.color = Color.white;
        curY += 3f;
    }
}
