using UnityEngine;

namespace HAU;

public class ChatTab : ITab
{
    public string name => "Chat";

    public void Draw()
    {
        UIHelpers.BeginSection("Chat access", "Your client can change the chat interface. The server still checks submitted messages.");
        CheatToggles.enableChat = GUILayout.Toggle(CheatToggles.enableChat, " Always Chat");
        CheatToggles.bypassUrlBlock = GUILayout.Toggle(CheatToggles.bypassUrlBlock, " Send Links with Commas");
        CheatToggles.lowerRateLimits = GUILayout.Toggle(CheatToggles.lowerRateLimits, " Shorter Chat Delay");
        UIHelpers.Hint("Always Chat keeps your chat interface openable. Links replaces dots in detected web/email addresses with commas, such as example.com to example,com. Shorter Chat Delay reduces the local wait by one second.");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Writing messages");
        CheatToggles.unlockCharacters = GUILayout.Toggle(CheatToggles.unlockCharacters, " Extra Chat Characters");
        CheatToggles.longerMessages = GUILayout.Toggle(CheatToggles.longerMessages, " Longer Messages / 120 characters");
        CheatToggles.unlockClipboard = GUILayout.Toggle(CheatToggles.unlockClipboard, " Copy, Cut and Paste");
        UIHelpers.Hint("Ctrl+C copies the textbox, Ctrl+X cuts it, and Ctrl+V appends the clipboard. Extra Characters still excludes markup control characters.");
        UIHelpers.Hint("These options affect your textbox. Message delivery depends on the game's network checks.");
        UIHelpers.EndSection();
    }
}
