using System;
using System.Diagnostics.CodeAnalysis;
using RimWorld;
using UnityEngine;

namespace JobInBar;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class Dialog_LabelSettings : Window
{
    private const float ButtonHeight = 32f;
    private const float ButtonWidth = 80f;
    private const float BottomButtonsMargin = 12f;
    private const float ColorPickerSectionWidth = 300f;

    private readonly LabelsTracker_WorldComponent _labelsComp;

    /// <summary>
    ///     Save the old data in case the user cancels
    /// </summary>
    private readonly LabelData _oldLabelData;

    private readonly string? _oldTitle;

    private static bool _draggingColorPicker = false;

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

    public override Vector2 InitialSize => new(700f, 300f);

    public override void DoWindowContents(Rect inRect)
    {
        var innerRect = inRect.TopPartPixels(inRect.height - ButtonHeight - BottomButtonsMargin);

        var optionsRect = innerRect.LeftPartPixels(innerRect.width - ColorPickerSectionWidth);
        optionsRect.xMax -= 8f;
        DoOptions(optionsRect);

        var colorPickerRect = innerRect.RightPartPixels(ColorPickerSectionWidth)
            .MiddlePartPixels(ColorPickerSectionWidth, innerRect.height);
        DoColorPickerSection(colorPickerRect);

        var buttonRowRect = inRect.BottomPartPixels(ButtonHeight);
        DoBottomButtonRow(buttonRowRect);
    }

    private void DoOptions(Rect inRect)
    {
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

        // Name
        DoLabelOptionsPaletteRow(
            innerRect,
            ref curY,
            "JobInBar_NameColor".Translate(),
            false,
            CurLabelData.NameColor ?? GenMapUI.DefaultThingLabelColor,
            LabelType.Name,
            0.2f
        );
        // Title
        DoTitleTextfieldRow(innerRect, ref curY, CurLabelData.BackstoryColor ?? DefaultBackstoryTitleColor, 0.1f);
        DoLabelOptionsCheckboxRow(
            innerRect,
            ref curY,
            "JobInBar_ShowJobLabelFor".Translate(),
            ref CurLabelData.ShowBackstory,
            !Settings.DrawJobTitle,
            CurLabelData.BackstoryColor ?? DefaultBackstoryTitleColor,
            LabelType.JobTitle,
            0.2f
        );
        if (ModsConfig.RoyaltyActive)
            // Royal title
            DoLabelOptionsCheckboxRow(
                innerRect,
                ref curY,
                "JobInBar_ShowRoyaltyLabelFor".Translate(),
                ref CurLabelData.ShowRoyalTitle,
                !_pawn.HasRoyalTitle(),
                CurLabelData.RoyalTitleColor ?? Settings.RoyalTitleColorDefault,
                LabelType.RoyalTitle,
                0.1f
            );

        if (ModsConfig.IdeologyActive)
            // Ideo role
            DoLabelOptionsCheckboxRow(
                innerRect,
                ref curY,
                "JobInBar_ShowIdeoLabelFor".Translate(),
                ref CurLabelData.ShowIdeoRole,
                !_pawn.HasIdeoRole(),
                CurLabelData.IdeoRoleColor ?? GenMapUI.DefaultThingLabelColor,
                LabelType.IdeoRole,
                0.2f
            );
    }

    private void DoLabelOptionsPaletteRow(Rect inRect, ref float curY, string label, bool disabled, Color labelColor,
        LabelType labelType, float bgAlpha = 0f)
    {
        var lineHeight = Text.LineHeightOf(GameFont.Small);
        var rowRect = new Rect(inRect.xMin, curY, inRect.width, lineHeight);
        var color = ColorLibrary.Grey.WithAlpha(bgAlpha);
        Widgets.DrawBoxSolid(rowRect, color);
        if (_curLabelType == labelType)
            Widgets.DrawHighlight(rowRect);
        GUI.color = disabled ? Widgets.InactiveColor : labelColor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rowRect with { xMax = rowRect.xMax - lineHeight }, label);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        if (!disabled)
            if (Widgets.ButtonImage(new Rect(rowRect.xMax - lineHeight * 2f, curY, lineHeight, lineHeight),
                    Icons.PaletteIcon, labelColor.WithAlpha(_curLabelType == labelType ? 0.9f : 0.7f),
                    labelColor.WithAlpha(1)))
                _curLabelType = labelType;

        curY += 2f + lineHeight;
    }

    private void DoTitleTextfieldRow(Rect inRect, ref float curY, Color labelColor, float bgAlpha)
    {
        var lineHeight = Text.LineHeightOf(GameFont.Small);
        var rowRect = new Rect(inRect.xMin, curY, inRect.width, lineHeight);
        Widgets.DrawBoxSolid(rowRect, ColorLibrary.Grey.WithAlpha(bgAlpha));

        GUI.color = labelColor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rowRect.LeftPart(0.5f), "JobInBar_CustomJobTitle".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        CharacterCardUtility.DoNameInputRect(rowRect.RightPart(0.4f), ref _pawn.story!.title, 12);
        if (_pawn.story!.title?.Length == 0)
            _pawn.story!.title = null!;

        curY += 2f + lineHeight;
    }

    private void DoLabelOptionsCheckboxRow(Rect inRect, ref float curY, string label, ref bool checkOn, bool disabled,
        Color labelColor, LabelType labelType, float bgAlpha = 0f)
    {
        var lineHeight = Text.LineHeightOf(GameFont.Small);
        var rowRect = new Rect(inRect.xMin, curY, inRect.width, lineHeight);
        var color = ColorLibrary.Grey.WithAlpha(bgAlpha);
        if (_curLabelType == labelType)
            Widgets.DrawHighlight(rowRect);
        Widgets.DrawBoxSolid(rowRect, color);
        GUI.color = disabled ? Widgets.InactiveColor : labelColor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rowRect with { xMax = rowRect.xMax - lineHeight }, label);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;

        if (!disabled)
            if (Widgets.ButtonImage(new Rect(rowRect.xMax - lineHeight * 2f, curY, lineHeight, lineHeight),
                    Icons.PaletteIcon, labelColor.WithAlpha(_curLabelType == labelType ? 0.9f : 0.7f),
                    labelColor.WithAlpha(1)))
                _curLabelType = labelType;

        Widgets.Checkbox(rowRect.xMax - lineHeight, curY, ref checkOn, lineHeight, disabled, true);

        curY += 2f + lineHeight;
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

        var gearRect = inRect.BottomPartPixels(32f).RightPartPixels(32f);
        if (Widgets.ButtonImage(gearRect, Icons.GearIcon))
        {
            Find.WindowStack!.Add(new Dialog_ModSettings(JobInBarMod.Instance!));
        }

        if (Mouse.IsOver(gearRect))
        {
            TooltipHandler.TipRegion(gearRect, "JobInBar_Settings_Button".Translate());
        }
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
        CurLabelData = _oldLabelData;
        _pawn.story!.title = _oldTitle!;
    }

    private void OnApply()
    {
        CurLabelData = _newLabelData;
    }
}
