using System.Security.Claims;

namespace OAuth_OpenID_Connect_Client.Models;

public class TokenDebugViewModel
{
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }

    public bool IsIdTokenValid { get; set; }
    public string? IdTokenValidationError { get; set; }
    public List<Claim> IdTokenClaims { get; set; } = new();
    
    public UserInfoResponse? UserInfo { get; set; }
}