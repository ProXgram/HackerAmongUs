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
            CheatToggles.infiniteJudge = GUILayout.Toggle(CheatToggles.infiniteJudge, " Refill Judge Use / Freeplay only");
            GUILayout.Label(InfiniteJudgeHandler.StatusText);
            UIHelpers.Hint("Choose Judge in Freeplay first. Refills one spent use at the next meeting; normal task requirements still apply.");
            UIHelpers.EndSection();
            UIHelpers.BeginSection("Local player experiments");
            UIHelpers.ChoosePlayer("Fake Role / Freeplay preview...", ref CheatToggles.setFakeRole, Utils.isPlayer);
            UIHelpers.ChoosePlayer("Alive or Ghost / Freeplay preview...", ref CheatToggles.setFakeAlive, Utils.isPlayer);
            UIHelpers.Hint("These previews change the local practice player.");
            UIHelpers.EndSection();
        }
        finally { GUI.enabled = previous; }
    }
}
