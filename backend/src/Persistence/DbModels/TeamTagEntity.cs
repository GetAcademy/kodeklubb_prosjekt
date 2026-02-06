namespace Persistence.DbModels;

public class TeamTagEntity
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long TagId { get; set; }
    public DateTime CreatedAt { get; set; }

    public TeamEntity? Team { get; set; }
    public TagEntity? Tag { get; set; }
}