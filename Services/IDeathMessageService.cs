using SDG.Unturned;
using Steamworks;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 死亡消息服务接口
    /// </summary>
    public interface IDeathMessageService
    {
        /// <summary>
        /// 处理玩家死亡事件
        /// </summary>
        void HandlePlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator);
    }
}
