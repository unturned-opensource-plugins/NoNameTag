using System;
using System.Collections.Generic;
using Emqo.NoNameTag.Utilities;

namespace Emqo.NoNameTag.Services
{
    public sealed class ChatMessageService
    {
        private const float LocalChatRangeSqr = 4096f;

        private readonly IChatMessageSettings _config;
        private readonly IFormattedNameProvider _formattedNameProvider;
        private readonly IChatMessageSender _sender;

        public ChatMessageService(IChatMessageSettings config, IFormattedNameProvider formattedNameProvider, IChatMessageSender sender)
        {
            _config = config;
            _formattedNameProvider = formattedNameProvider;
            _sender = sender;
        }

        public bool HandleChat(ChatMessageRequest request)
        {
            if (!ShouldHandle(request))
                return false;

            var formatted = BuildFormattedMessage(request.Sender, request.Message, request.ChatMode);
            if (string.IsNullOrEmpty(formatted.Message))
                return false;

            foreach (var recipient in ResolveRecipients(request))
            {
                _sender.Send(new ChatMessageDispatch
                {
                    Sender = request.Sender,
                    Recipient = recipient,
                    Message = formatted.Message,
                    AvatarUrl = formatted.AvatarUrl,
                    ChatMode = request.ChatMode
                });
            }

            return true;
        }

        public ChatFormattedMessage BuildFormattedMessage(ChatMessageParticipant sender, string message, ChatMessageMode chatMode)
        {
            if (sender == null)
                return ChatFormattedMessage.Empty;

            var formattedName = _formattedNameProvider.GetFormattedPlayerName(sender.SteamId, sender.DisplayName);
            var safeMessage = RichTextSanitizer.SanitizeUntrustedPlayerText(message);
            var modePrefix = GetModePrefix(chatMode);

            return new ChatFormattedMessage
            {
                Message = $"{modePrefix}{formattedName}: {safeMessage}",
                AvatarUrl = null
            };
        }

        private bool ShouldHandle(ChatMessageRequest request)
        {
            return _config != null
                && _config.Enabled
                && _config.ApplyToChatMessages
                && request != null
                && !request.IsCanceled
                && request.Sender != null
                && !string.IsNullOrEmpty(request.Message)
                && !request.Message.StartsWith("/", StringComparison.Ordinal);
        }

        private IEnumerable<ChatMessageParticipant> ResolveRecipients(ChatMessageRequest request)
        {
            var recipients = request.Recipients ?? Array.Empty<ChatMessageParticipant>();

            switch (request.ChatMode)
            {
                case ChatMessageMode.Local:
                    foreach (var recipient in recipients)
                    {
                        if (recipient == null)
                            continue;

                        var distanceSqr = recipient.Position.DistanceSquaredTo(request.Sender.Position);
                        if (distanceSqr <= LocalChatRangeSqr)
                            yield return recipient;
                    }
                    yield break;

                case ChatMessageMode.Group:
                    if (request.Sender.GroupId == 0)
                    {
                        yield return request.Sender;
                        yield break;
                    }

                    foreach (var recipient in recipients)
                    {
                        if (recipient != null && recipient.GroupId == request.Sender.GroupId)
                            yield return recipient;
                    }
                    yield break;

                default:
                    yield return null;
                    yield break;
            }
        }

        private static string GetModePrefix(ChatMessageMode chatMode)
        {
            if (chatMode == ChatMessageMode.Local)
                return "[A] ";

            if (chatMode == ChatMessageMode.Group)
                return "[G] ";

            return string.Empty;
        }
    }

    public sealed class ChatFormattedMessage
    {
        public static readonly ChatFormattedMessage Empty = new ChatFormattedMessage { Message = string.Empty };

        public string Message { get; set; }
        public string AvatarUrl { get; set; }
    }
}
