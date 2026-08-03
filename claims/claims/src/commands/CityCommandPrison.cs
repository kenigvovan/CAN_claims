using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using claims.src.agreement;
using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.cityplotsgroups;
using claims.src.delayed.cooldowns;
using claims.src.delayed.invitations;
using claims.src.delayed.teleportation;
using claims.src.economy;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.rights;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public partial class CityCommand
    {
        /*==============================================================================================*/
        /*=====================================PRISON===================================================*/
        /*==============================================================================================*/
        public static TextCommandResult PrisonList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            if (!HelperFunctionPrison(player, out City city, out Plot plotHere, tcr))
            {
                return tcr;
            }
            Prison prison = plotHere.Prison;
            StringBuilder sb = new StringBuilder();

            int i = 0;
            foreach (var it in prison.getPrisonCells())
            {
                sb.Append(i.ToString()).Append(". ").Append(it.getSpawnPosition().ToString()).Append("\n");
                i++;
            }
            tcr.StatusMessage = sb.ToString();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult RemovePrisonCell(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Error;
            if (!HelperFunctionPrison(player, out City city, out Plot plotHere, tcr))
            {
                return tcr;
            }

            if (plotHere.Prison.getPrisonCells().Count == 1)
            {
                return TextCommandResult.Success("claims:last_cell");
            }
            int index = (int)args.LastArg;
            if (index < 0 || plotHere.Prison.getPrisonCells().Count <= index)
            {
                return TextCommandResult.Error("claims:need_number");
            }
            // Taken before the removal: the packet says which cell went, and without it the client
            // was told "a cell was removed" with nothing to identify it.
            Vec3i removedPoint = plotHere.Prison.getPrisonCells()[index].getSpawnPosition();

            plotHere.Prison.removePrisonCell(index);
            plotHere.saveToDatabase();
            plotHere.Prison.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            tcr.StatusMessage = "claims:prison_cell_removed";
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new PrisonCellElement(removedPoint, new HashSet<string>()) } },
                EnumPlayerRelatedInfo.CITY_REMOVE_PRISON_CELL);
            return tcr;
        }
        public static TextCommandResult CRemovePrisonCell(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            City city = null;
            if (!playerInfo.hasCity())
            {
                tcr.StatusMessage = "claims:you_dont_have_city";
                return tcr;
            }
            city = playerInfo.City;
            Vec3i searchPoint = new((int)args.Parsers[0].GetValue(), (int)args.Parsers[1].GetValue(), (int)args.Parsers[2].GetValue());
            Plot savedPlot = null;
            bool found = false;
            foreach(var it in city.getPrisons())
            {
                foreach(var cell_it in it.getPrisonCells())
                {
                    if(cell_it.getSpawnPosition().Equals(searchPoint))
                    {
                        savedPlot = it.Plot;
                        it.removePrisonCell(cell_it);
                        found = true;
                        break;
                    }
                }
                if(found)
                {
                    break;
                }
            }
            if(!found)
            {
                tcr.StatusMessage = "claims:no_cell_found";
                return tcr;
            }
            savedPlot.saveToDatabase();
            savedPlot.Prison.saveToDatabase();
            tcr.Status = EnumCommandStatus.Success;
            tcr.StatusMessage = "claims:prison_cell_removed";
            // A whole cell element, not the bare position: that is what the client deserializes for
            // this key, and a Vec3i arrived as a cell with no position at all.
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                new Dictionary<string, object> { { "value", new PrisonCellElement(searchPoint, new HashSet<string>()) } },
                EnumPlayerRelatedInfo.CITY_REMOVE_PRISON_CELL);
            return tcr;
        }
        public static TextCommandResult AddPrisonCell(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new();
            tcr.Status = EnumCommandStatus.Error;
            if (!HelperFunctionPrison(player, out City city, out Plot plotHere, tcr))
            {
                return tcr;
            }
            if (plotHere.Prison.getPrisonCells().Count > claims.config.MAX_CELLS_PER_PRISON)
            {
                tcr.StatusMessage = "claims:too_much_cells";
                tcr.Status = EnumCommandStatus.Success;
                return tcr;
            }
            var newPoint = player.Entity.Pos.AsBlockPos.AsVec3i.Clone();
            plotHere.Prison.addPrisonCell(new PrisonCellInfo(newPoint));
            plotHere.saveToDatabase();
            plotHere.Prison.saveToDatabase();
            tcr.StatusMessage = "claims:prison_cell_created";
            tcr.Status = EnumCommandStatus.Success;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, new Dictionary<string, object> { { "value", new PrisonCellElement(newPoint, new HashSet<string>()) } },  EnumPlayerRelatedInfo.CITY_ADD_PRISON_CELL);
            return tcr;
        }
    }
}
