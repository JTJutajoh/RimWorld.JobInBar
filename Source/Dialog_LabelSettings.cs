using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RimWorld;
using UnityEngine;

namespace JobInBar;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class Dialog_LabelSettings : Window
{
    private const float RowHeight = 48f;
    private const float ButtonHeight = 48f;
    private const float ButtonWidth = 128f;
    private const float BottomButtonsMargin = 12f;
    private const float ColorPickerSectionWidth = 340f;
    private const float ColorPickerSectionHeight = 254f;

    private readonly LabelsTracker_WorldComponent _labelsComp;

    /// <summary>
    ///     Save the old data in case the user cancels
    /// </summary>
    private readonly LabelData _oldLabelData;

    private readonly string? _oldTitle;

    private static bool _draggingColorPicker;
    private string? _hexStringBuffer;

    private readonly Pawn _pawn;
    private readonly Color DefaultBackstoryTitleColor = Settings.DefaultJobLabelColor;
    private readonly bool DrawBackstoryTitleBackground = Settings.DrawJobTitleBackground;

    private LabelType _curLabelType = LabelType.JobTitle;

    private LabelData _newLabelData;

    private bool _saveOnClose;

    /// <summary>
    ///     Custom dialog for modifying and setting all the values stored in the <see cref="LabelData" /> for a given pawn.
    /// </summary>
    /// <param name="pawn">The pawn whose label data will be modified by this dialog</param>
    public Dialog_LabelSettings(Pawn pawn)
    {
        // Vanilla Window settings
        closeOnClickedOutside = true;
        doCloseX = false;
        forcePause = true;
        absorbInputAroundWindow = true;

        // Check for and cache the world component
        if (LabelsTracker_WorldComponent.Instance != null)
            _labelsComp = LabelsTracker_WorldComponent.Instance;
        else
            throw new InvalidOperationException("Labels tracker is null.");

        _pawn = pawn;
        // Save the original data for if the user cancels
        _oldLabelData = _labelsComp[_pawn];
        // Create a copy of the original data and assign it to the pawn
        _newLabelData = new LabelData(_oldLabelData);
        CurLabelData = _newLabelData;
        _oldTitle = _pawn.story?.title;
    }

    /// <summary>
    ///     Reference to the current pawn's current label data cached in the world component.<br />
    ///     Setting this to a new value copies the supplied value, stores it in <see cref="_newLabelData" />, and applies it
    ///     to the cache.<br />
    /// </summary>
    private LabelData CurLabelData
    {
        get => _labelsComp[_pawn];
        set
        {
            _newLabelData = new LabelData(value);
            _labelsComp[_pawn] = _newLabelData;
        }
    }

    public override Vector2 InitialSize => new(700f, 400f);

    public override void DoWindowContents(Rect inRect)
    {
        var innerRect = inRect.TopPartPixels(inRect.height - ButtonHeight - BottomButtonsMargin);

        var optionsRect = innerRect.LeftPartPixels(innerRect.width - ColorPickerSectionWidth);
        optionsRect.xMax -= 8f;
        DoOptions(optionsRect);

        var colorPickerRect = innerRect.RightPartPixels(ColorPickerSectionWidth)
            .MiddlePartPixels(ColorPickerSectionWidth, ColorPickerSectionHeight);
        DoColorPickerSection(colorPickerRect);

        var buttonRowRect = inRect.BottomPartPixels(ButtonHeight);
        DoBottomButtonRow(buttonRowRect);
    }

