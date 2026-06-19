using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.interfaces;
using Vintagestory.API.Config;

namespace claims.src.delayed.invitations
{
    public class Invitation : IGetStatus
    {
        ISender sender;
        IReceiver receiver;
        long timeoutStamp;
        Action onApproval;
        Action onDissent;

        public Invitation(ISender sender, IReceiver receiver, long timeoutStamp, Action onApproval, Action onDissent)
        {
            this.sender = sender;
            this.receiver = receiver;
            this.timeoutStamp = timeoutStamp;
            this.onApproval = onApproval;
            this.onDissent = onDissent;
        }

        public ISender getSender() => sender;
        public IReceiver getReceiver() => receiver;
        public long getTimeStamp() => timeoutStamp;

        public void accept()
        {
            InvitationHandler.removeInvitationIfExists(sender, receiver);
            onApproval?.Invoke();
        }

        public void deny()
        {
            InvitationHandler.removeInvitationIfExists(sender, receiver);
            onDissent?.Invoke();
        }

        public List<string> getStatus(PlayerInfo forPlayer = null)
        {
            List<string> outStrings = new List<string>();
            outStrings.Add(sender.getNameSender() + " --+ \n");
            outStrings.Add(receiver.getNameReceiver() + " +-- \n");
            outStrings.Add(Lang.Get("claims:will_expire_invitation", TimeFunctions.getDateFromEpochSeconds(timeoutStamp)));
            return outStrings;
        }
    }
}
