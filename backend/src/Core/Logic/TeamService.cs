using Core.Commands;
using Core.DomainEvents;
using Core.Outcomes;
using Core.State;

namespace Core.Logic;

public static class TeamService
{
    // public static (Outcome outcome, List<IDomainEvent> events) Handle(
    //     CreateTeamCommand command,
    //     DateTime now
    // )
    // {
    //     // Validate team name
    //     if (string.IsNullOrWhiteSpace(command.Name))
    //         return (Outcome.Rejected("Team name is required"), new List<IDomainEvent>());
    //
    //     if (command.Name.Length > 100)
    //         return (Outcome.Rejected("Team name must be 100 characters or less"), new List<IDomainEvent>());
    //
    //     // Create the event
    //     var teamCreated = new TeamCreated(
    //         command.TeamId,
    //         command.Name,
    //         command.Description,
    //         command.AdminUserId,
    //         now
    //     );
    //
    //     return (Outcome.Accepted(), new List<IDomainEvent> { teamCreated });
    // }

    public static TeamResult HandleCreateTeam(
        TeamState state,
        CreateTeamCommand command,
        DateTime now
        )
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "TeamNameNotSpecified"),
                state,
                new List<IDomainEvent>()
                );
        }

        if (command.Name.Length > 100)
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "TeamNameExceedsMaxCharacters"),
                state,
                new List<IDomainEvent>()
                );
        }

        var newState = new TeamState(TeamId: command.TeamId, PendingInvitations: new List<Guid>(), Members: new List<Guid>());

        return new TeamResult(
            Outcome.Accepted(),
            newState,
            new List<IDomainEvent>
                { 
                    new TeamCreated(
                    command.TeamId,
                    command.Name, 
                    command.Description,
                    command.AdminUserId, 
                    now) 
                }
            );
    }

    public static TeamResult HandleRequestToJoinTeam(
        TeamState state,
        RequestToJoinTeamCommand command,
        DateTime now
        )
    {
        if (state.Members.Contains(command.UserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "UserAlreadyInTeam"),
                state,
                new List<IDomainEvent>()
            );
        }
        if (state.PendingInvitations.Contains(command.UserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "UserAlreadySentARequest"),
                state,
                new List<IDomainEvent>()
                );
        }

        var newPendingInvitations = state.PendingInvitations
            .Append(command.UserId)
            .ToList();
        var newState = state with
        {
            PendingInvitations = newPendingInvitations
        };
        return new TeamResult(
            Outcome.Accepted(),
            newState,
            new List<IDomainEvent>
            {
                new UserRequestedToJoinTeam(state.TeamId, command.UserId, now)
            });
    }

    public static TeamResult HandleApproveRequest(
        TeamState state,
        ApproveJoinRequestCommand command,
        DateTime now,
        Guid adminId
        )
    {
        if (!state.Members.Contains(adminId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "HandlerIsNotAMember"),
                state,
                new List<IDomainEvent>()
                );
        }

        if (!state.PendingInvitations.Contains(command.UserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "UserHasNotSentRequest"),
                state,
                new List<IDomainEvent>()
                );
        }

        if (state.Members.Contains(command.UserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "UserIsAlreadyAMember"),
                state,
                new List<IDomainEvent>()
                );
        }
        
        var newMembers = state.Members
            .Append(command.UserId)
            .ToList();
        var newState = state with
        {
            Members = newMembers
        };
        return new TeamResult(
            Outcome.Accepted(),
            newState,
            new List<IDomainEvent>
            {
                new JoinRequestApproved(state.TeamId, command.RequestId, command.UserId, adminId, now)
            }
        );
    }

    public static TeamResult HandleDeclineRequest(
        TeamState state,
        DeclineJoinRequestCommand command,
        DateTime now,
        Guid adminId
        )
    {
        if (!state.Members.Contains(adminId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "HandlerIsNotAMember"),
                state,
                new List<IDomainEvent>()
                );
        }
        if (!state.PendingInvitations.Contains(command.UserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "UserHasNotSentRequest"),
                state,
                new List<IDomainEvent>()
            );
        }

        if (state.Members.Contains(command.UserId))
        {
            return new TeamResult(
                new Outcome(OutcomeStatus.Rejected, "UserIsAlreadyAMember"),
                state,
                new List<IDomainEvent>()
            );
        }

        var newPendingInvitations = state.PendingInvitations
            .Where(inv => inv != command.UserId)
            .ToList();
        var newState = state with
        {
            PendingInvitations = newPendingInvitations
        };

        return new TeamResult(
            Outcome.Accepted(),
            newState,
            new List<IDomainEvent>
            {
                new JoinRequestDeclined(
                    state.TeamId, 
                    command.RequestId, 
                    command.UserId, 
                    command.DeclinedByUserId, 
                    DateTime.UtcNow)
            }
            );
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
        var newState = state with
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