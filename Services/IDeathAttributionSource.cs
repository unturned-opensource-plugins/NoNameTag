namespace Emqo.NoNameTag.Services
{
    public interface IDeathAttributionSource
    {
        bool TryGetBleedDeathAttribution(ulong victimSteamId, out DeathAttributionRecord record);
        bool TryGetRecentDeathAttribution(ulong victimSteamId, out DeathAttributionRecord record);
    }
}
