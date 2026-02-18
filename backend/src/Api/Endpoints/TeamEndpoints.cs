using Api;
using Persistence;
using Core.Commands;
using Core.Logic;
using Core.Outcomes;
using Core.DomainEvents;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using System.Text.Json;
using Core.Models;
using Core.State;
using Persistence.DbModels;
using Core.Endpoints.teams;
namespace Api.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/discover").WithName("Teams");

        group.MapPost("/", Endpoints.teams.CreateTeam).WithName("CreateTeam");
        group.MapGet("/my-teams", Endpoints.user.GetUserTeams).WithName("GetUserTeams");
        group.MapGet("/available", Endpoints.teams.GetAvailableTeams).WithName("GetAvailableTeams");
        group.MapGet("/{teamId}", Endpoints.teams.GetTeamDetails).WithName("GetTeamDetails");
        group.MapGet("/{teamId}/content", Endpoints.teams.GetTeamContent).WithName("GetTeamContent");
        group.MapPost("/{teamId}/request", Endpoints.teams.RequestToJoinTeam).WithName("RequestToJoinTeam");
        group.MapGet("/{teamId}/requests", Endpoints.teams.GetTeamRequests).WithName("GetTeamRequests");
        group.MapPatch("/{teamId}/requests/{requestId}/approve", Endpoints.teams.ApproveTeamRequest).WithName("ApproveTeamRequest");
        group.MapPatch("/{teamId}/requests/{requestId}/decline", Endpoints.teams.DeclineTeamRequest).WithName("DeclineTeamRequest");
    }

    
public record TeamListItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsOpenToJoinRequests,
    Guid CreatedBy,
    DateTime CreatedAt,
    string[] Tags
);

public record CreateTeamRequest(
    string Name,
    string? Description,
    Guid AdminUserId
);

public record AdminActionRequest(
    string DiscordId
);