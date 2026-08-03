namespace claims.src.gui.playerGui.structures.cellElements
{
    public class ClientToPlotsGroupInvitation
    {
        public string CityName { get; set; }
        public string PlotsGroupName { get; set; }
        public long TimeoutStamp { get; set; }

        /// <summary>
        /// What joining will cost daily. Carried with the invitation because accepting is the moment
        /// the player starts paying it, and the group's own card is not visible to them until then.
        /// </summary>
        public double Fee { get; set; }

        public ClientToPlotsGroupInvitation(string cityName, string plotsGroupName, long timeoutStamp,
                                            double fee = 0)
        {
            CityName = cityName;
            PlotsGroupName = plotsGroupName;
            TimeoutStamp = timeoutStamp;
            Fee = fee;
        }
    }
}
