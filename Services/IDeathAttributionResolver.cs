namespace Emqo.NoNameTag.Services
{
    public interface IDeathAttributionResolver
    {
        DeathAttributionContext Resolve(DeathAttributionRequest request);
    }
}
