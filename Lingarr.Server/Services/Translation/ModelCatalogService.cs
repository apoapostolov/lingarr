using System.Collections.Concurrent;
using Lingarr.Contracts.Models;
using Lingarr.Server.Interfaces.Services.Translation;

namespace Lingarr.Server.Services.Translation;

public sealed class ModelCatalogService : IModelCatalogService
{
    private readonly ITranslationServiceFactory _factory;
    private readonly ILogger<ModelCatalogService> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    private sealed record CacheEntry(ModelsResponse Response, DateTimeOffset FetchedAt);

    public ModelCatalogService(ITranslationServiceFactory factory, ILogger<ModelCatalogService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<ModelsResponse> GetModelsAsync(string provider, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var key = provider.Trim().ToLowerInvariant();
        _cache.TryGetValue(key, out var hit);
        if (!forceRefresh && hit is not null && DateTimeOffset.UtcNow - hit.FetchedAt < Ttl)
        {
            return hit.Response;
        }

        try
        {
            var service = _factory.CreateTranslationService(key);
            var models = await service.GetModels();
            if (models.Options is { Count: > 0 })
            {
                _cache[key] = new CacheEntry(models, DateTimeOffset.UtcNow);
            }
            else if (!forceRefresh && hit is not null)
            {
                _logger.LogWarning("Empty model list for {Provider}; serving stale cache.", key);
                return hit.Response;
            }
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model catalog fetch failed for {Provider}", key);
            if (_cache.TryGetValue(key, out var stale))
            {
                return new ModelsResponse
                {
                    Options = stale.Response.Options,
                    Message = (stale.Response.Message ?? "") + " (cached; refresh failed)"
                };
            }
            return new ModelsResponse { Message = "Error fetching models: " + ex.Message };
        }
    }
}
