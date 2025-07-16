namespace JobInBar;

internal static class PawnLabelExtensions
{
    internal static bool TryGetLabelData(this Pawn pawn, out LabelData? labelData)
    {
        labelData = null;
        return LabelsTracker_WorldComponent.Instance?.TryGetLabelData(pawn, out labelData) ?? false;
    }

    internal static bool HasLabelData(this Pawn pawn)
    {
        return LabelsTracker_WorldComponent.Instance?.TrackedPawns.ContainsKey(pawn) ?? false;
    }

    internal static bool TryGetCache(this Pawn pawn, out PawnCache? cache)
    {
        return PawnCache.TryGet(pawn, out cache);
    }

    internal static PawnCache GetOrCache(this Pawn pawn)
    {
        return PawnCache.GetOrCache(pawn);
    }

    internal static void SetLabelData(this Pawn pawn, LabelData labelData)
    {
        var labelsComp = LabelsTracker_WorldComponent.Instance;
        if (labelsComp is null)
        {
            Log.Error($"Couldn't set label data for {pawn.LabelCap} because the labels tracker is not initialized");
            return;
        }
        labelsComp[pawn] = labelData;
    }
}
