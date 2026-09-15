using System.Security.Claims;
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

            var validUsername = !string.IsNullOrEmpty(auth.AdminUsername) && dto.Username == auth.AdminUsername;

            var validPassword = false;
            if (!string.IsNullOrEmpty(auth.AdminPasswordHash))
            {
                try
                {
                    validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, auth.AdminPasswordHash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Auth:AdminPasswordHash isn't a valid bcrypt hash (e.g. a misconfigured secret).
                    // Fail the login instead of crashing the request.
                    validPassword = false;
                }
            }

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
}