    private void DoOptions(Rect inRect)
    {
        var cache = PawnCache.GetOrCache(_pawn);
        // Just force a recache every frame while the dialog is open.
        // The performance impact for doing this with 1 pawn at a time will be basically zero
        cache.Dirty = true;

        Widgets.DrawMenuSection(inRect);
        var innerRect = inRect.ContractedBy(4f);
        var curY = innerRect.yMin + 4f;

        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(innerRect.xMin, ref curY, innerRect.width,
            "JobInBar_LabelOptionsHeader".Translate(_pawn.NameShortColored));
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(innerRect.xMin, curY, innerRect.width);
        curY += 3f;
        GUI.color = Color.white;

        if (Settings.AllowNameColors)
        {
            // Name
            DoLabelOptionsPaletteRow(
                innerRect,
                ref curY,
                "JobInBar_NameColor".Translate(),
                false,
                CurLabelData.NameColor ?? GenMapUI.DefaultThingLabelColor,
                LabelType.Name
            );
            CustomWidgets.DrawSeparatorLine(innerRect.xMin, ref curY, innerRect.width);
        }

        var enabledLabels = new List<LabelType>();
        foreach (var label in _labelsComp[_pawn].LabelOrder)
        {
            switch (label)
            {
                case LabelType.JobTitle when
                    !Settings.DrawJobTitle:
                case LabelType.RoyalTitle when
                    !(ModsConfig.RoyaltyActive && Settings.DrawRoyalTitles && cache.RoyalTitle != null):
                case LabelType.IdeoRole when
                    !(ModsConfig.IdeologyActive && Settings.DrawIdeoRoles && cache.IdeoRole != null):
                case LabelType.Name:
                    break;
                default:
                    enabledLabels.Add(label);
                    break;
            }
        }

        var labelOrderCount = enabledLabels.Count;
        for (var i = 0; i < labelOrderCount; i++)
        {
            var label = enabledLabels[i];
            switch (label)
            {
                case LabelType.JobTitle:
                    if (Settings.DrawJobTitle)
                    {
                        DoTitleRow(innerRect, ref curY, CurLabelData.BackstoryColor ?? DefaultBackstoryTitleColor,
                            cache, i, labelOrderCount);
                    }

                    break;
                case LabelType.RoyalTitle:
                    if (ModsConfig.RoyaltyActive && Settings.DrawRoyalTitles && cache.RoyalTitle != null)
                    {
                        // Royal title
                        DoLabelOptionsCheckboxRow(
                            innerRect,
                            ref curY,
                            RowHeight,
                            "JobInBar_RoyaltyOptionsRow".Translate(),
                            ref CurLabelData.ShowRoyalTitle,
                            cache.RoyalTitle == null,
                            CurLabelData.RoyalTitleColor ?? Settings.RoyalTitleColorDefault,
                            LabelType.RoyalTitle,
                            i,
                            labelOrderCount
                        );
                    }

                    break;
                case LabelType.IdeoRole:
                    if (ModsConfig.IdeologyActive && Settings.DrawIdeoRoles && cache.IdeoRole != null)
                    {
                        // Ideo role
                        DoLabelOptionsCheckboxRow(
                            innerRect,
                            ref curY,
                            RowHeight,
                            "JobInBar_IdeoRoleOptionsRow".Translate(),
                            ref CurLabelData.ShowIdeoRole,
                            cache.IdeoRole == null,
                            CurLabelData.IdeoRoleColor ?? GenMapUI.DefaultThingLabelColor,
                            LabelType.IdeoRole,
                            i,
                            labelOrderCount
                        );
                    }

                    break;
            }

            if (i < labelOrderCount - 1)
                CustomWidgets.DrawSeparatorLine(innerRect.xMin, ref curY, innerRect.width);
        }
    }

