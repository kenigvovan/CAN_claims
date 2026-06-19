using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;

namespace claims.src.cityplotsgroups
{
    public class CityPlotsGroupInvitationsHandler
    {
        //city: group: invites
        public static ConcurrentDictionary<string, Dictionary<string, HashSet<CityPlotsGroupInvitation>>> cityPlotsGroupInvitations = new ConcurrentDictionary<string, Dictionary<string, HashSet<CityPlotsGroupInvitation>>>();
        public static bool addNewCityPlotGroupInvitation(CityPlotsGroupInvitation invitation)
        {
            if(cityPlotsGroupInvitations.TryGetValue(invitation.Sender.Guid, out var cityInvites))
            {
                if(cityInvites.TryGetValue(invitation.GroupName, out var groupInvites))
                {
                    if (!groupInvites.Add(invitation))
                    {
                        return false;
                    }
                }
                else
                {
                    groupInvites = new HashSet<CityPlotsGroupInvitation> { invitation };
                    cityInvites[invitation.GroupName] = groupInvites;
                }
            }
            else
            {
                cityInvites = new Dictionary<string, HashSet<CityPlotsGroupInvitation>>();               
                cityPlotsGroupInvitations[invitation.Sender.Guid] = cityInvites;
                cityInvites.Add(invitation.GroupName, new HashSet<CityPlotsGroupInvitation> { invitation });               
            }
            invitation.Receiver.groupInvitations.Add(invitation);
            invitation.Sender.groupInvitations.Add(invitation);
            return true;
        }

        public static bool RemoveInvitation(CityPlotsGroupInvitation invitation)
        {
            if(cityPlotsGroupInvitations.TryGetValue(invitation.Sender.Guid, out var cityDict))
            {
                if(cityDict.TryGetValue(invitation.GroupName, out var groupDict))
                {
                    if (groupDict.Remove(invitation))
                    {
                        invitation.Sender.groupInvitations.Remove(invitation);
                        invitation.Receiver.groupInvitations.Remove(invitation);
                        if (groupDict.Count == 0)
                        {
                            cityDict.Remove(invitation.GroupName);
                        }
                        if (cityDict.Count == 0)
                        {
                            cityPlotsGroupInvitations.TryRemove(invitation.Sender.Guid, out var _);
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        public static void updateCityPlotsGroupInvitations()
        {
            long now = TimeFunctions.getEpochSeconds();
            foreach (var cityDict in cityPlotsGroupInvitations)
            {
                foreach(var groupDict in cityDict.Value)
                {
                    HashSet<CityPlotsGroupInvitation> groupInvitations = groupDict.Value;
                    foreach (CityPlotsGroupInvitation invitation in groupInvitations.ToArray())
                    {
                        if (invitation.TimeStampFinished < now)
                        {
                            invitation.Sender.groupInvitations.Remove(invitation);
                            invitation.Receiver.groupInvitations.Remove(invitation);
                            groupInvitations.Remove(invitation);
                        }
                    }
                }
            }
        }
    }
}
