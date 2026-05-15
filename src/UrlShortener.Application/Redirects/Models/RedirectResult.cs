namespace UrlShortener.Application.Redirects.Models;

// Discriminated union of every outcome the redirect pipeline can produce.
// The API layer pattern-matches over this to emit the correct HTTP response.
public abstract record RedirectResult;

public sealed record RedirectFound(string TargetUrl) : RedirectResult;

// Link exists but is either inactive or past its ExpiresAt.
public sealed record RedirectGone : RedirectResult;

// No link matches the short code (or the matching link is soft-deleted).
public sealed record RedirectNotFound : RedirectResult;

// Link exists and is active but needs a password. LastAttemptFailed is true
// when this is the response to a wrong-password POST so the form can show
// an inline error.
public sealed record RedirectPasswordRequired(bool LastAttemptFailed = false) : RedirectResult;
