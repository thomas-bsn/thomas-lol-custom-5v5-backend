using Custom5v5.Application.DTOs.Players;

namespace Custom5v5.Api.Interfaces;

public interface IPlayersSource
{
    IReadOnlyList<PlayerDto> GetAllSnapshot();
}