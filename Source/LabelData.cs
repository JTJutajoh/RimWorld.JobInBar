using System.Collections.Generic;
using UnityEngine;

namespace JobInBar;

/// <summary>
///     Struct storing all of the user-set data for one pawn's label.
/// </summary>
public class LabelData : IExposable
{
    private Color? _nameColor;

    public Color? NameColor
    {
        get => _nameColor;
        set => SetColor(LabelType.Name, value);
    }

    private Color? _backstoryColor;

    public Color? BackstoryColor
    {
        get => _backstoryColor;
        set => SetColor(LabelType.JobTitle, value);
    }

    private Color? _ideoRoleColor;

    public Color? IdeoRoleColor
    {
        get => _ideoRoleColor;
        set => SetColor(LabelType.IdeoRole, value);
    }

    private Color? _royalTitleColor;

    public Color? RoyalTitleColor
    {
        get => _royalTitleColor;
        set => SetColor(LabelType.RoyalTitle, value);
    }

    public Pawn? Pawn;
    public bool ShowBackstory = true;

    public bool ShowIdeoRole = true;

    public bool ShowRoyalTitle = true;

    internal List<LabelType> LabelOrder = new() { LabelType.JobTitle, LabelType.RoyalTitle, LabelType.IdeoRole };


    public LabelData()
    {
        Log.Trace("Creating new empty label data");
        // Empty constructor for scribe.
        // This is only used when loading existing save files.
    }

    public LabelData(Pawn pawn)
    {
        Log.Trace($"Creating new label data for pawn {pawn.Name}");
        Pawn = pawn;
    }

    public LabelData(LabelData other)
    {
        Log.Trace("Creating new label data from existing data");
        Pawn = other.Pawn;

        NameColor = other.NameColor;

        ShowBackstory = other.ShowBackstory;
        BackstoryColor = other.BackstoryColor;

        ShowRoyalTitle = other.ShowRoyalTitle;
        RoyalTitleColor = other.RoyalTitleColor;

        ShowIdeoRole = other.ShowIdeoRole;
        IdeoRoleColor = other.IdeoRoleColor;

        LabelOrder = other.LabelOrder.ListFullCopy()!;
    }

    internal void SetColor(LabelType type, Color? color)
    {
        switch (type)
        {
            default:
            case LabelType.JobTitle:
                _backstoryColor = color?.IndistinguishableFrom(Settings.DefaultJobLabelColor) ?? true ? null : color;
                break;
            case LabelType.Name:
                _nameColor = color?.IndistinguishableFrom(GenMapUI.DefaultThingLabelColor) ?? true ? null : color;
                break;
            case LabelType.RoyalTitle:
                _royalTitleColor = color?.IndistinguishableFrom(Settings.RoyalTitleColorDefault) ?? true ? null : color;
                break;
            case LabelType.IdeoRole:
                _ideoRoleColor = color?.IndistinguishableFrom(GenMapUI.DefaultThingLabelColor) ?? true ? null : color;
                break;
        }
    }

    internal void ShiftLabelOrder(LabelType type, int shift)
    {
        var index = LabelOrder.IndexOf(type);
        if (index == -1) return;

        var newIndex = (index + shift + LabelOrder.Count) % LabelOrder.Count;
        LabelOrder.RemoveAt(index);
        LabelOrder.Insert(newIndex, type);
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref Pawn, "pawn");

        Scribe_Values.Look(ref ShowBackstory, "ShowBackstory", true);
        Scribe_Values.Look(ref _backstoryColor, "BackstoryColor");
        Scribe_Values.Look(ref _nameColor, "NameColor");
        Scribe_Values.Look(ref _royalTitleColor, "RoyalTitleColor");
        Scribe_Values.Look(ref _ideoRoleColor, "IdeoRoleColor");
        Scribe_Values.Look(ref ShowRoyalTitle, "ShowRoyalTitle", true);
        Scribe_Values.Look(ref ShowIdeoRole, "ShowIdeoRole", true);

        Scribe_Collections.Look(ref LabelOrder, "LabelOrder", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            LabelOrder ??= new List<LabelType> { LabelType.JobTitle, LabelType.RoyalTitle, LabelType.IdeoRole };
        }
    }
}