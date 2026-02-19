namespace Custom5v5.Application.Matches.Dtos;

public sealed class MatchDto
{
    public MetadataDto Metadata { get; init; } = new();
    public InfoDto Info { get; init; } = new();
}