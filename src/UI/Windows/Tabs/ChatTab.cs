using UnityEngine;

namespace HAU;

public class ChatTab : ITab
{
    public string name => "Chat";

    public void Draw()
    {
        UIHelpers.BeginSection("Chat access", "Your client can change the chat interface. The server still checks submitted messages.");
        CheatToggles.enableChat = GUILayout.Toggle(CheatToggles.enableChat, " Show chat outside normal chat periods");
        CheatToggles.bypassUrlBlock = GUILayout.Toggle(CheatToggles.bypassUrlBlock, " Allow links in the local textbox");
        CheatToggles.lowerRateLimits = GUILayout.Toggle(CheatToggles.lowerRateLimits, " Shorten the local send delay");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Writing messages");
        CheatToggles.unlockCharacters = GUILayout.Toggle(CheatToggles.unlockCharacters, " Allow extra characters");
        CheatToggles.longerMessages = GUILayout.Toggle(CheatToggles.longerMessages, " Increase the textbox length");
        CheatToggles.unlockClipboard = GUILayout.Toggle(CheatToggles.unlockClipboard, " Enable clipboard paste");
        UIHelpers.Hint("These options affect your textbox. Message delivery depends on the game's network checks.");
        UIHelpers.EndSection();
    }
}
