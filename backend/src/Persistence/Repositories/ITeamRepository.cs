using Persistence.DbModels;
using Core.Commands;

namespace Persistence.Repositories;

public interface ITeamRepository
{
    Task<List<TeamEntity>> GetAvailableTeamsAsync(string? discordId);
    Task<List<TeamEntity>> GetUserTeamsAsync(string discordId);
    Task<TeamEntity?> CreateTeamAsync(CreateTeamCommand command);
    Task<InvitationEntity?> RequestToJoinTeamAsync(RequestToJoinTeamCommand command);
    Task<List<InvitationEntity>> GetTeamRequestsAsync(long teamId);
    Task<bool> ApproveTeamRequestAsync(long teamId, long requestId, long adminUserId);
    Task<bool> DeclineTeamRequestAsync(long teamId, long requestId, long adminUserId);
    Task<TeamDetailsDto?> GetTeamDetailsAsync(long teamId);
}

