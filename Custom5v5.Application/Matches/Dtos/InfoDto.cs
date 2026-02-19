namespace Custom5v5.Application.Matches.Dtos;

public sealed class InfoDto
{
    public string EndOfGameResult { get; init; } = "";
    public long GameCreation { get; init; }
    public long GameDuration { get; init; }
    public long? GameEndTimestamp { get; init; }
    public long GameId { get; init; }
    public string GameMode { get; init; } = "";
    public string GameName { get; init; } = "";
    public long GameStartTimestamp { get; init; }
    public string GameType { get; init; } = "";
    public string GameVersion { get; init; } = "";
    public int MapId { get; init; }
    public string PlatformId { get; init; } = "";
    public int QueueId { get; init; }
    public string TournamentCode { get; init; } = "";

    public List<ParticipantDto> Participants { get; init; } = new();
    public List<TeamDto> Teams { get; init; } = new();
}