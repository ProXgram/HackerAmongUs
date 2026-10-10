using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace HAU;

public class MenuUI : MonoBehaviour
{
    public static int windowHeight = 590;
    public static int windowWidth = 700;
    public static Rect windowRect;

    public static bool isGUIActive = false;
    private List<ITab> _tabs = new();
    private int _selectedTab;
    private Vector2 _tabScrollPosition;
    private Vector2 _contentScrollPosition;
    public static string ActiveTabName => HAUPlugin.menuUI &&
        HAUPlugin.menuUI._selectedTab >= 0 && HAUPlugin.menuUI._selectedTab < HAUPlugin.menuUI._tabs.Count
        ? HAUPlugin.menuUI._tabs[HAUPlugin.menuUI._selectedTab].name : "";
    public static float hue; // For RGB mode
    public static float ContentWidth => Mathf.Max(80f, windowRect.width - 200f);
    public static string ContextText => Utils.isFreePlay ? "FREEPLAY"
        : Utils.isHost ? "HOST" : Utils.isLobby || Utils.isInGame ? "GUEST" : "MAIN MENU";

    public static void CloseMenu()
    {
        isGUIActive = false;
        OutfitPickerUI.Close();
        NicknameEntry.StopEditing();
    }

    public static void OpenTab(string name)
    {
        var menu = HAUPlugin.menuUI;
        if (!menu) return;
        int index = menu._tabs.FindIndex(tab => tab.name == name);
        if (index < 0) return;
        menu._selectedTab = index;
        menu._contentScrollPosition = Vector2.zero;
        NicknameEntry.StopEditing();
    }

