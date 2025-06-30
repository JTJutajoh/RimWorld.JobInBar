using System;
using RimWorld;
using UnityEngine;

namespace JobInBar;

public static class PawnLabelExtensions
{
    public static Color JobLabelColor(this Pawn pawn)
    {
        return LabelsTracker_WorldComponent.Instance?[pawn].BackstoryColor ?? Settings.DefaultJobLabelColor;
    }


#if !(v1_1 || v1_2)
    // Looks up the pawn's ideology and returns the rgb color associated with that ideology, adjusting it for readability
    public static Color IdeoLabelColor(this Pawn pawn)
    {
        var fallbackColor = GenMapUI.DefaultThingLabelColor;
        // If the user has disabled the automatic color assignment, check for any individual color settings and otherwise use the global setting
        if (!Settings.UseIdeoColorForRole)
            return LabelsTracker_WorldComponent.Instance?[pawn].IdeoRoleColor ?? fallbackColor;

        Precept_Role? role = null;
        // Get a cached color override (or null if no override has been set)
        Color? ideoColor = LabelsTracker_WorldComponent.Instance?[pawn].IdeoRoleColor;
        // If there isn't an override saved in the cache, determine a color
        if (ideoColor == null)
        {
            try
            {
                role = pawn.ideo?.Ideo?.GetRole(pawn);
                ideoColor = role?.ideo?.colorDef?.color;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Ideo label color.", true);
                return fallbackColor;
            }

            ideoColor = Color.Lerp(ideoColor ?? fallbackColor, Color.white, 0.35f);
        }

        if (role == null) return ideoColor.Value;

        if (Settings.RoleColorOnlyIfAbilityAvailable)
        {
            return PawnCache.GetOrCache(pawn).IdeoRoleAbilityIsReady ? ideoColor.Value : fallbackColor;
        }

        return ideoColor.Value;
    }

#endif

    internal static Color RoyalTitleColor(this Pawn pawn)
    {
        return LabelsTracker_WorldComponent.Instance?[pawn].RoyalTitleColor ?? Settings.RoyalTitleColorDefault;
    }
}
