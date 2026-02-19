namespace Custom5v5.Application.Matches.Dtos;

public sealed class MetadataDto
{
    public string DataVersion { get; init; } = "";
    public string MatchId { get; init; } = "";
    public List<string> Participants { get; init; } = new();
}