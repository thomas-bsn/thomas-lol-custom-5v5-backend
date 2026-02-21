using Custom5v5.Application.Matches;
using Custom5v5.Application.Matches.Scoring.Performance;
using Custom5v5.Application.Matches.Scoring.Performance.Axes;


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
builder.Services.AddScoped<IGlobalScoreAggregator, WeightedGlobalScoreAggregator>();
builder.Services.AddScoped<IPerformanceAxisCalculator, GlobalAxisCalculator>();
builder.Services.AddScoped<IPerformanceAxisCalculator, VersusOpponentAxisCalculator>();
builder.Services.AddScoped<IPerformanceAxisCalculator, ObjectivesAxisCalculator>();
builder.Services.AddScoped<IPerformanceAxisCalculator, TeamImpactAxisCalculator>();
builder.Services.AddScoped<IPerformanceAxisCalculator, RoleImpactAxisCalculator>();
builder.Services.AddScoped<IPlayerPerformanceCalculator, PlayerPerformanceCalculator>();

builder.Services.AddScoped<IMatchProvider, MatchProvider>(); // pour l’instant (mock)
builder.Services.AddScoped<MatchProcessor>();   

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();