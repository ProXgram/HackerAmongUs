# Guest Judge request checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/GuestJudgeRequest.Tests/GuestJudgeRequest.Tests.csproj -c Release
```

Compiles the guest experiment and policy with `GUEST_KILL_EXPERIMENT` and `JUDGE_ROLE_EXPERIMENT`. Checks cover the own-player Judge argument, one attempt per round, thrown calls, cancel/rebind, read-only UI paths, round identity, eligibility and kill-path exclusion.

Native methods are local recorders. These checks do not establish that a real host/server accepts a guest role request or avoids disconnecting the requester.
