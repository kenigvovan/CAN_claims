using claims.src.auxialiry;
using claims.src.delayed.invitations;
using claims.src.part;
using claims.src.part.interfaces;
using claims.src.part.structure.conflict;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace claims.src.part.structure
{
    public class Alliance : Part, ISender, IGetStatus, ICooldown, IConflictParty
    {
        public string MoneyAccountName => claims.config.CITY_ACCOUNT_STRING_PREFIX + Guid;
        public PlayerInfo Leader { get; set; }
        List<Invitation> ListSentInvitations = new List<Invitation>();
        public List<City> Cities { get; set; } = new List<City>(); 
        public City MainCity { get; set; }
        public List<IConflictParty> HostileParties { get; set; } = new List<IConflictParty>();
        public List<Alliance> ComradAlliancies { get; set; } = new List<Alliance>();
        public int AllianceFee { get; set; } = 0;
        public long TimeStampCreated { get; set; }
        public bool Neutral { get; set; } = false;
        public HashSet<Conflict> RunningConflicts { get; } = new HashSet<Conflict>();
        private string prefix = "";
        public string Prefix
        {
            get { return prefix; }
            set
            {
                if (value.Length > claims.config.ALLIANCE_PREFIX_LENGTH)
                {
                    prefix = value.Substring(0, claims.config.ALLIANCE_PREFIX_LENGTH);
                }
                else
                {
                    prefix = value;
                }
            }
        }
        public Alliance(string val, string guid) : base(val, guid)
        {
        }

        public static event Action<Alliance> AllianceCreated;
        public static event Action<Alliance> AllianceDestroyed;
        public static event Action<Alliance, City> CityJoined;
        public static event Action<Alliance, City, EnumCityLeaveReason> CityLeft;
        public static event Action<Alliance, IConflictParty> ConflictDeclared;
        public static event Action<Alliance, IConflictParty, EnumConflictEndReason> ConflictEnded;

        public static void FireAllianceCreated(Alliance alliance) => AllianceCreated?.Invoke(alliance);
        public static void FireAllianceDestroyed(Alliance alliance) => AllianceDestroyed?.Invoke(alliance);
        public void FireCityJoined(City city) => CityJoined?.Invoke(this, city);
        public void FireCityLeft(City city, EnumCityLeaveReason reason) => CityLeft?.Invoke(this, city, reason);
        public void FireConflictDeclared(IConflictParty opponent) => ConflictDeclared?.Invoke(this, opponent);
        public void FireConflictEnded(IConflictParty opponent, EnumConflictEndReason reason) => ConflictEnded?.Invoke(this, opponent, reason);
        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().saveAlliance(this, update);
        }
        public List<Invitation> GetSentInvitations()
        {
            return ListSentInvitations;
        }
        public List<IServerPlayer> getOnlineCitizens()
        {
            List<IServerPlayer> outList = new List<IServerPlayer>();
            foreach (var city in Cities)
            {
                foreach (var player in city.getCityCitizens())
                {
                    IServerPlayer tmp = (IServerPlayer)claims.sapi.World.PlayerByUid(player.Guid);
                    if (tmp != null && claims.sapi.World.AllOnlinePlayers.Contains(tmp))
                    {
                        outList.Add(tmp);
                    }

                }
            }
            return outList;
        }
        public bool IsLeader(PlayerInfo player)
        {
            return MainCity?.getMayor()?.Equals(player) == true || Leader?.Equals(player) == true;
        }
        public void deleteSentInvitation(Invitation invitation)
        {
            this.ListSentInvitations.Remove(invitation);
        }

        public void addSentInvitation(Invitation invitation)
        {
            this.ListSentInvitations.Add(invitation);
        }

        public int getMaxSentInvitations()
        {

            return claims.config.MAX_SENT_INVITATIONS_ALLIANCE;
        }

        public string getNameSender()
        {
            return GetPartName();
        }

        public List<string> getStatus(PlayerInfo forPlayer)
        {
            List<string> outList = new List<string>
            {
                "[" + this.getPartNameReplaceUnder() + "]\n",
                Lang.Get("claims:main_city") + (this.MainCity?.getPartNameReplaceUnder() ?? "?") + "\n",
                StringFunctions.makeFeasibleStringFromNames(StringFunctions.getNamesOfCities(Lang.Get("claims:cities"), Cities), ',') + "\n",
            };
            if (claims.economyProvider.SupportsPlayerWallet)
                outList.Add(Lang.Get("claims:bank_status") + claims.economyProvider.GetBalance(this.MoneyAccountName) + "\n");
            if (this.Neutral)
            {
                outList.Add(Lang.Get("claims:neutral") + "\n");
            }
            
            if (this.HostileParties.Count > 0)
            {
                outList.Add(StringFunctions.makeFeasibleStringFromNames(StringFunctions.
                getNamesOfPartReplaceUnder(Lang.Get("claims:hostiles"), HostileParties.Cast<Part>().ToList())
                , ','));
            }
            if (this.ComradAlliancies.Count > 0)
            {
                outList.Add(StringFunctions.makeFeasibleStringFromNames(StringFunctions.
                    getNamesOfPartReplaceUnder(Lang.Get("claims:allies"), new List<Part>(this.ComradAlliancies))
                    , ','));
            }
            return outList;
        }
        public static string GetUnusedGuid()
        {
            string newGuid;
            while (true)
            {
                newGuid = System.Guid.NewGuid().ToString();
                if (claims.dataStorage.checkGuidForCityVillage(newGuid))
                    break;
            }
            return newGuid;
        }

        // IConflictParty
        public List<City> GetCities() => Cities;
        public void AddHostileParty(IConflictParty party)
        {
            if (!HostileParties.Contains(party))
                HostileParties.Add(party);
        }
        public void RemoveHostileParty(IConflictParty party) => HostileParties.Remove(party);
        public IEnumerable<IConflictParty> GetHostileParties() => HostileParties;

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Guid.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj is Alliance other)
                return this.Guid.Equals(other.Guid);
            return false;
        }
    }
}
