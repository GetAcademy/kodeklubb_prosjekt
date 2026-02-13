namespace Core.Commands;

public record DeclineJoinRequestCommand(
    Guid TeamId,
    Guid UserId,
    Guid RequestId,
    Guid DeclinedByUserId
);