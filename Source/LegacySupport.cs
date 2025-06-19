// ReSharper disable RedundantUsingDirective

using RimWorld;
using UnityEngine;

namespace JobInBar;

/// <summary>
/// Class with helper functions that assist with multi-version support.<br />
/// Primarily, backporting functions from newer versions
/// </summary>
public static class LegacySupport
{
#if v1_1 || v1_2 || v1_3
    /// Extension method for a function that was added in RimWorld 1.4+
    public static float SliderLabeled(
        this Listing_Standard this_,
        string label,
        float val,
        float min,
        float max,
        float labelPct = 0.5f,
        string? tooltip = null)
    {
        var rect = this_.GetRect(30f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rect.LeftPart(labelPct), label);
        if (tooltip != null)
            TooltipHandler.TipRegion(rect.LeftPart(labelPct), (TipSignal)tooltip);
        Text.Anchor = TextAnchor.UpperLeft;
        var num = Widgets.HorizontalSlider(rect.RightPart(1f - labelPct), val, min, max, true);
        this_.Gap(this_.verticalSpacing);
        return num;
    }
#endif

#if v1_1 || v1_2 || v1_3
    /// New version of GetRect that also takes a widthPct parameter. Added in RW 1.4+
    public static Rect GetRect(this Listing_Standard this_, float height, float widthPct)
    {
        var rect = this_.GetRect(height);
        rect.width = this_.ColumnWidth * widthPct;
        return rect;
    }
#endif

#if v1_1 || v1_2 || v1_3 || v1_4 || v1_5
    // These two functions were added in 1.6. I copied them directly from there for legacy version support since they're so useful

    /// Backported from RimWorld 1.6 <see cref="GenUI" />
    public static Rect MiddlePart(this Rect rect, float pctWidth, float pctHeight)
    {
        return new Rect(
            (float)((double)rect.x + (double)rect.width / 2.0 - (double)rect.width * (double)pctWidth / 2.0),
            (float)((double)rect.y + (double)rect.height / 2.0 - (double)rect.height * (double)pctHeight / 2.0),
            rect.width * pctWidth, rect.height * pctHeight);
    }

    /// Backported from RimWorld 1.6 <see cref="GenUI" />
    public static Rect MiddlePartPixels(this Rect rect, float width, float height)
    {
        return new Rect((float)((double)rect.x + (double)rect.width / 2.0 - (double)width / 2.0),
            (float)((double)rect.y + (double)rect.height / 2.0 - (double)height / 2.0), width, height);
    }
#endif

#if v1_1 || v1_2 || v1_3 || v1_4 || v1_5
    public static TargetingParameters ForThing()
    {
        return new TargetingParameters()
        {
            canTargetPawns = true,
            canTargetBuildings = true,
            canTargetItems = true,
#if !(v1_1 || v1_2)
            canTargetPlants = true,
#endif
            canTargetFires = true,
            mapObjectTargetsMustBeAutoAttackable = false
        };
    }
#endif

#if v1_1 || v1_2 || v1_3 || v1_4
    public static void AdjustRectsForScrollView(Rect parentRect, ref Rect outRect, ref Rect viewRect)
    {
        if ((double)viewRect.height < (double)outRect.height)
            return;
        viewRect.width -= 20f;
        outRect.xMax -= 4f;
        outRect.yMin = Mathf.Max(parentRect.yMin + 6f, outRect.yMin);
        outRect.yMax = Mathf.Min(parentRect.yMax - 6f, outRect.yMax);
    }
#endif

#if v1_1 || v1_2 || v1_3
    public static Color ClampToValueRange(this Color color, FloatRange range)
    {
        float H;
        float S;
        float V;
        Color.RGBToHSV(color, out H, out S, out V);
        float range1 = range.ClampToRange(V);
        color = Color.HSVToRGB(H, S, range1);
        return color;
    }
#endif
    
#if v1_1 || v1_2 || v1_3
    /// <summary>
    /// Stub version of CheckboxLabeled that just ignores the 2 float params
    /// </summary>
    public static void CheckboxLabeled(this Listing_Standard listingStandard, string label, ref bool val,
        string? tooltip = null, float height = 0f, float labelPct = 1f)
    {
        listingStandard.CheckboxLabeled(label, ref val, tooltip);
    }
#endif
    
#if v1_1 || v1_2 || v1_3
    public static Rect SubLabel(this Listing_Standard listingStandard, string label, float widthPct)
    {
        Rect rect = listingStandard.GetRect(Text.CalcHeight(label, listingStandard.ColumnWidth * widthPct), widthPct);
        float num = 20f;
        rect.x += num;
        rect.width -= num;
        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        Widgets.Label(rect, label);
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        listingStandard.Gap(listingStandard.verticalSpacing);
        return rect;
    }
#endif
}