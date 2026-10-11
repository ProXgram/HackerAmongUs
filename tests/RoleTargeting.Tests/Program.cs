using HAU;

int passed = 0, failed = 0, assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception(message);
}
void Run(string name, Action test)
{
    try { test(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
}

foreach (var kind in Enum.GetValues<RoleKind>())
{
    void Fallback(string label, Action<Fixture> arrange) => Run(kind + " / " + label, () =>
    {
        var fixture = new Fixture(kind);
        arrange(fixture);
        var sentinel = fixture.Target(99, 100);
        PlayerControl result = sentinel;
        Check(fixture.Prefix(ref result), "Native target selection should run.");
        Check(ReferenceEquals(result, sentinel), "Fallback must leave the native result untouched.");
        Check(Utils.SnapshotReads == 0, "Fallback must not query the world snapshot.");
    });
    void NoTarget(string label, Action<Fixture> arrange, bool noValidation = true) => Run(kind + " / " + label, () =>
    {
        var fixture = new Fixture(kind);
        arrange(fixture);
        PlayerControl result = fixture.Target(99, 100);
        Check(!fixture.Prefix(ref result), "An eligible enabled override should provide its own result.");
        Check(result is null, "No eligible candidate must produce null.");
        Check(Utils.SnapshotReads == 1, "A selection pass reads one sorted snapshot.");
        if (noValidation) Check(fixture.Role.ValidationCalls == 0, "Malformed candidates must be rejected before native validation.");
        Check(fixture.Local.Data.Role.Pointer == fixture.Role.Pointer, "Selection must preserve the assigned role.");
    });

    Fallback("disabled toggle", fixture => fixture.Enabled = false);
    Fallback("unrelated range toggles cannot activate this role", fixture =>
    {
        CheatToggles.killReach = CheatToggles.interrogateReach = CheatToggles.trackReach = true;
        fixture.Enabled = false;
    });
    Fallback("null role", fixture => fixture.Role = null);
    Fallback("destroyed role", fixture => fixture.Role.Destroyed = true);
    Fallback("missing local player", _ => PlayerControl.LocalPlayer = null);
    Fallback("destroyed local player", fixture => fixture.Local.Destroyed = true);
    Fallback("nonowned local player", fixture => fixture.Local.AmOwner = false);
    Fallback("missing local data", fixture => fixture.Local.Data = null);
    Fallback("destroyed local data", fixture => fixture.Local.Data.Destroyed = true);
    Fallback("missing current role", fixture => fixture.Local.Data.Role = null);
    Fallback("replaced role binding", fixture => fixture.Local.Data.Role = new RoleBehaviour());
    Fallback("living flag cannot activate a ghost role", fixture => fixture.Role.IsDead = true);
    Fallback("missing role player", fixture => fixture.Role.Player = null);
    Fallback("destroyed role player", fixture => fixture.Role.Player = new PlayerControl { AmOwner = true, Destroyed = true });
    Fallback("foreign role actor", fixture => fixture.Role.Player = fixture.Target(2, 2));
    Fallback("foreign actor with owner flag still cannot target for local player", fixture =>
        fixture.Role.Player = new PlayerControl { AmOwner = true });
    Fallback("owner flag on local pointer alias is required", fixture =>
        fixture.Role.Player = new PlayerControl { Pointer = fixture.Local.Pointer, AmOwner = false });
    Fallback("dead local player", fixture => fixture.Local.Data.IsDead = true);
    Fallback("disconnected local player", fixture => fixture.Local.Data.Disconnected = true);
    Fallback("missing local collider", fixture => fixture.Local.Collider = null);
    Fallback("destroyed local collider", fixture => fixture.Local.Collider.Destroyed = true);
    Fallback("missing game data", _ => GameData.Instance = null);
    Fallback("destroyed game data", _ => GameData.Instance.Destroyed = true);
    Fallback("lobby outside gameplay", _ => Utils.isInGame = false);
    Fallback("meeting context", _ => Utils.isMeeting = true);
    Fallback("exile context", _ => Utils.isExiling = true);

    NoTarget("null sorted snapshot", _ => Utils.Players = null);
    NoTarget("empty sorted snapshot", _ => Utils.Players.Clear());
    NoTarget("null and destroyed actors", fixture =>
        Utils.Players.AddRange(new PlayerControl[] { null, new() { Destroyed = true } }));
    NoTarget("disconnected candidate", fixture =>
    { var target = fixture.Target(2, 1); target.Data.Disconnected = true; Utils.Players.Add(target); });
    NoTarget("candidate without data", fixture =>
    { var target = fixture.Target(2, 1); target.Data = null; Utils.Players.Add(target); });
    NoTarget("destroyed candidate data", fixture =>
    { var target = fixture.Target(2, 1); target.Data.Destroyed = true; Utils.Players.Add(target); });
    NoTarget("candidate without role", fixture =>
    { var target = fixture.Target(2, 1); target.Data.Role = null; Utils.Players.Add(target); });
    NoTarget("destroyed candidate role", fixture =>
    { var target = fixture.Target(2, 1); target.Data.Role.Destroyed = true; Utils.Players.Add(target); });
    NoTarget("candidate without collider", fixture =>
    { var target = fixture.Target(2, 1); target.Collider = null; Utils.Players.Add(target); });
    NoTarget("destroyed candidate collider", fixture =>
    { var target = fixture.Target(2, 1); target.Collider.Destroyed = true; Utils.Players.Add(target); });
    NoTarget("disabled candidate collider", fixture =>
    { var target = fixture.Target(2, 1); target.Collider.enabled = false; Utils.Players.Add(target); });
    NoTarget("candidate without data actor", fixture =>
    { var target = fixture.Target(2, 1); target.Data.Object = null; Utils.Players.Add(target); });
    NoTarget("destroyed data actor", fixture =>
    { var target = fixture.Target(2, 1); target.Data.Object = new PlayerControl { Destroyed = true }; Utils.Players.Add(target); });
    NoTarget("stale candidate data actor", fixture =>
    { var target = fixture.Target(2, 1); target.Data.Object = fixture.Target(3, 3); Utils.Players.Add(target); });
    NoTarget("self target", fixture => Utils.Players.Add(fixture.Local));
    NoTarget("same native pointer is self even through another wrapper", fixture =>
    { var alias = fixture.Target(2, 1); alias.Pointer = fixture.Local.Pointer; Utils.Players.Add(alias); });
    NoTarget("dead candidate rejected by native role rules", fixture =>
    { var target = fixture.Target(2, 1); target.Data.IsDead = true; Utils.Players.Add(target); }, noValidation: false);
    NoTarget("native predicate remains authoritative", fixture =>
    {
        fixture.Role.Validation = _ => false;
        Utils.Players.Add(fixture.Target(2, 1));
    }, noValidation: false);
    NoTarget("Freeplay supports an empty snapshot", _ => { Utils.isInGame = false; Utils.isFreePlay = true; });

    Run(kind + " / nearest valid candidate and typed native predicate", () =>
    {
        var fixture = new Fixture(kind);
        var far = fixture.Target(2, 10);
        var near = fixture.Target(3, 2);
        var invalidNear = fixture.Target(4, 1); invalidNear.Data.Disconnected = true;
        var dead = fixture.Target(5, 0.5f); dead.Data.IsDead = true;
        Utils.Players.AddRange([far, fixture.Local, invalidNear, near, dead]);
        var assigned = fixture.Local.Data.Role;
        PlayerControl result = null;
        Check(!fixture.Prefix(ref result), "Enabled owned role should override.");
        Check(ReferenceEquals(result, near), "Select nearest eligible target, skipping closer invalid actors.");
        Check(fixture.Role.TypedValidationCalls == 2, "Dispatch must preserve the actual role's native predicate.");
        Check(fixture.Role.ValidationCalls == 2, "Only physically valid candidates reach native validation.");
        Check(ReferenceEquals(fixture.Local.Data.Role, assigned), "Assigned role must not be changed.");
        Check(far.Data.Disconnected == false && dead.Data.IsDead, "Target selection must not mutate candidates.");
    });
    Run(kind + " / collider-disabled owner keeps targeting preference", () =>
    {
        var fixture = new Fixture(kind); fixture.Local.Collider.enabled = false;
        var target = fixture.Target(2, 2); Utils.Players.Add(target);
        PlayerControl result = null;
        Check(!fixture.Prefix(ref result) && ReferenceEquals(result, target), "Owner noclip must not disable role selection.");
    });
}

Run("Existing Impostor Kill Anyone ghost eligibility is preserved", () =>
{
    var fixture = new Fixture(RoleKind.Impostor);
    var target = fixture.Target(2, 2); target.Data.IsDead = true;
    Utils.Players.Add(target); CheatToggles.killAnyone = true; Utils.isHost = true;
    PlayerControl result = null;
    Check(!fixture.Prefix(ref result) && ReferenceEquals(result, target),
        "The existing native/postfix target decision must not be overridden by a new dead-target filter.");
});

Run("Guest kill-anyone flag cannot replace the native decision", () =>
{
    var fixture = new Fixture(RoleKind.Impostor);
    CheatToggles.killAnyone = true;
    bool result = false;
    ImpostorRole_IsValidTarget.Postfix((ImpostorRole)fixture.Role, fixture.Target(2, 1).Data, ref result);
    Check(!result, "A stale guest/profile toggle must preserve the native result.");
});
Run("Foreign assigned role keeps its native decision on a host", () =>
{
    var fixture = new Fixture(RoleKind.Impostor);
    CheatToggles.killAnyone = true; Utils.isHost = true;
    var foreign = new ImpostorRole { Player = fixture.Target(3, 1) };
    bool result = true;
    ImpostorRole_IsValidTarget.Postfix(foreign, fixture.Local.Data, ref result);
    Check(result, "Changing local targeting must not replace another actor's incoming validation.");
});
Run("Guest vanished-kill flag cannot replace the native decision", () =>
{
    var fixture = new Fixture(RoleKind.Impostor);
    CheatToggles.killVanished = true;
    bool result = false;
    PhantomRole_IsValidTarget.Postfix(new PhantomRole { Player = fixture.Local }, fixture.Target(2, 1).Data, ref result);
    Check(!result, "Guest vanished targeting must preserve native eligibility.");
});

Console.WriteLine(passed + "/" + (passed + failed) + " role-targeting scenarios passed; " + assertions + " assertions.");
Console.WriteLine("Linked production prefixes with offline game contracts only; no installed game, Harmony runtime or network calls.");
return failed == 0 ? 0 : 1;

enum RoleKind { Impostor, Detective, Tracker }
sealed class Fixture
{
    public RoleKind Kind { get; }
    public PlayerControl Local { get; }
    public RoleBehaviour Role { get; set; }
    public bool Enabled
    {
        set
        {
            if (Kind == RoleKind.Impostor) CheatToggles.killReach = value;
            else if (Kind == RoleKind.Detective) CheatToggles.interrogateReach = value;
            else CheatToggles.trackReach = value;
        }
    }
    public Fixture(RoleKind kind)
    {
        Kind = kind;
        Utils.isInGame = true; Utils.isHost = Utils.isFreePlay = Utils.isMeeting = Utils.isExiling = false;
        Utils.Players = new(); Utils.SnapshotReads = 0;
        CheatToggles.killReach = CheatToggles.interrogateReach = CheatToggles.trackReach =
            CheatToggles.killAnyone = CheatToggles.killVanished = false;
        GameData.Instance = new();
        Local = Target(1, 0); Local.AmOwner = true; PlayerControl.LocalPlayer = Local;
        Role = kind switch
        {
            RoleKind.Impostor => new ImpostorRole(),
            RoleKind.Detective => new DetectiveRole(),
            _ => new TrackerRole()
        };
        Role.Player = Local; Local.Data.Role = Role;
        Enabled = true;
    }
    public PlayerControl Target(int id, float position)
    {
        var player = new PlayerControl { Position = position };
        player.Data = new NetworkedPlayerInfo { Object = player, Role = new RoleBehaviour() };
        return player;
    }
    public bool Prefix(ref PlayerControl result) => Kind switch
    {
        RoleKind.Impostor => ImpostorRole_FindClosestTarget.Prefix((ImpostorRole)Role, ref result),
        RoleKind.Detective => DetectiveRole_FindClosestTarget.Prefix((DetectiveRole)Role, ref result),
        _ => TrackerRole_FindClosestTarget.Prefix((TrackerRole)Role, ref result)
    };
}
