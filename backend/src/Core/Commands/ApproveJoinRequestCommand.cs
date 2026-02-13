namespace Core.Commands;

public record ApproveJoinRequestCommand(
    Guid TeamId,
    Guid UserId,
    Guid RequestId
);