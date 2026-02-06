using System;
using System.Collections.Generic;

namespace Persistence.Repositories;

public record TeamDetailsDto(
    long Id,
    string Name,
    string? Description,
    long CreatedBy,
    long TeamAdminId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version,
    List<TeamTagDto> TeamTags,
    List<TeamMemberDto> TeamMembers
);

public record TeamTagDto(long Id, long TeamId, long TagId, DateTime CreatedAt, string? TagName);

public record TeamMemberDto(long Id, long TeamId, long UserId, string? Role, string? Status, string? Username);