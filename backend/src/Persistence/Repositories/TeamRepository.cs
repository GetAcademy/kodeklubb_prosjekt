using Microsoft.EntityFrameworkCore;
using Persistence.DbModels;
using Core.Commands;

namespace Persistence.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _context;

    public TeamRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TeamEntity?> CreateTeamAsync(CreateTeamCommand command)
    {
        try
        {
            // Verify the admin user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == command.AdminUserId);
            if (!userExists)
            {
                return null;
            }

            // Create the team
            var team = new TeamEntity
            {
                Name = command.Name,
                Description = command.Description,
                CreatedBy = command.AdminUserId,
                TeamAdminId = command.AdminUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Add the admin user as a team member with 'admin' role
            var adminMember = new TeamMemberEntity
            {
                TeamId = team.Id,
                UserId = command.AdminUserId,
                Role = "admin",
                Status = "active",
                JoinedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.TeamMembers.Add(adminMember);
            await _context.SaveChangesAsync();

            return team;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<TeamEntity>> GetAvailableTeamsAsync(string? discordId)
    {
        var query = _context.Teams
            .Include(t => t.TeamTags)
                .ThenInclude(tt => tt.Tag)
            .Include(t => t.TeamMembers)
                .ThenInclude(tm => tm.User)
            .AsNoTracking();

        // If discordId provided, filter to show only teams where user is NOT already a member
        // If no discordId, show all teams
        if (!string.IsNullOrWhiteSpace(discordId))
        {
            query = query.Where(t => !t.TeamMembers.Any(tm => tm.User != null && tm.User.DiscordId == discordId));
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TeamEntity>> GetUserTeamsAsync(string discordId)
    {
        return await _context.Teams
            .Include(t => t.TeamTags)
                .ThenInclude(tt => tt.Tag)
            .Include(t => t.TeamMembers)
                .ThenInclude(tm => tm.User)
            .Where(t => t.TeamMembers.Any(tm => tm.User != null && tm.User.DiscordId == discordId))
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> JoinTeamAsync(JoinTeamCommand command)
    {
        try
        {
            var teamExists = await _context.Teams.AnyAsync(t => t.Id == command.TeamId);
            if (!teamExists)
            {
                return false;
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == command.UserId);
            if (!userExists)
            {
                return false;
            }

            var alreadyMember = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == command.TeamId && tm.UserId == command.UserId);
            if (alreadyMember)
            {
                return false;
            }

            var teamMember = new TeamMemberEntity
            {
                TeamId = command.TeamId,
                UserId = command.UserId,
                Role = "member",
                Status = "active",
                JoinedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<InvitationEntity?> RequestToJoinTeamAsync(RequestToJoinTeamCommand command)
    {
        try
        {
            var teamExists = await _context.Teams.AnyAsync(t => t.Id == command.TeamId);
            if (!teamExists)
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId);
            if (user == null)
            {
                return null;
            }

            var existingRequest = await _context.Invitations
                .FirstOrDefaultAsync(i => i.TeamId == command.TeamId && i.InvitedUserId == command.UserId && i.Status == "pending");
            if (existingRequest != null)
            {
                return existingRequest;
            }

            var invitation = new InvitationEntity
            {
                TeamId = command.TeamId,
                InvitedUserId = command.UserId,
                InvitedBy = command.UserId, // User is requesting, so they're the initiator
                Status = "pending",
                InvitedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.Invitations.Add(invitation);
            await _context.SaveChangesAsync();

            return invitation;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<InvitationEntity>> GetTeamRequestsAsync(long teamId)
    {
        return await _context.Invitations
            .Where(i => i.TeamId == teamId && i.Status == "pending")
            .Include(i => i.InvitedUser)
            .OrderByDescending(i => i.InvitedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveTeamRequestAsync(long teamId, long requestId, long adminUserId)
    {
        try
        {
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null || team.TeamAdminId != adminUserId)
            {
                return false;
            }

            var request = await _context.Invitations.FindAsync(requestId);
            if (request == null || request.Status != "pending" || request.TeamId != teamId)
            {
                return false;
            }

            // Add user as team member
            var alreadyMember = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == request.TeamId && tm.UserId == request.InvitedUserId);
            
            if (!alreadyMember)
            {
                var teamMember = new TeamMemberEntity
                {
                    TeamId = request.TeamId,
                    UserId = request.InvitedUserId,
                    Role = "member",
                    Status = "active",
                    JoinedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 1
                };

                _context.TeamMembers.Add(teamMember);
            }

            request.Status = "accepted";
            request.RespondedAt = DateTime.UtcNow;
            request.Version++;

            _context.Invitations.Update(request);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeclineTeamRequestAsync(long teamId, long requestId, long adminUserId)
    {
        try
        {
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null || team.TeamAdminId != adminUserId)
            {
                return false;
            }

            var request = await _context.Invitations.FindAsync(requestId);
            if (request == null || request.Status != "pending" || request.TeamId != teamId)
            {
                return false;
            }

            request.Status = "declined";
            request.RespondedAt = DateTime.UtcNow;
            request.Version++;

            _context.Invitations.Update(request);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsUserMemberAsync(long teamId, string discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId)) return false;

        return await _context.TeamMembers
            .Include(tm => tm.User)
            .AnyAsync(tm => tm.TeamId == teamId && tm.User != null && tm.User.DiscordId == discordId && tm.Status == "active");
    }
}
