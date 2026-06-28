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

            var recipient = dispatch.Recipient?.SteamId > 0
                ? BroadcastHelper.GetSteamPlayer(new CSteamID(dispatch.Recipient.SteamId))
                : null;

            if (dispatch.Recipient?.SteamId > 0 && recipient == null)
                return;

            var runtimeMode = dispatch.Recipient == null
                ? EChatMode.GLOBAL
                : EChatMode.SAY;

            ChatManager.serverSendMessage(
                dispatch.Message,
                Color.white,
                null,
                recipient,
                runtimeMode,
                dispatch.AvatarUrl,
                true);
        }
    }
}
