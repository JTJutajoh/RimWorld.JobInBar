using System;
using JetBrains.Annotations;
using UnityEngine;

namespace JobInBar;

internal enum LabelType
{
    Name,
    JobTitle,
    RoyalTitle,
    IdeoRole
}

[UsedImplicitly]
public class JobInBarMod : Mod
{
    public JobInBarMod(ModContentPack content) : base(content)
    {
        Instance = this;
        // ReSharper disable once RedundantArgumentDefaultValue
        Log.Initialize(this, "cyan");

        GetSettings<Settings>();
    }

    public static JobInBarMod? Instance { get; private set; }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        try
        {
            base.DoSettingsWindowContents(inRect);
            GetSettings<Settings>()?.DoWindowContents(inRect);
        }
        catch (Exception e)
        {
            Log.Exception(e, "Error drawing mod settings window.", true);
            Widgets.DrawBoxSolid(inRect, new Color(0, 0, 0, 0.5f));
            var errorRect = inRect.MiddlePart(0.4f, 0.25f);
            Widgets.DrawWindowBackground(errorRect);
            Widgets.Label(errorRect.ContractedBy(16f),
                $"Error rendering settings window:\n\"{e.Message}\", see log for stack trace.\nPlease report this to the mod author.");
        }
    }

    public override string SettingsCategory()
    {
        return "JobInBar_SettingsCategory".Translate();
    }
}
