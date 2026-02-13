namespace Core.Commands;

public enum JoinRequestAction
{
    Request,
    Approve,
    Decline
}

public record JoinRequestTransitionCommand(
    Guid TeamId,
    JoinRequestAction Action,
    Guid ActorUserId,
    Guid? TargetUserId = null,
    Guid? RequestId = null
);
