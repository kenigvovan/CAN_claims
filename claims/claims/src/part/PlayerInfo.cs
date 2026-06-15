using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using claims.src.delayed.invitations;
using claims.src.part.interfaces;
using claims.src.part.structure;
using claims.src.perms;
using claims.src.rights;
using Vintagestory.API.Config;

namespace claims.src.part
{
    public class PlayerInfo : Part, IReceiver, IGetStatus, ICooldown
    {
        public long TimeStampLasOnline { get; set; }
        public long TimeStampFirstJoined { get; set; }
        public string Prefix { get; set; }  = "";
        public string AfterName { get; set; } = "";
        public int PrisonHoursLeft { get; set; }
        public Prison PrisonedIn { get; set; }
        public bool AwaitForTeleporation { get; set; }
        public City City { get; private set; }     
        public Alliance Alliance { get { return City?.Alliance; } }
        public HashSet<PlayerInfo> Friends { get; set; }
        public HashSet<Plot> PlayerPlots { get; set; }
        public PermsHandler PermsHandler { get; set; }
        public PlayerCache PlayerCache { get; set; }
        public string MoneyAccountName => Guid;
        public PlayerPermissions PlayerPermissionsHandler { get; set; }
        List<Invitation> receivedInvitations = new List<Invitation> ();
        public List<CityPlotsGroupInvitation> groupInvitations = new List<CityPlotsGroupInvitation> ();                  
        HashSet<string> cityTitles = new HashSet<string> ();       
        public bool showBorders = false;
        public EnumShowPlotMovement showPlotMovement = EnumShowPlotMovement.SHOW_HUD;
        HashSet<string> AllianceTitles { get; set; } = new HashSet<string>();
        public PlayerInfo(string val, string uid) : base(val, uid)
        {
            PermsHandler = new PermsHandler();
            Friends = new HashSet<PlayerInfo>();
            PlayerPlots = new HashSet<Plot>();
            PlayerCache = new PlayerCache();
            PlayerPermissionsHandler = new PlayerPermissions();
        }

        /*==============================================================================================*/
        /*==============================================================================================*/
        /*==============================================================================================*/
        public bool HasAlliance()
        {
            return hasCity() && City.HasAlliance();
        }
        public void ClearAllAllianceTitles()
        {
            AllianceTitles.Clear();
        }
        public void setCity(City city)
        {
            this.City = city;
            RightsHandler.reapplyRights(this);
        }
        public double getRansomPrice()
        {
            if(hasCity() )
            {
                if (City.isMayor(this))
                {
                    return claims.config.RANSOM_FOR_MAYOR;
                }
                else { return claims.config.RANSOM_FOR_CITIZEN; }
            }
            return claims.config.RANSOM_FOR_NO_CITIZEN;
        }
        public void addCityTitle(string title)
        {
            cityTitles.Add(title);
        }
        public void removeCityTitle(string title)
        {
            cityTitles.Remove(title);
        }
        public bool hasCityTitle(string title)
        {
            return cityTitles.Contains(title);
        }
        public bool hasAfterName()
        {
            return AfterName != "";
        }
        public bool hasTitle()
        {
            return Prefix != "";
        }

        public bool isPrisoned()
        {
            return PrisonedIn != null;
        }

        public HashSet<string> getCityTitles()
        {
            return cityTitles;
        }

        public void resetCity()
        {
            City = null;
            Prefix = "";
            AfterName = "";
            cityTitles.Clear();
            RightsHandler.reapplyRights(this);
            this.saveToDatabase();
        }
        public void clearCity(bool clearAlsoEmbassies = false)
        {
            if (City == null) return;
            City.getPlayerInfos().Remove(this);
            foreach(Plot plot in PlayerPlots.ToArray())
            {
                if(clearAlsoEmbassies || plot.Type != PlotType.EMBASSY)
                {
                    plot.resetOwner();
                    plot.Price = -1;
                    plot.Type = PlotType.DEFAULT;
                    plot.saveToDatabase();
                    PlayerPlots.Remove(plot);
                }
            }
            City.saveToDatabase();
            resetCity();
        }
        public string getNameForChat()
        {
            string prefix = hasTitle() ? Prefix + " " : "";               

            string postfix = hasAfterName() ? AfterName : "";
            return
                (prefix.Length > 0 
                    ? StringFunctions.setBold(StringFunctions.setStringColor(prefix, claims.config.PREFIX_COLOR_PLAYER))
                    : "")
                + StringFunctions.setBold(StringFunctions.setStringColor(GetPartName(), claims.config.NAME_COLOR_PLAYER))
                + (postfix.Length > 0
                    ? StringFunctions.setBold(StringFunctions.setStringColor(" " + postfix, claims.config.POSTFIX_COLOR_PLAYER))
                    : "" );
        }
        public bool hasCity()
        {
            return City != null;
        }
        public bool removeComrade(PlayerInfo val)
        {
            if(val == null)
            {
                return false;
            }
            return Friends.Remove(val);
        }
        public bool addComrade(PlayerInfo val)
        {
            if (val == null)
                return false;
            return this.Friends.Add(val);
        }      

        public void setPerms(string loadedString)
        {
            this.PermsHandler.setPerms(loadedString);
        }
        public Dictionary<string, int> GetNextPaymentsDict()
        {
            Dictionary<string, int> payments = new();

            if (this.hasCity() && !this.City.isMayor(this))
            {
                payments.Add("city", this.City.fee);
            }

            double plotsPayment = 0;
            foreach (var it in this.PlayerPlots)
            {
                plotsPayment += it.getCustomTax();
            }

            if (plotsPayment != 0)
            {
                payments.Add("plots", (int)plotsPayment);
            }
            return payments;
        }
        /*********************************************************/
        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().savePlayerInfo(this, update);
        }

        public List<Invitation> getReceivedInvitations()
        {
            return receivedInvitations;
        }

        public void deleteReceivedInvitation(Invitation invitation)
        {
            this.receivedInvitations.Remove(invitation);
        }

        public void addReceivedInvitation(Invitation invitation)
        {
            this.receivedInvitations.Add(invitation);
        }

        public int getMaxReceivedInvitations()
        {
            return claims.config.MAX_RECEIVED_INVITATIONS_PLAYER;
        }

        public List<string> getStatus(PlayerInfo forPlayer = null)
        {
            List<string> status = new List<string>
            {
                Lang.Get("claims:last_online", TimeFunctions.getDateFromEpochSeconds(TimeStampLasOnline)) + "\n",
                Lang.Get("claims:first_joined", TimeFunctions.getDateFromEpochSeconds(TimeStampFirstJoined)) + "\n"
            };
            if (City != null)
                status.Add(Lang.Get("claims:city") + City.GetPartName() + "\n");
            return status;
        }

        public string getNameReceiver()
        {
            return GetPartName();
        }
    }
}
