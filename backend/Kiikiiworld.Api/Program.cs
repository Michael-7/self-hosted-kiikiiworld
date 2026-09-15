using System.Threading.RateLimiting;
using Kiikiiworld.Api.Data;
using Kiikiiworld.Api.Endpoints;
using Kiikiiworld.Api.Models;
using Kiikiiworld.Api.Options;
using Kiikiiworld.Api.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";
const string LoginRateLimiterPolicy = "login";

var dataDir = builder.Configuration["DataDir"] ?? "data";
Directory.CreateDirectory(dataDir);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Enables automatic DataAnnotations validation for minimal API parameters.
builder.Services.AddValidation();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton(new MediaStorage(dataDir));

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "kiikiiworld_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        // This is an API, not a page app — return status codes instead of redirecting to a login page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(LoginRateLimiterPolicy, limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

builder.AddDb(dataDir);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCorsPolicy);

app.UseHttpsRedirection();

// Public, unauthenticated — serves everything under dataDir/media at /media/*.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Path.GetFullPath(dataDir), "media")),
    RequestPath = "/media",
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapPostEndpoints();
app.MapMediaEndpoints();

app.MigrateDB();

app.Run();
