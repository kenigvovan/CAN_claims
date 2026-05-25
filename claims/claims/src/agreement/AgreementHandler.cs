using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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
            if(agreements.TryGetValue(agreement.getPlayerUid(), out _))
            {
                agreements.TryRemove(agreement.getPlayerUid(), out _);
            }
            agreements.TryAdd(agreement.getPlayerUid(), agreement);

			var tokenSource = new CancellationTokenSource();
			agreement.setTokenSource(tokenSource);
			Task.Run(async delegate
			{
				await Task.Delay(claims.config.AGREEMENT_TIMEOUT_SECONDS * 1000, tokenSource.Token);
				string uid = agreement.getPlayerUid();
				if (agreements.TryGetValue(uid, out _))
				{
					agreements.TryRemove(uid, out _);
					IServerPlayer onlinePlayer = claims.sapi.World.PlayerByUid(uid) as IServerPlayer;
					if (onlinePlayer != null)
					{
						MessageHandler.sendMsgToPlayer(onlinePlayer, Lang.Get("claims:agreement_timeout"));
					}
				}
			}, tokenSource.Token);
		}

		public static bool agreeFor(IServerPlayer player)
        {
			if(agreements.TryRemove(player.PlayerUID, out Agreement agreement))
            {
				agreement.getToken().Cancel();
				claims.sapi.Event.RegisterCallback((dt =>
				{
					agreement.getOnAgree().Start();
				}), 0);
				return true;
			}
			return false;
        }
    }
}
