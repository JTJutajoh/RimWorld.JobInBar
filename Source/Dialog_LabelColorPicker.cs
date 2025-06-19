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

    private Pawn _pawn;
    private Color _color;
    private string _key;
    private string? _exampleText;
    private bool _drawBackground;
    private Action<Color>? _onColorChosen;
    private Color _defaultColor;

    public Dialog_LabelColorPicker(
        Pawn pawn,
        string key,
        Color? color,
        bool drawBackground,
        Action<Color>? onColorChosen,
        Color defaultColor,
        string? exampleText = null
    )
    {
        Instance?.Close();
        Instance = this;

        closeOnClickedOutside = true;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;

        _pawn = pawn;
        _key = key;
        _color = color ?? (Color)Settings.DefaultSettings[key];
        _drawBackground = drawBackground;
        _onColorChosen = onColorChosen;
        _defaultColor = defaultColor;
        _exampleText = exampleText;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var innerRect = inRect.TopPartPixels(inRect.height - 32f - 12f);
        innerRect = innerRect.MiddlePartPixels(292f,
            200f);
        LabelColorPicker(innerRect,
            ref _color,
            _key,
            _drawBackground,
            exampleText: _exampleText,
            windowBackground: false,
            defaultButton: true,
            defaultColor: _defaultColor);

        var buttonsRect = inRect.BottomPartPixels(32f);

        if (Widgets.ButtonText(buttonsRect.LeftPartPixels(80f),
                "Cancel".Translate()))
        {
            Close();
        }

        if (Widgets.ButtonText(buttonsRect.RightPartPixels(80f),
                "Confirm".Translate()))
        {
            _onColorChosen?.Invoke(_color);
            Close();
        }
    }

    public override void Close(bool doCloseSound = true)
    {
        // If the color was reset to the default (or unchanged from default), clear the record from the labels tracker
        if (_color.IndistinguishableFrom(Settings.DefaultJobLabelColor))
        {
            LabelsTracker_WorldComponent.Instance![_pawn].BackstoryColor = null;
        }
        Find.WindowStack.TryRemove(this, doCloseSound);
    }
    

    /// <summary>
    /// Helper function to draw a full color picker with example labels.<br />
    /// Auto-detects if it should do a horizontal or vertical layout based on which dimension of the input rect is larger.
    /// </summary>
    /// <param name="rect">The rect to draw it within.</param>
    /// <param name="color">The color to be set</param>
    /// <param name="key">The name of the setting in <see cref="Settings"/> that this corresponds to.</param>
    /// <param name="labelBackgrounds">Add the background to the label examples</param>
    /// <param name="exampleText">If provided, will be used for the label example text instead of the auto-translated key</param>
    /// <param name="windowBackground">If true, adds a background around the whole element</param>
    /// <param name="defaultButton">Include a "Default" button to reset to the harvested default value</param>
    /// <param name="onDefault"></param>
    /// <param name="defaultColor"></param>
    internal static void LabelColorPicker(Rect rect, ref Color color, string key, bool labelBackgrounds,
        string? exampleText = null, bool windowBackground = true, bool defaultButton = false, Action? onDefault = null, Color? defaultColor = null)
    {
        if (windowBackground)
        {
            Widgets.DrawWindowBackground(rect);
            rect = rect.ContractedBy(4f);
        }

        var settingsRect = new Rect(rect);
        var curY = settingsRect.yMin + 4f;

        var colorLabel = exampleText ?? Settings.GetLabel(key);
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