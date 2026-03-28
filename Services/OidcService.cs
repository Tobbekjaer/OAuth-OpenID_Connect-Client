using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OAuth_OpenID_Connect_Client.Models;

namespace OAuth_OpenID_Connect_Client.Services;

public class OidcService : IOidcService
{
    private readonly OpenIdConnectOptions _options;
    private readonly HttpClient _httpClient;

    public OidcService(
        IOptions<OpenIdConnectOptions> options,
        HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public string BuildAuthorizationUrl(string state, string codeChallenge)
    {
        var authorizationEndpoint = $"{_options.Authority}/protocol/openid-connect/auth";

        var queryParams = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid profile email",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        return QueryHelpers.AddQueryString(authorizationEndpoint, queryParams);
    }

    public async Task<TokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        var tokenEndpoint = $"{_options.Authority}/protocol/openid-connect/token";

        var formValues = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
            ["code_verifier"] = codeVerifier
        };

        using var content = new FormUrlEncodedContent(formValues);
        using var response = await _httpClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Token request failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<TokenResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    public async Task<OpenIdConnectDiscoveryDocument?> GetDiscoveryDocumentAsync()
    {
        var discoveryEndpoint = $"{_options.Authority}/.well-known/openid-configuration";

        using var response = await _httpClient.GetAsync(discoveryEndpoint);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Discovery request failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<OpenIdConnectDiscoveryDocument>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    public async Task<ValidatedTokenResult> ValidateIdTokenAsync(string idToken)
    {
        try
        {
            var documentRetriever = new HttpDocumentRetriever
            {
                RequireHttps = false
            };

            var configurationManager =
                new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{_options.Authority}/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(),
                    documentRetriever);

            var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Authority,

                ValidateAudience = true,
                ValidAudience = _options.ClientId,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),

                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys
            };

            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(
                idToken,
                tokenValidationParameters,
                out _);

            return new ValidatedTokenResult
            {
                IsValid = true,
                Claims = principal.Claims.ToList()
            };
        }
        catch (Exception ex)
        {
            return new ValidatedTokenResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    public async Task<UserInfoResponse?> GetUserInfoAsync(string accessToken)
    {
        var discoveryDocument = await GetDiscoveryDocumentAsync();

        if (discoveryDocument?.UserInfoEndpoint is null)
        {
            throw new InvalidOperationException("UserInfo endpoint blev ikke fundet i discovery document.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, discoveryDocument.UserInfoEndpoint);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"UserInfo request failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<UserInfoResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }
}