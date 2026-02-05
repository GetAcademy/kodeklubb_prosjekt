```mermaid
sequenceDiagram
 actor tm as TeamMember
 actor ta as TeamAdmin
 participant rtjt as RequestToJoinTeam Endpoint
 participant rtjta as RequestToJoinTeamAsync 
 participant gtr as GetTeamRequests Endpoint
 participant dtr as DeclineTeamRequest Endpoint
 participant dtra as DeclineTeamRequestAsync
 participant atra as ApproveTeamRequest Endpoint
 participant atraa as ApproveTeamRequestAsync 
 participant db as Database
 
 tm ->> rtjt: Sender request om å joine team
 rtjt ->> rtjta: RequestToJoinTeamAsync (med member's userId)
 rtjta ->> db: INSERT Invitation (status: pending)
 db -->> rtjta: invitation created
 rtjta -->> rtjt: returnerer invitation
 rtjt -->> tm: Request sent successfully
 
 ta ->> gtr: GetTeamRequests (for å se pending requests)
 gtr ->> db: Hent alle pending invitations for team
 db -->> gtr: returnerer pending requests
 gtr -->> ta: Viser liste av requests
 
 alt Admin godkjenner
   ta ->> atra: ApproveTeamRequest (med requestId)
   atra ->> atraa: ApproveTeamRequestAsync
   atraa ->> db: INSERT TeamMemberEntity
   db -->> atraa: member added
   atraa ->> db: UPDATE invitation status til "accepted"
   db -->> atraa: updated
   atraa -->> atra: true
   atra -->> ta: Request approved
   ta -->> tm: Du er lagt til i laget!
 else Admin avslår
   ta ->> dtr: DeclineTeamRequest (med requestId)
   dtr ->> dtra: DeclineTeamRequestAsync
   dtra ->> db: UPDATE invitation status til "declined"
   db -->> dtra: updated
   dtra -->> dtr: true
   dtr -->> ta: Request declined
   ta -->> tm: Din request ble avslått
 end
```
