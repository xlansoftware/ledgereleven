namespace ledger11.model.Data;

public enum AccessLevel
{
    Owner,
    Editor,
    Viewer
}

public class SpaceMember
{
    public Guid SpaceId { get; set; }
    public Space Space { get; set; } = default!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public AccessLevel AccessLevel { get; set; }

}
