using Custom5v5.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class RefreshRanksJob : BackgroundService
{
    private readonly IServiceProvider _services;

    public RefreshRanksJob(IServiceProvider services) => _services = services;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();
                await playerService.RefreshAllRanksAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshRanksJob error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }
}