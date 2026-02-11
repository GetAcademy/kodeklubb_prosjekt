namespace Core.Commands;

public record ApproveJoinRequestCommand(
    Guid TeamId,
    Guid RequestId,
    Guid ApprovedByUserId
);
