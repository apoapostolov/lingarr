using Lingarr.Contracts.Models;

namespace Lingarr.Server.Interfaces.Services.Translation;

public interface IModelCatalogService
{
    Task<ModelsResponse> GetModelsAsync(string provider, bool forceRefresh = false, CancellationToken cancellationToken = default);
}
