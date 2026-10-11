using AmongUs.Data;

namespace HAU;
public static class PlayerDisplayOverrides
{
    public static void SpoofLevel()
    {

        if (!string.IsNullOrEmpty(HAUPlugin.spoofLevel.Value) &&
            uint.TryParse(HAUPlugin.spoofLevel.Value, out uint parsedLevel) &&
            parsedLevel != DataManager.Player.Stats.Level)
        {


            DataManager.Player.stats.level = parsedLevel - 1;
            DataManager.Player.Save();
        }
    }

    public static string SpoofFriendCode()
    {
        string friendCode = HAUPlugin.guestFriendCode.Value;
        if (string.IsNullOrWhiteSpace(friendCode))
        {
            friendCode = DestroyableSingleton<AccountManager>.Instance.GetRandomName();
        }
        return friendCode;
    }
}
