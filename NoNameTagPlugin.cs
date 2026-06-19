using Emqo.NoNameTag.Services;
using Emqo.NoNameTag.Utilities;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag
{
    public class NoNameTagPlugin : RocketPlugin<NoNameTagConfiguration>
    {
        public static NoNameTagPlugin Instance { get; private set; }

        public IPermissionService PermissionService { get; private set; }
        public INameTagManager NameTagManager { get; private set; }
        public IBroadcastService BroadcastService { get; private set; }
        public IPlayerStatsService PlayerStatsService { get; private set; }
        public IDamageAttributionService DamageAttributionService { get; private set; }
        public IDeathAttributionResolver DeathAttributionResolver { get; private set; }
        private ChatMessageService ChatMessageService { get; set; }

        protected override void Load()
        {
            Instance = this;
            Logger.DebugEnabled = Configuration.Instance.DebugMode;

            try
            {
                if (!ConfigValidator.ValidateConfiguration(Configuration.Instance, out var configError))
                {
                    Logger.Warning($"Configuration validation failed: {configError}");
                }

                InitializeServices();
                RegisterEventHandlers();
                RefreshAllDisplays();
                BroadcastService?.StartAllBroadcasts();

                Logger.Info($"{Name} {Assembly.GetName().Version.ToString(3)} has been loaded!");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to load plugin");
            }
        }

        protected override void Unload()
        {
            try
            {
                UnregisterEventHandlers();
                BroadcastService?.Dispose();
                DamageAttributionService?.ClearAll();
                PlayerStatsService?.Dispose();
                NameTagManager?.ClearAll();
                PermissionService?.ClearAllCache();
                UnityMainThreadDispatcher.DestroyInstance();
                Instance = null;
                Logger.Info($"{Name} has been unloaded!");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error during plugin unload");
            }
        }

        private void RegisterEventHandlers()
        {
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerChatted += OnPlayerChatted;
            DamageTool.damagePlayerRequested += OnDamagePlayerRequested;
            PlayerLife.OnTellBleeding_Global += OnPlayerBleedingUpdated;
            PlayerLife.OnRevived_Global += OnPlayerRevived;
            PlayerLife.onPlayerDied += OnPlayerDied;
        }

        private void UnregisterEventHandlers()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerChatted -= OnPlayerChatted;
            DamageTool.damagePlayerRequested -= OnDamagePlayerRequested;
            PlayerLife.OnTellBleeding_Global -= OnPlayerBleedingUpdated;
            PlayerLife.OnRevived_Global -= OnPlayerRevived;
            PlayerLife.onPlayerDied -= OnPlayerDied;
        }

        private void RefreshAllDisplays()
        {
            NameTagManager.RefreshAllPlayers();
        }

        private void InitializeServices()
        {
            PermissionService = new PermissionService(Configuration.Instance);
            PlayerStatsService = new PlayerStatsService(Configuration.Instance.StatsSettings);
            DamageAttributionService = new DamageAttributionService(Configuration.Instance.StatsSettings);
            DeathAttributionResolver = new DeathAttributionResolver(DamageAttributionService);
            NameTagManager = new NameTagManager(Configuration.Instance, PermissionService);
            ChatMessageService = new ChatMessageService(Configuration.Instance, NameTagManager, new RuntimeChatMessageSender());
            BroadcastService = new BroadcastService(Configuration.Instance, NameTagManager);

            Logger.Debug("All services initialized");
        }

        public void ReloadServices()
        {
            try
            {
                Logger.DebugEnabled = Configuration.Instance.DebugMode;
                Configuration.Instance.ClearCache();
                BroadcastService?.Dispose();
                DamageAttributionService?.ClearAll();
                PlayerStatsService?.Dispose();
                NameTagManager?.ClearAll();
                PermissionService?.ClearAllCache();
                InitializeServices();
                RefreshAllDisplays();
                BroadcastService?.StartAllBroadcasts();
                Logger.Info("Services reloaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to reload services");
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (!IsValidPlayerConnection(player)) return;

            try
            {
                ApplyPlayerEffects(player);
                LogPlayerConnection(player);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error handling player connect: {player?.DisplayName}");
            }
        }

        private bool IsValidPlayerConnection(UnturnedPlayer player)
        {
            return Configuration.Instance.Enabled
                && player != null
                && player.Player != null
                && player.CSteamID != CSteamID.Nil;
        }

        private void ApplyPlayerEffects(UnturnedPlayer player)
        {
            PlayerStatsService?.EnsurePlayer(player.CSteamID.m_SteamID);
            NameTagManager.ApplyDisplayEffect(player);
            BroadcastService?.SendWelcomeMessage(player);
        }

        private void LogPlayerConnection(UnturnedPlayer player)
        {
            Logger.Debug($"Player connected: {player.DisplayName}");
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null) return;

            try
            {
                CleanupPlayerData(player);
                LogPlayerDisconnection(player);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error handling player disconnect: {player?.DisplayName}");
            }
        }

        private void CleanupPlayerData(UnturnedPlayer player)
        {
            BroadcastService?.SendLeaveMessage(player);
            DamageAttributionService?.ClearVictim(player.CSteamID.m_SteamID);
            PlayerStatsService?.ReleasePlayer(player.CSteamID.m_SteamID);
            NameTagManager.RemoveDisplayEffect(player);
            PermissionService?.ClearPlayerCache(player.CSteamID.m_SteamID);
        }

        private void LogPlayerDisconnection(UnturnedPlayer player)
        {
            Logger.Debug($"Player disconnected: {player.DisplayName}");
        }

        private void OnPlayerChatted(UnturnedPlayer player, ref Color color, string message, EChatMode chatMode, ref bool cancel)
        {
            if (!ShouldHandleChatEvent(player, message, cancel))
                return;

            try
            {
                var mode = ToChatMessageMode(chatMode);
                var handled = ChatMessageService?.HandleChat(new ChatMessageRequest
                {
                    Sender = CreateChatParticipant(player),
                    Recipients = RequiresRecipientSnapshot(mode) ? GetChatParticipants() : null,
                    Message = message,
                    ChatMode = mode,
                    IsCanceled = cancel
                }) == true;

                if (!handled)
                    return;

                cancel = true;
                Logger.Debug($"Chat message - Player: {player?.DisplayName}, SteamID: {player?.CSteamID.m_SteamID}, ChatMode: {chatMode}", LogCategory.Plugin);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error handling chat message from {player?.DisplayName}");
            }
        }

        private bool ShouldHandleChatEvent(UnturnedPlayer player, string message, bool cancel)
        {
            return Configuration.Instance.Enabled
                && Configuration.Instance.ApplyToChatMessages
                && player != null
                && !cancel
                && !string.IsNullOrEmpty(message)
                && !message.StartsWith("/", StringComparison.Ordinal);
        }

        private static bool RequiresRecipientSnapshot(ChatMessageMode mode)
        {
            return mode == ChatMessageMode.Local || mode == ChatMessageMode.Group;
        }

        private static ChatMessageMode ToChatMessageMode(EChatMode chatMode)
        {
            switch (chatMode)
            {
                case EChatMode.LOCAL:
                    return ChatMessageMode.Local;
                case EChatMode.GROUP:
                    return ChatMessageMode.Group;
                case EChatMode.SAY:
                    return ChatMessageMode.Say;
                default:
                    return ChatMessageMode.Global;
            }
        }

        private ChatMessageParticipant CreateChatParticipant(UnturnedPlayer player)
        {
            if (player == null)
                return null;

            return new ChatMessageParticipant
            {
                SteamId = player.CSteamID.m_SteamID,
                DisplayName = player.DisplayName,
                GroupId = player.Player?.quests == null ? 0UL : player.Player.quests.groupID.m_SteamID,
                Position = ToChatPosition(player.Player?.transform?.position ?? Vector3.zero)
            };
        }

        private static System.Collections.Generic.IReadOnlyList<ChatMessageParticipant> GetChatParticipants()
        {
            var participants = new System.Collections.Generic.List<ChatMessageParticipant>();
            foreach (var client in Provider.clients)
            {
                if (client?.player == null)
                    continue;

                var steamId = client.player.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
                if (steamId == 0)
                    continue;

                participants.Add(new ChatMessageParticipant
                {
                    SteamId = steamId,
                    DisplayName = client.player.channel?.owner?.playerID.characterName,
                    GroupId = client.player.quests == null ? 0UL : client.player.quests.groupID.m_SteamID,
                    Position = ToChatPosition(client.player.transform?.position ?? Vector3.zero)
                });
            }

            return participants;
        }


        private static ChatMessagePosition ToChatPosition(Vector3 position)
        {
            return new ChatMessagePosition(position.x, position.y, position.z);
        }

        private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (!Configuration.Instance.Enabled) return;

            try
            {
                var victimSteamId = sender?.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
                var attribution = DeathAttributionResolver?.Resolve(new DeathAttributionRequest
                {
                    VictimSteamId = victimSteamId,
                    InstigatorSteamId = instigator.m_SteamID,
                    Cause = cause
                }) ?? DeathAttributionContext.Empty;

                if (victimSteamId != 0)
                {
                    PlayerStatsService?.RecordPlayerDeath(victimSteamId, attribution.KillerSteamId ?? 0);
                    RefreshCachedDisplayNames(victimSteamId, attribution.KillerSteamId);
                }

                BroadcastService?.HandlePlayerDeath(sender, cause, limb, instigator, attribution);

                if (victimSteamId != 0)
                {
                    DamageAttributionService?.ClearVictim(victimSteamId);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error handling player death");
            }
        }

        private void OnDamagePlayerRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (!Configuration.Instance.Enabled)
                return;

            if (!shouldAllow)
                return;

            try
            {
                var victim = parameters.player;
                if (victim == null)
                    return;

                var attackerSteamId = parameters.killer.m_SteamID;
                var victimSteamId = victim.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
                var bleedingModifier = parameters.bleedingModifier;
                if (attackerSteamId == 0 || victimSteamId == 0 || attackerSteamId == victimSteamId)
                {
                    return;
                }

                if (IsAttributionTrackableCause(parameters.cause))
                {
                    var attacker = TryResolvePlayer(attackerSteamId);
                    var weaponName = ResolveWeaponName(parameters, attacker);
                    var distanceMeters = ResolveHitDistanceMeters(parameters, attacker);
                    DamageAttributionService?.RecordAttributedHit(attackerSteamId, victimSteamId, parameters.cause, weaponName, distanceMeters);

                    var isAlreadyBleeding = victim.life?.isBleeding == true;
                    if (bleedingModifier != DamagePlayerParameters.Bleeding.Never
                        && bleedingModifier != DamagePlayerParameters.Bleeding.Heal
                        && IsBleedTrackableCause(parameters.cause)
                        && (bleedingModifier == DamagePlayerParameters.Bleeding.Always || isAlreadyBleeding))
                    {
                        DamageAttributionService?.HandleBleedingStateChanged(victimSteamId, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Skipped damage attribution update: {ex.Message}", LogCategory.Plugin);
            }
        }

        private void OnPlayerBleedingUpdated(PlayerLife playerLife)
        {
            if (!Configuration.Instance.Enabled || playerLife == null)
                return;

            try
            {
                var steamId = playerLife.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
                if (steamId == 0)
                    return;

                DamageAttributionService?.HandleBleedingStateChanged(steamId, playerLife.isBleeding);
                DamageAttributionService?.ClearExpired();
            }
            catch (Exception ex)
            {
                Logger.Debug($"Skipped bleeding attribution update: {ex.Message}", LogCategory.Plugin);
            }
        }

        private void OnPlayerRevived(PlayerLife playerLife)
        {
            var steamId = playerLife?.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
            if (steamId == 0)
                return;

            DamageAttributionService?.ClearVictim(steamId);
        }

        private static bool IsAttributionTrackableCause(EDeathCause cause)
        {
            switch (cause)
            {
                case EDeathCause.GUN:
                case EDeathCause.MELEE:
                case EDeathCause.PUNCH:
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL:
                case EDeathCause.GRENADE:
                case EDeathCause.MISSILE:
                case EDeathCause.CHARGE:
                case EDeathCause.SPLASH:
                case EDeathCause.SHRED:
                case EDeathCause.LANDMINE:
                case EDeathCause.SENTRY:
                case EDeathCause.KILL:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBleedTrackableCause(EDeathCause cause)
        {
            return IsAttributionTrackableCause(cause);
        }

        private void RefreshCachedDisplayNames(ulong victimSteamId, ulong? killerSteamId)
        {
            RefreshCachedDisplayName(victimSteamId);

            if (killerSteamId.HasValue && killerSteamId.Value != 0 && killerSteamId.Value != victimSteamId)
                RefreshCachedDisplayName(killerSteamId.Value);
        }

        private void RefreshCachedDisplayName(ulong steamId)
        {
            var player = TryResolvePlayer(steamId);
            if (player != null)
                NameTagManager?.RefreshPlayer(player);
        }

        private static string ResolveWeaponName(DamagePlayerParameters parameters, UnturnedPlayer attacker)
        {
            if (attacker?.Player?.equipment?.asset is ItemAsset itemAsset && !string.IsNullOrWhiteSpace(itemAsset.itemName))
            {
                return itemAsset.itemName;
            }

            return GetFallbackWeaponName(parameters.cause);
        }

        private static int? ResolveHitDistanceMeters(DamagePlayerParameters parameters, UnturnedPlayer attacker)
        {
            var victim = parameters.player;
            if (attacker?.Player?.transform == null || victim?.transform == null)
            {
                return null;
            }

            try
            {
                var distance = Vector3.Distance(attacker.Player.transform.position, victim.transform.position);
                if (float.IsNaN(distance) || float.IsInfinity(distance))
                {
                    return null;
                }

                return Mathf.RoundToInt(distance);
            }
            catch
            {
                return null;
            }
        }

        private static UnturnedPlayer TryResolvePlayer(ulong steamId)
        {
            if (steamId == 0)
            {
                return null;
            }

            try
            {
                return UnturnedPlayer.FromCSteamID(new CSteamID(steamId));
            }
            catch
            {
                return null;
            }
        }

        private static string GetFallbackWeaponName(EDeathCause cause)
        {
            switch (cause)
            {
                case EDeathCause.GUN: return "枪械";
                case EDeathCause.MELEE: return "近战";
                case EDeathCause.PUNCH: return "拳击";
                case EDeathCause.GRENADE: return "手雷";
                case EDeathCause.MISSILE: return "导弹";
                case EDeathCause.CHARGE: return "炸药";
                case EDeathCause.LANDMINE: return "地雷";
                case EDeathCause.SPLASH: return "爆炸";
                case EDeathCause.SHRED: return "陷阱";
                case EDeathCause.SENTRY: return "哨戒炮";
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL: return "载具";
                case EDeathCause.KILL: return "管理员处决";
                default: return null;
            }
        }
    }
}
