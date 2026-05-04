namespace UrlShortener.Domain.Entities;

public class Click
{
    public Guid Id { get; set; }
    public Guid ShortLinkId { get; set; }
    public DateTime ClickedAt { get; set; }

    // Captured at request time. Geo / device / bot fields below are filled
    // asynchronously by the click enrichment background job.
    public required string IpAddress { get; set; }
    public required string UserAgent { get; set; }

    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Referrer { get; set; }
    public bool IsBot { get; set; }
}
