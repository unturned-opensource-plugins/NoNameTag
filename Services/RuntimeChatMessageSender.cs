using Emqo.NoNameTag.Utilities;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Emqo.NoNameTag.Services
{
    public sealed class RuntimeChatMessageSender : IChatMessageSender
    {
        public void Send(ChatMessageDispatch dispatch)
        {
            if (dispatch == null || string.IsNullOrEmpty(dispatch.Message))
                return;

            var sender = dispatch.Sender?.SteamId > 0
                ? BroadcastHelper.GetSteamPlayer(new CSteamID(dispatch.Sender.SteamId))
                : null;
            var recipient = dispatch.Recipient?.SteamId > 0
                ? BroadcastHelper.GetSteamPlayer(new CSteamID(dispatch.Recipient.SteamId))
                : null;

            ChatManager.serverSendMessage(
                dispatch.Message,
                Color.white,
                sender,
                recipient,
                ToRuntimeMode(dispatch.ChatMode),
                dispatch.AvatarUrl,
                true);
        }

        private static EChatMode ToRuntimeMode(ChatMessageMode mode)
        {
            switch (mode)
            {
                case ChatMessageMode.Local:
                    return EChatMode.LOCAL;
                case ChatMessageMode.Group:
                    return EChatMode.GROUP;
                case ChatMessageMode.Say:
                    return EChatMode.SAY;
                default:
                    return EChatMode.GLOBAL;
            }
        }
    }
}