    private void DoLabelOptionsPaletteRow(Rect inRect, ref float curY, string label, bool disabled, Color labelColor,
        LabelType labelType)
    {
        var rowRect = new Rect(inRect.xMin, curY, inRect.width, RowHeight);

        if (!disabled && Widgets.ButtonInvisible(rowRect, false))
        {
            _curLabelType = labelType;
            _hexStringBuffer = null;
        }

        if (_curLabelType == labelType)
            Widgets.DrawHighlight(rowRect);

        GUI.color = disabled ? Widgets.InactiveColor : labelColor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rowRect with { xMin = rowRect.xMin + 32f }, label);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        curY += 2f + RowHeight;
    }

    private void DoTitleRow(Rect inRect, ref float curY, Color labelColor, PawnCache cache, int i, int labelOrderCount)
    {
        const float height = RowHeight + 8f;
        if (_curLabelType == LabelType.JobTitle)
            Widgets.DrawHighlight(inRect with { yMin = curY, height = height });

        if (Widgets.ButtonInvisible(
                inRect with
                {
                    xMin = inRect.xMin + 4f + (height / 2f) + 4f,
                    xMax = inRect.xMax - (inRect.width * 0.4f) - 4f,
                    yMin = curY,
                    height = height
                }, false))
        {
            _curLabelType = LabelType.JobTitle;
            _hexStringBuffer = null;
        }

        var reorderButtonsRect = (inRect with { xMin = inRect.xMin + 4f, yMin = curY, height = height });
        reorderButtonsRect = reorderButtonsRect.LeftPartPixels(RowHeight / 2f)
            .MiddlePartPixels(RowHeight / 2f, RowHeight);
        DoLabelOptionsCheckboxRow(
            inRect,
            ref curY,
            RowHeight / 2f,
            "JobInBar_TitleOptionsRow".Translate(),
            ref CurLabelData.ShowBackstory,
            !Settings.DrawJobTitle || cache.Title == null,
            CurLabelData.BackstoryColor ?? DefaultBackstoryTitleColor,
            LabelType.JobTitle,
            i,
            labelOrderCount,
            false,
            reorderButtonsRect
        );
        const float bottomHeight = RowHeight / 2f;
        var rowRect = new Rect(inRect.xMin, curY, inRect.width, bottomHeight);

        GUI.color = labelColor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rowRect with { xMin = rowRect.xMin + 32f, xMax = rowRect.xMax - 48f - 8f - 4f },
            "JobInBar_CustomJobTitle".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        GUI.color = Color.white;
        var textFieldRect = rowRect.RightPart(0.4f);
        textFieldRect = textFieldRect.MiddlePartPixels(textFieldRect.width, 24f);
        CharacterCardUtility.DoNameInputRect(textFieldRect, ref _pawn.story!.title, 12);
        if (_pawn.story!.title?.Length == 0)
            _pawn.story!.title = null!;

        curY += 2f + bottomHeight + 8f;
    }

    private void DoLabelOptionsCheckboxRow(
        Rect inRect,
        ref float curY,
        float height,
        string label,
        ref bool checkOn,
        bool disabled,
        Color labelColor,
        LabelType labelType,
        int index,
        int count,
        bool highlight = true,
        Rect? reorderButtonsRect = null
    )
    {
        var rowRect = new Rect(inRect.xMin, curY, inRect.width, height);

        if (!disabled && Widgets.ButtonInvisible(
                rowRect with { xMin = rowRect.xMin + 4f + (height / 2f) + 4f, xMax = rowRect.xMax - 24f - 4f },
                false))
        {
            _curLabelType = labelType;
            _hexStringBuffer = null;
        }

        reorderButtonsRect ??= (rowRect with { xMin = rowRect.xMin + 4f }).LeftPartPixels(height / 2f)
            .MiddlePartPixels(height / 2f, height);
        if (!disabled && checkOn && index > 0 && Widgets.ButtonImage(reorderButtonsRect.Value.TopHalf(), TexButton.ReorderUp!, Color.white))
        {
            CurLabelData.ShiftLabelOrder(labelType, -1);
        }

        if (!disabled && checkOn && index < count - 1 &&
            Widgets.ButtonImage(reorderButtonsRect.Value.BottomHalf(), TexButton.ReorderDown!, Color.white))
        {
            CurLabelData.ShiftLabelOrder(labelType, +1);
        }

        if (highlight && _curLabelType == labelType)
            Widgets.DrawHighlight(rowRect);

        GUI.color = disabled ? Widgets.InactiveColor : labelColor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rowRect with { xMin = rowRect.xMin + 32f, xMax = rowRect.xMax - 48f - 8f - 4f }, label);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        var buttonsRect = rowRect.RightPartPixels(48f + 8f).MiddlePartPixels(48f + 8f, 24f);
        // if (!disabled)
        //     if (Widgets.ButtonImage(buttonsRect.LeftPartPixels(24f), Icons.PaletteIcon,
        //             labelColor.WithAlpha(_curLabelType == labelType ? 0.9f : 0.7f), labelColor.WithAlpha(1)))
        //     {
        //         _curLabelType = labelType;
        //         _hexStringBuffer = null;
        //     }

        Widgets.Checkbox(buttonsRect.xMax - 24f, buttonsRect.yMin, ref checkOn, 24f, disabled, true);

        curY += 2f + height;
    }

    private void DoColorPickerSection(Rect inRect)
    {
        Color prevColor;
        string colorPickerHeader;
        string exampleText;
        bool drawBackground;
        Color defaultColor;
        switch (_curLabelType)
        {
            case LabelType.JobTitle:
            default:
                colorPickerHeader = "JobInBar_BackstoryJobTitle".Translate();
                exampleText = _pawn.story?.TitleCap ?? "JobInBar_JobTitle".Translate();
                prevColor = CurLabelData.BackstoryColor ?? DefaultBackstoryTitleColor;
                defaultColor = DefaultBackstoryTitleColor;
                drawBackground = DrawBackstoryTitleBackground;
                break;
            case LabelType.Name:
                colorPickerHeader = "JobInBar_PawnName".Translate();
                exampleText = _pawn.Name!.ToStringFull!;
                prevColor = CurLabelData.NameColor ?? GenMapUI.DefaultThingLabelColor;
                defaultColor = GenMapUI.DefaultThingLabelColor;
                drawBackground = true;
                break;
            case LabelType.RoyalTitle:
                colorPickerHeader = "JobInBar_RoyaltyTitle".Translate();
                exampleText = _pawn.royalty?.MainTitle()?.GetLabelCapFor(_pawn) ?? "JobInBar_RoyalTitle".Translate();
                prevColor = CurLabelData.RoyalTitleColor ?? Settings.RoyalTitleColorDefault;
                defaultColor = Settings.RoyalTitleColorDefault;
                drawBackground = Settings.DrawRoyalTitleBackground;
                break;
            case LabelType.IdeoRole:
                colorPickerHeader = "JobInBar_IdeoRole".Translate();
                exampleText = _pawn.ideo?.Ideo?.GetRole(_pawn)?.LabelForPawn(_pawn) ?? "JobInBar_IdeoRole".Translate();
                prevColor = CurLabelData.IdeoRoleColor ?? GenMapUI.DefaultThingLabelColor;
                defaultColor = GenMapUI.DefaultThingLabelColor;
                drawBackground = Settings.DrawIdeoRoleBackground;
                break;
        }

        var newColor = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a);

        try
        {
            CustomWidgets.LabelColorPicker(
                inRect,
                ref newColor,
                drawBackground,
                exampleText,
                ref _draggingColorPicker,
                ref _hexStringBuffer,
                true,
                true,
                defaultColor: defaultColor,
                header: colorPickerHeader + " " + "JobInBar_Color".Translate()
            );
        }
        catch (Exception e)
        {
            Log.Exception(e, "Drawing color picker widget.", true);
        }

        // Every frame, if the color is non-default, apply it
        // this way, the actual label in the vanilla colonist bar will update
        if (newColor.IndistinguishableFromFast(prevColor)) return;

        switch (_curLabelType)
        {
            case LabelType.JobTitle:
            default:
                CurLabelData.BackstoryColor = newColor;
                break;
            case LabelType.Name:
                CurLabelData.NameColor = newColor;
                break;
            case LabelType.RoyalTitle:
                CurLabelData.RoyalTitleColor = newColor;
                break;
            case LabelType.IdeoRole:
                CurLabelData.IdeoRoleColor = newColor;
                break;
        }
    }

    private void DoBottomButtonRow(Rect inRect)
    {
        var acceptRect = inRect.RightHalf().MiddlePartPixels(ButtonWidth, ButtonHeight);
        if (Widgets.ButtonText(acceptRect, "Accept".Translate().CapitalizeFirst()))
        {
            _saveOnClose = true;
            Close();
        }

        var cancelRect = inRect.LeftHalf().MiddlePartPixels(ButtonWidth, ButtonHeight);
        if (Widgets.ButtonText(cancelRect, "Cancel".Translate().CapitalizeFirst()))
        {
            _saveOnClose = false;
            Close();
        }

#if !(v1_1 || v1_2 || v1_3)
        var gearRect = inRect.BottomPartPixels(32f).RightPartPixels(32f);
        if (Widgets.ButtonImage(gearRect, Icons.GearIcon))
        {
            Find.WindowStack!.Add(new Dialog_ModSettings(JobInBarMod.Instance!));
        }

        if (Mouse.IsOver(gearRect))
        {
            TooltipHandler.TipRegion(gearRect, "JobInBar_Settings_Button".Translate());
        }
#endif
    }

    public override void Close(bool doClouseSound = true)
    {
        if (_saveOnClose)
            OnApply();
        else
            OnCancel();

        base.Close(doClouseSound);
    }

    private void OnCancel()
    {
        // CurLabelData = _oldLabelData;
        _labelsComp[_pawn] = _oldLabelData;
        _pawn.story!.title = _oldTitle!;
    }

    private void OnApply()
    {
        CurLabelData = _newLabelData;
    }
}
