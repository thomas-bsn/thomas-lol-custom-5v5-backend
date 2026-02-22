using Custom5v5.Api.Contracts;

namespace Custom5v5.Api.Interfaces;

public interface IPlayersSource
{
    IReadOnlyList<PlayerDto> GetAllSnapshot();
}