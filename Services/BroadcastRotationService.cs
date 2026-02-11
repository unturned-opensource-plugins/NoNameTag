using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 广播轮播服务
    /// </summary>
    internal class BroadcastRotationService : IDisposable
    {
        private readonly NoNameTagConfiguration _config;
        private readonly Dictionary<string, Timer> _broadcastTimers;
        private readonly Dictionary<string, int> _currentMessageIndices;
        private readonly object _timerLock = new object();
        private bool _disposed;

        public BroadcastRotationService(NoNameTagConfiguration config)
        {
            _config = config;
            _broadcastTimers = new Dictionary<string, Timer>();
            _currentMessageIndices = new Dictionary<string, int>();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopAll();
        }

        public void StartAll()
        {
            if (!_config.Enabled || _config.Broadcast?.BroadcastGroups == null)
                return;

            foreach (var group in _config.Broadcast.BroadcastGroups)
            {
                if (group.Enabled && group.Messages != null && group.Messages.Count > 0)
                    StartGroup(group);
            }
        }

        public void StopAll()
        {
            lock (_timerLock)
            {
                foreach (var kvp in _broadcastTimers)
                {
                    kvp.Value.Stop();
                    kvp.Value.Dispose();
                    Logger.Debug($"Broadcast group '{kvp.Key}' stopped", LogCategory.Plugin);
                }
                _broadcastTimers.Clear();
                _currentMessageIndices.Clear();
            }
        }

        public void Reload()
        {
            StopAll();
            StartAll();
            Logger.Info("Broadcasts reloaded", LogCategory.Plugin);
        }

        public Dictionary<string, bool> GetStatus()
        {
            var status = new Dictionary<string, bool>();
            if (_config.Broadcast?.BroadcastGroups == null)
                return status;

            foreach (var group in _config.Broadcast.BroadcastGroups)
            {
                lock (_timerLock)
                {
                    status[group.Name] = _broadcastTimers.ContainsKey(group.Name);
                }
            }
            return status;
        }

        public void SendManual(string groupName, string message)
        {
            var group = _config.Broadcast.BroadcastGroups.FirstOrDefault(g => g.Name == groupName);
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

        private void StartGroup(BroadcastGroupConfig group)
        {
            if (group.DisplayMode == DisplayMode.None)
                return;

            lock (_timerLock)
            {
                if (_broadcastTimers.ContainsKey(group.Name))
                    StopGroup(group.Name);

                _currentMessageIndices[group.Name] = 0;

                var capturedGroup = group;
                var timer = new Timer(capturedGroup.RotationInterval * 1000.0);
                timer.Elapsed += (sender, e) => OnTimerElapsed(capturedGroup);
                timer.AutoReset = true;
                timer.Start();

                _broadcastTimers[group.Name] = timer;
                Logger.Debug($"Broadcast group '{group.Name}' started with interval {group.RotationInterval}s", LogCategory.Plugin);
            }
        }

        private void StopGroup(string groupName)
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

        private void OnTimerElapsed(BroadcastGroupConfig group)
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

                    var messageText = BroadcastHelper.ReplaceVariables(message.Message);
                    messageText = messageText.Replace("{", "<").Replace("}", ">");

                    // 应用广播消息字体颜色和大小
                    if (!string.IsNullOrEmpty(message.FontColor))
                        messageText = NameFormatter.WrapWithStyle(messageText, message.FontColor, message.FontSize);
                    else if (message.FontSize > 0)
                        messageText = $"<size={message.FontSize}>{messageText}</size>";

                    string avatarUrl = message.Avatar;
                    if (!string.IsNullOrEmpty(avatarUrl))
                        avatarUrl = BroadcastHelper.ReplaceVariables(avatarUrl);

                    var displayMode = group.DisplayMode;

                    if (displayMode == DisplayMode.Console || displayMode == DisplayMode.Both)
                        Logger.Info($"[广播] {BroadcastHelper.StripRichText(messageText)}", LogCategory.Plugin);

                    if (displayMode == DisplayMode.Chat || displayMode == DisplayMode.Both)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            try
                            {
                                ChatManager.serverSendMessage(messageText, Color.white, null, null, EChatMode.GLOBAL, avatarUrl, true);
                            }
                            catch (Exception ex)
                            {
                                Logger.Exception(ex, $"Error sending broadcast message for group '{group.Name}'", LogCategory.Plugin);
                            }
                        });
                    }

                    if (_config.DebugMode)
                        Logger.Debug($"Broadcast '{group.Name}': {messageText} (DisplayMode: {displayMode})", LogCategory.Plugin);

                    _currentMessageIndices[group.Name] = (currentIndex + 1) % group.Messages.Count;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error in broadcast timer for group '{group.Name}'", LogCategory.Plugin);
            }
        }
    }
}
