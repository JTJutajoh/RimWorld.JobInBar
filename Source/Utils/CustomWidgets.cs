using System;
using UnityEngine;

namespace JobInBar.Utils;

/// <summary>
///     Helper static class that contains methods to create common widgets I use in my mods.<br />
///     Many of these are implemented as extension methods, some of which are overloads for vanilla methods (such as ones
///     for <see cref="Listing_Standard" />).
/// </summary>
internal static class CustomWidgets
{
#if !(v1_1 || v1_2 || v1_3) // HSVColorWheel was added in RW 1.4+
    private static bool _currentlyDraggingHSVWheel;
#endif
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
    /// <param name="windowBackground">If true, adds a background around the whole element</param>
    /// <param name="defaultButton">Include a "Default" button to reset to the harvested default value</param>
    /// <param name="onDefault">Invoked whenever the Default button is clicked</param>
    /// <param name="defaultColor">Optional color to be used when the Default button is clicked</param>
    internal static void LabelColorPicker(
        Rect rect,
        ref Color color,
        bool labelBackgrounds,
        string exampleText,
        bool windowBackground = true,
        bool defaultButton = false,
        Action? onDefault = null,
        Color? defaultColor = null
    )
    {
        if (windowBackground)
        {
            Widgets.DrawWindowBackground(rect);
            rect = rect.ContractedBy(4f);
        }

        var settingsRect = new Rect(rect);
        var curY = settingsRect.yMin + 4f;

        var colorLabel = exampleText;
        Widgets.DrawBoxSolid(new Rect(settingsRect.xMin, curY - 2f, settingsRect.width, 16f),
            new Color(0.2f, 0.2f, 0.2f));
        LabelDrawer.DrawCustomLabel(new Vector2(settingsRect.center.x, curY), colorLabel, color,
            drawBg: labelBackgrounds);
        curY += 12f + 4f;

        Widgets.DrawBoxSolid(new Rect(settingsRect.xMin, curY - 2, settingsRect.width, 16f),
            new Color(0.8f, 0.8f, 0.8f));
        LabelDrawer.DrawCustomLabel(new Vector2(settingsRect.center.x, curY), colorLabel, color,
            drawBg: labelBackgrounds);
        curY += 12f + 4f;

        var colorPickerHeight = rect.height - (curY - rect.yMin);
        if (defaultButton) colorPickerHeight -= 32f + 4f;

        var colorPickerRect = new Rect(rect.xMin, curY, rect.width, colorPickerHeight);
        ColorPicker(colorPickerRect, ref color, false);


        if (!defaultButton) return;

        var defaultButtonRect = new Rect(rect.xMin, colorPickerRect.yMax + 2f, rect.width, 32f);
        if (Widgets.ButtonText(defaultButtonRect, "JobInBar_Settings_Default".Translate()))
        {
            color = defaultColor ?? color;
            onDefault?.Invoke();
        }
    }

    /// <summary>
    ///     Creates an HSV color picker along with HSVA sliders.<br />
    ///     Uses the HSV color wheel widget added in RW 1.4. If using an older version, the wheel will simply be skipped.
    /// </summary>
    internal static void ColorPicker(Rect rect, ref Color color, bool windowBackground = true)
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

#if !(v1_1 || v1_2 || v1_3) // HSVColorWheel was added in RW 1.4+
        Widgets.HSVColorWheel(hsvRect, ref color, ref _currentlyDraggingHSVWheel);
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
        color = Color.HSVToRGB(hue, saturation, value);
        color.a = oldAlpha;

        color.a =
            Widgets.HorizontalSlider(new Rect(slidersRect.xMin, curY, slidersRect.width, 24f), color.a, 0f, 1f,
                true, "JobInBar_Alpha".Translate());
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
}
