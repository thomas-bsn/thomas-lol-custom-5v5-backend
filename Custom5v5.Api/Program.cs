using Custom5v5.Api.Interfaces;
using Custom5v5.Api.Services;
using Custom5v5.Application.Matches;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();