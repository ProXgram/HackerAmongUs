using System;
using System.Collections.Generic;

namespace HAU;

/// <summary>Stable profile IDs distinguish persistent switches from explicit requests.</summary>
public static class ControlPolicy
{
    private static readonly HashSet<string> Requests = new(StringComparer.Ordinal)
    {
        nameof(CheatToggles.teleportPlayer), nameof(CheatToggles.setFakeRole),
        nameof(CheatToggles.animShields), nameof(CheatToggles.animEmptyGarbage),
        nameof(CheatToggles.setFakeAlive), nameof(CheatToggles.completeMyTasks),
        nameof(CheatToggles.sabotageMap), nameof(CheatToggles.closeMeeting),
        nameof(CheatToggles.callMeeting), nameof(CheatToggles.reportBody),
        nameof(CheatToggles.mushSab), nameof(CheatToggles.mushSpore),
        nameof(CheatToggles.openAllDoors), nameof(CheatToggles.closeAllDoors),
        nameof(CheatToggles.kickVents), nameof(CheatToggles.forceRole),
        nameof(CheatToggles.skipMeeting), nameof(CheatToggles.forceStartGame),
        nameof(CheatToggles.ejectPlayer), nameof(CheatToggles.killPlayer),
        nameof(CheatToggles.telekillPlayer), nameof(CheatToggles.killAll),
        nameof(CheatToggles.killAllCrew), nameof(CheatToggles.killAllImps),
        nameof(CheatToggles.panicMode), nameof(CheatToggles.reloadConfig),
        nameof(CheatToggles.openConfig), nameof(CheatToggles.loadProfile),
        nameof(CheatToggles.saveProfile), nameof(CheatToggles.overloadAll),
        nameof(CheatToggles.overloadHost), nameof(CheatToggles.overloadCrew),
        nameof(CheatToggles.overloadImps), nameof(CheatToggles.overloadReset)
    };

    // These mirror a live round or need a fresh target. Preserve their normal
    // on/off shortcut behavior, but never reconstruct them from a saved file.
    private static readonly HashSet<string> LiveState = new(StringComparer.Ordinal)
    {
        nameof(CheatToggles.automaticTasks), nameof(CheatToggles.spectate),
        nameof(CheatToggles.commsSab), nameof(CheatToggles.elecSab),
        nameof(CheatToggles.reactorSab), nameof(CheatToggles.oxygenSab),
        nameof(CheatToggles.unfixableLights)
    };

    private static readonly HashSet<string> HostControls = new(StringComparer.Ordinal)
    {
        nameof(CheatToggles.killAll), nameof(CheatToggles.killAllCrew),
        nameof(CheatToggles.killAllImps), nameof(CheatToggles.killPlayer),
        nameof(CheatToggles.telekillPlayer), nameof(CheatToggles.ejectPlayer),
        nameof(CheatToggles.noKillCd), nameof(CheatToggles.killAnyone),
        nameof(CheatToggles.killVanished), nameof(CheatToggles.forceStartGame),
        nameof(CheatToggles.skipMeeting),
        nameof(CheatToggles.voteImmune), nameof(CheatToggles.noGameEnd),
        nameof(CheatToggles.showProtectMenu), nameof(CheatToggles.showRolesMenu),
        nameof(CheatToggles.noOptionsLimits), nameof(CheatToggles.unfixableLights),
        nameof(CheatToggles.kickVents), nameof(CheatToggles.forceRole)
    };

    public static bool IsMomentaryRequest(string id) => id != null && Requests.Contains(id);
    public static bool HasRestorableState(string id) => id != null && !IsMomentaryRequest(id) && !LiveState.Contains(id);
    public static bool RequiresHostActivation(string id) => id != null && HostControls.Contains(id);
    public static bool ShouldClearForProfileLoad(string id, bool hostOrPractice) =>
        IsMomentaryRequest(id) || id == nameof(CheatToggles.automaticTasks) ||
        (RequiresHostActivation(id) && !hostOrPractice);
    public static bool CanRestoreEnabled(string id, bool hostOrPractice) =>
        HasRestorableState(id) &&
        (!RequiresHostActivation(id) || hostOrPractice);
}
