using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 统一广播服务接口
    /// 处理死亡消息和轮播公告
    /// </summary>
    public interface IBroadcastService
    {
        /// <summary>
        /// 处理玩家死亡事件
        /// </summary>
        void HandlePlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator);

        /// <summary>
        /// 启动所有广播组
        /// </summary>
        void StartAllBroadcasts();

        /// <summary>
        /// 停止所有广播组
        /// </summary>
        void StopAllBroadcasts();

        /// <summary>
        /// 重新加载广播配置
        /// </summary>
        void ReloadBroadcasts();

        /// <summary>
        /// 获取所有广播组的运行状态
        /// </summary>
        Dictionary<string, bool> GetBroadcastStatus();

        /// <summary>
        /// 手动发送广播消息
        /// </summary>
        void SendBroadcast(string groupName, string message);
    }

    /// <summary>
    /// 统一广播服务实现
    /// 同时管理死亡消息和轮播公告
    /// </summary>
    public class BroadcastService : IBroadcastService
    {
        private readonly NoNameTagConfiguration _config;
        private readonly NameTagManager _nameTagManager;

        private readonly Dictionary<string, Timer> _broadcastTimers;
        private readonly Dictionary<string, int> _currentMessageIndices;
        private readonly object _timerLock = new object();

        public BroadcastService(NoNameTagConfiguration config, NameTagManager nameTagManager)
        {
            _config = config;
            _nameTagManager = nameTagManager;

            _broadcastTimers = new Dictionary<string, Timer>();
            _currentMessageIndices = new Dictionary<string, int>();
        }

        /// <summary>
        /// 处理玩家死亡事件（增强版，支持可见性控制）
        /// </summary>
        public void HandlePlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (!_config.Enabled || _config.Broadcast?.DeathMessage == null || !_config.Broadcast.DeathMessage.Enabled)
                return;

            try
            {
                if (sender == null || sender.player == null)
                    return;

                var victim = UnturnedPlayer.FromPlayer(sender.player);
                if (victim == null) return;

                UnturnedPlayer killer = null;

                // 尝试从 instigator 获取 killer
                if (instigator != CSteamID.Nil && instigator.m_SteamID != 0)
                {
                    try
                    {
                        killer = UnturnedPlayer.FromCSteamID(instigator);
                        // 如果 killer 为 null 或者是受害者自己，则认为是环境死亡
                        if (killer != null)
                        {
                            try
                            {
                                if (killer.CSteamID.m_SteamID == victim.CSteamID.m_SteamID)
                                {
                                    killer = null;
                                }
                            }
                            catch
                            {
                                // 如果无法比较 SteamID，则认为 killer 无效
                                killer = null;
                            }
                        }
                    }
                    catch
                    {
                        // 如果无法获取 killer，则认为是环境死亡
                        killer = null;
                    }
                }

                var message = FormatDeathMessage(victim, killer, cause);
                if (!string.IsNullOrEmpty(message))
                {
                    BroadcastDeathMessage(message, victim, killer);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error handling player death", LogCategory.DeathMessage);
            }
        }

        /// <summary>
        /// 格式化死亡消息
        /// </summary>
        private string FormatDeathMessage(UnturnedPlayer victim, UnturnedPlayer killer, EDeathCause cause)
        {
            // 添加 null 检查
            if (victim == null)
                return null;

            try
            {
                var deathConfig = _config.Broadcast.DeathMessage;
                if (deathConfig == null)
                    return null;

                string format;

                // 安全地检查 killer
                bool isSelfKill = false;
                bool isPlayerKill = false;

                if (killer != null)
                {
                    try
                    {
                        isSelfKill = killer.CSteamID == victim.CSteamID;
                        isPlayerKill = !isSelfKill;
                    }
                    catch
                    {
                        isPlayerKill = true;
                    }
                }

                if (isSelfKill)
                {
                    format = deathConfig.SelfKillFormat;
                }
                else if (isPlayerKill)
                {
                    format = deathConfig.Format;
                }
                else
                {
                    format = GetFormatByCause(cause, deathConfig);
                }

                var victimName = FormatPlayerName(victim);
                var killerName = killer != null ? FormatPlayerName(killer) : "";

                // 使用 StringBuilder 优化字符串替换
                var replacements = new Dictionary<string, string>
                {
                    { "{victim}", victimName },
                    { "{killer}", killerName }
                };

                return ReplaceMultiple(format, replacements);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error in FormatDeathMessage", LogCategory.DeathMessage);
                return null;
            }
        }

        /// <summary>
        /// 使用 StringBuilder 进行多个字符串替换，性能更优
        /// </summary>
        private string ReplaceMultiple(string text, Dictionary<string, string> replacements)
        {
            var sb = new System.Text.StringBuilder(text);
            foreach (var kvp in replacements)
            {
                sb.Replace(kvp.Key, kvp.Value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 根据死亡原因获取对应的格式字符串
        /// </summary>
        private string GetFormatByCause(EDeathCause cause, DeathMessageConfig config)
        {
            switch (cause)
            {
                case EDeathCause.BLEEDING:
                    return config.BleedingFormat;
                case EDeathCause.ZOMBIE:
                    return config.ZombieFormat;
                case EDeathCause.ANIMAL:
                    return config.AnimalFormat;
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL:
                    return config.VehicleFormat;
                case EDeathCause.BONES:
                    return config.FallFormat;
                case EDeathCause.BREATH:
                    return config.DrownFormat;
                case EDeathCause.FREEZING:
                    return config.FreezeFormat;
                case EDeathCause.BURNING:
                case EDeathCause.BURNER:
                    return config.BurnFormat;
                case EDeathCause.FOOD:
                    return config.FoodFormat;
                case EDeathCause.WATER:
                    return config.WaterFormat;
                case EDeathCause.INFECTION:
                    return config.InfectionFormat;
                case EDeathCause.SUICIDE:
                    return config.SelfKillFormat;
                default:
                    return config.DefaultFormat;
            }
        }

        /// <summary>
        /// 格式化玩家名称（带颜色）
        /// </summary>
        private string FormatPlayerName(UnturnedPlayer player)
        {
            if (player == null)
                return "Unknown";

            try
            {
                // 尝试获取玩家名称，如果失败则使用备用名称
                string playerName = "Unknown";
                try
                {
                    playerName = player.DisplayName ?? player.CharacterName ?? "Unknown";
                }
                catch
                {
                    // 如果 DisplayName 失败，尝试直接访问 Player 对象
                    try
                    {
                        if (player.Player != null && player.Player.channel != null)
                        {
                            playerName = player.Player.channel.owner.playerID.characterName ?? "Unknown";
                        }
                    }
                    catch
                    {
                        playerName = "Unknown";
                    }
                }

                // 尝试获取权限组效果
                try
                {
                    var group = _nameTagManager.GetPlayerEffect(player.CSteamID.m_SteamID);
                    if (group?.DisplayEffect != null)
                    {
                        return NameFormatter.FormatColoredName(playerName, group.DisplayEffect);
                    }
                }
                catch
                {
                    // 忽略权限组获取错误
                }

                return playerName;
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 广播死亡消息（支持可见性控制）
        /// </summary>
        private void BroadcastDeathMessage(string message, UnturnedPlayer victim, UnturnedPlayer killer)
        {
            var visibility = _config.Broadcast.DeathMessage.Visibility;

            switch (visibility)
            {
                case DeathMessageVisibility.All:
                    // 全员可见
                    ChatManager.serverSendMessage(message, Color.white, null, null, EChatMode.GLOBAL, null, true);
                    break;

                case DeathMessageVisibility.KillerOnly:
                    // 仅击杀者可见
                    if (killer != null)
                    {
                        var killerSteamPlayer = GetSteamPlayer(killer.CSteamID);
                        if (killerSteamPlayer != null)
                        {
                            ChatManager.serverSendMessage(message, Color.white, killerSteamPlayer, null, EChatMode.SAY, null, true);
                        }
                    }
                    break;

                case DeathMessageVisibility.VictimOnly:
                    // 仅被杀者可见
                    if (victim != null)
                    {
                        var victimSteamPlayer = GetSteamPlayer(victim.CSteamID);
                        if (victimSteamPlayer != null)
                        {
                            ChatManager.serverSendMessage(message, Color.white, victimSteamPlayer, null, EChatMode.SAY, null, true);
                        }
                    }
                    break;

                case DeathMessageVisibility.KillerAndVictimOnly:
                    // 击杀者和被杀者可见
                    if (killer != null)
                    {
                        var killerSteamPlayer = GetSteamPlayer(killer.CSteamID);
                        if (killerSteamPlayer != null)
                        {
                            ChatManager.serverSendMessage(message, Color.white, killerSteamPlayer, null, EChatMode.SAY, null, true);
                        }
                    }
                    if (victim != null)
                    {
                        var victimSteamPlayer = GetSteamPlayer(victim.CSteamID);
                        if (victimSteamPlayer != null)
                        {
                            ChatManager.serverSendMessage(message, Color.white, victimSteamPlayer, null, EChatMode.SAY, null, true);
                        }
                    }
                    break;
            }

            Logger.Debug($"Death message broadcasted: {message} (Visibility: {visibility})", LogCategory.DeathMessage);
        }

        /// <summary>
        /// 根据 CSteamID 获取 SteamPlayer
        /// </summary>
        private SteamPlayer GetSteamPlayer(CSteamID steamId)
        {
            foreach (var client in Provider.clients)
            {
                if (client.playerID.steamID == steamId)
                {
                    return client;
                }
            }
            return null;
        }

        /// <summary>
        /// 启动所有广播组
        /// </summary>
        public void StartAllBroadcasts()
        {
            if (!_config.Enabled || _config.Broadcast?.BroadcastGroups == null)
                return;

            foreach (var group in _config.BroadcastGroups)
            {
                if (group.Enabled && group.Messages != null && group.Messages.Count > 0)
                {
                    StartBroadcastGroup(group);
                }
            }
        }

        /// <summary>
        /// 启动单个广播组
        /// </summary>
        private void StartBroadcastGroup(BroadcastGroupConfig group)
        {
            lock (_timerLock)
            {
                // 如果已存在，先停止
                if (_broadcastTimers.ContainsKey(group.Name))
                {
                    StopBroadcastGroup(group.Name);
                }

                // 初始化消息索引
                _currentMessageIndices[group.Name] = 0;

                // 创建定时器（转换为毫秒）
                var timer = new Timer(group.RotationInterval * 1000.0);
                timer.Elapsed += (sender, e) => OnBroadcastTimerElapsed(group);
                timer.AutoReset = true;
                timer.Start();

                _broadcastTimers[group.Name] = timer;

                Logger.Debug($"Broadcast group '{group.Name}' started with interval {group.RotationInterval}s", LogCategory.Plugin);
            }
        }

        /// <summary>
        /// 定时器回调：发送广播消息
        /// </summary>
        private void OnBroadcastTimerElapsed(BroadcastGroupConfig group)
        {
            try
            {
                if (group.Messages == null || group.Messages.Count == 0)
                    return;

                lock (_timerLock)
                {
                    if (!_currentMessageIndices.ContainsKey(group.Name))
                        return;

                    var currentIndex = _currentMessageIndices[group.Name];
                    var message = group.Messages[currentIndex];

                    // 准备消息文本
                    var messageText = message.Message;

                    // 替换消息中的变量并转换富文本格式
                    messageText = ReplaceVariables(messageText);
                    messageText = messageText.Replace("{", "<").Replace("}", ">");

                    // 准备头像 URL
                    string avatarUrl = message.Avatar;
                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        // 替换头像 URL 中的变量
                        avatarUrl = ReplaceVariables(avatarUrl);
                    }

                    // 发送到主线程执行
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        try
                        {
                            ChatManager.serverSendMessage(
                                messageText,
                                Color.white,
                                null,
                                null,
                                EChatMode.GLOBAL,
                                avatarUrl,
                                true
                            );

                            if (_config.DebugMode)
                            {
                                Logger.Debug($"Broadcast '{group.Name}': {messageText}", LogCategory.Plugin);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception(ex, $"Error sending broadcast message for group '{group.Name}'", LogCategory.Plugin);
                        }
                    });

                    // 更新索引到下一条消息
                    _currentMessageIndices[group.Name] = (currentIndex + 1) % group.Messages.Count;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error in broadcast timer for group '{group.Name}'", LogCategory.Plugin);
            }
        }

        /// <summary>
        /// 停止所有广播组
        /// </summary>
        public void StopAllBroadcasts()
        {
            lock (_timerLock)
            {
                foreach (var kvp in _broadcastTimers)
                {
                    var timer = kvp.Value;
                    timer.Stop();
                    timer.Dispose();
                    Logger.Debug($"Broadcast group '{kvp.Key}' stopped", LogCategory.Plugin);
                }

                _broadcastTimers.Clear();
                _currentMessageIndices.Clear();
            }
        }

        /// <summary>
        /// 停止单个广播组
        /// </summary>
        private void StopBroadcastGroup(string groupName)
        {
            lock (_timerLock)
            {
                if (_broadcastTimers.TryGetValue(groupName, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _broadcastTimers.Remove(groupName);
                    Logger.Debug($"Broadcast group '{groupName}' stopped", LogCategory.Plugin);
                }

                _currentMessageIndices.Remove(groupName);
            }
        }

        /// <summary>
        /// 重新加载广播配置
        /// </summary>
        public void ReloadBroadcasts()
        {
            StopAllBroadcasts();
            StartAllBroadcasts();
            Logger.Info("Broadcasts reloaded", LogCategory.Plugin);
        }

        /// <summary>
        /// 获取所有广播组的运行状态
        /// </summary>
        public Dictionary<string, bool> GetBroadcastStatus()
        {
            var status = new Dictionary<string, bool>();

            if (_config.Broadcast?.BroadcastGroups == null)
                return status;

            foreach (var group in _config.BroadcastGroups)
            {
                lock (_timerLock)
                {
                    status[group.Name] = _broadcastTimers.ContainsKey(group.Name);
                }
            }

            return status;
        }

        /// <summary>
        /// 替换变量（如 {server_icon}）
        /// </summary>
        private string ReplaceVariables(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.Replace("{server_icon}", Provider.configData.Browser.Icon);
            text = text.Replace("{server_thumbnail}", Provider.configData.Browser.Thumbnail);
            text = text.Replace("{server_name}", Provider.serverName);
            text = text.Replace("{server_players}", Provider.clients.Count.ToString("N0"));
            text = text.Replace("{server_maxplayers}", Provider.maxPlayers.ToString("N0"));
            text = text.Replace("{server_map}", Level.info?.name ?? string.Empty);
            text = text.Replace("{server_mode}", Provider.mode.ToString());

            return text;
        }

        /// <summary>
        /// 手动发送广播消息
        /// </summary>
        public void SendBroadcast(string groupName, string message)
        {
            var group = _config.BroadcastGroups.FirstOrDefault(g => g.Name == groupName);
            if (group == null)
            {
                Logger.Warning($"Broadcast group '{groupName}' not found", LogCategory.Plugin);
                return;
            }

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ChatManager.serverSendMessage(message, Color.white, null, null, EChatMode.GLOBAL, null, true);
            });

            Logger.Debug($"Manual broadcast sent to group '{groupName}': {message}", LogCategory.Plugin);
        }
    }
}
