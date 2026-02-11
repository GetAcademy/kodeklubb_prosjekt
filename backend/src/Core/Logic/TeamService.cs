using Core.Commands;
using Core.DomainEvents;
using Core.Outcomes;
using Core.State;

namespace Core.Logic;

public static class TeamService
{
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

    public static TeamResult Handle(
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

    public static (Outcome outcome, List<IDomainEvent> events) Handle(
        RequestToJoinTeamCommand command,
        DateTime now
    )
    {
        // Create the domain event for the join request
        var requestEvent = new UserRequestedToJoinTeam(
            command.TeamId,
            command.UserId,
            now
        );

        return (Outcome.Accepted(), new List<IDomainEvent> { requestEvent });
    }
}