using claims.src.delayed.invitations;
using claims.src.part;
using Moq;

namespace claims.tests
{
    /// <summary>
    /// City invitations are written to the database and removed from it with every way they end:
    /// they used to live in memory only, and a restart between invite and accept dropped them.
    /// </summary>
    [Collection("ModStatics")]
    public class InvitationPersistenceTests : IDisposable
    {
        private readonly ServerStubs stubs = new();
        private readonly City city = new("TestCity", "inv-city-guid");
        private readonly PlayerInfo player = new("Invitee", "inv-player-uid");

        public void Dispose()
        {
            // The handler's list is static and shared by every test.
            InvitationHandler.removeInvitationIfExists(city, player);
            stubs.Dispose();
        }

        private Invitation CityInvite(long expires = long.MaxValue) => CityInvitation.Create(city, player, expires);

        private void VerifySaved(Times times) =>
            stubs.Db.Verify(d => d.saveInvitation(CityInvitation.Kind, city.Guid, player.Guid, It.IsAny<long>()), times);

        private void VerifyDeleted(Times times) =>
            stubs.Db.Verify(d => d.deleteInvitation(CityInvitation.Kind, city.Guid, player.Guid), times);

        [Fact]
        public void Invite_IsSaved_WithItsExpiry()
        {
            Assert.True(InvitationHandler.addNewInvite(CityInvite(5000)));

            stubs.Db.Verify(d => d.saveInvitation(CityInvitation.Kind, city.Guid, player.Guid, 5000), Times.Once);
        }

        [Fact]
        public void SecondInviteOfTheSamePair_IsRefused_AndNotSavedAgain()
        {
            InvitationHandler.addNewInvite(CityInvite());

            Assert.False(InvitationHandler.addNewInvite(CityInvite()));
            VerifySaved(Times.Once());
        }

        [Fact]
        public void Withdrawn_IsDeleted()
        {
            InvitationHandler.addNewInvite(CityInvite());

            Assert.True(InvitationHandler.removeInvitationIfExists(city, player));

            VerifyDeleted(Times.Once());
            Assert.Empty(player.getReceivedInvitations());
        }

        [Fact]
        public void Declined_IsDeleted()
        {
            Invitation invitation = CityInvite();
            InvitationHandler.addNewInvite(invitation);

            InvitationHandler.removeInvitationIfExists(invitation.getSender(), invitation.getReceiver());

            VerifyDeleted(Times.Once());
        }

        [Fact]
        public void Overdue_IsDeleted()
        {
            InvitationHandler.addNewInvite(CityInvite(expires: 1));

            InvitationHandler.findAndDeleteOverdueInvitations();

            VerifyDeleted(Times.Once());
            Assert.Empty(player.getReceivedInvitations());
        }

        [Fact]
        public void CityDemolished_ItsInvitationsAreDeleted()
        {
            InvitationHandler.addNewInvite(CityInvite());

            InvitationHandler.deleteAllInvitationsForSender(city);

            VerifyDeleted(Times.Once());
        }

        [Fact]
        public void Restored_IsNotWrittenAgain()
        {
            Assert.True(InvitationHandler.Restore(CityInvite()));

            VerifySaved(Times.Never());
            Assert.Single(player.getReceivedInvitations());
        }

        [Fact]
        public void InvitationWithoutKind_StaysInMemoryOnly()
        {
            var invitation = new Invitation(city, player, long.MaxValue, null, null);

            InvitationHandler.addNewInvite(invitation);
            InvitationHandler.removeInvitationIfExists(city, player);

            stubs.Db.Verify(d => d.saveInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
            stubs.Db.Verify(d => d.deleteInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
