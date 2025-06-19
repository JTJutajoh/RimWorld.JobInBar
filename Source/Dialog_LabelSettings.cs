using System;
using System.Diagnostics.CodeAnalysis;
using JobInBar.Utils;
using UnityEngine;

namespace JobInBar;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class Dialog_LabelSettings : Window
{
    internal static Dialog_LabelSettings? Instance;
    private readonly Color _defaultColor;
    private readonly bool _exampleBackgrounds;
    private readonly string _exampleText;
    private readonly Action? _onCancel;

    private readonly Action<Color>? _onColorApply;
    private readonly Action<Color>? _onColorChanged;

    private Color _color;

    /// <summary>
    ///     Custom color picker dialog with label examples
    /// </summary>
    /// <param name="color">Current or "starting" color when the dialog is opened.</param>
    /// <param name="defaultColor">The default color used when the Default button is clicked.</param>
    /// <param name="exampleText">Text used in the example labels of the color picker</param>
    /// <param name="exampleBackgrounds">Whether to draw the background texture behind the example labels</param>
    /// <param name="onColorApply">
    ///     When the user clicks the "Confirm" button to apply their color selection, this delegate is
    ///     invoked.
    /// </param>
    /// <param name="onColorChanged">
    ///     Invoked every frame if the currently selected color is different from the previous
    ///     frame's.
    /// </param>
    /// <param name="onCancel">When the user clicks the "Cancel" button, this delegate is invoked.</param>
    public Dialog_LabelSettings(
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

    public override Vector2 InitialSize => new(340f, 300f);

    public override void DoWindowContents(Rect inRect)
    {
        var innerRect = inRect.TopPartPixels(inRect.height - 32f - 12f);
        innerRect = innerRect.MiddlePartPixels(292f,
            200f);
        var prevColor = _color;
        try
        {
            CustomWidgets.LabelColorPicker(innerRect,
                ref _color,
                _exampleBackgrounds,
                _exampleText,
                false,
                true,
                defaultColor: _defaultColor
            );
        }
        catch (Exception e)
        {
            Log.Exception(e, "Drawing color picker dialog.");
            Close();
            return;
        }

        // Every frame, send a signal if the color is different from the last frame, so it can be used elsewhere
        if (!_color.IndistinguishableFromFast(prevColor)) _onColorChanged?.Invoke(_color);

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
}