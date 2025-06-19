using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace JobInBar;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class Dialog_LabelColorPicker : Window
{
    public override Vector2 InitialSize => new(340f, 300f);

    internal static Dialog_LabelColorPicker? Instance;

    private static bool _currentlyDraggingHSVWheel;

    private Color _color;
    private string _exampleText;
    private bool _exampleBackgrounds;
    private Color _defaultColor;
    
    private Action<Color>? _onColorApply;
    private Action<Color>? _onColorChanged;
    private Action? _onCancel;

    /// <summary>
    /// Custom color picker dialog with label examples
    /// </summary>
    /// <param name="color">Current or "starting" color when the dialog is opened.</param>
    /// <param name="defaultColor">The default color used when the Default button is clicked.</param>
    /// <param name="exampleText">Text used in the example labels of the color picker</param>
    /// <param name="exampleBackgrounds">Whether to draw the background texture behind the example labels</param>
    /// <param name="onColorApply">When the user clicks the "Confirm" button to apply their color selection, this delegate is invoked.</param>
    /// <param name="onColorChanged">Invoked every frame if the currently selected color is different from the previous frame's.</param>
    /// <param name="onCancel">When the user clicks the "Cancel" button, this delegate is invoked.</param>
    public Dialog_LabelColorPicker(
        Color? color,
        Color defaultColor,
        string exampleText,
        bool exampleBackgrounds,
        Action<Color>? onColorApply,
        Action<Color>? onColorChanged,
        Action? onCancel
    )
    {
        Instance?.Close();
        Instance = this;

        closeOnClickedOutside = true;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;

        _onColorApply = onColorApply;
        _onColorChanged = onColorChanged;
        _onCancel = onCancel;
        
        _exampleBackgrounds = exampleBackgrounds;
        _defaultColor = defaultColor;
        _exampleText = exampleText;
        _color = color ?? defaultColor;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var innerRect = inRect.TopPartPixels(inRect.height - 32f - 12f);
        innerRect = innerRect.MiddlePartPixels(292f,
            200f);
        var prevColor = _color;
        LabelColorPicker(innerRect,
            ref _color,
            labelBackgrounds: _exampleBackgrounds,
            exampleText: _exampleText,
            windowBackground: false,
            defaultButton: true,
            defaultColor: _defaultColor
        );

        if (!_color.IndistinguishableFromFast(prevColor))
        {
            _onColorChanged?.Invoke(_color);
        }

        var buttonsRect = inRect.BottomPartPixels(32f);

        if (Widgets.ButtonText(buttonsRect.LeftPartPixels(80f),
                "Cancel".Translate()))
        {
            _onCancel?.Invoke();
            Close();
        }

        if (Widgets.ButtonText(buttonsRect.RightPartPixels(80f),
                "Confirm".Translate()))
        {
            _onColorApply?.Invoke(_color);
            Close();
        }
    }

    /// <summary>
    /// Helper function to draw a full color picker with example labels.<br />
    /// Auto-detects if it should do a horizontal or vertical layout based on which dimension of the input rect is larger.
    /// </summary>
    /// <param name="rect">The rect to draw it within.</param>
    /// <param name="color">The color to be set</param>
    /// <param name="labelBackgrounds">Add the background to the label examples</param>
    /// <param name="exampleText">Used for the label example text instead of the auto-translated settingName</param>
    /// <param name="windowBackground">If true, adds a background around the whole element</param>
    /// <param name="defaultButton">Include a "Default" button to reset to the harvested default value</param>
    /// <param name="onDefault">Invoked whenever the Default button is clicked</param>
    /// <param name="defaultColor">Optional color to be used when the Default button is clicked</param>
    internal static void LabelColorPicker(Rect rect, ref Color color, bool labelBackgrounds,
        string exampleText, bool windowBackground = true, bool defaultButton = false, Action? onDefault = null, Color? defaultColor = null)
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
        if (defaultButton)
        {
            colorPickerHeight -= 32f + 4f;
        }

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
    /// Creates an HSV color picker along with HSVA sliders.
    /// </summary>
    internal static void ColorPicker(Rect rect, ref Color color, bool windowBackground = true)
    {
        if (windowBackground)
        {
            Widgets.DrawWindowBackground(rect);
            rect = rect.ContractedBy(4f);
        }

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

        Widgets.HSVColorWheel(hsvRect, ref color, ref _currentlyDraggingHSVWheel);

        Color.RGBToHSV(color, out var hue, out var saturation, out var value);

        curY += 4f;

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
    }

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