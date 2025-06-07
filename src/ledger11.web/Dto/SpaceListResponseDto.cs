namespace ledger11.model.Api;

public class SpaceListResponseDto
{
    public SpaceDto? Current { get; set; }
    public List<SpaceDto> Spaces { get; set; } = new();
}
