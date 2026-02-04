using Emqo.NoNameTag.Models;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 名称标签管理器接口
    /// </summary>
    public interface INameTagManager
    {
        /// <summary>
        /// 应用显示效果
        /// </summary>
        void ApplyDisplayEffect(UnturnedPlayer player);

        /// <summary>
        /// 移除显示效果
        /// </summary>
        void RemoveDisplayEffect(UnturnedPlayer player);

        /// <summary>
        /// 刷新所有玩家
        /// </summary>
        void RefreshAllPlayers();

        /// <summary>
        /// 刷新指定玩家
        /// </summary>
        void RefreshPlayer(UnturnedPlayer player);

        /// <summary>
        /// 获取玩家效果
        /// </summary>
        PermissionGroupConfig GetPlayerEffect(ulong steamId);
    }
}
