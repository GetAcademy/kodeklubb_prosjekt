$path = "backend\src\Api\Endpoints\TeamEndpoints.cs"
$content = Get-Content $path -Raw

$anchor1 = 'public record CreateTeamRequest(string Name, string? Description, Guid AdminUserId, string? DiscordServerId = null);'
$m1 = ([regex]::Matches($content, [regex]::Escape($anchor1))).Count

$anchor2Lines = @(
    '        await TeamEventHandler.HandleAsync(result.Events, db, emailService);',
    '',
    '        if (!string.IsNullOrWhiteSpace(body.DiscordServerId))',
    '        {',
    '            await db.ExecuteAsync(TeamSql.SetDiscordServerId(), new { TeamId = state.TeamId, DiscordServerId = body.DiscordServerId });',
    '        }',
    '',
    '        await db.CommitAsync();'
)
$anchor2 = $anchor2Lines -join "`r`n"
$m2 = ([regex]::Matches($content, [regex]::Escape($anchor2))).Count

if ($m1 -eq 1 -and $m2 -eq 1) {
    $content = $content.Replace($anchor1, 'public record CreateTeamRequest(string Name, string? Description, Guid AdminUserId);')
    $replacement2 = "        await TeamEventHandler.HandleAsync(result.Events, db, emailService);`r`n`r`n        await db.CommitAsync();"
    $content = $content.Replace($anchor2, $replacement2)
    Set-Content -Path $path -Value $content -NoNewline
    Write-Host "TeamEndpoints.cs reverted."
} else {
    Write-Host "Anchor 1 found $m1 times, anchor 2 found $m2 times (expected 1 each) - not touching the file."
}