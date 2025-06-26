using UnityEngine;

namespace JobInBar;

[StaticConstructorOnStartup]
internal static class Icons
{
    internal static readonly Texture2D LabelToggleIcon = ContentFinder<Texture2D>.Get("UI/LabelToggle")!;
    internal static readonly Texture2D LabelSettingsIcon = ContentFinder<Texture2D>.Get("UI/LabelOptionsButton")!;
    internal static readonly Texture2D PaletteIcon = ContentFinder<Texture2D>.Get("UI/Palette")!;
    internal static readonly Texture2D GearIcon = ContentFinder<Texture2D>.Get("UI/Gear")!;
}
