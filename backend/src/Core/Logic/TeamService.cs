using Core.Commands;
using Core.DomainEvents;
using Core.Outcomes;
using Core.State;

namespace Core.Logic;

public static class TeamService
{
    private static TeamResult Rejected(TeamState state, string message) =>
        new(
            new Outcome(OutcomeStatus.Rejected, message),
            state,
            new List<IDomainEvent>());

    public static (Outcome outcome, List<IDomainEvent> events) Handle(
        CreateTeamCommand command,
        DateTime now
    )
    {
        // Validate team name
        if (string.IsNullOrWhiteSpace(command.Name))
            return (Outcome.Rejected("Team name is required"), new List<IDomainEvent>());

        if (command.Name.Length > 100)
            return (Outcome.Rejected("Team name must be 100 characters or less"), new List<IDomainEvent>());

        // Create the event
        var teamCreated = new TeamCreated(
            command.TeamId,
            command.Name,
            command.Description,
            command.AdminUserId,
            now
        );

        return (Outcome.Accepted(), new List<IDomainEvent> { teamCreated });
    }

    public static TeamResult HandleJoinRequestTransition(
        TeamState state,
        JoinRequestTransitionCommand command,
        DateTime now)
    {
        switch (command.Action)
        {
            case JoinRequestAction.Request:
            {
                var requestingUserId = command.ActorUserId;

                if (state.Members.Contains(requestingUserId))
                    return Rejected(state, "UserAlreadyInTeam");

                if (state.PendingInvitations.Contains(requestingUserId))
                    return Rejected(state, "UserAlreadySentARequest");

                var newState = state with
                {
                    PendingInvitations = state.PendingInvitations
                        .Append(requestingUserId)
                        .ToList()
                };

                return new TeamResult(
                    Outcome.Accepted(),
                    newState,
                    new List<IDomainEvent>
                    {
                        new UserRequestedToJoinTeam(state.TeamId, requestingUserId, now)
                    });
            }

            case JoinRequestAction.Approve:
            {
                if (command.TargetUserId is null || command.RequestId is null)
                    return Rejected(state, "InvalidJoinRequestTransition");

                var approvingUserId = command.ActorUserId;
                var targetUserId = command.TargetUserId.Value;
                var requestId = command.RequestId.Value;

                if (!state.Members.Contains(approvingUserId))
                    return Rejected(state, "InviterIsNotAMember");

                if (!state.PendingInvitations.Contains(targetUserId))
                    return Rejected(state, "UserHasNotSentRequest");

                if (state.Members.Contains(targetUserId))
                    return Rejected(state, "UserAlreadyInTeam");

                var newMembers = state.Members.Append(targetUserId).ToList();
                var newPending = state.PendingInvitations.Where(id => id != targetUserId).ToList();
                var newState = state with
                {
                    Members = newMembers,
                    PendingInvitations = newPending
                };

                return new TeamResult(
                    Outcome.Accepted(),
                    newState,
                    new List<IDomainEvent>
                    {
                        new JoinRequestApproved(state.TeamId, requestId, targetUserId, approvingUserId, now)
                    }
                );
            }

            case JoinRequestAction.Decline:
            {
                if (command.TargetUserId is null || command.RequestId is null)
                    return Rejected(state, "InvalidJoinRequestTransition");

                var decliningUserId = command.ActorUserId;
                var targetUserId = command.TargetUserId.Value;
                var requestId = command.RequestId.Value;

                if (!state.Members.Contains(decliningUserId))
                    return Rejected(state, "InviterIsNotAMember");

                if (!state.PendingInvitations.Contains(targetUserId))
                    return Rejected(state, "UserHasNotSentRequest");

                var newPending = state.PendingInvitations.Where(id => id != targetUserId).ToList();
                var newState = state with
                {
                    PendingInvitations = newPending
                };

                return new TeamResult(
                    Outcome.Accepted(),
                    newState,
                    new List<IDomainEvent>
                    {
                        new JoinRequestDeclined(state.TeamId, requestId, targetUserId, decliningUserId, now)
                    }
                );
            }

            default:
                return Rejected(state, "InvalidJoinRequestTransition");
        }
    }
    
    public static TeamResult HandleInviteToTeam(
        TeamState state,
        InviteUserToTeamCommand command,
        DateTime now
        )
    {
        if (!state.Members.Contains(command.InvitedByUserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "InviterNotMember"),
                state,
                new List<IDomainEvent>()
                );
        }

        if (state.Members.Contains(command.InvitedUserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "InvitedUserAlreadyMember"),
                state,
                new List<IDomainEvent>()
                );
        }

        if (state.PendingInvitations.Contains(command.InvitedUserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "InvitedUserAlreadyInvited"),
                state,
                new List<IDomainEvent>()
                );
        }

        var newPendingInvitations = state.PendingInvitations
            .Append(command.InvitedUserId)
            .ToList();
        TeamState newState = state with
        {
            PendingInvitations = newPendingInvitations
        };
        return new TeamResult(
            Outcome.Accepted(),
            newState,
            new List<IDomainEvent>
            {
                new UserInvitedToTeam(state.TeamId, command.InvitedUserId, now)
            }
        );
    }

}