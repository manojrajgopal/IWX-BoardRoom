using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IWX.AuthService.Domain;
using Microsoft.IdentityModel.Tokens;

namespace IWX.AuthService.Infrastructure;

public sealed class PasswordHasher
{
    public (string hash, string salt) Hash(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(password, salt);
        return (hash, salt);
    }

    public bool Verify(string password, string hash, string salt)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(ComputeHash(password, salt)),
            Encoding.UTF8.GetBytes(hash));

    private static string ComputeHash(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var bytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(bytes);
    }
}

public sealed class JwtIssuer
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly int _expiresMinutes;

    public JwtIssuer(IConfiguration cfg)
    {
        var jwt = cfg.GetSection("Jwt");
        _issuer = jwt["Issuer"] ?? "iwx-auth";
        _audience = jwt["Audience"] ?? "iwx";
        _expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 60;
        var secret = jwt["Secret"] ?? "iwx-dev-secret-please-rotate-32+chars-min";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public (string token, string tokenId, DateTime expiresUtc) Issue(IwxUser user, IEnumerable<string> roles)
    {
        var tokenId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new("tenant", user.TenantId),
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_issuer, _audience, claims, now, expires, creds);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), tokenId, expires);
    }

    public TokenValidationParameters ValidationParameters => new()
    {
        ValidIssuer = _issuer,
        ValidAudience = _audience,
        IssuerSigningKey = _key,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
}
