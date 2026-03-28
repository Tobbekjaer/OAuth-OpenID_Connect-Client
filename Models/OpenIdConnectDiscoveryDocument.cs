using System.Text.Json.Serialization;

namespace OAuth_OpenID_Connect_Client.Models;

public class OpenIdConnectDiscoveryDocument
{
    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("authorization_endpoint")]
    public string? AuthorizationEndpoint { get; set; }

    [JsonPropertyName("token_endpoint")]
    public string? TokenEndpoint { get; set; }

    [JsonPropertyName("userinfo_endpoint")]
    public string? UserInfoEndpoint { get; set; }

    [JsonPropertyName("jwks_uri")]
    public string? JwksUri { get; set; }
}