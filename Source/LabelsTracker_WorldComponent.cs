using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using RimWorld.Planet;
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

/// <summary>
///     Singleton class that keeps track of all pawns that the player has set a label for<br />
///     Tracked pawns are accessed via indexing. If the pawn is not tracked, indexing it will start tracking it.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[UsedImplicitly]
public class LabelsTracker_WorldComponent : WorldComponent
{
    internal static LabelsTracker_WorldComponent? Instance;

    // Used for reassembling the dictionary of pawns and their label data.
    // This is necessary because the data is loaded from the save in multiple passes and the dictionary can only
    // be reassembled after all of the data has been loaded, so the lists must persist outside of each run of ExposeData
    private static List<Pawn>? _pawnList;
    private static List<LabelData>? _labelDataList;
    private Dictionary<Pawn, LabelData> _trackedPawns = new();

    public LabelsTracker_WorldComponent(World world) : base(world)
    {
        Instance = this;
    }

    public Dictionary<Pawn, LabelData> TrackedPawns => _trackedPawns;

    public LabelData this[Pawn pawn]
    {
        get
        {
            if (_trackedPawns.TryGetValue(pawn, out var data)) return data!;

            Log.Trace($"Pawn {pawn.Name} not tracked. Creating new label data for it.");
            data = new LabelData(pawn);
            this[pawn] = data;

            return data;
        }
        internal set => _trackedPawns[pawn] = value;
    }

    /// <summary>
    ///     Alternate way of accessing <see cref="LabelData" /> for a given pawn that does NOT create a new blank cache entry
    ///     if the pawn is not already tracked.
    /// </summary>
    internal LabelData? GetExistingLabelData(Pawn pawn)
    {
        return _trackedPawns.TryGetValue(pawn, out var data) ? data : null;
    }

    public bool Remove(Pawn pawn)
    {
        return _trackedPawns.Remove(pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Collections.Look(
            ref _trackedPawns,
            "TrackedPawns",
            LookMode.Reference,
            LookMode.Deep,
            ref _pawnList,
            ref _labelDataList
        );

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            // Safety checks to sanitize null refs or mismatched data
            foreach (var pawn in TrackedPawns.Keys)
            {
                if (pawn == null)
                {
                    Log.Warning("Null pawn key in label tracker while loading.");
                    Remove(null!); // Remove null key if it somehow exists
                    continue;
                }

                if (this[pawn] == null)
                {
                    Log.Warning($"Null label data for pawn {pawn.Name}. Creating new label data.");
                    this[pawn] = new LabelData(pawn);
                    continue;
                }

                if (this[pawn].Pawn != pawn)
                {
                    Log.Warning($"Pawn {pawn.Name} has incorrect pawn reference in label data. Resetting.");
                    this[pawn].Pawn = pawn;
                }
            }

            _pawnList = null;
            _labelDataList = null;
        }
    }
}
