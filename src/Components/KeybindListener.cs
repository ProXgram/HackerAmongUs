using UnityEngine;

namespace HAU;

public class KeybindListener : MonoBehaviour
{
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            MovementAutomation.Stop();
            ColorCycleHandler.Stop();
        }
        if (!Application.isFocused)
        {
            MovementAutomation.Stop();
            ColorCycleHandler.Stop();
        }

        SprintHandler.Tick();
        ColorCycleHandler.Tick();
        NicknameEntry.Tick();
        if (HAUPlugin.isPanicked || !Application.isFocused) return;

        if (MenuUI.isGUIActive) return;


        if (HudManager.InstanceExists && HudManager.Instance.Chat && HudManager.Instance.Chat.IsOpenOrOpening) return;


        foreach (var (name, key) in CheatToggles.Keybinds)
        {
            if (key == KeyCode.None) continue;
            if (!Input.GetKeyDown(key)) continue;

            if (!CheatToggles.ToggleFields.TryGetValue(name, out var field)) continue;
            if (ControlPolicy.RequiresHostActivation(name) && !(Utils.isHost || Utils.isFreePlay)) continue;

            var current = (bool)field.GetValue(null);
            field.SetValue(null, ControlPolicy.IsMomentaryRequest(name) || !current);
        }
    }

    public void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            MovementAutomation.Stop();
            ColorCycleHandler.Stop();
            NicknameEntry.StopEditing();
            SprintHandler.Reset();
        }
    }

    public void OnDisable()
    {
        MovementAutomation.Stop();
        ColorCycleHandler.Stop();
        NicknameEntry.StopEditing();
        SprintHandler.Reset();
    }

    public void OnDestroy()
    {
        MovementAutomation.Stop();
        ColorCycleHandler.Stop();
        NicknameEntry.StopEditing();
        SprintHandler.Reset();
    }
}
