using System.Net;

namespace UrlShortener.Api.Templates;

// Server-rendered unlock form for password-protected links. Kept here as a
// raw string rather than a Razor page to avoid pulling MVC views into the
// otherwise controller-only project.
internal static class PasswordUnlockPage
{
    public static string Render(string shortCode, bool lastAttemptFailed)
    {
        // The shortCode comes from the URL and is reflected in the form action;
        // encode it so a hostile path can't break out and inject markup.
        var safeShortCode = WebUtility.HtmlEncode(shortCode);
        var errorBlock = lastAttemptFailed
            ? "<p class=\"error\">Incorrect password. Try again.</p>"
            : string.Empty;

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Protected link</title>
    <style>
        body { font-family: system-ui, -apple-system, sans-serif; max-width: 360px; margin: 4rem auto; padding: 0 1rem; color: #111; }
        h1 { font-size: 1.25rem; margin-bottom: 1rem; }
        form { display: flex; flex-direction: column; gap: 0.5rem; }
        input, button { padding: 0.5rem 0.75rem; font-size: 1rem; border-radius: 4px; border: 1px solid #ccc; }
        button { background: #111; color: white; border: 0; cursor: pointer; }
        .error { color: #c00; font-size: 0.9rem; margin-bottom: 0.5rem; }
    </style>
</head>
<body>
    <h1>This link is password protected</h1>
    {{errorBlock}}
    <form method="post" action="/{{safeShortCode}}">
        <input type="password" name="password" placeholder="Enter password" autofocus required />
        <button type="submit">Unlock</button>
    </form>
</body>
</html>
""";
    }
}
