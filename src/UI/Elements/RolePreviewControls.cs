using UnityEngine;

namespace HAU;

public static class RolePreviewControls
{
    public static void DrawAlwaysImpostor(string label)
    {
        bool wasEnabled = CheatToggles.alwaysImpostor;
        CheatToggles.alwaysImpostor = GUILayout.Toggle(CheatToggles.alwaysImpostor, label);
        if (wasEnabled != CheatToggles.alwaysImpostor)
            AlwaysImpostorHandler.ResetFreeplayAttempt();

        if (CheatToggles.alwaysImpostor)
            GUILayout.Label(AlwaysImpostorHandler.StatusText, GUIStylePreset.TabSubtitle,
                GUILayout.MaxWidth(Mathf.Min(420f, MenuUI.ContentWidth - 32f)));
    }
}
