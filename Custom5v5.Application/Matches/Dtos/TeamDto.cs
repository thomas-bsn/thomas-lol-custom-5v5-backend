namespace Custom5v5.Application.Matches.Dtos;

public sealed class TeamDto
{
    public int TeamId { get; init; } // 100 / 200
    public bool Win { get; init; }

    public List<BanDto> Bans { get; init; } = new();
    public ObjectivesDto Objectives { get; init; } = new();
}