# Groot AI Workflow — default setup

*Status: working configuration, validated 2026-08-19. The orchestrator (this DSH session) runs this loop; reviewers and implementer are AI subprocesses.*

## The loop (per task)

```
ORCHESTRATOR  = the DSH session (DeepSeek model)
  1. writes the brief (context, acceptance criteria, constraints, "don't commit")
  2. launches IMPLEMENTER
  3. launches REVIEWER(s)
  4. triages findings (dedupe, kill false positives) -> feeds back to implementer
  5. verifies (tests + builds + CI), commits/pushes, ships (APK -> the Windows box -> emulator)
  6. escalates only on real blockers or preference calls
```

## Role assignment (budget-aware)

| Role            | Model                | How                                              | Why |
|-----------------|----------------------|--------------------------------------------------|-----|
| **Implementer** | **Claude Max**       | headless `claude -p <brief> --dangerously-skip-permissions --output-format text --max-turns 80` (in-repo), or `subagent_claude_code` tool | biggest budget = workhorse; best multi-file feature work |
| **Primary reviewer** | **Codex**        | `codex exec --skip-git-repo-check`, or `subagent_codex` tool | read-only by design (auto-cancels approvals); catches behavioral bugs (races, time-loss) |
| **Secondary reviewer** | **GLM 5.3** via **opencode-go** | POST https://opencode.ai/zen/go/v1/chat/completions, key `OPENCODE_GO_API_KEY` in ~/.dsh/.credentials.yaml | cheapest thorough line-by-line walkthrough |
| **Orchestrator** | **DeepSeek** (DSH session) | this session | coordination, verification, git/deploy glue |

Budget notes:
- Claude Max is the spend — do NOT use it for reviewing.
- Codex: small sub, ~78k tokens per full review. Use on every change.
- GLM 5.3: cheapest reviewer; use on features, skip on tiny fixes.
- z.ai direct route (`ZAI_API_KEY`) is OUT OF BALANCE — use opencode-go for the same glm-5.3 model.

## Evidence (2026-08-19, the run-session feature)

- GLM 5.3: traced every line (90KB review) — caught CTS dispose race, parse validation gaps, test brittleness.
- Codex: found the pause time-loss, StartAsync token race, RunSession mutation exposure, catalog topology gaps (GLM missed these).
- Each reviewer had 1-2 false positives killed by the orchestrator (e.g. "missing data files" — they were already in the repo).
- Two reviewers complement each other; the orchestrator triage is what makes it pay off.

## Refinements that work

1. **Tell each reviewer what the previous one flagged** — they verify the fixes instead of re-reporting them.
2. **Reviewers get the diff + pointers to files, not the whole repo** — cheaper and more focused.
3. **Fix loop**: reviewer findings go back to Claude (the implementer), not the orchestrator — keeps implementation style consistent.

## Default per change size

- Small fix: Claude implements -> Codex reviews -> fix -> ship.
- Feature: Claude implements -> Codex + GLM 5.3 review -> Claude fixes -> ship.
- Claude flags risk: add a second Claude review pass before shipping.

## Infrastructure

- Harness: the VPS (this machine) runs the DSH web. Agent preset `code-claude` exposes `subagent_claude_code` + `subagent_codex` (both enabled; profile patch + ~/.dsh/.agent-presets/code-claude/).
- Android: emulator + SDK live on the Windows box (P:\Groot dev checkout; APK at P:\Groot\src\Groot.App\bin\Release\net10.0-android\). the VPS has no KVM — emulator stays on the Windows box.
- Repo sync: git bundle over scp (the Windows box has no GitHub creds): `git bundle create /tmp/groot-main.bundle main` -> scp -> `git pull <bundle> main`.
