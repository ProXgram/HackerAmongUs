using HarmonyLib;
using System;

namespace HAU;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
public static class HudManager_Start
{

	public static void Postfix(HudManager __instance)
	{
		__instance.MapButton.OnClick.RemoveAllListeners();



		__instance.MapButton.OnClick.AddListener((Action) (() =>
        {
			__instance.ToggleMapVisible(new MapOptions
			{
				Mode = MapOptions.Modes.Normal
			});

		}));
	}
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudManager_Update
{
	public static void Postfix(HudManager __instance)
    {
		__instance.ShadowQuad.gameObject.SetActive(!InformationTools.IsFullbrightActive());

		if (Utils.IsChatUiActive())
		{
			__instance.Chat.gameObject.SetActive(true);
		}
		else
		{
			Utils.CloseChat();
			__instance.Chat.gameObject.SetActive(false);
		}

		GameActions.UseVentCheat(__instance);
		InformationTools.ZoomOut(__instance);
		InformationTools.FreecamCheat();


		if (PlayerPickMenu.playerpickMenu != null && CheatToggles.ShouldPPMClose())
		{
            PlayerPickMenu.playerpickMenu.Close();
        }
    }
}
