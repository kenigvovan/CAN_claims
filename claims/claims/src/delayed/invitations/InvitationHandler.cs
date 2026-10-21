using System.Collections.Generic;
using claims.src.part;
using claims.src.part.interfaces;

namespace claims.src.delayed.invitations
{
    public class InvitationHandler
    {
        static ExpiringList<Invitation> invites = new();

        public static void findAndDeleteOverdueInvitations()
        {
            invites.ExpireOverdue(
                inv => inv.getTimeStamp(),
                inv =>
                {
                    inv.getReceiver().deleteReceivedInvitation(inv);
                    inv.getSender().deleteSentInvitation(inv);
                    Forget(inv);
                });
        }

        public static bool addNewInvite(Invitation invitation)
        {
            if (!Register(invitation)) return false;
            if (invitation.Kind != null && invitation.getSender() is Part sender && invitation.getReceiver() is Part receiver)
            {
                claims.getModInstance()?.getDatabaseHandler()?.saveInvitation(invitation.Kind, sender.Guid, receiver.Guid, invitation.getTimeStamp());
            }
            return true;
        }

        /// <summary>An invitation read back from the database: registered, not written again.</summary>
        public static bool Restore(Invitation invitation) => Register(invitation);

        private static bool Register(Invitation invitation)
        {
            foreach (var it in invites.Snapshot())
            {
                if (it.getSender().Equals(invitation.getSender()) && it.getReceiver().Equals(invitation.getReceiver()))
                    return false;
            }
            if (invitation.getSender().GetSentInvitations().Count >= invitation.getSender().getMaxSentInvitations())
                return false;
            if (invitation.getReceiver().getReceivedInvitations().Count >= invitation.getReceiver().getMaxReceivedInvitations())
                return false;

            invites.Add(invitation);
            invitation.getReceiver().addReceivedInvitation(invitation);
            invitation.getSender().addSentInvitation(invitation);
            return true;
        }

        /// <summary>Drops the saved row of an invitation that has been answered, withdrawn or has run out.</summary>
        private static void Forget(Invitation invitation)
        {
            if (invitation.Kind != null && invitation.getSender() is Part sender && invitation.getReceiver() is Part receiver)
            {
                claims.getModInstance()?.getDatabaseHandler()?.deleteInvitation(invitation.Kind, sender.Guid, receiver.Guid);
            }
        }

        public static bool removeInvitationIfExists(ISender sender, IReceiver receiver)
        {
            foreach (var it in invites.Snapshot())
            {
                if (it.getSender().Equals(sender) && it.getReceiver().Equals(receiver))
                {
                    it.getSender().GetSentInvitations().Remove(it);
                    it.getReceiver().getReceivedInvitations().Remove(it);
                    invites.Remove(it);
                    Forget(it);
                    return true;
                }
            }
            return false;
        }

        public static List<Invitation> getInvitesForReceiver(IReceiver receiver)
        {
            List<Invitation> result = new();
            foreach (var it in invites.Snapshot())
            {
                if (it.getReceiver().Equals(receiver))
                    result.Add(it);
            }
            return result;
        }

        public static void deleteAllInvitationsForReceiver(IReceiver receiver)
        {
            foreach (var it in invites.Snapshot())
            {
                if (it.getReceiver() == receiver)
                {
                    it.getReceiver().deleteReceivedInvitation(it);
                    it.getSender().deleteSentInvitation(it);
                    invites.Remove(it);
                    Forget(it);
                }
            }
        }

        public static void deleteAllInvitationsForSender(ISender sender)
        {
            foreach (var it in invites.Snapshot())
            {
                if (it.getSender() == sender)
                {
                    it.getReceiver().deleteReceivedInvitation(it);
                    invites.Remove(it);
                    Forget(it);
                }
            }
        }
    }
}
