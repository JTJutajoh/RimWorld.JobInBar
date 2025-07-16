using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using RimWorld.Planet;

namespace JobInBar;

/// <summary>
///     Singleton class that keeps track of all pawns that the player has set a label for<br />
///     Tracked pawns are accessed via indexing. If the pawn is not tracked, indexing it will start tracking it.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[UsedImplicitly]
public class LabelsTracker_WorldComponent : WorldComponent
{
    //TODO: (Eventually) change this to be static instead of a WorldComponent
    internal static LabelsTracker_WorldComponent? Instance;

    // Used for reassembling the dictionary of pawns and their label data.
    // This is necessary because the data is loaded from the save in multiple passes and the dictionary can only
    // be reassembled after all of the data has been loaded, so the lists must persist outside of each run of ExposeData
    [Obsolete("Replaced by pawn ExposeData patch. Only exists for legacy saves")] private static List<Pawn>? _pawnList;
    [Obsolete("Replaced by pawn ExposeData patch. Only exists for legacy saves")] private static List<LabelData>? _labelDataList;
    private Dictionary<Pawn, LabelData> _trackedPawns;

    public Dictionary<Pawn, LabelData> TrackedPawns => _trackedPawns;

    public LabelsTracker_WorldComponent(World world) : base(world)
    {
        Instance = this;
        _trackedPawns = new Dictionary<Pawn, LabelData>();
        _pawnList = new List<Pawn>();
        _labelDataList = new List<LabelData>();
    }

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
        internal set
        {
            _trackedPawns ??= new Dictionary<Pawn, LabelData>();
            _trackedPawns[pawn] = value;
        }
    }

    /// <summary>
    ///     Alternate way of accessing <see cref="LabelData" /> for a given pawn that does NOT create a new blank cache entry
    ///     if the pawn is not already tracked.
    /// </summary>
    internal bool TryGetLabelData(Pawn pawn, out LabelData? data)
    {
        return _trackedPawns.TryGetValue(pawn, out data);
    }

    public bool Remove(Pawn pawn)
    {
        return _trackedPawns.Remove(pawn);
    }

    [Obsolete("Replaced by pawn ExposeData patch. Only exists for legacy saves")]
    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            Log.Trace($"WorldComponent ExposeData in saving mode... skipping");
            return;
        }

        if (_trackedPawns.Count > 0)
        {
            // Pawns have already been loaded by the new method
            // Skip loading anything
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                Log.Trace($"WorldComponent already had pawns loaded during mode {Scribe.mode}, skipping");
            return;
        }

        // For legacy saves only. Remove this later on when most users will be updated already.
        Scribe_Collections.Look(
            ref _trackedPawns,
            "TrackedPawns",
            LookMode.Reference,
            LookMode.Deep,
            ref _pawnList,
            ref _labelDataList
        );

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (_trackedPawns != null)
                Log.Message($"Loaded legacy LabelsTracker_WorldComponent data. Next save will use the new method.");
            _trackedPawns ??= new Dictionary<Pawn, LabelData>();
        }
    }
}
