#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


def extract_method(source: str, method_name: str) -> str:
    match = re.search(
        rf"^\s*(?:public|private|protected|internal)\s+(?:static\s+)?[^\n=;]+?\b{re.escape(method_name)}\s*\(",
        source,
        re.MULTILINE,
    )
    assert match is not None, f"method {method_name} not found"
    idx = match.start()
    brace = source.find("{", idx)
    assert brace != -1, f"method {method_name} has no body"
    depth = 0
    for pos in range(brace, len(source)):
        char = source[pos]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[brace : pos + 1]
    raise AssertionError(f"method {method_name} body not closed")


def test_chat_formatting_uses_cached_display_name():
    plugin = read("NoNameTagPlugin.cs")
    body = extract_method(plugin, "BuildFormattedChatMessage")
    assert "NameTagManager.GetFormattedPlayerName" in body
    assert "NameFormatter.FormatPlayerName" not in body


def test_damage_handler_resolves_attacker_player_once():
    plugin = read("NoNameTagPlugin.cs")
    body = extract_method(plugin, "OnDamagePlayerRequested")
    assert body.count("TryResolvePlayer(attackerSteamId)") == 1
    assert "ResolveWeaponName(parameters, attacker" in body
    assert "ResolveHitDistanceMeters(parameters, attacker" in body


def test_group_chat_does_not_wrap_each_client_as_unturned_player():
    plugin = read("NoNameTagPlugin.cs")
    body = extract_method(plugin, "OnPlayerChatted")
    assert "UnturnedPlayer.FromSteamPlayer(client)" not in body
    assert "client.player.quests.groupID" in body


def test_chat_sanitization_uses_single_pass_helper():
    plugin = read("NoNameTagPlugin.cs")
    body = extract_method(plugin, "BuildFormattedChatMessage")
    assert "RichTextSanitizer.SanitizeUntrustedPlayerText(message)" in body
    assert ".Replace(\"<\", \"\")" not in body


def test_chat_sender_steam_player_is_cached_per_message():
    plugin = read("NoNameTagPlugin.cs")
    body = extract_method(plugin, "OnPlayerChatted")
    assert "var senderSteamPlayer = player.SteamPlayer();" in body
    assert body.count("player.SteamPlayer()") == 1


def test_formatted_name_cache_cleanup_includes_formatted_only_entries():
    manager = read("Services/NameTagManager.cs")
    body = extract_method(manager, "CleanupCache")
    compact_body = re.sub(r"\s+", "", body)
    assert "_playerEffects.Keys.Concat(_formattedPlayerNames.Keys)" in compact_body
    assert ".Distinct()" in body


def test_formatted_name_cache_miss_does_not_read_stats():
    manager = read("Services/NameTagManager.cs")
    body = extract_method(manager, "GetFormattedPlayerName")
    assert "FormatPlayerNameWithoutStats" in body
    assert "FormatPlayerName(" not in body

# Stage 1 hardening contracts from docs/prd/nonametag-architecture-performance-hardening.md

def test_targeted_death_messages_send_to_recipient_not_sender_slot():
    death_service = read("Services/DeathMessageService.cs")
    body = extract_method(death_service, "SendToPlayer")
    assert "ChatManager.serverSendMessage(message, Color.white, null, steamPlayer" in body
    assert "ChatManager.serverSendMessage(message, Color.white, steamPlayer, null" not in body


def test_player_disconnect_releases_player_stats_cache():
    plugin = read("NoNameTagPlugin.cs")
    body = extract_method(plugin, "CleanupPlayerData")
    assert "PlayerStatsService?.ReleasePlayer(player.CSteamID.m_SteamID)" in body


def test_release_player_keeps_dirty_cache_when_flush_fails():
    stats_service = read("Services/PlayerStatsService.cs")
    body = extract_method(stats_service, "ReleasePlayer")
    assert "var flushed = FlushDirtyRecords()" in body
    assert "if (!flushed || _dirtySteamIds.ContainsKey(steamId))" in body
    assert body.index("return;") < body.index("_cachedStats.TryRemove(steamId, out _)")
    flush_body = extract_method(stats_service, "FlushDirtyRecords")
    assert "private bool FlushDirtyRecords()" in stats_service
    assert "return false;" in flush_body


