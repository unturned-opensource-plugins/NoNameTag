namespace Emqo.NoNameTag.Services
{
    public interface IChatMessageSettings
    {
        bool Enabled { get; }
        bool ApplyToChatMessages { get; }
    }
}
