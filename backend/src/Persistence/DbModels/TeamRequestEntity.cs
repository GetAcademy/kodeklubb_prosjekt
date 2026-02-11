namespace Persistence.DbModels;

public record TeamRequestEntity(
    Guid teamId,
    Guid userId
    );