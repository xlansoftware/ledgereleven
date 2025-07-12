using ledger11.model.Data;

namespace ledger11.model.Api;

public static class SpaceMappingExtensions
{
    public static SpaceDto ToDto(this Space space) =>
        new SpaceDto
        {
            Id = space.Id,
            Name = space.Name,
        };

    public static List<SpaceDto> ToDtoList(this IEnumerable<Space> spaces) =>
        spaces.Select(s => s.ToDto()).ToList();
}
