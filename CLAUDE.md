@AGENTS.md

# Claude Code in this repo

AGENTS.md (imported above) is the rule set for every agent: Claude, Codex, GLM. Read it first.
What follows applies to Claude Code only, on this machine.

- Preview servers: `preview_start` with `groot-web` (5063) or `groot-gallery` (5200). When
  another session holds the port, use `groot-web-b` (5064) or `groot-gallery-b` (5201).
- `.claude/settings.json` runs `.claude/hooks/guard-generated.sh` before every Edit and Write
  and refuses generated files. If it fires, edit `GrootPalette.cs` and rebuild instead.
- The NuGet cache lives at `D:\DataStorage\.nuget` (set through `NUGET_PACKAGES`), not under
  `~/.nuget`. Resolve it with `dotnet nuget locals global-packages --list` instead of guessing.
- Deleting files or folders: `Move-Item` to the session scratchpad. The shell delete guard on
  this machine blocks `Remove-Item` and `rmdir` with false positives.
- `Groot.App` builds on this machine: `dotnet build src/Groot.App -f net10.0-android` takes a
  few minutes; run it in the background and keep working.
