using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 统一广播服务接口
    /// </summary>
    public interface IBroadcastService : IDisposable
    {
        void HandlePlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator);
        void SendWelcomeMessage(UnturnedPlayer player);
        void SendLeaveMessage(UnturnedPlayer player);
        void StartAllBroadcasts();
        void StopAllBroadcasts();
        void ReloadBroadcasts();
        Dictionary<string, bool> GetBroadcastStatus();
        void SendBroadcast(string groupName, string message);
    }

    /// <summary>
    /// 统一广播服务（门面模式，委托给子服务）
    /// </summary>
    public class BroadcastService : IBroadcastService
    {
        private readonly DeathMessageService _deathMessageService;
        private readonly BroadcastRotationService _broadcastRotationService;
        private readonly WelcomeMessageService _welcomeMessageService;
        private bool _disposed;

        public BroadcastService(NoNameTagConfiguration config, INameTagManager nameTagManager)
        {
            _deathMessageService = new DeathMessageService(config, nameTagManager);
            _broadcastRotationService = new BroadcastRotationService(config);
            _welcomeMessageService = new WelcomeMessageService(config);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _broadcastRotationService.Dispose();
        }

        public void HandlePlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
            => _deathMessageService.HandlePlayerDeath(sender, cause, limb, instigator);

        public void SendWelcomeMessage(UnturnedPlayer player)
            => _welcomeMessageService.SendWelcomeMessage(player);

        public void SendLeaveMessage(UnturnedPlayer player)
            => _welcomeMessageService.SendLeaveMessage(player);

        public void StartAllBroadcasts()
            => _broadcastRotationService.StartAll();

        public void StopAllBroadcasts()
            => _broadcastRotationService.StopAll();

        public void ReloadBroadcasts()
            => _broadcastRotationService.Reload();

        public Dictionary<string, bool> GetBroadcastStatus()
            => _broadcastRotationService.GetStatus();

        public void SendBroadcast(string groupName, string message)
            => _broadcastRotationService.SendManual(groupName, message);
    }
}
