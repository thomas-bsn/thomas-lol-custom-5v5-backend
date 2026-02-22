using Custom5v5.Api.Interfaces;
using Custom5v5.Api.Services;
using Custom5v5.Application.Matches;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Custom5v5.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtIssuer = builder.Configuration["Auth:Jwt:Issuer"];
var jwtAudience = builder.Configuration["Auth:Jwt:Audience"];
var jwtKey = builder.Configuration["Auth:Jwt:SigningKey"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// DI
builder.Services.AddHttpClient("Riot", client =>
{
    client.BaseAddress = new Uri("https://europe.api.riotgames.com");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IMatchProvider, MatchProvider>(); // pour l’instant (mock)
builder.Services.AddScoped<MatchProcessor>();
builder.Services.AddSingleton<IOAuthStateStore, OAuthStateStore>();
builder.Services.AddHttpClient<IDiscordOAuthClient, DiscordOAuthClient>();
builder.Services.AddSingleton<IJwtIssuer, JwtIssuer>();
builder.Services.AddSingleton<IPlayersSource, JsonPlayersSource>();
builder.Services.AddSingleton<PollStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();