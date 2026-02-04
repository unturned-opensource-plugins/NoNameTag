using Rocket.Unturned.Player;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 名称标签显示服务接口
    /// </summary>
    public interface INameTagDisplayService
    {
        /// <summary>
        /// 应用名称标签
        /// </summary>
        void ApplyNameTag(UnturnedPlayer player);

        /// <summary>
        /// 移除名称标签
        /// </summary>
        void RemoveNameTag(UnturnedPlayer player);

        /// <summary>
        /// 刷新名称标签
        /// </summary>
        void RefreshNameTag(UnturnedPlayer player);

        /// <summary>
        /// 刷新所有名称标签
        /// </summary>
        void RefreshAllNameTags();

        /// <summary>
        /// 清理所有名称标签
        /// </summary>
        void ClearAllNameTags();
    }
}
