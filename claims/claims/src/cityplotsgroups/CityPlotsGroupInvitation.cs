using System;
using claims.src.part;

namespace claims.src.cityplotsgroups
{
    public class CityPlotsGroupInvitation
    {
        public City Sender { get; set; }
        public PlayerInfo Receiver { get; set; }
        public long TimeStampFinished { get; set; }
        Action onAccept;
        Action onReject;
        public string GroupName { get; set; }

        public CityPlotsGroupInvitation(City sender, PlayerInfo receive, long timeStampFinished, Action onAccept, Action onReject, string groupName)
        {
            this.Sender = sender;
            this.Receiver = receive;
            this.TimeStampFinished = timeStampFinished;
            this.onAccept = onAccept;
            this.onReject = onReject;
            this.GroupName = groupName;
        }

        public void reject()
        {
            CityPlotsGroupInvitationsHandler.RemoveInvitation(this);
            onReject?.Invoke();
        }

        public void accept()
        {
            CityPlotsGroupInvitationsHandler.RemoveInvitation(this);
            onAccept?.Invoke();
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (obj is not CityPlotsGroupInvitation other) return false;
            return Receiver == other.Receiver && Sender == other.Sender;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Receiver.GetHashCode();
            hash = (hash * 7) + Sender.GetHashCode();
            return hash;
        }
    }
}
