using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services.Integration;

public static class ArrImportHistory
{
    public static async Task<List<int>> GetIdsSince(
        IIntegrationService integration,
        string historyPath,
        IntegrationSettingKeys keys,
        DateTime since,
        Func<ArrHistoryRecord, int> idSelector)
    {
        var ids = new List<int>();
        var sinceUtc = since.ToUniversalTime();
        for (var page = 1; page <= 10; page++)
        {
            var body = await integration.GetApiResponse<ArrHistoryPage>(
                $"{historyPath}?page={page}&pageSize=100&sortKey=date&sortDirection=descending&eventType=3",
                keys);
            if (body?.Records == null || body.Records.Count == 0)
            {
                break;
            }

            var reachedOlder = false;
            foreach (var record in body.Records)
            {
                if (!string.Equals(record.EventType, "downloadFolderImported", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!DateTime.TryParse(record.Date, out var when) || when.ToUniversalTime() < sinceUtc)
                {
                    reachedOlder = true;
                    continue;
                }

                var id = idSelector(record);
                if (id > 0 && !ids.Contains(id))
                {
                    ids.Add(id);
                }
            }

            if (reachedOlder || body.Records.Count < 100)
            {
                break;
            }
        }

        return ids;
    }
}
