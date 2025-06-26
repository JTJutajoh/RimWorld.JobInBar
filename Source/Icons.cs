using UnityEngine;

namespace JobInBar;

[StaticConstructorOnStartup]
internal static class Icons
{
    internal static readonly Texture2D LabelToggleIcon = ContentFinder<Texture2D>.Get("UI/LabelToggle")!;
    internal static readonly Texture2D LabelSettingsIcon = ContentFinder<Texture2D>.Get("UI/Gear")!;
    internal static readonly Texture2D PaletteIcon = TexButton.ToggleDevPalette!;
    internal static readonly Texture2D GearIcon = ContentFinder<Texture2D>.Get("UI/Gear")!;
}