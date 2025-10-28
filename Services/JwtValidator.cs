using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppPlatformWebSocket.Services;

/// <summary>
/// Responsável por validar tokens JWT recebidos via Header Authorization (prioritário)
/// ou via query string (?token=) como fallback.
/// </summary>
public class JwtValidator
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtValidator> _logger;

    public JwtValidator(IOptions<JwtOptions> options, ILogger<JwtValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Tenta obter e validar o token a partir do HttpContext.
    /// Primeiro busca no Header Authorization: Bearer, depois na query string (?token=).
    /// </summary>
    public bool TryValidateFromHttpContext(HttpContext context, out ClaimsPrincipal? principal, out string? error)
    {
        principal = null;
        error = null;

        // HEADER (prioritário)
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var auth = authHeader.ToString();
            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = auth.Substring("Bearer ".Length).Trim();
                return TryValidateToken(token, out principal, out error);
            }
        }

        // QUERY STRING (fallback)
        if (context.Request.Query.TryGetValue("token", out var tokenValues))
        {
            var token = tokenValues.ToString();
            return TryValidateToken(token, out principal, out error);
        }

        error = "Token ausente no header e na query string.";
        return false;
    }

    /// <summary>
    /// Executa a validação criptográfica do token JWT.
    /// </summary>
    public bool TryValidateToken(string token, out ClaimsPrincipal? principal, out string? error)
    {
        principal = null;
        error = null;

        var handler = new JwtSecurityTokenHandler();

        var keyBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
        if (keyBytes.Length < 32)
        {
            error = "Chave secreta inválida (deve ter pelo menos 32 caracteres).";
            _logger.LogError(error);
            return false;
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(_options.Issuer),
            ValidateAudience = !string.IsNullOrEmpty(_options.Audience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };

        try
        {
            principal = handler.ValidateToken(token, parameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
                _logger.LogDebug("JWT válido para usuário {Sub} (expira em {Exp})",
                    jwtToken.Subject, jwtToken.ValidTo);

            return true;
        }
        catch (SecurityTokenExpiredException ex)
        {
            error = "Token expirado.";
            _logger.LogWarning(ex, error);
        }
        catch (SecurityTokenException ex)
        {
            error = $"Falha de validação do token: {ex.Message}";
            _logger.LogWarning(ex, error);
        }
        catch (Exception ex)
        {
            error = $"Erro inesperado ao validar o token: {ex.Message}";
            _logger.LogError(ex, error);
        }

        return false;
    }
}
