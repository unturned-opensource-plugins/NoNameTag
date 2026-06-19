# NoNameTag Architecture and Performance Hardening PRD

## Problem Statement

NoNameTag has accumulated several types of risk: a likely correctness bug in targeted death messages, user-facing documentation that still describes unsupported overhead name tag behavior, configuration templates that expose removed or half-implemented options, and architectural coupling that keeps chat routing, death attribution, stats updates, and Rocket/Unturned runtime calls concentrated in the plugin entrypoint.

From a server operator's perspective, the plugin should be predictable, fast on hot paths, accurately documented, and safe to configure. From a maintainer's perspective, future changes should be testable without requiring a live Rocket/Unturned server whenever the behavior is pure or can be isolated behind a runtime seam.

## Solution

Harden NoNameTag in two implementation stages.

Stage 1 will fix correctness and configuration drift while improving test coverage and CI. It will repair targeted death message delivery, release player stats cache entries when players disconnect, remove or correct unsupported configuration surface, align color validation with supported color values, protect untrusted player text from rich text injection, update documentation around formatted names, and add CI/test gates. Stage 1 will be eligible for a `v1.0.2` patch release.

Stage 2 will pay down architecture debt by extracting chat handling behind a testable sender seam and unifying death attribution behind a resolver and shared context. It will keep NoNameTag as the historical project and command name, but internal behavior and tests should use the domain terms `Formatted Name`, `Display Effect`, `Death Attribution`, and `Broadcast Group`. Stage 2 will be eligible for a `v1.1.0` release.

Both stages must be implemented with TDD and must complete subagent-based Spec Review and Standards Review before release.

## User Stories

1. As a server operator, I want death messages configured as killer-only to go only to the killer, so that private death visibility settings are reliable.
2. As a server operator, I want death messages configured as victim-only to go only to the victim, so that private death visibility settings are reliable.
3. As a server operator, I want killer-and-victim-only death messages to reach exactly those participants, so that death visibility rules are predictable.
4. As a server operator, I want formatted names to be documented as message display names rather than overhead name tags, so that I do not configure an impossible feature.
5. As a server operator, I want the README to describe what the plugin actually does, so that setup and troubleshooting are not based on stale behavior.
6. As a server operator, I want one authoritative example configuration, so that I can copy a valid template without guessing which XML file is current.
7. As a server operator, I want legacy or invalid configuration files removed from the root of the project, so that I do not accidentally deploy a stale template.
8. As a server operator, I want the current server configuration sample to retain its server-specific message content, so that cleanup does not erase useful deployment examples.
9. As a server operator, I want unsupported overhead-name settings removed from examples, so that I do not expect NoNameTag to alter in-world player name tags.
10. As a server operator, I want unsupported avatar settings removed from examples, so that configuration only includes settings the plugin actually understands.
11. As a server operator, I want half-implemented broadcast message delay settings removed, so that every documented broadcast option has real behavior.
12. As a server operator, I want color settings to accept both hex colors and Unity color names consistently, so that existing values like `white` remain valid and documented.
13. As a server operator, I want configuration validation to match runtime formatting behavior, so that valid colors are not rejected and invalid colors are caught early.
14. As a player, I want my chat text to be displayed without allowing rich text injection, so that other players cannot alter message formatting through chat input.
15. As a player, I want player display names inserted into plugin messages to be sanitized, so that a player name cannot inject rich text into chat, welcome, leave, or death messages.
16. As a server operator, I want configured message text to remain trusted and rich-text capable, so that I can intentionally use formatting in server-authored messages.
17. As a server operator, I want player stats cache entries released when players disconnect, so that long-running servers do not retain every seen player's cached stats indefinitely.
18. As a player, I want disconnecting not to reset my current killstreak unless existing gameplay rules already do so, so that cache cleanup does not change combat semantics.
19. As a maintainer, I want dirty player stats flushed before cache release, so that disconnect cleanup does not lose kills or deaths.
20. As a maintainer, I want a CI workflow that runs tests and builds on pushes and pull requests, so that regressions are caught before release.
21. As a maintainer, I want the manual release workflow to run the same critical checks before publishing, so that released DLLs are built from verified code.
22. As a maintainer, I want TDD evidence for every behavior change, so that the implementation is driven by observable outcomes rather than speculative refactors.
23. As a maintainer, I want subagent Spec Review after each stage, so that the implementation can be checked against this PRD independently.
24. As a maintainer, I want subagent Standards Review after each stage, so that terminology, architecture decisions, and style do not drift.
25. As a maintainer, I want chat routing moved out of the plugin entrypoint, so that chat behavior has a focused module boundary.
26. As a maintainer, I want chat sending isolated behind a sender seam, so that local, group, and global routing can be tested without a live Rocket/Unturned server.
27. As a maintainer, I want local chat routing tests, so that only nearby recipients receive local messages.
28. As a maintainer, I want group chat routing tests, so that only members of the sender's group receive group messages.
29. As a maintainer, I want nil-group chat routing tests, so that a player without a group receives only their own group message.
30. As a maintainer, I want global chat routing tests, so that global messages preserve existing broadcast behavior.
31. As a maintainer, I want formatted name lookup in chat to use cached formatted names, so that chat does not become a stats/database read path.
32. As a maintainer, I want death attribution resolved once per death event, so that stats and death messages cannot disagree about the killer.
33. As a maintainer, I want direct death instigators to take precedence over delayed attribution, so that obvious kills are credited correctly.
34. As a maintainer, I want bleeding deaths to use bleed attribution when no direct instigator exists, so that delayed bleed deaths can still be credited.
35. As a maintainer, I want recent explosive or environmental attribution to be available for supported causes, so that delayed kill messages and stats remain accurate.
36. As a maintainer, I want self-kill cases excluded from killer credit, so that stats are not inflated by suicides or self-damage.
37. As a maintainer, I want weapon and distance metadata carried by the attribution context, so that death messages do not need to recompute attribution separately.
38. As a maintainer, I want NoNameTag to remain the project and command name for compatibility, so that existing operators do not lose familiar commands.
39. As a maintainer, I want user-facing language to use Formatted Name instead of NameTag for functionality, so that future changes do not reintroduce overhead-name confusion.
40. As a release manager, I want Stage 1 releasable as `v1.0.2`, so that correctness and configuration fixes can ship before larger architecture changes.
41. As a release manager, I want Stage 2 releasable as `v1.1.0`, so that the architecture hardening is communicated as a larger internal improvement.

