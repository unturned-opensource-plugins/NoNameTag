namespace Emqo.NoNameTag.Services
{
    public interface IFormattedNameProvider
    {
        string GetFormattedPlayerName(ulong steamId, string fallbackPlayerName);
    }
}
