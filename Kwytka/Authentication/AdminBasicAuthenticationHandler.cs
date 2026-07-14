using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Kwytka.Authentication;

public static class AdminBasicAuthenticationDefaults
{
    public const string Scheme = "AdminBasic";
}

public sealed class AdminBasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expectedLogin = configuration["Admin:Login"];
        var expectedPassword = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(expectedLogin) || string.IsNullOrWhiteSpace(expectedPassword))
        {
            return Task.FromResult(AuthenticateResult.Fail("Admin credentials are not configured."));
        }

        var authorization = Request.Headers[HeaderNames.Authorization].ToString();
        const string prefix = "Basic ";

        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[prefix.Length..]));
            var separatorIndex = credentials.IndexOf(':');

            if (separatorIndex < 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication credentials."));
            }

            var login = credentials[..separatorIndex];
            var password = credentials[(separatorIndex + 1)..];

            if (!CredentialsMatch(login, expectedLogin) || !CredentialsMatch(password, expectedPassword))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid login or password."));
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, login)],
                AdminBasicAuthenticationDefaults.Scheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AdminBasicAuthenticationDefaults.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic authentication credentials."));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers[HeaderNames.WWWAuthenticate] = "Basic realm=\"Kwytka Admin\", charset=\"UTF-8\"";
        return Task.CompletedTask;
    }

    private static bool CredentialsMatch(string provided, string expected)
    {
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));

        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }
}
