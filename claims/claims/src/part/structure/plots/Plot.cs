using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.auxialiry.innerclaims;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.interfaces;
using claims.src.part.structure.plots;
using claims.src.perms;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.part.structure
{
    public class Plot : Part, IGetStatus
    {
        City city;
        PlayerInfo ownerOfPlot;
        public PlotType Type { get; set; }
        public PlotPosition plotPosition;
        double customTax = 0;
        public Prison Prison { get; set; }
        public int Price { get; set; } = -1;
        public bool IsForSale => Price != -1;
        CityPlotsGroup plotGroup;
        PermsHandler permsHandler = new PermsHandler();
        public bool MarkedNoPvp { get; set; } = false;
        public PlotDesc PlotDesc { get; set; }
        public bool extraBought { get; set; }
        public bool BorderPlot { get; set; } = false;
        public bool WasCaptured { get; set; } = false;
        public Plot(Vec2i chunkPos) : base("", "")
        {
            this.plotPosition = new PlotPosition(chunkPos);
        }
        public Plot(PlotPosition plp) : base("", "")
        {
            this.plotPosition = plp;
        }
        /*****************************************************************/
        public bool hasCutomTax()
        {
            return customTax > 0;
        }
        public void setPlotGroup(CityPlotsGroup cityPlotsGroup)
        {
            this.plotGroup = cityPlotsGroup;
        }
        public void setPlotOwner(PlayerInfo playerInfo)
        {
            ownerOfPlot = playerInfo;
        }

        public PermsHandler getPermsHandler()
        {
            return permsHandler;
        }
         
        public bool hasPlotOwner()
        {
            return ownerOfPlot != null;
        }
        public bool hasCity()
        {
            return city != null;
        }

        
        public bool hasPlotGroup()
        {
            return plotGroup != null;
        }
        public CityPlotsGroup getPlotGroup()
        {
            return plotGroup;
        }

        public PlayerInfo getPlotOwner()
        {
            return ownerOfPlot;
        }
        public void resetOwner()
        {
            ownerOfPlot = null;
        }
        public City getCity()
        {
            return city;
        }
        public PlayerInfo getPlayerInfo()
        {
            return this.ownerOfPlot;
        }
        public void setCity(City city)
        {
            this.city = city;
        }
        public Vec2i getPos()
        {
            return plotPosition.getPos();
        }
        /// <summary>
        /// Set new type of the plot, there is some co-routines for some types of plots which should be run after we change some of them.
        /// Jail should be removed for example, or summon points removed, etc.
        /// </summary>
        /// <param name="tcr"></param>
        /// <param name="newPlotType"></param>
        /// <param name="player"></param>
        /// <returns>If type was changed successfully.</returns>
        public bool setNewType(TextCommandResult tcr, string newPlotType, IServerPlayer player)
        {
            PlotType plotType = PlotInfo.nameToPlotType[newPlotType];

            if(plotType == this.Type)
            {
                tcr.StatusMessage = "claims:plot_the_same_type_set";
                return false; 
            }

            if (plotType == PlotType.CAMP || plotType == PlotType.TOURNAMENT)
            {
                tcr.StatusMessage = "claims:use_other_command_for_that";
                return false;
            }

            //Remove all saved data for plot type
            CleanUpCurrentPlotTypeData();


            //Set new type and init info needed
            if (plotType == PlotType.SUMMON)
            {
                CityLevelInfo cli = Settings.getCityLevelInfo(getCity().getCityCitizens().Count);
                if (getCity().summonPlots.Count >= cli.SummonPlots)
                {
                    tcr.StatusMessage = "claims:limit_summon_plots";
                    return false;
                }
                Type = PlotType.SUMMON;
                getCity().summonPlots.Add(this);
                PlotDescSummon pds = new PlotDescSummon(player.Entity.Pos.XYZ);
                pds.Name = "Point" + ((int)pds.SummonPoint.X % 10).ToString() + ((int)pds.SummonPoint.Z % 10).ToString();
                this.PlotDesc = pds;
                saveToDatabase();
                getCity().saveToDatabase();
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement((this.PlotDesc as PlotDescSummon).SummonPoint.AsVec3i.Clone(),
                    (this.PlotDesc as PlotDescSummon).Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ADD);
                tcr.StatusMessage = "claims:plot_set_type";
                tcr.MessageParams = new object[] { newPlotType };
                return true;
            }
            else if (plotType == PlotType.PRISON)
            {
                PartInits.initPrison(this, getCity(), player);
                Type = plotType;
                tcr.StatusMessage = "claims:plot_set_type";
                tcr.MessageParams = new object[] { newPlotType };
                UsefullPacketsSend.AddToQueueCityInfoUpdate(this.getCity().Guid,
                    new Dictionary<string, object> { { "value", new PrisonCellElement(player.Entity.Pos.AsBlockPos.AsVec3i.Clone(), new HashSet<string>()) } }, EnumPlayerRelatedInfo.CITY_ADD_PRISON_CELL);
                return true;
            }
            else if (plotType == PlotType.TAVERN)
            {
                int tavernCount = 0;
                foreach (var it in getCity().getCityPlots())
                {
                    if (it.Type == PlotType.TAVERN)
                        tavernCount++;
                }
                if (tavernCount >= claims.config.MAX_NUMBER_TAVERN_PER_CITY)
                {
                    tcr.StatusMessage = "claims:too_much_taverns";
                    tcr.MessageParams = new object[] { claims.config.MAX_NUMBER_TAVERN_PER_CITY };
                    return false;
                }
                Type = plotType;
                PlotDescTavern pdt = new PlotDescTavern();
                PlotDesc = pdt;
                saveToDatabase();
                tcr.StatusMessage = "claims:plot_set_type";
                tcr.MessageParams = new object[] { newPlotType };
                return true;
            }
            else if( plotType == PlotType.TEMPLE)
            {
                Type = plotType;
            }

            Type = plotType;
            saveToDatabase();
            tcr.StatusMessage = "claims:plot_set_type";
            tcr.MessageParams = new object[] { newPlotType };
            return true;
        }

        public void CleanUpCurrentPlotTypeData()
        {
            PlotType currentPlotType = this.Type;
            if (currentPlotType == PlotType.SUMMON)
            {
                UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement((this.PlotDesc as PlotDescSummon).SummonPoint.AsVec3i.Clone(),
                    (this.PlotDesc as PlotDescSummon).Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_REMOVE);
                this.PlotDesc = null;
                getCity().summonPlots.Remove(this);
            }
            else if (currentPlotType == PlotType.PRISON)
            {
                foreach (PrisonCellInfo cell in Prison.getPrisonCells())
                {
                    foreach (PlayerInfo player in cell.getPlayerInfos())
                    {
                        EntityPos ep = claims.sapi.World.DefaultSpawnPosition;
                        (claims.sapi.World.PlayerByUid(player.Guid) as IServerPlayer).SetSpawnPosition(new PlayerSpawnPos((int)ep.X, (int)ep.Y, (int)ep.Z));
                        (claims.sapi.World.PlayerByUid(player.Guid) as IServerPlayer).
                            Entity.TeleportToDouble(ep.X, ep.Y, ep.Z);
                    }
                }

                claims.dataStorage.removePrison(Prison.Guid);
                if (Prison.City != null)
                {
                    Prison.City.getPrisons().Remove(Prison);
                    Prison.City.saveToDatabase();
                }
                Prison.Plot.Type = PlotType.DEFAULT;
                claims.getModInstance().getDatabaseHandler().deleteFromDatabasePrison(Prison);
                Prison = null;
                this.saveToDatabase();                
            }
            else if (currentPlotType == PlotType.EMBASSY)
            {
                if (!this.hasPlotOwner())
                {
                    return;
                }

                PlayerInfo playerInfo = this.getPlotOwner();
                if (playerInfo.hasCity() && this.getCity().Equals(playerInfo.City))
                {
                    return;
                }
                playerInfo.PlayerPlots.Remove(this);
                playerInfo.saveToDatabase();
            }
            else if(currentPlotType == PlotType.TEMPLE)
            {
                city.RemoveTempleRespawnPoint(this);
            }
        }
        public double getCustomTax()
        {
            return customTax;
        }
        public bool setCustomTax(double val)
        {
            if(customTax == val)
            {
                return false;
            }
            customTax = val;
            return true;
        }
        /*****************************************************************/
        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().savePlot(this, update);
        }
        public bool hasCityPlotsGroup()
        {
            return plotGroup != null;
        }
        public List<ClientInnerClaim> GetClientInnerClaimFromDefault(PlayerInfo playerInfo)
        {
            List<ClientInnerClaim> tmpCIC = new List<ClientInnerClaim>();
            foreach (var it in (this.PlotDesc as PlotDescTavern).innerClaims)
            {
                if(it.membersUids.Contains(playerInfo.Guid))
                {
                    tmpCIC.Add(new ClientInnerClaim(it.pos1, it.pos2, it.permissionsFlags));
                }
            }
            if(tmpCIC.Count > 0) 
            {
                return tmpCIC;
            }
            return null;
        }
        public List<string> getStatus(PlayerInfo forPlayer = null)
        {           
            List<string> outStrings = new List<string>();
            if(GetPartName() != "")
            {
                outStrings.Add(GetPartName() + "\n");
            }
           
            if(hasCity())
            {
                outStrings.Add(Lang.Get("claims:city") + getCity().getPartNameReplaceUnder() + "\n");
            }

            if(hasPlotOwner())
            {
                outStrings.Add(Lang.Get("claims:plot_owner", getPlotOwner().GetPartName()) + "\n");
            }
            PlotInfo.dictPlotTypes.TryGetValue(this.Type, out PlotInfo plotInfo);
            outStrings.Add(Lang.Get("claims:" + plotInfo.getFullName()) + "\n");
            if(customTax > 0)
            {
                outStrings.Add(Lang.Get("claims:custom_plottax", customTax));
                outStrings.Add("\n");
            }
            if(hasPlotGroup())
            {
                outStrings.Add(Lang.Get("claims:plot_group", getPlotGroup().getPartNameReplaceUnder()));
            }
            outStrings.Add(permsHandler.getStringForChat() + "\n");
            return outStrings;
        }
        public void UpdateBorderPlotValue()
        {
            this.CheckBorderPlotValue();
            PlotPosition posTmp = new PlotPosition(0, 0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if ((Math.Abs(i) + Math.Abs(j)) != 1) continue;
                    posTmp.X = this.plotPosition.X + i;
                    posTmp.Z = this.plotPosition.Z + j;
                    if (claims.dataStorage.GetPlot(posTmp, out var targetPlot))
                    {
                        targetPlot.CheckBorderPlotValue();
                    }
                }
            }          
        }
        public void CheckBorderPlotValue()
        {
            PlotPosition posTmp = new PlotPosition(0, 0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if ((Math.Abs(i) + Math.Abs(j)) != 1) continue;
                    posTmp.X = this.plotPosition.X + i;
                    posTmp.Z = this.plotPosition.Z + j;
                    if (!claims.dataStorage.GetPlot(posTmp, out var nearPlot))
                    {
                        this.BorderPlot = true;
                        return;                      
                    }
                    else
                    {
                        if (!nearPlot.getCity().Equals(this.getCity()))
                        {
                            this.BorderPlot = true;
                            return;
                        }
                    }
                }
                this.BorderPlot = false;
            }
        }
    }
}
