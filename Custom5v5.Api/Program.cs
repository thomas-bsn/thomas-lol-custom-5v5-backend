using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Custom5v5.Api.Interfaces;
using Custom5v5.Api.Services;
using Custom5v5.Application.Matches;
using Custom5v5.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Controllers + JSON
// --------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
    );

// --------------------
// Swagger
// --------------------
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
        In = ParameterLocation.Header
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

// --------------------
// Options binding
// --------------------

// Riot
builder.Services.AddOptions<RiotApiOptions>()
    .Bind(builder.Configuration.GetSection("Auth:Riot"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Auth:Riot:ApiKey missing")
    .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "Auth:Riot:BaseUrl missing")
    .ValidateOnStart();

// Discord OAuth
builder.Services.AddOptions<DiscordOAuthOptions>()
    .Bind(builder.Configuration.GetSection("Auth:Discord"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "Auth:Discord:ClientId missing")
    .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "Auth:Discord:ClientSecret missing")
    .Validate(o => !string.IsNullOrWhiteSpace(o.RedirectUri), "Auth:Discord:RedirectUri missing")
    .ValidateOnStart();

// JWT
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Auth:Jwt"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Auth:Jwt:Issuer missing")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Auth:Jwt:Audience missing")
    .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Auth:Jwt:SigningKey missing")
    .ValidateOnStart();

// --------------------
// Auth (JWT)
// --------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// --------------------
// HttpClients
// --------------------

// Riot API
builder.Services.AddHttpClient("Riot", (sp, client) =>
{
    var riot = sp.GetRequiredService<IOptions<RiotApiOptions>>().Value;

    client.BaseAddress = new Uri(riot.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("X-Riot-Token", riot.ApiKey);
});

// Discord OAuth
builder.Services.AddHttpClient<IDiscordOAuthClient, DiscordOAuthClient>((sp, client) =>
{
    var discord = sp.GetRequiredService<IOptions<DiscordOAuthOptions>>().Value;
    client.BaseAddress = new Uri("https://discord.com/api/");
});

// --------------------
// DI App
// --------------------
builder.Services.AddScoped<IMatchProvider, MatchProvider>();
builder.Services.AddScoped<MatchProcessor>();

builder.Services.AddSingleton<IOAuthStateStore, OAuthStateStore>();
builder.Services.AddSingleton<IJwtIssuer, JwtIssuer>();
builder.Services.AddSingleton<IPlayersSource, JsonPlayersSource>();
builder.Services.AddSingleton<PollStore>();

// --------------------
// App
// --------------------
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