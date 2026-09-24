using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kiikiiworld.Api.Dtos;
using Kiikiiworld.Api.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Kiikiiworld.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        // LOGIN
        group.MapPost("/login", async (LoginDto dto, IOptions<AuthOptions> authOptions, HttpContext httpContext) =>
        {
            var auth = authOptions.Value;

            var validUsername = !string.IsNullOrEmpty(auth.AdminUsername) && FixedTimeEquals(dto.Username, auth.AdminUsername);
            var validPassword = !string.IsNullOrEmpty(auth.AdminPassword) && FixedTimeEquals(dto.Password, auth.AdminPassword);

            if (!validUsername || !validPassword)
            {
                return Results.Unauthorized();
            }

            var claims = new List<Claim> { new(ClaimTypes.Name, auth.AdminUsername) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return Results.Ok();
        })
        .RequireRateLimiting("login");

        // LOGOUT
        group.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        })
        .RequireAuthorization();

        // CURRENT USER
        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new { Username = user.Identity!.Name });
        })
        .RequireAuthorization();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        // Padding keeps the comparison length-independent so responses don't leak length via timing.
        var length = Math.Max(aBytes.Length, bBytes.Length);
        Array.Resize(ref aBytes, length);
        Array.Resize(ref bBytes, length);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes) && a.Length == b.Length;
    }
}
