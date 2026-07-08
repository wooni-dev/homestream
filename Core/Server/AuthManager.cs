using System.Security.Cryptography;

namespace HomeStream.Server;

internal sealed class AuthManager
{
    public string Token { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();

    private readonly Dictionary<string, long> _sessions = new();
    private const long CookieMaxAgeMs = 7L * 24 * 3600 * 1000;
    internal const string CookieName = "hs_sid";

    public bool CheckSession(string cookieHeader)
    {
        long now = Environment.TickCount64;
        foreach (var part in cookieHeader.Split(';'))
        {
            var kv = part.Trim();
            int eq = kv.IndexOf('=');
            if (eq < 0) continue;
            if (kv[..eq] == CookieName)
            {
                string sid = kv[(eq + 1)..];
                if (_sessions.TryGetValue(sid, out long exp) && exp > now)
                    return true;
                _sessions.Remove(sid);
            }
        }
        return false;
    }

    public (string sid, string setCookie) CreateSession()
    {
        long now = Environment.TickCount64;
        foreach (var expired in _sessions.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToArray())
            _sessions.Remove(expired);

        string sid = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();
        _sessions[sid] = now + CookieMaxAgeMs;
        string setCookie = $"{CookieName}={sid}; Path=/; Max-Age=604800; HttpOnly; SameSite=Lax";
        return (sid, setCookie);
    }
}
