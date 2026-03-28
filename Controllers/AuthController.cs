using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OAuth_OpenID_Connect_Client.Models;
using OAuth_OpenID_Connect_Client.Services;

namespace OAuth_OpenID_Connect_Client.Controllers;

public class AuthController : Controller
{
    private readonly IOidcService _oidcService;

    public AuthController(IOidcService oidcService)
    {
        _oidcService = oidcService;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/login")]
    public IActionResult Login()
    {
        var state = Guid.NewGuid().ToString("N");
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        HttpContext.Session.SetString("oidc_state", state);
        HttpContext.Session.SetString("oidc_code_verifier", codeVerifier);

        var authorizationUrl = _oidcService.BuildAuthorizationUrl(state, codeChallenge);

        return Redirect(authorizationUrl);
    }

    [HttpGet("/auth/callback")]
    public async Task<IActionResult> Callback(string? code, string? state)
    {
        var expectedState = HttpContext.Session.GetString("oidc_state");
        var codeVerifier = HttpContext.Session.GetString("oidc_code_verifier");

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Authorization code mangler.");
        }

        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            return BadRequest("Code verifier mangler i session.");
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            return BadRequest("State mangler.");
        }

        if (string.IsNullOrWhiteSpace(expectedState) || state != expectedState)
        {
            return BadRequest("State matcher ikke session.");
        }

        HttpContext.Session.Remove("oidc_state");
        HttpContext.Session.Remove("oidc_code_verifier");

        var tokenResponse = await _oidcService.ExchangeCodeForTokensAsync(code, codeVerifier);

        if (tokenResponse is null)
        {
            return StatusCode(500, "Kunne ikke deserialize token response.");
        }

        if (string.IsNullOrWhiteSpace(tokenResponse.IdToken))
        {
            return StatusCode(500, "ID token mangler i token response.");
        }

        var validationResult = await _oidcService.ValidateIdTokenAsync(tokenResponse.IdToken);

        if (!validationResult.IsValid)
        {
            return BadRequest($"ID token validering fejlede: {validationResult.ErrorMessage}");
        }

        UserInfoResponse? userInfo = null;

        if (!string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            userInfo = await _oidcService.GetUserInfoAsync(tokenResponse.AccessToken);
        }

        var claims = new List<Claim>(validationResult.Claims);

        if (!string.IsNullOrWhiteSpace(userInfo?.PreferredUsername) &&
            !claims.Any(c => c.Type == ClaimTypes.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, userInfo.PreferredUsername));
        }

        if (!string.IsNullOrWhiteSpace(userInfo?.Email) &&
            !claims.Any(c => c.Type == ClaimTypes.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        // return View("TokenDebug", model);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Index));
        }

        var claims = User.Claims.ToList();
        return View(claims);
    }

    [HttpGet("/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Index));
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}