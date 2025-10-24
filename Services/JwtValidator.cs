using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppPlatformWebSocket.Services;

public class JwtValidator
{
    private readonly JwtOptions _options;

    public JwtValidator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public bool TryValidateFromHttpContext(HttpContext context, out ClaimsPrincipal? principal, out string? error)
    {
        principal = null;
        error = null;

        // 1) Query string ?token=
        if (context.Request.Query.TryGetValue("token", out var tokenValues))
        {
            var token = tokenValues.ToString();
            return TryValidateToken(token, out principal, out error);
        }

        // 2) Header Authorization: Bearer <token>
        var auth = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth.Substring("Bearer ".Length).Trim();
            return TryValidateToken(token, out principal, out error);
        }

        error = "Token ausente.";
        return false;
    }

    public bool TryValidateToken(string token, out ClaimsPrincipal? principal, out string? error)
    {
        principal = null;
        error = null;

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(_options.Issuer),
            ValidateAudience = !string.IsNullOrEmpty(_options.Audience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey))
        };

        try
        {
            principal = handler.ValidateToken(token, parameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Token inválido: {ex.Message}";
            return false;
        }
    }
}
