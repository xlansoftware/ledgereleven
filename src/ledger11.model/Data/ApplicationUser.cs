using Microsoft.AspNetCore.Identity;

namespace ledger11.model.Data;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<SpaceMember> SpaceMemberships { get; set; } = new List<SpaceMember>();
    public Guid? CurrentSpaceId { get; set; }
    public Space? CurrentSpace { get; set; }
}
