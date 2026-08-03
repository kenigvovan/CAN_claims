using System.Collections.Concurrent;
using claims.src.messages;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.agreement
{
	//Player can agree to create a new city, alliance
    public class AgreementHandler
    {
        public static ConcurrentDictionary<string, Agreement> agreements = new ConcurrentDictionary<string, Agreement>();

        public static void addNewAgreementOrReplace(Agreement agreement)
        {
            if(agreements.TryRemove(agreement.getPlayerUid(), out Agreement oldAgreement))
            {
                claims.sapi.Event.UnregisterCallback(oldAgreement.getTimeoutCallbackId());
            }

            //Timeout callback runs on the main server thread (unlike the previous Task.Delay version)
            long callbackId = claims.sapi.Event.RegisterCallback((dt =>
            {
                string uid = agreement.getPlayerUid();
                if (agreements.TryRemove(uid, out _))
                {
                    IServerPlayer onlinePlayer = claims.sapi.World.PlayerByUid(uid) as IServerPlayer;
                    if (onlinePlayer != null)
                    {
                        MessageHandler.sendMsgToPlayer(onlinePlayer, Lang.Get("claims:agreement_timeout"));
                    }
                }
            }), claims.config.AGREEMENT_TIMEOUT_SECONDS * 1000);
            agreement.setTimeoutCallbackId(callbackId);
            agreements.TryAdd(agreement.getPlayerUid(), agreement);
		}

		public static bool agreeFor(IServerPlayer player)
        {
			if(agreements.TryRemove(player.PlayerUID, out Agreement agreement))
            {
				claims.sapi.Event.UnregisterCallback(agreement.getTimeoutCallbackId());
				claims.sapi.Event.RegisterCallback((dt =>
				{
					agreement.getOnAgree()?.Invoke();
				}), 0);
				return true;
			}
			return false;
        }
    }
}
