using Persistence.DbModels;
using Core.Commands;

namespace Persistence.Repositories;

public interface ITeamRepository
{
    Task<List<TeamEntity>> GetAvailableTeamsAsync(string? discordId);
    Task<bool> JoinTeamAsync(JoinTeamCommand command);
    Task<InvitationEntity?> RequestToJoinTeamAsync(RequestToJoinTeamCommand command);
    Task<List<InvitationEntity>> GetTeamRequestsAsync(long teamId);
    Task<bool> ApproveTeamRequestAsync(long requestId);
    Task<bool> DeclineTeamRequestAsync(long requestId);
}

