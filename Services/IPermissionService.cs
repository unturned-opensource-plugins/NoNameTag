using Emqo.NoNameTag.Models;
using Rocket.API;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 权限服务接口
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 获取玩家权限组
        /// </summary>
        PermissionGroupConfig GetPlayerPermissionGroup(IRocketPlayer player);

        /// <summary>
        /// 检查玩家是否有任何权限组
        /// </summary>
        bool HasAnyPermissionGroup(IRocketPlayer player);

        /// <summary>
        /// 清理指定玩家的缓存
        /// </summary>
        void ClearPlayerCache(ulong steamId);

        /// <summary>
        /// 清理所有缓存
        /// </summary>
        void ClearAllCache();
    }
}
