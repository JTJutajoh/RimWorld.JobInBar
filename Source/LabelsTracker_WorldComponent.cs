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
    public Color? BackstoryColor;
    public Pawn? Pawn;
    public bool ShowBackstory = true;

    public bool ShowIdeoRole = true;

    public bool ShowRoyalTitle = true;

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

    public void ExposeData()
    {
        Scribe_References.Look(ref Pawn, "pawn");

        Scribe_Values.Look(ref ShowBackstory, "ShowBackstory", true);
        Scribe_Values.Look(ref BackstoryColor, "BackstoryColor", Settings.DefaultJobLabelColor);
        Scribe_Values.Look(ref ShowRoyalTitle, "ShowRoyalTitle", true);
        Scribe_Values.Look(ref ShowIdeoRole, "ShowIdeoRole", true);
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
                    //TODO: Add a patch to more gracefully stop tracking a pawn if they're destroyed
                    Log.Warning("Null pawn key in label tracker while loading.");
                    TrackedPawns.Remove(null!); // Remove null key if it somehow exists
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
