using UnityEngine;

namespace HAU;

public class PracticeTab : ITab
{
    public string name => "Practice";

    public void Draw()
    {
        UIHelpers.Hint(Utils.isFreePlay ? "Freeplay is active. Practice controls are available."
            : "Open Freeplay from the game's Practice menu to use these controls.");
        bool previous = GUI.enabled;
        GUI.enabled = previous && Utils.isFreePlay;
        try
        {
            UIHelpers.BeginSection("Impostor practice", "Changes your Freeplay role.");
            RolePreviewControls.DrawAlwaysImpostor(" Always Impostor in Freeplay");
            UIHelpers.EndSection();
            UIHelpers.BeginSection("Judge practice");
            CheatToggles.infiniteJudge = GUILayout.Toggle(CheatToggles.infiniteJudge, " Enable Judge practice controls");
            GUILayout.Label(InfiniteJudgeHandler.StatusText);
            UIHelpers.Hint("Freeplay only. The current practice handler permits one Judge use per meeting.");
            UIHelpers.EndSection();
            UIHelpers.BeginSection("Local player experiments");
            UIHelpers.ChoosePlayer("Choose a role preview", ref CheatToggles.setFakeRole, Utils.isPlayer);
            UIHelpers.ChoosePlayer("Choose alive or ghost preview", ref CheatToggles.setFakeAlive, Utils.isPlayer);
            UIHelpers.Hint("These previews change the local practice player.");
            UIHelpers.EndSection();
        }
        finally { GUI.enabled = previous; }
    }
}
