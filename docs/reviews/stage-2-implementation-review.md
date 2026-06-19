# Stage 2 Implementation Review

Date: 2026-06-19
Target: `docs/prd/nonametag-architecture-performance-hardening.md` Stage 2 / `v1.1.0`
Baseline: `HEAD` before Stage 2 commit

## Local verification

- `python3 tests/performance_contract_tests.py`: PASS
- `git diff --check`: PASS
- `dotnet build NoNameTag.csproj --configuration Release --no-restore`: PASS
- `dotnet test tests/NoNameTag.Tests/NoNameTag.Tests.csproj --configuration Release --no-restore`: compiled project and test assembly, then aborted on macOS because the local net48 test host requires `mono`. GitHub Actions on `windows-latest` remains the release gate for full C# test execution.

## Spec Review

Subagent: `019ede93-ae98-7de1-91ad-00f8c1287832`

Blocking findings: none after staging all Stage 2 files.

Confirmed:

- `ChatMessageService` and `IChatMessageSender` were introduced.
- Local, group, nil-group, and global chat routing are covered by C# behavior tests.
- Chat formatting uses cached formatted-name lookup through a seam and does not reintroduce stats/database reads on the hot path.
- `NoNameTagPlugin` chat handling is reduced to event filtering, mode conversion, and participant snapshots.
- `DeathAttributionResolver` and `DeathAttributionContext` were introduced.
- Direct instigator, bleeding attribution, recent attribution, missing killer, and self-kill scenarios are tested.
- Stats and death messages consume the same attribution result.
- `DeathMessageService` no longer queries `DamageAttributionService` for attribution.
- Project version is `1.1.0`.

## Standards / Architecture Review

Subagent: `019ede93-d9a0-72e2-90e2-3f5c2601156f`

Initial blockers:

1. Chat models leaked runtime (`SteamPlayer`, Unity/SDG types) into the service/test seam.
2. `OnPlayerChatted` built participant snapshots before cheap early-return checks and scanned recipients for global chat.
3. `ChatMessageService` depended on `INameTagManager`, forcing pure chat tests to reference Rocket/Unturned types.

Resolutions:

- Replaced runtime-bearing chat models with pure `ChatMessageMode`, `ChatMessagePosition`, and `ulong` identifiers.
- Kept runtime conversion and `SteamPlayer` resolution inside `RuntimeChatMessageSender`.
- Added `ShouldHandleChatEvent` early-return before participant snapshots.
- Only Local/Group modes build recipient snapshots; Global/Say dispatch through broadcast-style delivery.
- Added `IFormattedNameProvider` so chat service tests no longer depend on `INameTagManager` or Rocket/Unturned types.
- Removed old death-message overload that could bypass the shared attribution context.

Remaining non-blocking note:

- `RuntimeChatMessageSender` resolves sender/recipient per dispatch. This is inside the runtime seam and not a stats/database regression; it may be optimized later with per-message caching if needed.

## Stage gate conclusion

Stage 2 is ready to commit and push for CI validation. Release must wait until GitHub Actions verifies C# tests and Release build successfully.
