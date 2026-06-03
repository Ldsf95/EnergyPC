using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EnergyPC.Api.Controllers;

/// <summary>
/// Authentification locale : délivre un jeton JWT signé.
/// Bonne pratique : sécuriser l'API même en local.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    public AuthController(IConfiguration cfg) => _cfg = cfg;

    /// <summary>
    /// Échange identifiant/mot de passe contre un token JWT (valable 8 h).
    /// Les identifiants de démonstration sont dans appsettings.json.
    /// </summary>
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest req)
    {
        var userAttendu = _cfg["Auth:Utilisateur"] ?? "admin";
        var passAttendu = _cfg["Auth:MotDePasse"] ?? "energy";

        if (req.Utilisateur != userAttendu || req.MotDePasse != passAttendu)
            return Unauthorized(new { error = "Identifiants invalides." });

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: "EnergyPC",
            audience: "EnergyPC.Desktop",
            claims: new[] { new Claim(ClaimTypes.Name, req.Utilisateur) },
            expires: expiration,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new LoginResponse(jwt, expiration);
    }
}
