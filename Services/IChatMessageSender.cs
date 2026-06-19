namespace Emqo.NoNameTag.Services
{
    public interface IChatMessageSender
    {
        void Send(ChatMessageDispatch dispatch);
    }
}
