# Task Walker console and progress checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/TaskWalker.Tests/TaskWalker.Tests.csproj --configuration Release
```

Links the Task Walker handler with game stubs. Checks cover assigned-task selection and handoff, console eligibility, offsite refusal, later-stage destinations, timer waits, pause/ownership gates, native failures, scene replacement and Freeplay. Missing assignments, consoles, role data or timer prefabs remain waits rather than completion signals.

Approach recovery checks nearby legal points, alternate consoles, bounded retries and task use only after arrival. Timer tasks use configured game durations; the handler advances task progress at eligible consoles rather than reproducing each minigame's mouse gestures.

The optional `NativeReview` utility reads installed Xbox binary metadata using LibCpp2IL and Iced. Neither it nor the stub tests execute native game code or establish online task acceptance.
