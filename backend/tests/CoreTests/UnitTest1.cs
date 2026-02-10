using Core.Commands;
using Core.Logic;
using Core.Outcomes;
using Core.State;

namespace tests;

public class Tests
{
    [TestFixture]
    public class TeamServiceTests
    {
        private Guid _teamId;
        private Guid _invitedByUserId;
        private Guid _invitedUserId;
        private DateTime _now;
    
        [SetUp]
        public void Setup()
        {
            _teamId = Guid.NewGuid();
            _invitedByUserId = Guid.NewGuid();
            _invitedUserId = Guid.NewGuid();
            _now = new DateTime(2026, 01, 23);
        }

        [Test]
        public void InviterPutsUserInPendingAndReturnsEvent()
        {
            var teamState = new TeamState(_teamId, new List<Guid> { _invitedByUserId }, new List<Guid> { });
            var command = new InviteUserToTeamCommand(teamState.TeamId, _invitedUserId, _invitedByUserId);

            var result = TeamService.HandleInviteToTeam(teamState, command, _now);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.NewState.PendingInvitations, Contains.Item(_invitedUserId));
                Assert.That(result.Events, Has.Count.EqualTo(1));
                Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Accepted));
            }

            Console.WriteLine(result.Outcome);
        }
    }
}
