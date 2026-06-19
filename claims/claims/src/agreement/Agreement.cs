using System;

namespace claims.src.agreement
{
    public class Agreement
    {
        Action onAgree;
        long timeoutCallbackId;
        string playerUID;

        public Agreement(Action onAgree, string playerUID)
        {
            this.onAgree = onAgree;
            this.playerUID = playerUID;
        }

        public string getPlayerUid() => playerUID;
        public Action getOnAgree() => onAgree;
        public long getTimeoutCallbackId() => timeoutCallbackId;
        public void setTimeoutCallbackId(long callbackId) => timeoutCallbackId = callbackId;
    }
}
