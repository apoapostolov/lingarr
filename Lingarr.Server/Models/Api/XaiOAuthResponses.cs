namespace Lingarr.Server.Models.Api;

public sealed class XaiOAuthStatusResponse
{
    public bool Connected { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Label { get; set; } = "Experimental";
}

public sealed class XaiOAuthDeviceResponse
{
    public string FlowId { get; set; } = "";
    public string UserCode { get; set; } = "";
    public string VerificationUri { get; set; } = "";
    public string? VerificationUriComplete { get; set; }
    public int IntervalSeconds { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class XaiOAuthPollResponse
{
    public string Status { get; set; } = "pending";
    public int? IntervalSeconds { get; set; }
    public string? Message { get; set; }
}