    private void Start()
    {
        _tabs.Add(new HomeTab());
        _tabs.Add(new MovementTab());
        _tabs.Add(new PlayerToolsTab());
        _tabs.Add(new ColorsTab());
        _tabs.Add(new ESPTab());
        _tabs.Add(new TasksTab());
        _tabs.Add(new RolesTab());
        _tabs.Add(new HostOnlyTab());
        _tabs.Add(new PracticeTab());
        _tabs.Add(new ConfigTab());
        _tabs.Add(new AboutTab());
        _tabs.Add(new ConsoleTab());
#if GUEST_KILL_EXPERIMENT
        _tabs.Add(new GuestRoundTestTab());
#endif

        // Instantiate 2D area of MenuUI
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    public void InitStyles()
    {
        GUI.skin.toggle.fontSize = GUI.skin.button.fontSize = GUI.skin.label.fontSize = 15;
        GUI.skin.label.wordWrap = true;
    }

    private void Update()
    {
        MultiRoleHandler.Tick();
        if (Input.GetKeyDown(Utils.StringToKeycode(HAUPlugin.menuKeybind.Value)))
        {
            // Enable or disable GUI with DELETE key
            if (isGUIActive) CloseMenu();
            else isGUIActive = true;

            if (HAUPlugin.menuOpenOnMouse.Value)
            {
                // Teleport the window to the mouse for immediate use
                Vector2 mousePosition = Input.mousePosition;
                windowRect.position = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            }
        }

        if (isGUIActive)
        {
            MovementAutomation.Pause();
            SprintHandler.Reset();
        }

        if (CheatToggles.rgbMode)
        {
            hue += Time.deltaTime * 0.3f; // Adjust speed of color change, higher multiplier = faster
            if (hue > 1f) hue -= 1f; // Loop hue back to 0 when it exceeds 1
        }

        if (CheatToggles.stealthMode != HAUPlugin.inStealthMode)
        {
            HAUPlugin.inStealthMode = CheatToggles.stealthMode;

            Scene scene = SceneManager.GetActiveScene();

            if (scene.name == "MainMenu" || scene.name == "MatchMaking")
            {
                SceneManager.LoadScene(scene.name);
            }
        }

        if (CheatToggles.panicMode) Utils.Panic();

        var stamp = ModManager.Instance.ModStamp;
        if (stamp) stamp.enabled = !(HAUPlugin.inStealthMode || HAUPlugin.isPanicked);

        if (CheatToggles.openConfig)
        {
            Utils.OpenConfigFile();
            CheatToggles.openConfig = false;
        }

        if (CheatToggles.reloadConfig)
        {
            HAUPlugin.Plugin.Config.Reload();
            CheatToggles.reloadConfig = false;
        }

        if (CheatToggles.saveProfile)
        {
            CheatToggles.saveProfile = false; // Disable first to avoid saving it to profile
            CheatToggles.SaveTogglesToProfile();
        }

        if (CheatToggles.loadProfile)
        {
            CheatToggles.LoadTogglesFromProfile();
            CheatToggles.loadProfile = false;
        }

        // Some cheats only work if the LocalPlayer exists, so they are turned off if it does not
        if(!Utils.isPlayer)
        {
            CheatToggles.setFakeRole = false;
            CheatToggles.setFakeAlive = false;
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.teleportPlayer = false;
            CheatToggles.spectate = false;
            CheatToggles.freecam = false;
            CheatToggles.killPlayer = false;
            CheatToggles.callMeeting = false;

            if (CheatToggles.runOverload)
            {
                OverloadUI.StopOverload();
            }
        }

        // Some cheats only work if the ship exists, so they are turned off if it does not
        if(!Utils.isShip)
        {
            CheatToggles.sabotageMap = false;
            CheatToggles.unfixableLights = false;
            CheatToggles.completeMyTasks = false;
            CheatToggles.kickVents = false;
            CheatToggles.reportBody = false;
            CheatToggles.closeMeeting = false;
            CheatToggles.reactorSab = false;
            CheatToggles.oxygenSab = false;
            CheatToggles.commsSab = false;
            CheatToggles.elecSab = false;
            CheatToggles.mushSab = false;
            CheatToggles.closeAllDoors = false;
            CheatToggles.openAllDoors = false;
            CheatToggles.spamCloseAllDoors = false;
            CheatToggles.spamOpenAllDoors = false;
            CheatToggles.mushSpore = false;

            GameActions.StopShipAnimCheats();
        }

        if(!Utils.isHost && !Utils.isFreePlay)
        {
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.killPlayer = false;
            CheatToggles.ejectPlayer = false;
            CheatToggles.noKillCd = false;
            CheatToggles.killAnyone = false;
            CheatToggles.killVanished = false;
            CheatToggles.forceStartGame = false;
            CheatToggles.skipMeeting = false;
            CheatToggles.voteImmune = false;
            CheatToggles.noGameEnd = false;
            CheatToggles.showProtectMenu = false;
            CheatToggles.showRolesMenu = false;
            CheatToggles.noOptionsLimits = false;
            CheatToggles.unfixableLights = false;
            CheatToggles.kickVents = false;
        }

        // Some cheats only work if in a meeting, so they are turned off if it does not
        if (!Utils.isMeeting)
        {
            CheatToggles.skipMeeting = false;
            CheatToggles.ejectPlayer = false;
        }
    }

    public void OnGUI()
    {
        if (!isGUIActive || HAUPlugin.isPanicked || OutfitPickerUI.IsOpen) return;
        UIHelpers.RefreshTheme();

        // Theme changes are scoped to this window; the game's own GUI keeps its skin.
        var button = GUI.skin.button;
        var toggle = GUI.skin.toggle;
        var label = GUI.skin.label;
        var background = GUI.backgroundColor;
        var foreground = GUI.contentColor;
        var enabled = GUI.enabled;
        GUI.skin.button = GUIStylePreset.NormalButton;
        GUI.skin.toggle = GUIStylePreset.NormalToggle;
        GUI.skin.label = GUIStylePreset.Label;
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.white;
        try
        {
            var bounds = MenuLayout.Fit(Screen.width, Screen.height, windowRect.x, windowRect.y);
            windowRect = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            windowRect = GUI.Window((int)WindowId.MenuUI, windowRect,
                (GUI.WindowFunction)WindowFunction, ModBranding.Name + " / v" + HAUPlugin.displayVersion,
                GUIStylePreset.Window);
        }
        finally
        {
            GUI.skin.button = button;
            GUI.skin.toggle = toggle;
            GUI.skin.label = label;
            GUI.backgroundColor = background;
            GUI.contentColor = foreground;
            GUI.enabled = enabled;
        }
    }

    public void WindowFunction(int windowID)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(ModBranding.ShortName, GUIStylePreset.Brand, GUILayout.Width(72f));
        GUILayout.BeginVertical();
        GUILayout.Label(ModBranding.Name, GUIStylePreset.Label);
        GUILayout.Label("by " + ModBranding.Creator, GUIStylePreset.Hint);
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.Label(ContextText, GUIStylePreset.Context, GUILayout.Width(110f));
        GUILayout.EndHorizontal();

        float contentHeight = Mathf.Max(100f, windowRect.height - MenuLayout.VerticalChrome);
        GUILayout.BeginHorizontal();
        _tabScrollPosition = GUILayout.BeginScrollView(_tabScrollPosition, false, false,
            GUILayout.Width(MenuLayout.SidebarWidth), GUILayout.Height(contentHeight));
        GUILayout.BeginVertical(GUILayout.Width(138f));
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (i == 1) GUILayout.Label("PLAYER", GUIStylePreset.Hint);
            if (i == 7) GUILayout.Label("LOBBY", GUIStylePreset.Hint);
            if (i == 9) GUILayout.Label("MENU", GUIStylePreset.Hint);
            if (GUILayout.Button(_tabs[i].name,
                i == _selectedTab ? GUIStylePreset.SelectedTab : GUIStylePreset.TabButton,
                GUILayout.Height(27f)))
            {
                _selectedTab = i;
                _contentScrollPosition = Vector2.zero;
                NicknameEntry.StopEditing();
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUILayout.Space(9f);
        _contentScrollPosition = GUILayout.BeginScrollView(_contentScrollPosition, false, true,
            GUILayout.Width(windowRect.width - MenuLayout.HorizontalChrome), GUILayout.Height(contentHeight));
        GUILayout.BeginVertical(GUILayout.Width(ContentWidth - 12f));
        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            GUILayout.Label(_tabs[_selectedTab].name, GUIStylePreset.TabTitle);
            _tabs[_selectedTab].Draw();
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        bool compact = windowRect.width < 540f;
        if (GUILayout.Button("Close / " + HAUPlugin.menuKeybind.Value, GUILayout.Width(compact ? 100f : 145f)))
            CloseMenu();
        if (GUILayout.Button(compact ? "Stop / F8" : "Stop movement / F8", GUILayout.Width(compact ? 100f : 175f)))
        {
            MovementAutomation.Stop();
            ColorCycleHandler.Stop();
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label("v" + HAUPlugin.displayVersion, GUIStylePreset.Hint, GUILayout.Width(compact ? 45f : 65f));
        GUILayout.EndHorizontal();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 77f));
    }
}