## Implementation Decisions

- NoNameTag remains the project name and `/nametag` / `/nt` remain the administrator commands for compatibility.
- Functional language must use `Formatted Name` for player names shown in plugin-controlled messages. `NameTag` should not be used to imply overhead name tag behavior.
- Stage 1 focuses on correctness, configuration cleanup, cache release, sanitization, color consistency, documentation, tests, and CI.
- Stage 2 focuses on architecture: chat handling extraction with a sender seam, and unified death attribution with a resolver and shared context.
- The legacy configuration file with the old root element will be removed rather than deprecated in place.
- The authoritative configuration template will be regenerated around currently supported options.
- The current server configuration sample will keep server-specific message content but remove unsupported fields.
- Unsupported overhead-name settings will not be preserved for compatibility.
- Broadcast message delay will be removed completely from the model, validation, templates, and documentation rather than implemented or marked obsolete.
- Color values will consistently support both hex color values and Unity color names wherever configurable text colors are accepted.
- Server-authored configuration text is trusted message text and may contain supported rich text markup.
- Player-supplied or player-derived text is untrusted player text and must be sanitized before insertion into plugin-controlled rich text messages.
- Player stats cache release will occur on player disconnect.
- Player stats cache release must flush dirty stats before removing cached persisted stats for that player.
- Player stats cache release must not reset current killstreaks merely because a player disconnects.
- Stage 1 should add a release method to the player stats service boundary rather than exposing cache internals.
- Stage 2 chat handling should introduce a focused service for formatting, sanitizing, and routing chat messages.
- Stage 2 chat sending should be routed through a testable sender seam so routing decisions can be verified without Rocket/Unturned runtime calls.
- Stage 2 death handling should introduce a dedicated resolver that produces one death attribution context per death event.
- Player stats and death message formatting must consume the same death attribution context.
- The plugin entrypoint should move toward lifecycle and event wiring responsibilities rather than containing chat routing and death attribution logic.
- Review is part of the implementation definition of done: each stage must use separate subagents for Spec Review and Standards Review.
- Stage 1 release target is `v1.0.2` after tests, subagent review, and CI pass.
- Stage 2 release target is `v1.1.0` after tests, subagent review, and CI pass.

## Testing Decisions

- Use TDD for behavior changes: write one failing test, implement the smallest change to pass it, then refactor while green.
- Prefer behavior tests through public or module-level seams where feasible.
- Keep Python source-level contract tests for Rocket/Unity call shapes that are not practical to execute locally.
- Add C# tests for pure logic in Stage 1, especially text sanitization and configuration validation.
- Do not require a live Rocket/Unturned server for Stage 1 tests.
- Stage 1 should test that targeted death message delivery uses the intended recipient position in the chat send call shape.
- Stage 1 should test that player stats release is called on disconnect.
- Stage 1 should test that dirty stats are flushed before cached stats are released.
- Stage 1 should test that current killstreak is not cleared by stats cache release.
- Stage 1 should test that rich text control characters are removed from untrusted player text.
- Stage 1 should test that trusted configuration text remains able to contain rich text markup.
- Stage 1 should test that color validation accepts both hex color values and Unity color names.
- Stage 1 should test that unsupported configuration fields do not appear in the authoritative example template.
- Stage 1 should test or contract-check that broadcast delay support is fully removed.
- Stage 1 CI must run Python contract tests, C# tests, and a release build.
- Stage 1 release workflow must run critical tests before publishing.
- Stage 2 should add behavior tests for local chat routing, group chat routing, nil-group routing, and global chat routing through the sender seam.
- Stage 2 should test that chat uses cached formatted names rather than reading player stats on the hot path.
- Stage 2 should test direct instigator, bleeding attribution, recent attribution, missing killer, and self-kill death attribution scenarios.
- Stage 2 should test that player stats and death messages consume the same attribution result.
- Each stage must finish with subagent Spec Review and Standards Review, with findings resolved or explicitly accepted before release.

## Out of Scope

- Renaming the project, repository, plugin identity, or administrator command away from NoNameTag.
- Implementing overhead/in-world player name tag modification.
- Introducing a full fake Rocket/Unturned runtime.
- Replacing all concurrent collections with non-concurrent collections.
- Reworking the entire persistence model or replacing LiteDB.
- Implementing broadcast per-message delays.
- Implementing a full LRU or global player stats cache eviction policy beyond disconnect release.
- Publishing this PRD to GitHub Issues in this step.
- Releasing Stage 2 changes as part of Stage 1.

## Further Notes

- The domain glossary is recorded in `CONTEXT.md` and should be used by future tests, documentation, and review prompts.
- The staged architecture decision is recorded in an ADR.
- The existing architecture/performance review remains the supporting analysis for this PRD.
- The current local environment does not provide a .NET build toolchain; GitHub Actions is the authoritative build verification environment unless local tooling is installed later.
