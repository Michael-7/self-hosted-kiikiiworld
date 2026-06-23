using Kiikiiworld.Api.Data;
using Kiikiiworld.Api.Endpoints;
using Kiikiiworld.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Enables automatic DataAnnotations validation for minimal API parameters.
builder.Services.AddValidation();

builder.AddDb();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPostEndpoints();

app.MigrateDB();

app.Run();
