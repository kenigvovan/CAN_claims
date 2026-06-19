using System.Collections.Generic;
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
                });
        }

        public static bool addNewInvite(Invitation invitation)
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

        public static bool removeInvitationIfExists(ISender sender, IReceiver receiver)
        {
            foreach (var it in invites.Snapshot())
            {
                if (it.getSender().Equals(sender) && it.getReceiver().Equals(receiver))
                {
                    it.getSender().GetSentInvitations().Remove(it);
                    it.getReceiver().getReceivedInvitations().Remove(it);
                    invites.Remove(it);
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
                }
            }
        }
    }
}
