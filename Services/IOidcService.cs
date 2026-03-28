using OAuth_OpenID_Connect_Client.Models;

namespace OAuth_OpenID_Connect_Client.Services;

public interface IOidcService
{
    string BuildAuthorizationUrl(string state, string codeChallenge);
    Task<TokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier);
    Task<OpenIdConnectDiscoveryDocument?> GetDiscoveryDocumentAsync();
    Task<ValidatedTokenResult> ValidateIdTokenAsync(string idToken);
    Task<UserInfoResponse?> GetUserInfoAsync(string accessToken);
}