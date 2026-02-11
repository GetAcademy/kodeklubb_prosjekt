INSERT INTO invitations (
    id,
    team_id,
    invited_user_id,
    invited_by,
    status,
    invited_at
) VALUES (
    @Id,
    @TeamId,
    @InvitedUserId,
    @InvitedBy,
    @Status,
    @InvitedAt
);
