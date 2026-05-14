namespace UrlShortener.Application.Analytics.Dtos;

// From and To are treated as UTC. Granularity values supported by the
// service: "day", "week", "month" — mapped to Postgres date_trunc fields.
public record ByTimeQuery(DateTime From, DateTime To, string Granularity = "day");