def test_broadcast_delay_seconds_is_fully_removed():
    searched_paths = [
        "Models/BroadcastConfig.cs",
        "Utilities/ConfigValidator.cs",
        "NoNameTagConfiguration.cs",
        "NoNameTagConfiguration.example.xml",
        "NoNameTagConfiguration.xml",
        "README.md",
    ]
    for path in searched_paths:
        assert "DelaySeconds" not in read(path), f"DelaySeconds remains in {path}"


def test_legacy_configuration_file_is_removed():
    assert not (ROOT / "NoNameTag.configuration.xml").exists()


def test_checked_in_configuration_matches_authoritative_example():
    assert read("NoNameTagConfiguration.xml") == read("NoNameTagConfiguration.example.xml")


def test_unsupported_overhead_and_avatar_settings_are_removed_from_user_docs():
    searched_paths = ["NoNameTagConfiguration.example.xml", "NoNameTagConfiguration.xml", "README.md"]
    forbidden_terms = ["ApplyToNameTags", "AvatarSettings", "NameTagDisplayService", "头顶"]
    for path in searched_paths:
        text = read(path)
        for term in forbidden_terms:
            assert term not in text, f"{term} remains in {path}"


def test_untrusted_player_text_uses_shared_rich_text_sanitizer():
    plugin = read("NoNameTagPlugin.cs")
    welcome = read("Services/WelcomeMessageService.cs")
    death = read("Services/DeathMessageService.cs")
    assert (ROOT / "Utilities/RichTextSanitizer.cs").exists()
    assert "RichTextSanitizer.SanitizeUntrustedPlayerText(message)" in plugin
    assert "RichTextSanitizer.SanitizeUntrustedPlayerText(player.DisplayName" in welcome
    assert "RichTextSanitizer.SanitizeUntrustedPlayerText(playerName" in death


def test_trusted_config_text_keeps_rich_text_surface():
    welcome = read("Services/WelcomeMessageService.cs")
    welcome_body = extract_method(welcome, "SendWelcomeMessage")
    assert "RichTextSanitizer.SanitizeUntrustedPlayerText(player.DisplayName)" in welcome_body
    assert "RichTextSanitizer.SanitizeUntrustedPlayerText(welcomeConfig.Text)" not in welcome_body
    assert 'messageText.Replace("{", "<").Replace("}", ">")' in welcome_body


def test_color_values_match_documented_unity_color_names():
    validator = read("Utilities/ConfigValidator.cs")
    formatter = read("Utilities/NameFormatter.cs")
    example = read("NoNameTagConfiguration.example.xml")
    readme = read("README.md")
    for unsupported_name in ["orange", "purple"]:
        assert unsupported_name not in validator
        assert unsupported_name not in formatter
        assert unsupported_name not in example
        assert unsupported_name not in readme


def test_ci_and_release_workflows_run_stage1_tests_before_build_or_publish():
    ci_path = ROOT / ".github/workflows/ci.yml"
    assert ci_path.exists()
    ci = ci_path.read_text(encoding="utf-8")
    release = read(".github/workflows/manual-release.yml")
    for workflow_name, workflow in [("ci", ci), ("release", release)]:
        assert "python3 tests/performance_contract_tests.py" in workflow or "python tests/performance_contract_tests.py" in workflow, workflow_name
        assert "dotnet test" in workflow, workflow_name
        assert "dotnet build --configuration Release" in workflow, workflow_name


def test_stage1_version_is_1_0_2():
    assert "<Version>1.0.2</Version>" in read("NoNameTag.csproj")


if __name__ == "__main__":
    tests = [name for name in globals() if name.startswith("test_")]
    failures = []
    for test_name in tests:
        try:
            globals()[test_name]()
            print(f"PASS {test_name}")
        except Exception as exc:
            failures.append((test_name, exc))
            print(f"FAIL {test_name}: {exc}")
    if failures:
        raise SystemExit(1)
