using System.Security.Claims;

namespace OAuth_OpenID_Connect_Client.Models;

public class ValidatedTokenResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Claim> Claims { get; set; } = new();
}