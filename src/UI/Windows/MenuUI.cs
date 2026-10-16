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
    private readonly MenuSearchInput _searchInput = new();
    private Rect _searchBounds;
    private IReadOnlyList<MenuDestination> _searchResults = System.Array.Empty<MenuDestination>();
    public static string ActiveTabName => HAUPlugin.menuUI &&
        HAUPlugin.menuUI._selectedTab >= 0 && HAUPlugin.menuUI._selectedTab < HAUPlugin.menuUI._tabs.Count
        ? HAUPlugin.menuUI._tabs[HAUPlugin.menuUI._selectedTab].name : "";
    public static float hue;
    public static bool IsSearchEditing => HAUPlugin.menuUI && HAUPlugin.menuUI._searchInput.IsEditing;
    public static float ContentWidth => Mathf.Max(80f, windowRect.width - 200f);
    public static string ContextText => Utils.isFreePlay ? "FREEPLAY"
        : Utils.isHost ? "HOST" : Utils.isLobby || Utils.isInGame ? "GUEST" : "MAIN MENU";

    public static void CloseMenu()
    {
        isGUIActive = false;
        OutfitPickerUI.Close();
        NicknameEntry.StopEditing();
        if (HAUPlugin.menuUI) HAUPlugin.menuUI._searchInput.StopEditing();
    }

    public static void CloseMenuFromPointer()
    {
        PointerInputGuard.ConsumeGuiClose("Menu.Close", windowRect);
        CloseMenu();
    }

    public static void OpenTab(string name)
    {
        var menu = HAUPlugin.menuUI;
        if (!menu) return;
        string resolved = MenuNavigation.ResolveTab(name);
        int index = menu._tabs.FindIndex(tab => tab.name == resolved);
        if (index < 0) return;
        menu._selectedTab = index;
        menu._contentScrollPosition = Vector2.zero;
        menu.ClearSearch();
        NicknameEntry.StopEditing();
    }

    public static void OpenFeature(string id)
    {
        var destination = MenuNavigation.Find(id);
        if (destination == null) return;
        OpenTab(destination.Tab);
        var menu = HAUPlugin.menuUI;
        if (menu && menu._tabs[menu._selectedTab] is IMenuPage page) page.OpenPage(destination.Page);
    }

    private void ClearSearch()
    {
        _searchInput.Clear();
        _searchResults = System.Array.Empty<MenuDestination>();
        if (Event.current != null) GUI.FocusControl(null);
    }

    private void Start()
    {
        _tabs.Add(new HomeTab());
        _tabs.Add(new MovementTab());
        _tabs.Add(new ColorsTab());
        _tabs.Add(new ESPTab());
        _tabs.Add(new TasksTab());
        _tabs.Add(new RolesTab());
        _tabs.Add(new ShipTab());
        _tabs.Add(new ChatTab());
        _tabs.Add(new HostOnlyTab());
        _tabs.Add(new PracticeTab());
        _tabs.Add(new ConfigTab());
        _tabs.Add(new AboutTab());
        _tabs.Add(new ConsoleTab());
#if GUEST_KILL_EXPERIMENT
        _tabs.Add(new GuestRoundTestTab());
#endif


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
        TickSearchInput();
        if (!_searchInput.IsEditing && Input.GetKeyDown(Utils.StringToKeycode(HAUPlugin.menuKeybind.Value)))
        {

            if (isGUIActive) CloseMenu();
            else isGUIActive = true;

            if (HAUPlugin.menuOpenOnMouse.Value)
            {

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
            hue += Time.deltaTime * 0.3f;
            if (hue > 1f) hue -= 1f;
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
            HAU.Updates.AutomaticUpdateHandler.SetEnabled(HAUPlugin.automaticUpdates.Value);
            CheatToggles.reloadConfig = false;
        }

        if (CheatToggles.saveProfile)
        {
            CheatToggles.saveProfile = false;
            CheatToggles.SaveTogglesToProfile();
        }

        if (CheatToggles.loadProfile)
        {
            CheatToggles.LoadTogglesFromProfile();
            CheatToggles.loadProfile = false;
        }


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
            if (_tabs[i] is ConsoleTab) continue;
            if (_tabs[i] is MovementTab) GUILayout.Label("YOUR PLAYER", GUIStylePreset.Hint);
            if (_tabs[i] is HostOnlyTab) GUILayout.Label("HOST & PRACTICE", GUIStylePreset.Hint);
            if (_tabs[i] is ConfigTab) GUILayout.Label("MENU", GUIStylePreset.Hint);
            if (GUILayout.Button(_tabs[i].name,
                i == _selectedTab || _tabs[i] is ConfigTab && _tabs[_selectedTab] is ConsoleTab
                    ? GUIStylePreset.SelectedTab : GUIStylePreset.TabButton,
                GUILayout.Height(27f)))
            {
                _selectedTab = i;
                _contentScrollPosition = Vector2.zero;
                ClearSearch();
                NicknameEntry.StopEditing();
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUILayout.Space(9f);
        GUILayout.BeginVertical(GUILayout.Width(windowRect.width - MenuLayout.HorizontalChrome));
        DrawSearch();
        _contentScrollPosition = GUILayout.BeginScrollView(_contentScrollPosition, false, true,
            GUILayout.Width(windowRect.width - MenuLayout.HorizontalChrome), GUILayout.Height(Mathf.Max(64f, contentHeight - 42f)));
        GUILayout.BeginVertical(GUILayout.Width(ContentWidth - 12f));
        if (_searchInput.IsEditing || !string.IsNullOrWhiteSpace(_searchInput.Text)) DrawSearchResults();
        else if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            GUILayout.Label(_tabs[_selectedTab].name, GUIStylePreset.TabTitle);
            _tabs[_selectedTab].Draw();
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        bool compact = windowRect.width < 540f;
        if (GUILayout.Button("Close / " + HAUPlugin.menuKeybind.Value, GUILayout.Width(compact ? 100f : 145f)))
            CloseMenuFromPointer();
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

    private void DrawSearch()
    {
        if (_searchInput.IsEditing && Event.current.type == EventType.MouseDown && !_searchBounds.Contains(Event.current.mousePosition))
            _searchInput.StopEditing();
        GUILayout.BeginHorizontal();
        string preview = _searchInput.Text.Length > 34 ? _searchInput.Text.Substring(0, 34) + "..." : _searchInput.Text;
        string label = _searchInput.IsEditing ? (_searchInput.AllSelected ? "Selected: " : "Typing: ") + preview
            : preview.Length == 0 ? "Find a feature..." : preview;
        if (GUILayout.Button(label, GUIStylePreset.SearchField, GUILayout.Height(32f)))
        {
            _searchInput.BeginEditing();
            NicknameEntry.StopEditing();
            GUI.FocusControl(null);
        }
        if (Event.current.type == EventType.Repaint) _searchBounds = GUILayoutUtility.GetLastRect();
        if (GUILayout.Button("Clear", GUILayout.Width(60f), GUILayout.Height(32f))) ClearSearch();
        GUILayout.EndHorizontal();
    }

    private void TickSearchInput()
    {
        if (!_searchInput.IsEditing) return;
        if (!isGUIActive || OutfitPickerUI.IsOpen || !Application.isFocused || HAUPlugin.isPanicked)
        {
            _searchInput.StopEditing();
            return;
        }
        MenuTextInputPatches.CaptureSearchInputFrame();
        if (Input.GetKeyDown(KeyCode.Escape)) { ClearSearch(); return; }
        string previous = _searchInput.Text;
        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool submitted = false;
        if (control)
        {
            if (Input.GetKeyDown(KeyCode.A)) _searchInput.SelectAll();
            else if (Input.GetKeyDown(KeyCode.V)) _searchInput.Paste(GUIUtility.systemCopyBuffer ?? "");
        }
        else if (!string.IsNullOrEmpty(Input.inputString)) submitted = _searchInput.Type(Input.inputString);
        if (_searchInput.Text != previous)
        {
            _searchResults = MenuNavigation.Search(_searchInput.Text);
            _contentScrollPosition = Vector2.zero;
        }
        if (submitted && _searchResults.Count > 0) OpenFeature(_searchResults[0].Id);
    }

    private void DrawSearchResults()
    {
        GUILayout.Label("Find a feature", GUIStylePreset.TabTitle);
        UIHelpers.Hint("Type a name. Enter opens the first match. Ctrl+A replaces your search; Escape clears it.");
        if (string.IsNullOrWhiteSpace(_searchInput.Text)) return;
        if (_searchResults.Count == 0)
        {
            UIHelpers.Hint("No matches. Try sprint, outfit, vent, sabotage or ghost vote.");
            return;
        }
        UIHelpers.Hint(_searchResults.Count + (_searchResults.Count == 1 ? " match. Choose it to open its settings." : " matches. Choose one to open its settings."));
        foreach (var destination in _searchResults)
            if (GUILayout.Button(destination.Title + "\n" + destination.Location, GUILayout.Height(50f)))
            {
                OpenFeature(destination.Id);
                break;
            }
    }
}
