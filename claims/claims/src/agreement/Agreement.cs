using System;
using System.Threading;

namespace claims.src.agreement
{
    public class Agreement
    {
        Action onAgree;
        CancellationTokenSource source;
        string playerUID;

        public Agreement(Action onAgree, string playerUID)
        {
            this.onAgree = onAgree;
            this.playerUID = playerUID;
        }

        public string getPlayerUid() => playerUID;
        public Action getOnAgree() => onAgree;
        public CancellationTokenSource getToken() => source;
        public void setTokenSource(CancellationTokenSource token) => source = token;
    }
}
