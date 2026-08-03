using System;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using Vintagestory.API.Config;

namespace claims.src.part.structure.plots
{
    /// <summary>
    /// Writes a finished land deal into everything that has to know about it: the plot, the sale
    /// history, both city logs, the map and the two chats.
    ///
    /// Called once the money has already moved - by a price-tag purchase and by a won auction alike.
    /// Sits below both, so neither has to reach through the other to record a sale.
    /// </summary>
    public static class PlotDealCloser
    {
        public static void Complete(Plot plot, City seller, City buyer, long price)
        {
            PlotTransferHelper.Transfer(plot, seller, buyer, markCaptured: false);
            Record(plot, seller, buyer, price);

            seller.AddLogEntry(EnumCityLogEvent.PlotSoldToCity, buyer.GetPartName(),
                plot.getPos().X + " " + plot.getPos().Y, price.ToString());
            buyer.AddLogEntry(EnumCityLogEvent.PlotBoughtFromCity, seller.GetPartName(),
                plot.getPos().X + " " + plot.getPos().Y, price.ToString());
            UsefullPacketsSend.AddToQueueCityInfoUpdate(seller.Guid,
                EnumPlayerRelatedInfo.CITY_LOG, EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(buyer.Guid,
                EnumPlayerRelatedInfo.CITY_LOG, EnumPlayerRelatedInfo.CITY_PLOT_MARKET_HISTORY);

            seller.FirePlotsMapChanged(EnumPlotsMapChangeReason.Unclaimed);
            buyer.FirePlotsMapChanged(EnumPlotsMapChangeReason.Claimed);

            MessageHandler.sendMsgInCity(seller,
                Lang.Get("claims:plot_trade_sold", buyer.getPartNameReplaceUnder(), price));
            MessageHandler.sendMsgInCity(buyer,
                Lang.Get("claims:plot_trade_bought", seller.getPartNameReplaceUnder(), price));
        }

        private static void Record(Plot plot, City seller, City buyer, long price)
        {
            var record = new PlotSaleRecord(Guid.NewGuid().ToString(), plot.getPos().X, plot.getPos().Y,
                seller, buyer, price, TimeFunctions.getEpochSeconds());
            claims.dataStorage.PlotSaleHistory.Add(record);
            claims.getModInstance().getDatabaseHandler().savePlotSale(record);
        }
    }
}
