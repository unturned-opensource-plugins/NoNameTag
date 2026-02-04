using System.Xml.Serialization;

namespace Emqo.NoNameTag.Models
{
    /// <summary>
    /// 头像配置类
    /// </summary>
    public class AvatarConfig
    {
        /// <summary>
        /// 是否启用头像功能
        /// </summary>
        [XmlElement("Enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 聊天消息头像大小（像素）
        /// </summary>
        [XmlElement("ChatAvatarSize")]
        public int ChatAvatarSize { get; set; } = 16;

        public AvatarConfig()
        {
        }
    }
}
