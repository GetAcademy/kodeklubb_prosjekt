namespace Core.Commands;

public record DeclineJoinRequestCommand(
    Guid TeamId,
    Guid RequestId,
    Guid DeclinedByUserId
);
