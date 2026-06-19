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
    assert "SanitizeChatMessage(message)" in body
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
