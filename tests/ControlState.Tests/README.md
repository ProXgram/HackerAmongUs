# Control state and profile checks

`dotnet run --project tests/ControlState.Tests -c Release`

Links the actual profile writer/reader, control policy and keyboard listener.
The suite checks independently audited one-shot actions, old true-valued
profiles, partial/missing profiles, preserved field IDs and shortcuts,
host/guest/practice boundaries, focus/menu/chat/panic, and F8 cleanup.
Temporary files are confined to a unique test directory. It does not load or
modify Among Us, its saved profile, or any network session.
